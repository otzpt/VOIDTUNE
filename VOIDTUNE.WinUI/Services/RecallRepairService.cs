using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace VOIDTUNE.WinUI.Services;

/// <summary>
/// Automatic startup repair for damage left behind by tweaks that older VOIDTUNE versions
/// shipped and later recalled as harmful. A Restore-tab button is not enough: most users will
/// never connect "my FPS dropped" to a specific registry value, so the app itself checks for
/// each recall's exact damage signature on every launch (it always runs elevated) and removes
/// it silently. Signature-based, not state-based — the applied-tweaks list drops removed IDs
/// on its first save after an update, so the registry itself is the only reliable witness.
/// </summary>
public static class RecallRepairService
{
    /// <summary>Human-readable descriptions of repairs performed this launch (empty = clean).</summary>
    public static List<string> RepairsDone { get; } = new();

    private const string BoostModeDefKey =
        @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\be337238-0d82-4146-a960-4f3749d470c7";

    public static Task RunAsync() => Task.Run(() =>
    {
        RepairTurboClamp();
        RepairHungAppAutoKill();
    });

    /// <summary>
    /// ix1/ix3 (removed 0.8.18) wrote ValueMax=0 / ValueMin=100 onto the machine-wide
    /// PERFBOOSTMODE definition, clamping CPU turbo boost off across every power plan.
    /// Those exact values at that exact key are our damage signature — no stock Windows
    /// install has them, and deleting them restores the Windows-default range either way.
    /// </summary>
    private static void RepairTurboClamp()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(BoostModeDefKey, writable: true);
            if (key is null) return;

            bool repaired = false;
            if (key.GetValue("ValueMax") is int max && max == 0)
            {
                key.DeleteValue("ValueMax", throwOnMissingValue: false);
                repaired = true;
            }
            if (key.GetValue("ValueMin") is int min && min == 100)
            {
                key.DeleteValue("ValueMin", throwOnMissingValue: false);
                repaired = true;
            }
            if (repaired)
                RepairsDone.Add("Removed a leftover from an older VOIDTUNE version that was clamping CPU turbo boost off — " +
                                "your CPU can boost normally again (fully applies after the next reboot).");
        }
        catch { /* repair is best-effort; never block startup */ }
    }

    /// <summary>
    /// Pre-0.8.18 ux1 set HungAppTimeout=2000 + AutoEndTasks=1, which made Windows auto-kill
    /// Explorer during routine 2-second stalls. Only repaired when the ux1 tweak is recorded
    /// as applied — this exact value pair can also be set intentionally by other tools, and we
    /// only clean up our own writes.
    /// </summary>
    private static void RepairHungAppAutoKill()
    {
        try
        {
            if (!AppSettingsStore.AppliedTweaks.Contains("ux1")) return;

            using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", writable: true);
            if (key is null) return;

            bool hadAutoEnd = string.Equals(key.GetValue("AutoEndTasks") as string, "1", StringComparison.Ordinal);
            if (!hadAutoEnd) return;

            key.DeleteValue("AutoEndTasks", throwOnMissingValue: false);
            key.DeleteValue("HungAppTimeout", throwOnMissingValue: false);
            RepairsDone.Add("Removed the aggressive hung-app auto-kill setting from an older VOIDTUNE version — " +
                            "this was causing Explorer to randomly restart.");
        }
        catch { /* best-effort */ }
    }
}
