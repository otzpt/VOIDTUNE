using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using VOIDTUNE.WinUI.Models;

namespace VOIDTUNE.WinUI.Services;

/// <summary>
/// DevTools Reg Diff: snapshot a registry subtree, change something (apply a tweak, flip a
/// Windows setting), snapshot again, and see exactly which values changed. Read-only —
/// it never writes to the registry.
/// </summary>
public static class RegDiffService
{
    /// <summary>Snapshot cap: a scope like HKLM\SOFTWARE would produce millions of values
    /// and lock the UI for minutes. Past this we abort and ask for a narrower scope.</summary>
    private const int MaxEntries = 400_000;

    /// <summary>Walks the subtree under <paramref name="root"/> (e.g. "HKCU\Software\Microsoft")
    /// into a value map: "subkey||valueName" → "KIND:data".</summary>
    public static Task<Dictionary<string, string>> SnapshotAsync(string root) => Task.Run(() =>
    {
        var (hive, subPath) = ParseRoot(root);
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
        using var start = subPath.Length == 0 ? baseKey : baseKey.OpenSubKey(subPath);
        if (start is null) throw new InvalidOperationException("Key not found: " + root);

        var stack = new Stack<(RegistryKey Key, string Path)>();
        stack.Push((start, ""));

        while (stack.Count > 0)
        {
            var (key, path) = stack.Pop();
            try
            {
                foreach (string name in key.GetValueNames())
                {
                    map[path + "||" + name] = FormatValue(key, name);
                    if (map.Count > MaxEntries)
                        throw new InvalidOperationException(
                            $"Scope too large (>{MaxEntries:N0} values) — narrow it, e.g. a specific subkey.");
                }
                foreach (string child in key.GetSubKeyNames())
                {
                    RegistryKey? sub = null;
                    try { sub = key.OpenSubKey(child); } catch { /* access denied — skip */ }
                    if (sub != null) stack.Push((sub, path.Length == 0 ? child : path + "\\" + child));
                }
            }
            catch (InvalidOperationException) { throw; }
            catch { /* access denied on this key — skip it */ }
            finally
            {
                // child keys were opened by us; the start key is disposed by its using block
                if (!ReferenceEquals(key, start)) key.Dispose();
            }
        }
        return map;
    });

    /// <summary>Compares two snapshots; returns changed → added → removed, capped for display.</summary>
    public static List<DiffRow> Diff(Dictionary<string, string> before, Dictionary<string, string> after, int cap = 600)
    {
        var rows = new List<DiffRow>();

        foreach (var (k, newVal) in after)
        {
            if (before.TryGetValue(k, out var oldVal))
            {
                if (!string.Equals(oldVal, newVal, StringComparison.Ordinal))
                    rows.Add(Row("CHANGED", "#F59E0B", k, $"{oldVal}  →  {newVal}"));
            }
            else
            {
                rows.Add(Row("ADDED", "#22C55E", k, newVal));
            }
        }
        foreach (var (k, oldVal) in before)
            if (!after.ContainsKey(k))
                rows.Add(Row("REMOVED", "#EF4444", k, oldVal));

        return rows
            .OrderBy(r => r.Change switch { "CHANGED" => 0, "ADDED" => 1, _ => 2 })
            .ThenBy(r => r.Key, StringComparer.OrdinalIgnoreCase)
            .Take(cap)
            .ToList();
    }

    /// <summary>Renders diff rows as ready-to-use reg add / reg delete commands.</summary>
    public static string ToCommands(IEnumerable<DiffRow> rows, string root)
    {
        var sb = new StringBuilder();
        foreach (var r in rows)
        {
            string fullKey = r.Key.Length == 0 ? root : root + "\\" + r.Key;
            string name = r.Name.Length == 0 ? "(Default)" : r.Name;
            string qn = name.Contains(' ') ? $"\"{name}\"" : name;

            if (r.Change == "REMOVED")
            {
                sb.AppendLine($"reg add \"{fullKey}\" /v {qn} /t {KindOf(r.Detail)} /d {DataOf(r.Detail)} /f   :: restore removed value");
                continue;
            }
            // ADDED / CHANGED → the new value is the target state
            string newPart = r.Change == "CHANGED" ? r.Detail.Split("  →  ").Last() : r.Detail;
            string kind = KindOf(newPart);
            if (kind.Length == 0)
                sb.AppendLine($":: {r.Change} {fullKey} \\ {name} = {newPart}  (type not scriptable)");
            else
                sb.AppendLine($"reg add \"{fullKey}\" /v {qn} /t {kind} /d {DataOf(newPart)} /f");
        }
        return sb.ToString();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static DiffRow Row(string change, string hex, string mapKey, string detail)
    {
        int sep = mapKey.LastIndexOf("||", StringComparison.Ordinal);
        return new DiffRow
        {
            Change = change,
            Hex = hex,
            Key = sep <= 0 ? "" : mapKey[..sep],
            Name = sep < 0 ? mapKey : mapKey[(sep + 2)..],
            Detail = detail,
        };
    }

    private static (RegistryHive hive, string subPath) ParseRoot(string root)
    {
        string r = root.Trim().TrimEnd('\\');
        int slash = r.IndexOf('\\');
        string hivePart = (slash < 0 ? r : r[..slash]).ToUpperInvariant();
        string rest = slash < 0 ? "" : r[(slash + 1)..];
        RegistryHive hive = hivePart switch
        {
            "HKLM" or "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
            "HKCU" or "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
            "HKCR" or "HKEY_CLASSES_ROOT" => RegistryHive.ClassesRoot,
            "HKU" or "HKEY_USERS" => RegistryHive.Users,
            _ => throw new InvalidOperationException("Scope must start with HKLM\\, HKCU\\, HKCR\\ or HKU\\"),
        };
        return (hive, rest);
    }

    private static string FormatValue(RegistryKey key, string name)
    {
        try
        {
            var kind = key.GetValueKind(name);
            object? v = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            return kind switch
            {
                RegistryValueKind.DWord => "DWORD:" + Convert.ToUInt32(Convert.ToInt64(v) & 0xFFFFFFFF),
                RegistryValueKind.QWord => "QWORD:" + v,
                RegistryValueKind.String => "SZ:" + v,
                RegistryValueKind.ExpandString => "EXPAND:" + v,
                RegistryValueKind.MultiString => "MULTI:" + string.Join("|", (string[])(v ?? Array.Empty<string>())),
                RegistryValueKind.Binary => "BIN:" + BitConverter.ToString(((byte[])(v ?? Array.Empty<byte>())).Take(24).ToArray()),
                _ => "OTHER:" + v,
            };
        }
        catch { return "UNREADABLE"; }
    }

    private static string KindOf(string formatted) => formatted.Split(':')[0] switch
    {
        "DWORD" => "REG_DWORD",
        "QWORD" => "REG_QWORD",
        "SZ" => "REG_SZ",
        "EXPAND" => "REG_EXPAND_SZ",
        _ => "",
    };

    private static string DataOf(string formatted)
    {
        int i = formatted.IndexOf(':');
        string data = i < 0 ? formatted : formatted[(i + 1)..];
        return data.Contains(' ') ? $"\"{data}\"" : data;
    }
}
