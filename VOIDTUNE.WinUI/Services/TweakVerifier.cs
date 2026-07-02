using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using VOIDTUNE.WinUI.Models;

namespace VOIDTUNE.WinUI.Services;

/// <summary>
/// Detects whether a tweak's effect is already present on the system by reading the live registry.
/// Handles the registry-based tweaks (the large majority). Returns null for tweaks we can't verify
/// generically (services, powercfg, netsh, fsutil, PowerShell) — those fall back to saved state.
/// </summary>
public static class TweakVerifier
{
    private static readonly Regex RegAdd = new(
        @"reg add\s+""(?<key>[^""]+)""\s+/v\s+(?<name>""[^""]+""|\S+)\s+/t\s+\S+\s+/d\s+(?<data>""[^""]+""|\S+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RegDel = new(
        @"reg delete\s+""(?<key>[^""]+)""\s+/v\s+(?<name>""[^""]+""|\S+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>true = applied, false = not applied, null = undeterminable (non-registry tweak).</summary>
    public static bool? IsApplied(Tweak t)
    {
        string cmd = t.ApplyCmd;
        if (string.IsNullOrWhiteSpace(cmd) || cmd.StartsWith("PS:", StringComparison.Ordinal)) return null;

        var add = RegAdd.Match(cmd);
        if (add.Success)
        {
            object? cur = ReadValue(add.Groups["key"].Value, Unquote(add.Groups["name"].Value));
            return cur is null ? false : ValueMatches(cur, Unquote(add.Groups["data"].Value));
        }

        var del = RegDel.Match(cmd);
        if (del.Success)
            return ReadValue(del.Groups["key"].Value, Unquote(del.Groups["name"].Value)) is null;  // applied == value gone

        return null;   // sc / powercfg / netsh / fsutil / bcdedit — can't verify generically
    }

    private static string Unquote(string s) => s.Trim('"');

    private static object? ReadValue(string fullKey, string name)
    {
        var (hive, sub) = SplitHive(fullKey);
        if (hive is null) return null;
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive.Value, RegistryView.Default);
            using var k = baseKey.OpenSubKey(sub);
            return k?.GetValue(name);
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
