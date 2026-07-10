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
        // Lightweight: just load the catalog + custom tweaks. The heavy live-state
        // reconciliation (a registry read per tweak) runs in InitializeAsync so the
        // startup loading screen can show progress instead of freezing the UI thread.
        foreach (var t in TweakCatalog.All) Tweaks.Add(t);
        foreach (var c in AppSettingsStore.CustomTweaks) Tweaks.Add(FromModel(c));
    }

    private bool _initialized;

    /// <summary>
    /// Reconciles every tweak's applied-state against the live system on a background thread,
    /// then resumes the auto-game-boost watcher. Safe to call more than once (no-op after the
    /// first). Awaited by the startup loading screen. Runs before any page binds, so mutating
    /// the tweaks' observable state off the UI thread here has no cross-thread listeners.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;
        // Recall repair runs FIRST — it removes damage signatures left by tweaks older versions
        // shipped and later recalled, before state reconciliation reads the registry.
        try { await RecallRepairService.RunAsync(); } catch { /* best-effort */ }
        try { await Task.Run(LoadState); } catch { /* corrupt reg / access — keep saved state */ }
        try { if (AppSettingsStore.AutoGameBoost || AppSettingsStore.GameTimePowerPlan) GameWatcherService.Start(); } catch { /* never block startup */ }

        foreach (string repair in RecallRepairService.RepairsDone)
            Log?.Invoke("Startup repair: " + repair);
    }

    /// <summary>Lets engines (game watcher etc.) write into the live DevTools log.</summary>
    public void EmitLog(string line) => Log?.Invoke(line);

    /// <summary>Adds a Tweak Lab creation to the catalog and persists it to settings.json.</summary>
    public void AddCustomTweak(Tweak t)
    {
        Tweaks.Add(t);
        AppSettingsStore.AddCustomTweak(new AppSettingsStore.CustomTweakModel
        {
            Id = t.Id,
            Name = t.Name,
            Category = t.Category,
            Tier = (int)t.Tier,
            Description = t.Description,
            ApplyCmd = t.ApplyCmd,
            RevertCmd = t.RevertCmd,
        });
    }

    private static Tweak FromModel(AppSettingsStore.CustomTweakModel c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Category = string.IsNullOrWhiteSpace(c.Category) ? "Custom" : c.Category,
        Tier = (TweakTier)c.Tier,
        Description = string.IsNullOrWhiteSpace(c.Description) ? "Custom tweak built in DevTools." : c.Description,
        ApplyCmd = c.ApplyCmd,
        RevertCmd = c.RevertCmd,
    };

    public int AppliedCount => Tweaks.Count(t => t.Applied);

    /// <summary>The failed tweaks (with their raw command output) from the most recent ApplyAsync
    /// call, so the UI can show exactly what went wrong instead of just a count.</summary>
    public IReadOnlyList<(Tweak Tweak, string Output)> LastApplyFailures { get; private set; } = Array.Empty<(Tweak, string)>();

    /// <summary>Path to the log file written for the most recent apply, if anything failed ("" otherwise).</summary>
    public string LastApplyLogPath { get; private set; } = "";

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
        var failures = new List<(Tweak, string)>();
        var logEntries = new List<(Tweak, bool, string)>();
        foreach (var (t, success, output) in results)
        {
            Log?.Invoke($"Applying: {t.Name}");

            // Layer 1 — outcome over exit code: netsh/sc/reg return non-zero for benign reasons.
            // If the command "failed" but the system already shows the desired state, it's applied.
            bool applied = success || TweakVerifier.IsApplied(t) == true;

            // Layer 2 — second method: the tweak's explicit FallbackCmd, or (for service tweaks)
            // a synthesized registry equivalent of its sc-config calls — SCM blocks the live
            // change on some systems while the registry Start value still converges at reboot.
            if (!applied)
            {
                string fallback = !string.IsNullOrEmpty(t.FallbackCmd) ? t.FallbackCmd : FallbackSynth.ForCommand(t.ApplyCmd);
                if (!string.IsNullOrEmpty(fallback))
                {
                    Log?.Invoke($"  retrying via fallback: {t.Name}");
                    var fb = await CommandRunner.ExecAsync(fallback);
                    applied = fb.Ok || TweakVerifier.IsApplied(t) == true;
                }
            }

            if (applied)
            {
                t.Applied = true; ok++;
                Log?.Invoke(success ? $"  OK  {t.Name}" : $"  OK (verified)  {t.Name}");
                logEntries.Add((t, true, output));
            }
            else
            {
                fail++;
                Log?.Invoke($"  FAILED  {t.Name}: {FirstLine(output)}");
                failures.Add((t, output));
                logEntries.Add((t, false, output));
            }
        }
        LastApplyFailures = failures;
        LastApplyLogPath = fail > 0 ? ApplyLogService.WriteLog("apply", logEntries) : "";
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
            // Same outcome-based fallback as apply: verified-gone beats a lying exit code.
            bool reverted = success || TweakVerifier.IsApplied(t) == false;

            // Same second-method safety net as apply — a revert that silently fails is worse
            // than an apply that fails, because the user believes they're back to stock.
            if (!reverted)
            {
                string fallback = FallbackSynth.ForCommand(t.RevertCmd);
                if (!string.IsNullOrEmpty(fallback))
                {
                    Log?.Invoke($"  retrying revert via fallback: {t.Name}");
                    var fb = await CommandRunner.ExecAsync(fallback);
                    reverted = fb.Ok || TweakVerifier.IsApplied(t) == false;
                }
            }
            if (reverted) ok++; else { fail++; Log?.Invoke($"  FAILED  {t.Name}: {FirstLine(output)}"); }
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
