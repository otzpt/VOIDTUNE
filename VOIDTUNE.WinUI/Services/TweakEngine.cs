using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VOIDTUNE.WinUI.Models;

namespace VOIDTUNE.WinUI.Services;

/// <summary>App-wide tweak state: the catalog, applied tracking, apply/revert, persistence.</summary>
public sealed class TweakEngine
{
    public static TweakEngine Instance { get; } = new();

    public ObservableCollection<Tweak> Tweaks { get; } = new();

    private static readonly string StateDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VOIDTUNE");
    private static readonly string StateFile = Path.Combine(StateDir, "applied.txt");

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
    public async Task<(int ok, int fail)> ApplyAsync(IEnumerable<Tweak> tweaks)
    {
        var list = tweaks as IList<Tweak> ?? new List<Tweak>(tweaks);
        if (list.Count == 0) return (0, 0);

        string backup = await BackupService.CreateAsync("apply");
        if (!string.IsNullOrEmpty(backup)) Log?.Invoke($"Registry backup: {backup}");

        // The commands are independent registry/power writes, so run them concurrently
        // (bounded) instead of one process at a time. The body only writes to its own
        // slot here; UI-bound state (Applied) is updated below, back on the caller's
        // thread, so we never raise PropertyChanged from a worker thread.
        var results = await RunBatchAsync(list, t => t.ApplyCmd);

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
    public async Task<(int ok, int fail)> RevertAsync(IEnumerable<Tweak> tweaks)
    {
        var list = tweaks as IList<Tweak> ?? new List<Tweak>(tweaks);
        if (list.Count == 0) return (0, 0);

        // Only tweaks with a revert command spawn a process; run those concurrently.
        var withCmd = list.Where(t => !string.IsNullOrEmpty(t.RevertCmd)).ToList();
        var results = await RunBatchAsync(withCmd, t => t.RevertCmd);

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
        IList<Tweak> list, Func<Tweak, string> cmd)
    {
        var results = new (Tweak Tweak, bool Ok, string Output)[list.Count];
        await Parallel.ForEachAsync(
            Enumerable.Range(0, list.Count),
            new ParallelOptions { MaxDegreeOfParallelism = MaxParallel },
            async (i, _) =>
            {
                var r = await CommandRunner.ExecAsync(cmd(list[i]));
                results[i] = (list[i], r.Ok, r.Output);
            });
        return results;
    }

    public Task<(int ok, int fail)> ApplyTierAsync(TweakTier maxTier)
        => ApplyAsync(Tweaks.Where(t => (int)t.Tier <= (int)maxTier));

    public Task<(int ok, int fail)> RevertAllAsync()
        => RevertAsync(Tweaks.Where(t => t.Applied).ToList());

    private void LoadState()
    {
        try
        {
            if (!File.Exists(StateFile)) return;
            var ids = File.ReadAllLines(StateFile).Where(l => l.Length > 0).ToHashSet();
            foreach (var t in Tweaks) if (ids.Contains(t.Id)) t.Applied = true;
        }
        catch { /* ignore */ }
    }

    private void SaveState()
    {
        try
        {
            Directory.CreateDirectory(StateDir);
            File.WriteAllLines(StateFile, Tweaks.Where(t => t.Applied).Select(t => t.Id));
        }
        catch { /* ignore */ }
    }

    private static string FirstLine(string s)
        => string.IsNullOrEmpty(s) ? "" : s.Split('\n')[0];
}
