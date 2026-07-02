using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VOIDTUNE.WinUI.Models;

namespace VOIDTUNE.WinUI.Services;

/// <summary>Progress update for a running apply/revert, surfaced to the UI progress dialog.</summary>
public readonly record struct TweakProgress(string Phase, int Done, int Total);

/// <summary>App-wide tweak state: the catalog, applied tracking, apply/revert, persistence.</summary>
public sealed class TweakEngine
{
    public static TweakEngine Instance { get; } = new();

    public ObservableCollection<Tweak> Tweaks { get; } = new();

    public event Action<string>? Log;

    /// <summary>
    /// Cap on how many tweak commands run at once. Each command spawns a process and
    /// spends most of its time waiting on it, so a degree above the core count still
    /// helps, but we keep it bounded so a full apply doesn't launch dozens of
    /// cmd.exe/powershell.exe instances simultaneously.
    /// </summary>
    private static readonly int MaxParallel = Math.Clamp(Environment.ProcessorCount, 4, 12);

    private TweakEngine()
    {
        foreach (var t in TweakCatalog.All) Tweaks.Add(t);
        LoadState();
    }

    public int AppliedCount => Tweaks.Count(t => t.Applied);

    public IEnumerable<string> Categories =>
        Tweaks.Select(t => t.Category).Distinct();

    /// <summary>Apply a set of tweaks. Returns (ok, failed). Creates a registry backup first.</summary>
    public async Task<(int ok, int fail)> ApplyAsync(IEnumerable<Tweak> tweaks, IProgress<TweakProgress>? progress = null, bool backup = true)
    {
        var list = tweaks as IList<Tweak> ?? new List<Tweak>(tweaks);
        if (list.Count == 0) return (0, 0);

        // Bulk applies snapshot the registry first; single toggles skip it (kept snappy).
        if (backup)
        {
            // Total == 0 tells the dialog to show an indeterminate bar for the backup phase.
            progress?.Report(new TweakProgress("Creating registry backup…", 0, 0));
            string b = await BackupService.CreateAsync("apply");
            if (!string.IsNullOrEmpty(b)) Log?.Invoke($"Registry backup: {b}");
        }

        // The commands are independent registry/power writes, so run them concurrently
        // (bounded) instead of one process at a time. The body only writes to its own
        // slot here; UI-bound state (Applied) is updated below, back on the caller's
        // thread, so we never raise PropertyChanged from a worker thread.
        var results = await RunBatchAsync(list, t => t.ApplyCmd, progress, "Applying");

        int ok = 0, fail = 0;
        foreach (var (t, success, output) in results)
        {
            Log?.Invoke($"Applying: {t.Name}");
            if (success) { t.Applied = true; ok++; Log?.Invoke($"  OK  {t.Name}"); }
            else { fail++; Log?.Invoke($"  FAILED  {t.Name}: {FirstLine(output)}"); }
        }
        SaveState();
        return (ok, fail);
    }

    /// <summary>Revert a set of tweaks.</summary>
    public async Task<(int ok, int fail)> RevertAsync(IEnumerable<Tweak> tweaks, IProgress<TweakProgress>? progress = null)
    {
        var list = tweaks as IList<Tweak> ?? new List<Tweak>(tweaks);
        if (list.Count == 0) return (0, 0);

        // Only tweaks with a revert command spawn a process; run those concurrently.
        var withCmd = list.Where(t => !string.IsNullOrEmpty(t.RevertCmd)).ToList();
        var results = await RunBatchAsync(withCmd, t => t.RevertCmd, progress, "Reverting");

        int ok = 0, fail = 0;
        foreach (var (t, success, output) in results)
        {
            Log?.Invoke($"Reverting: {t.Name}");
            if (success) ok++; else { fail++; Log?.Invoke($"  FAILED  {t.Name}: {FirstLine(output)}"); }
        }
        // Every requested tweak is marked reverted, even the no-op (empty RevertCmd) ones.
        foreach (var t in list) { t.Applied = false; t.Selected = false; }
        SaveState();
        return (ok, fail);
    }

    /// <summary>
    /// Runs <paramref name="cmd"/> for each tweak concurrently (bounded by <see cref="MaxParallel"/>)
    /// and returns the per-tweak outcomes in the original order. Command execution happens on
    /// worker threads; no tweak state is mutated here so callers can update UI-bound properties
    /// on their own thread once this completes.
    /// </summary>
    private static async Task<(Tweak Tweak, bool Ok, string Output)[]> RunBatchAsync(
        IList<Tweak> list, Func<Tweak, string> cmd, IProgress<TweakProgress>? progress, string phase)
    {
        var results = new (Tweak Tweak, bool Ok, string Output)[list.Count];
        int done = 0;
        progress?.Report(new TweakProgress(phase, 0, list.Count));
        await Parallel.ForEachAsync(
            Enumerable.Range(0, list.Count),
            new ParallelOptions { MaxDegreeOfParallelism = MaxParallel },
            async (i, _) =>
            {
                var r = await CommandRunner.ExecAsync(cmd(list[i]));
                results[i] = (list[i], r.Ok, r.Output);
                progress?.Report(new TweakProgress(phase, Interlocked.Increment(ref done), list.Count));
            });
        return results;
    }

    public Task<(int ok, int fail)> ApplyTierAsync(TweakTier maxTier)
        => ApplyAsync(Tweaks.Where(t => (int)t.Tier <= (int)maxTier));

    public Task<(int ok, int fail)> RevertAllAsync()
        => RevertAsync(Tweaks.Where(t => t.Applied).ToList());

    /// <summary>
    /// Reconciles applied-tweak state at startup: the actual system state wins (so tweaks already
    /// applied by another optimizer, an older VOIDTUNE, or a fresh install are detected), and the
    /// saved settings.json is the fallback for tweaks we can't verify (services / powercfg / PS).
    /// </summary>
    private void LoadState()
    {
        try
        {
            var saved = AppSettingsStore.AppliedTweaks.ToHashSet();
            foreach (var t in Tweaks)
            {
                bool? verified = TweakVerifier.IsApplied(t);   // reads the live registry
                t.Applied = verified ?? saved.Contains(t.Id);
            }
            SaveState();   // persist the reconciled truth
        }
        catch { /* ignore */ }
    }

    /// <summary>Persists applied-tweak state to settings.json so toggles survive a restart.</summary>
    public void SaveState() =>
        AppSettingsStore.SetAppliedTweaks(Tweaks.Where(t => t.Applied).Select(t => t.Id));

    private static string FirstLine(string s)
        => string.IsNullOrEmpty(s) ? "" : s.Split('\n')[0];
}
