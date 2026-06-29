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
        int ok = 0, fail = 0;
        var list = tweaks as IList<Tweak> ?? new List<Tweak>(tweaks);
        if (list.Count > 0)
        {
            string backup = await BackupService.CreateAsync("apply");
            if (!string.IsNullOrEmpty(backup)) Log?.Invoke($"Registry backup: {backup}");
        }
        foreach (var t in list)
        {
            Log?.Invoke($"Applying: {t.Name}");
            var r = await CommandRunner.ExecAsync(t.ApplyCmd);
            if (r.Ok) { t.Applied = true; ok++; Log?.Invoke($"  OK  {t.Name}"); }
            else { fail++; Log?.Invoke($"  FAILED  {t.Name}: {FirstLine(r.Output)}"); }
        }
        SaveState();
        return (ok, fail);
    }

    /// <summary>Revert a set of tweaks.</summary>
    public async Task<(int ok, int fail)> RevertAsync(IEnumerable<Tweak> tweaks)
    {
        int ok = 0, fail = 0;
        foreach (var t in tweaks)
        {
            if (!string.IsNullOrEmpty(t.RevertCmd))
            {
                Log?.Invoke($"Reverting: {t.Name}");
                var r = await CommandRunner.ExecAsync(t.RevertCmd);
                if (r.Ok) ok++; else { fail++; Log?.Invoke($"  FAILED  {t.Name}: {FirstLine(r.Output)}"); }
            }
            t.Applied = false;
            t.Selected = false;
        }
        SaveState();
        return (ok, fail);
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
