using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using VOIDTUNE.WinUI.Models;

namespace VOIDTUNE.WinUI.Services;

/// <summary>
/// Startup program manager: registry Run keys (HKCU/HKLM) + Startup folders.
/// Disabling stashes the entry in a VOIDTUNE backup key/folder so it can be restored.
/// </summary>
public sealed class StartupManager
{
    private const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string BackupPath = @"Software\VOIDTUNE\StartupDisabled";

    // Disabled Startup-folder shortcuts are stashed HERE, in LocalAppData — NOT inside the
    // Startup folder. Windows opens any *folder* placed in Startup as an Explorer window at
    // login, so an in-Startup stash folder popped open every boot. This lives outside it.
    private static readonly string StashDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VOIDTUNE", "StartupDisabled");

    public ObservableCollection<StartupItem> Items { get; } = new();

    public void Refresh()
    {
        Items.Clear();
        MigrateOldStashFolders();   // clean up any legacy VOIDTUNE_Disabled folder inside Startup

        // Enabled registry entries
        ReadRunKey(Registry.CurrentUser, "HKCU", true);
        ReadRunKey(Registry.LocalMachine, "HKLM", true);
        // Disabled registry entries (from our backup hive)
        ReadBackup("HKCU");
        ReadBackup("HKLM");

        // Startup folders
        ReadFolder(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Folder (user)");
        ReadFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), "Folder (all users)");
        ReadDisabledFolderItems();   // disabled folder shortcuts, stashed outside Startup
    }

    private void ReadRunKey(RegistryKey root, string scope, bool enabled)
    {
        try
        {
            using var k = root.OpenSubKey(RunPath);
            if (k == null) return;
            foreach (var name in k.GetValueNames())
            {
                if (string.IsNullOrEmpty(name)) continue;
                Items.Add(new StartupItem
                {
                    Name = name,
                    Command = k.GetValue(name)?.ToString() ?? "",
                    Location = $"{scope}\\…\\Run",
                    Scope = scope,
                    Enabled = enabled,
                    Committed = enabled,
                });
            }
        }
        catch { /* ignore */ }
    }

    private void ReadBackup(string scope)
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey($@"{BackupPath}\{scope}");
            if (k == null) return;
            foreach (var name in k.GetValueNames())
            {
                if (string.IsNullOrEmpty(name)) continue;
                Items.Add(new StartupItem
                {
                    Name = name,
                    Command = k.GetValue(name)?.ToString() ?? "",
                    Location = $"{scope}\\…\\Run (disabled)",
                    Scope = scope,
                    Enabled = false,
                    Committed = false,
                });
            }
        }
        catch { /* ignore */ }
    }

    private void ReadFolder(string folder, string label)
    {
        try
        {
            if (!Directory.Exists(folder)) return;
            foreach (var f in Directory.GetFiles(folder))
            {
                Items.Add(new StartupItem
                {
                    Name = Path.GetFileNameWithoutExtension(f),
                    Command = f,
                    Location = label,
                    Scope = "Folder:" + folder,
                    Enabled = true,
                    Committed = true,
                });
            }
        }
        catch { /* ignore */ }
    }

    // Older builds stashed disabled shortcuts in a "VOIDTUNE_Disabled" folder *inside* Startup,
    // which Windows opened as an Explorer window at every login. Move any leftovers out to the
    // proper stash and delete those folders (runs elevated, so it can fix the all-users Startup).
    private void MigrateOldStashFolders()
    {
        const string legacyName = "VOIDTUNE_Disabled";
        foreach (var startup in new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
        })
        {
            try
            {
                string legacy = Path.Combine(startup, legacyName);
                if (!Directory.Exists(legacy)) continue;
                Directory.CreateDirectory(StashDir);
                foreach (var f in Directory.GetFiles(legacy))
                {
                    if (Path.GetFileName(f).Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;
                    try { File.Move(f, Path.Combine(StashDir, Path.GetFileName(f)), true); } catch { }
                }
                try { Directory.Delete(legacy, true); } catch { /* files in use / perms */ }
            }
            catch { /* ignore */ }
        }
    }

    // Disabled Startup-folder shortcuts, stashed in LocalAppData (outside the Startup folder).
    // Re-enabling drops them back into the user's Startup folder.
    private void ReadDisabledFolderItems()
    {
        try
        {
            if (!Directory.Exists(StashDir)) return;
            string userStartup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            foreach (var f in Directory.GetFiles(StashDir))
            {
                if (Path.GetFileName(f).Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;
                Items.Add(new StartupItem
                {
                    Name = Path.GetFileNameWithoutExtension(f),
                    Command = f,
                    Location = "Startup folder (disabled)",
                    Scope = "Folder:" + userStartup,
                    Enabled = false,
                    Committed = false,
                });
            }
        }
        catch { /* ignore */ }
    }

    /// <summary>
    /// Applies the enable/disable change to the registry or Startup folder and updates the
    /// item in place (Location label, and the file path for folder entries). Deliberately does
    /// NOT rebuild the bound <see cref="Items"/> collection: rebuilding re-realizes the
    /// ToggleSwitch containers, whose TwoWay IsOn binding re-fires Toggled and loops/crashes.
    /// </summary>
    public void SetEnabled(StartupItem item, bool enable)
    {
        if (item.Scope.StartsWith("Folder:", StringComparison.Ordinal))
            ToggleFolder(item, enable);
        else
            ToggleRegistry(item, enable);

        // Reflect the new state in the label without touching the collection.
        const string suffix = " (disabled)";
        string baseLoc = item.Location.EndsWith(suffix, StringComparison.Ordinal)
            ? item.Location[..^suffix.Length]
            : item.Location;
        item.Location = enable ? baseLoc : baseLoc + suffix;
    }

    private void ToggleRegistry(StartupItem item, bool enable)
    {
        RegistryKey root = item.Scope == "HKLM" ? Registry.LocalMachine : Registry.CurrentUser;
        try
        {
            if (enable)
            {
                using var run = root.CreateSubKey(RunPath);
                run?.SetValue(item.Name, item.Command);
                using var bk = Registry.CurrentUser.CreateSubKey($@"{BackupPath}\{item.Scope}");
                bk?.DeleteValue(item.Name, false);
            }
            else
            {
                using var bk = Registry.CurrentUser.CreateSubKey($@"{BackupPath}\{item.Scope}");
                bk?.SetValue(item.Name, item.Command);
                using var run = root.OpenSubKey(RunPath, writable: true);
                run?.DeleteValue(item.Name, false);
            }
        }
        catch { /* ignore — likely a permission edge case */ }
    }

    private void ToggleFolder(StartupItem item, bool enable)
    {
        try
        {
            string folder = item.Scope.Substring("Folder:".Length);
            Directory.CreateDirectory(StashDir);
            string fileName = Path.GetFileName(item.Command);
            // enable → back into the Startup folder; disable → into the external stash.
            string dest = enable ? Path.Combine(folder, fileName) : Path.Combine(StashDir, fileName);
            if (File.Exists(item.Command)) File.Move(item.Command, dest, true);
            item.Command = dest;   // keep the path current so a later re-toggle finds the file
        }
        catch { /* ignore */ }
    }
}
