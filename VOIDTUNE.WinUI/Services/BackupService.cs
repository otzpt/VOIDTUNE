using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VOIDTUNE.WinUI.Models;

namespace VOIDTUNE.WinUI.Services;

/// <summary>Exports the registry keys VOIDTUNE touches to a timestamped backup folder before applying.</summary>
public static class BackupService
{
    public static readonly string BackupRoot =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VOIDTUNE", "backups");

    private static readonly string[] Keys =
    {
        @"HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl",
        @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
        @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
        @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
        @"HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
        @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
        @"HKLM\SOFTWARE\Microsoft\Windows\Dwm",
        @"HKCU\System\GameConfigStore",
        @"HKCU\Software\Microsoft\GameBar",
        @"HKCU\Control Panel\Desktop",
    };

    /// <summary>Creates a backup folder with one .reg export per key. Returns the folder name (or empty on failure).</summary>
    public static async Task<string> CreateAsync(string note)
    {
        try
        {
            string folder = Path.Combine(BackupRoot, $"backup_{DateTime.Now:yyyyMMdd_HHmmss}_{note}");
            Directory.CreateDirectory(folder);
            // Each export spawns its own reg.exe; they touch different keys and are
            // independent, so run them concurrently instead of one-at-a-time.
            var exports = Keys.Select((key, i) =>
                CommandRunner.ExecAsync($"reg export \"{key}\" \"{Path.Combine(folder, $"key{i}.reg")}\" /y"));
            await Task.WhenAll(exports);
            return Path.GetFileName(folder);
        }
        catch
        {
            return "";
        }
    }

    /// <summary>Lists existing backup folders, newest first.</summary>
    public static List<BackupItem> List()
    {
        var list = new List<BackupItem>();
        try
        {
            if (!Directory.Exists(BackupRoot)) return list;
            foreach (var dir in Directory.GetDirectories(BackupRoot))
            {
                string name = Path.GetFileName(dir);
                int keys = Directory.GetFiles(dir, "*.reg").Length;
                ParseName(name, out string created, out string note);
                list.Add(new BackupItem
                {
                    FolderName = name,
                    FullPath = dir,
                    Created = created,
                    Note = note,
                    KeyCount = keys,
                });
            }
        }
        catch { /* ignore */ }
        return list.OrderByDescending(b => b.FolderName).ToList();
    }

    /// <summary>Re-imports every .reg file in a backup folder.</summary>
    public static async Task<(int ok, int fail)> RestoreAsync(BackupItem backup)
    {
        int ok = 0, fail = 0;
        try
        {
            foreach (var reg in Directory.GetFiles(backup.FullPath, "*.reg"))
            {
                var r = await CommandRunner.ExecAsync($"reg import \"{reg}\"");
                if (r.Ok) ok++; else fail++;
            }
        }
        catch { fail++; }
        return (ok, fail);
    }

    public static bool Delete(BackupItem backup)
    {
        try { Directory.Delete(backup.FullPath, recursive: true); return true; }
        catch { return false; }
    }

    // backup_yyyyMMdd_HHmmss_note  ->  "yyyy-MM-dd HH:mm:ss", "note"
    private static void ParseName(string name, out string created, out string note)
    {
        created = name; note = "";
        try
        {
            var parts = name.Split('_');
            if (parts.Length >= 3 && parts[1].Length == 8 && parts[2].Length == 6)
            {
                string d = parts[1], t = parts[2];
                created = $"{d.Substring(0, 4)}-{d.Substring(4, 2)}-{d.Substring(6, 2)} {t.Substring(0, 2)}:{t.Substring(2, 2)}:{t.Substring(4, 2)}";
                note = parts.Length > 3 ? string.Join("_", parts[3..]) : "";
            }
        }
        catch { /* keep raw name */ }
    }
}
