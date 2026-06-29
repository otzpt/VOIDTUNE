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
    private const string DisabledFolderName = "VOIDTUNE_Disabled";

    public ObservableCollection<StartupItem> Items { get; } = new();

    public void Refresh()
    {
        Items.Clear();

        // Enabled registry entries
        ReadRunKey(Registry.CurrentUser, "HKCU", true);
        ReadRunKey(Registry.LocalMachine, "HKLM", true);
        // Disabled registry entries (from our backup hive)
        ReadBackup("HKCU");
        ReadBackup("HKLM");

        // Startup folders
        ReadFolder(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Folder (user)");
        ReadFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), "Folder (all users)");
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
                });
            }
        }
        catch { /* ignore */ }
    }

    private void ReadFolder(string folder, string label)
    {
        try
        {
            if (Directory.Exists(folder))
            {
                foreach (var f in Directory.GetFiles(folder))
                {
                    Items.Add(new StartupItem
                    {
                        Name = Path.GetFileNameWithoutExtension(f),
                        Command = f,
                        Location = label,
                        Scope = "Folder:" + folder,
                        Enabled = true,
                    });
                }
            }
            string disabled = Path.Combine(folder, DisabledFolderName);
            if (Directory.Exists(disabled))
            {
                foreach (var f in Directory.GetFiles(disabled))
                {
                    Items.Add(new StartupItem
                    {
                        Name = Path.GetFileNameWithoutExtension(f),
                        Command = f,
                        Location = label + " (disabled)",
                        Scope = "Folder:" + folder,
                        Enabled = false,
                    });
                }
            }
        }
        catch { /* ignore */ }
    }

    public void SetEnabled(StartupItem item, bool enable)
    {
        if (item.Scope.StartsWith("Folder:", StringComparison.Ordinal))
            ToggleFolder(item, enable);
        else
            ToggleRegistry(item, enable);
        Refresh();
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
            string disabledDir = Path.Combine(folder, DisabledFolderName);
            Directory.CreateDirectory(disabledDir);
            string fileName = Path.GetFileName(item.Command);
            if (enable)
            {
                string dest = Path.Combine(folder, fileName);
                if (File.Exists(item.Command)) File.Move(item.Command, dest, true);
            }
            else
            {
                string dest = Path.Combine(disabledDir, fileName);
                if (File.Exists(item.Command)) File.Move(item.Command, dest, true);
            }
        }
        catch { /* ignore */ }
    }
}
