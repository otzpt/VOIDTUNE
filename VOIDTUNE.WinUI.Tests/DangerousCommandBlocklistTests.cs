using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Tests;

/// <summary>
/// Static guardrail against a specific class of mistake we've already shipped once: a tweak
/// whose command is far more destructive than its one-line description suggests. "Full Reset to
/// Windows Defaults" in 0.8.10 called `powercfg -restoredefaultschemes` (silently deleted the
/// user's unlocked Ultimate Performance plan) and `netsh int tcp/ip reset` (reinitializes the
/// whole Winsock LSP catalog, capable of killing any process holding a live socket) — both had
/// to be hotfixed in 0.8.11. This test turns those two real incidents, plus a few structurally
/// similar footguns, into a build-breaking check so the same class of bug can't ship again
/// without a deliberate override.
/// </summary>
public class DangerousCommandBlocklistTests
{
    private sealed record Rule(string Description, Regex Pattern);

    private static readonly List<Rule> Blocklist = new()
    {
        new("powercfg -restoredefaultschemes wipes ALL unlocked custom power schemes (the 0.8.10 regression)",
            new Regex(@"powercfg\s*[-/]restoredefaultschemes", RegexOptions.IgnoreCase)),

        new("bare 'netsh int tcp|ip reset' reinitializes the whole TCP/IP stack incl. Winsock LSP catalog " +
            "(the 0.8.10 regression) — use targeted 'netsh int tcp set global ...' instead",
            new Regex(@"netsh\s+int(erface)?\s+(tcp|ip)\s+reset\b", RegexOptions.IgnoreCase)),

        new("'reg delete' without /v deletes an entire registry key subtree, not a single value",
            new Regex(@"reg\s+delete\s+""[^""]+""\s+/f", RegexOptions.IgnoreCase)),

        new("diskpart can repartition/wipe a disk depending on the script fed to it",
            new Regex(@"\bdiskpart\b", RegexOptions.IgnoreCase)),

        new("vssadmin delete shadows removes System Restore/File History recovery points",
            new Regex(@"vssadmin\s+delete\s+shadows", RegexOptions.IgnoreCase)),

        new("bcdedit boot-configuration edits can leave the system unbootable",
            new Regex(@"\bbcdedit\b", RegexOptions.IgnoreCase)),

        new("bare 'format X:' destroys a volume",
            new Regex(@"\bformat\s+[a-zA-Z]:", RegexOptions.IgnoreCase)),

        new("forced shutdown/restart should never be embedded in a tweak command — the app's own reboot prompt handles this",
            new Regex(@"shutdown\s+/[rs]\b", RegexOptions.IgnoreCase)),

        new("writing ValueMax/ValueMin under Control\\Power\\PowerSettings redefines a power setting's " +
            "allowed range MACHINE-WIDE, across every plan — the removed ix1 tweak did this and clamped " +
            "turbo boost off on all Intel machines (field-confirmed FPS loss that survived plan switches). " +
            "Change plan values with powercfg, never the setting definitions. (reg delete to CLEAN UP " +
            "stale overrides, as rst7 does, is fine.)",
            new Regex(@"reg\s+add\s+""HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\[^""]*""\s+/v\s+Value(Max|Min)\b", RegexOptions.IgnoreCase)),

        new("AutoEndTasks=1 makes Windows auto-kill any app it considers hung — combined with a short " +
            "HungAppTimeout it repeatedly killed Explorer during routine 2-second stalls (field-confirmed " +
            "\"Explorer randomly restarts\" from the pre-0.8.14 ux1 tweak). Deleting the value is fine; " +
            "setting it is not.",
            new Regex(@"reg\s+add\s+""HKCU\\Control Panel\\Desktop""\s+/v\s+AutoEndTasks\b", RegexOptions.IgnoreCase)),
    };

    public static IEnumerable<object[]> AllTweaks() =>
        TweakCatalog.All.Select(t => new object[] { t.Id, t.ApplyCmd, t.RevertCmd, t.FallbackCmd });

    [Theory]
    [MemberData(nameof(AllTweaks))]
    public void Tweak_command_does_not_match_the_blocklist(string id, string applyCmd, string revertCmd, string fallbackCmd)
    {
        string combined = string.Join(" \n ", new[] { applyCmd, revertCmd, fallbackCmd }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var hits = Blocklist.Where(r => r.Pattern.IsMatch(combined)).Select(r => r.Description).ToList();

        Assert.True(hits.Count == 0,
            $"Tweak '{id}' matched a dangerous-command rule: {string.Join(" | ", hits)}. " +
            "If this is genuinely intended, tighten the rule's regex rather than deleting it.");
    }
}
