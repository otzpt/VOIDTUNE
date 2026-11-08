using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using VOIDTUNE.WinUI.Models;

namespace VOIDTUNE.WinUI.Services;

/// <summary>Read access to system state, swappable so verification can be unit tested.</summary>
internal sealed record SystemProbes(
    Func<string, string, object?> ReadValue,
    Func<string, int?> ServiceStart,
    Func<string, bool?> TaskEnabled);

/// <summary>
/// Checks whether a command's effects are present on the system by reading live state, so a
/// chain ending in "&amp; exit /b 0" can't report success when one of its steps failed. Covers
/// every reg add/delete, sc config and schtasks step. Returns null for commands with nothing
/// checkable (PowerShell, powercfg, netsh, fsutil).
/// </summary>
public static class TweakVerifier
{
    private static readonly Regex RegAdd = new(
        @"reg add\s+""(?<key>[^""]+)""\s+/v\s+(?<name>""[^""]+""|\S+)\s+/t\s+\S+\s+/d\s+(?<data>""[^""]+""|\S+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RegDel = new(
        @"reg delete\s+""(?<key>[^""]+)""\s+/v\s+(?<name>""[^""]+""|\S+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ScConfig = new(
        @"sc\s+config\s+(?<name>""[^""]+""|\S+)\s+start=\s*(?<mode>disabled|demand|delayed-auto|auto)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SchTask = new(
        @"schtasks\s+/Change\s+/TN\s+""(?<name>[^""]+)""\s+/(?<state>Enable|Disable)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly SystemProbes Live = new(ReadValue, ServiceStart, TaskEnabled);

    /// <summary>true = applied, false = not applied, null = nothing checkable.</summary>
    public static bool? IsApplied(Tweak t)
    {
        if (t.ApplyCmd.StartsWith("ENGINE:autoboost", StringComparison.Ordinal))
            return AppSettingsStore.AutoGameBoost;
        return Verify(t.ApplyCmd);
    }

    /// <summary>Whether every checkable step of this command is in effect right now.</summary>
    public static bool? Verify(string cmd) => Verify(cmd, Live);

    internal static bool? Verify(string cmd, SystemProbes probes)
    {
        if (string.IsNullOrWhiteSpace(cmd) || IsOpaque(cmd)) return null;

        int checks = 0;
        foreach (Match m in RegAdd.Matches(cmd))
        {
            checks++;
            object? cur = probes.ReadValue(m.Groups["key"].Value, Unquote(m.Groups["name"].Value));
            if (cur is null || !ValueMatches(cur, Unquote(m.Groups["data"].Value))) return false;
        }
        foreach (Match m in RegDel.Matches(cmd))
        {
            checks++;
            if (probes.ReadValue(m.Groups["key"].Value, Unquote(m.Groups["name"].Value)) is not null) return false;
        }
        // A service or task this Windows doesn't have is not a failure: there is nothing to change.
        foreach (Match m in ScConfig.Matches(cmd))
        {
            int? start = probes.ServiceStart(Unquote(m.Groups["name"].Value));
            if (start is null) continue;
            checks++;
            if (start != FallbackSynth.StartValue(m.Groups["mode"].Value)) return false;
        }
        foreach (Match m in SchTask.Matches(cmd))
        {
            bool? enabled = probes.TaskEnabled(m.Groups["name"].Value);
            if (enabled is null) continue;
            checks++;
            if (enabled != m.Groups["state"].Value.Equals("Enable", StringComparison.OrdinalIgnoreCase)) return false;
        }
        return checks > 0 ? true : null;
    }

    /// <summary>True when a tweak only touches services and tasks that this Windows doesn't have.</summary>
    public static bool IsObsolete(Tweak t) => IsObsolete(t.ApplyCmd, Live, Environment.IsPrivilegedProcess);

    internal static bool IsObsolete(string cmd, SystemProbes probes, bool canReadTasks)
    {
        if (string.IsNullOrWhiteSpace(cmd) || IsOpaque(cmd)) return false;
        if (RegAdd.IsMatch(cmd) || RegDel.IsMatch(cmd)) return false;

        var services = ScConfig.Matches(cmd).Select(m => Unquote(m.Groups["name"].Value)).ToList();
        var tasks = SchTask.Matches(cmd).Select(m => m.Groups["name"].Value).ToList();
        if (services.Count + tasks.Count == 0) return false;
        if (tasks.Count > 0 && !canReadTasks) return false;   // unreadable is not the same as absent

        return services.All(s => probes.ServiceStart(s) is null) && tasks.All(n => probes.TaskEnabled(n) is null);
    }

    /// <summary>Every registry key a command writes to, for backing up before applying.</summary>
    public static IEnumerable<string> RegistryKeys(string cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd) || IsOpaque(cmd)) return Array.Empty<string>();
        return RegAdd.Matches(cmd).Concat(RegDel.Matches(cmd)).Select(m => m.Groups["key"].Value);
    }

    private static bool IsOpaque(string cmd) =>
        cmd.StartsWith("PS:", StringComparison.Ordinal) || cmd.StartsWith("ENGINE:", StringComparison.Ordinal);

    private static string Unquote(string s) => s.Trim('"');

    private static object? ReadValue(string fullKey, string name)
    {
        var (hive, sub) = SplitHive(fullKey);
        if (hive is null) return null;
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive.Value, RegistryView.Default);
            using var k = baseKey.OpenSubKey(sub);
            return k?.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        }
        catch { return null; }
    }

    private static int? ServiceStart(string name)
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{name}");
            return k?.GetValue("Start") as int?;
        }
        catch { return null; }
    }

    // A task's definition is an XML file; a disabled task has <Enabled>false</Enabled> in <Settings>.
    private static bool? TaskEnabled(string taskName)
    {
        try
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "Tasks", taskName.TrimStart('\\'));
            if (!File.Exists(path)) return null;
            var settings = Regex.Match(File.ReadAllText(path), "<Settings>.*?</Settings>", RegexOptions.Singleline);
            return !settings.Value.Contains("<Enabled>false</Enabled>", StringComparison.OrdinalIgnoreCase);
        }
        catch { return null; }
    }

    private static (RegistryHive? hive, string sub) SplitHive(string key)
    {
        int i = key.IndexOf('\\');
        if (i < 0) return (null, "");
        string root = key[..i].ToUpperInvariant();
        string sub = key[(i + 1)..];
        return root switch
        {
            "HKLM" or "HKEY_LOCAL_MACHINE" => (RegistryHive.LocalMachine, sub),
            "HKCU" or "HKEY_CURRENT_USER"  => (RegistryHive.CurrentUser, sub),
            "HKCR" or "HKEY_CLASSES_ROOT"  => (RegistryHive.ClassesRoot, sub),
            "HKU"  or "HKEY_USERS"         => (RegistryHive.Users, sub),
            _ => (null, ""),
        };
    }

    private static bool ValueMatches(object cur, string data)
    {
        if (cur is int dw)
        {
            if (int.TryParse(data, out int d)) return dw == d;
            if (data.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(data.AsSpan(2), NumberStyles.HexNumber, null, out int dh)) return dw == dh;
            if (uint.TryParse(data, out uint du)) return unchecked((int)du) == dw;   // e.g. 4294967295 -> -1
            return false;
        }
        return string.Equals(cur.ToString(), data, StringComparison.OrdinalIgnoreCase);
    }
}
