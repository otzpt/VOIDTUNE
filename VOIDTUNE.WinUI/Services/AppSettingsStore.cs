using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace VOIDTUNE.WinUI.Services;

/// <summary>
/// Single JSON-backed settings store: %LOCALAPPDATA%\VOIDTUNE\settings.json.
/// Owns applied-tweak persistence and app flags (dev mode). Auto-migrates the old applied.txt.
/// </summary>
public static class AppSettingsStore
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VOIDTUNE");
    private static readonly string SettingsPath = Path.Combine(Dir, "settings.json");
    private static readonly string LegacyApplied = Path.Combine(Dir, "applied.txt");
    private static readonly string BackupSettingsPath = SettingsPath + ".bak";
    private static readonly object SaveLock = new();

    private sealed class Model
    {
        public List<string> AppliedTweaks { get; set; } = new();
        public bool DevMode { get; set; }
        public bool AutoGameBoost { get; set; }
        public bool GameTimePowerPlan { get; set; }
        public List<CustomTweakModel> CustomTweaks { get; set; } = new();
    }

    /// <summary>A Tweak Lab creation, persisted so it survives restarts.</summary>
    public sealed class CustomTweakModel
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "Custom";
        public int Tier { get; set; }
        public string Description { get; set; } = "";
        public string ApplyCmd { get; set; } = "";
        public string RevertCmd { get; set; } = "";
    }

    private static readonly Model _m = Load();

    /// <summary>Raised when dev mode is toggled, so the shell can show/hide the DevTools category.</summary>
    public static event Action? DevModeChanged;

    public static bool DevMode
    {
        get => _m.DevMode;
        set { if (_m.DevMode == value) return; _m.DevMode = value; Save(); DevModeChanged?.Invoke(); }
    }

    /// <summary>Whether the automatic game-boost watcher is enabled (the Auto Game Boost tweak).</summary>
    public static bool AutoGameBoost
    {
        get => _m.AutoGameBoost;
        set { if (_m.AutoGameBoost == value) return; _m.AutoGameBoost = value; Save(); }
    }

    /// <summary>Whether the watcher switches to the VOIDTUNE Performance power plan while a
    /// game runs and back afterwards (the Game-Time Power Plan tweak).</summary>
    public static bool GameTimePowerPlan
    {
        get => _m.GameTimePowerPlan;
        set { if (_m.GameTimePowerPlan == value) return; _m.GameTimePowerPlan = value; Save(); }
    }

    public static IReadOnlyList<string> AppliedTweaks => _m.AppliedTweaks;

    public static void SetAppliedTweaks(IEnumerable<string> ids)
    {
        _m.AppliedTweaks = new List<string>(ids);
        Save();
    }

    public static IReadOnlyList<CustomTweakModel> CustomTweaks => _m.CustomTweaks;

    public static void AddCustomTweak(CustomTweakModel t)
    {
        _m.CustomTweaks.Add(t);
        Save();
    }

    public static void RemoveCustomTweak(string id)
    {
        _m.CustomTweaks.RemoveAll(t => t.Id == id);
        Save();
    }

    private static Model Load()
    {
        foreach (string path in new[] { SettingsPath, BackupSettingsPath })
        {
            try
            {
                if (!File.Exists(path)) continue;
                var loaded = JsonSerializer.Deserialize<Model>(File.ReadAllText(path));
                if (loaded is not null) return loaded;
            }
            catch { /* corrupt: try the previous good copy */ }
        }
        try
        {
            if (File.Exists(LegacyApplied))
                return new Model { AppliedTweaks = new List<string>(File.ReadAllLines(LegacyApplied)) };
        }
        catch { /* fall through to defaults */ }
        return new Model();
    }

    // Written to a temp file and swapped in, so a crash or power cut mid-write can't leave
    // settings.json truncated. The replaced version stays as settings.json.bak.
    public static void Save()
    {
        lock (SaveLock)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                string tmp = SettingsPath + ".tmp";
                File.WriteAllText(tmp, JsonSerializer.Serialize(_m, new JsonSerializerOptions { WriteIndented = true }));
                if (File.Exists(SettingsPath)) File.Replace(tmp, SettingsPath, BackupSettingsPath, ignoreMetadataErrors: true);
                else File.Move(tmp, SettingsPath);
            }
            catch { /* settings stay in memory for this session */ }
        }
    }
}
