using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace VOIDTUNE.WinUI.Services;

/// <summary>
/// Synthesizes a second-method fallback for tweaks that don't declare an explicit FallbackCmd.
/// Motivation: real field reports of "N safe tweaks failed" on other machines — the most common
/// legitimately-recoverable failure is `sc config` being blocked (service locked by policy, SCM
/// quirks, permission edge cases) while the equivalent registry write still works. The registry
/// Start value is the same setting the SCM reads at boot, so the fallback converges to the same
/// state — it just takes effect at the next reboot instead of immediately.
/// </summary>
public static class FallbackSynth
{
    private static readonly Regex ScConfig = new(
        @"sc\s+config\s+(""[^""]+""|\S+)\s+start=\s*(disabled|demand|delayed-auto|auto)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Registry Start values per SCM start type.</summary>
    private static int StartValue(string mode) => mode.ToLowerInvariant() switch
    {
        "disabled" => 4,
        "demand" => 3,
        _ => 2, // auto and delayed-auto
    };

    /// <summary>
    /// Returns a registry-based equivalent for every `sc config X start= Y` in the command,
    /// or "" when the command has no synthesizable pattern (PS:/ENGINE:/non-service commands
    /// are left alone — their failures are either already handled or not fallback-able).
    /// </summary>
    public static string ForCommand(string cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd)) return "";
        if (cmd.StartsWith("PS:") || cmd.StartsWith("ENGINE:")) return "";

        var parts = new List<string>();
        foreach (Match m in ScConfig.Matches(cmd))
        {
            string svc = m.Groups[1].Value.Trim('"');
            string mode = m.Groups[2].Value;
            parts.Add($@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\{svc}"" /v Start /t REG_DWORD /d {StartValue(mode)} /f");
            if (mode.Equals("delayed-auto", System.StringComparison.OrdinalIgnoreCase))
                parts.Add($@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\{svc}"" /v DelayedAutostart /t REG_DWORD /d 1 /f");
        }

        return parts.Count == 0 ? "" : string.Join(" & ", parts) + " & exit /b 0";
    }
}
