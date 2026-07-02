using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VOIDTUNE.WinUI.Models;

namespace VOIDTUNE.WinUI.Services;

/// <summary>An accent colour preset for the personalize palette.</summary>
public sealed record AccentColor(string Hex, string Name);

/// <summary>Personalization toggles + accent color, ported from modules/personalize.ps1.</summary>
public static class PersonalizeService
{
    public static IReadOnlyList<AccentColor> AccentColors { get; } = new List<AccentColor>
    {
        new("#0078D4", "Windows Blue"), new("#7C3AED", "VOIDTUNE Purple"), new("#06B6D4", "Cyan"),
        new("#16A34A", "Green"),        new("#E01818", "Red"),            new("#F97316", "Orange"),
        new("#EC4899", "Pink"),         new("#EAB308", "Yellow"),         new("#64748B", "Slate"),
        new("#0EA5E9", "Sky"),          new("#8B5CF6", "Violet"),         new("#10B981", "Emerald"),
    };

    public static IReadOnlyList<PersonalizeToggle> Toggles { get; } = new List<PersonalizeToggle>
    {
        new() { Id="DarkMode",       Group="Theme",   Name="Dark Mode",            Description="Use the dark theme for apps and system." },
        new() { Id="Transparency",   Group="Theme",   Name="Transparency Effects", Description="Acrylic / Mica transparency in the shell." },
        new() { Id="NoRoundCorners", Group="Theme",   Name="Dark Window Borders",  Description="Force dark window border mode (DWM)." },
        new() { Id="ClearType",      Group="Theme",   Name="ClearType Smoothing",  Description="Sub-pixel font anti-aliasing." },
        new() { Id="AeroPeek",       Group="Aero",    Name="Aero Peek",            Description="Hover the taskbar end to preview the desktop." },
        new() { Id="AeroTitlebar",   Group="Aero",    Name="Colored Title Bars",   Description="Apply the accent color to title bars." },
        new() { Id="AeroGlass",      Group="Aero",    Name="Glass Blend",          Description="Transparent glass colorization blend." },
        new() { Id="AeroAnimations", Group="Aero",    Name="Window Animations",    Description="Taskbar and window open/close animations." },
        new() { Id="AeroSnapAssist", Group="Aero",    Name="Snap Assist",          Description="Suggest windows when snapping." },
        new() { Id="AeroDragFull",   Group="Aero",    Name="Show Window While Dragging", Description="Render full window contents while moving." },
        new() { Id="ClassicAltTab",  Group="Aero",    Name="Classic Alt+Tab",      Description="Use the legacy Alt+Tab switcher." },
        new() { Id="OldContextMenu", Group="Taskbar", Name="Classic Context Menu", Description="Restore the full right-click menu (Win11)." },
        new() { Id="SmallIcons",     Group="Taskbar", Name="Small Taskbar Icons",  Description="Use compact taskbar buttons." },
        new() { Id="HideSearch",     Group="Taskbar", Name="Hide Search Box",      Description="Remove the taskbar search box." },
        new() { Id="HideWidgets",    Group="Taskbar", Name="Hide Widgets",         Description="Remove the News & Interests / Widgets button." },
        new() { Id="HideTaskView",   Group="Taskbar", Name="Hide Task View",       Description="Remove the Task View button." },
        new() { Id="CompactMode",    Group="Display", Name="Explorer Compact Mode",Description="Tighter spacing in File Explorer." },
        new() { Id="NoLoginBlur",    Group="Display", Name="No Login Blur",        Description="Disable the acrylic blur on the logon screen." },
        // ── Windhawk-style mods ──
        new() { Id="LeftTaskbar",    Group="Taskbar", Name="Left-align Taskbar",   Description="Move taskbar buttons to the left (Windows 11)." },
        new() { Id="ShowSeconds",    Group="Taskbar", Name="Seconds in Clock",     Description="Show seconds on the taskbar clock." },
        new() { Id="HideCopilot",    Group="Taskbar", Name="Hide Copilot",         Description="Remove the Copilot button from the taskbar." },
        new() { Id="HideChat",       Group="Taskbar", Name="Hide Chat",            Description="Remove the Teams Chat button from the taskbar." },
        new() { Id="EndTask",        Group="Taskbar", Name="End Task on Right-click", Description="Add 'End task' to the taskbar right-click menu." },
        new() { Id="ShowExtensions", Group="Explorer",Name="Show File Extensions", Description="Always show known file extensions." },
        new() { Id="ShowHidden",     Group="Explorer",Name="Show Hidden Files",    Description="Reveal hidden files and folders." },
        new() { Id="ThisPC",         Group="Explorer",Name="Open to This PC",      Description="Open File Explorer to This PC instead of Home." },
        new() { Id="VerboseLogon",   Group="Display", Name="Verbose Logon",        Description="Show detailed status messages at sign-in / shutdown." },
    };

    // ── State reads ──────────────────────────────────────────────────────────
    public static bool GetState(string id) => id switch
    {
        "DarkMode"       => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1) == 0,
        "Transparency"   => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 1) == 1,
        "ClassicAltTab"  => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer", "AltTabSettings", 0) == 1,
        "NoLoginBlur"    => Dword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\System", "DisableAcrylicBackgroundOnLogon", 0) == 1,
        "OldContextMenu" => KeyExists(Registry.CurrentUser, @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"),
        "NoRoundCorners" => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\DWM", "UseWindowDarkMode", 0) == 1,
        "AeroPeek"       => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\DWM", "EnableAeroPeek", 1) == 1,
        "AeroTitlebar"   => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\DWM", "ColorPrevalence", 0) == 1,
        "AeroGlass"      => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\DWM", "ColorizationOpaqueBlend", 1) == 0,
        "AeroAnimations" => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAnimations", 1) == 1,
        "AeroSnapAssist" => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "SnapAssist", 1) != 0,
        "AeroDragFull"   => Str(Registry.CurrentUser, @"Control Panel\Desktop", "DragFullWindows", "1") == "1",
        "SmallIcons"     => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarSmallIcons", 0) == 1,
        "HideSearch"     => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 1) == 0,
        "HideWidgets"    => Dword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 1) == 0,
        "HideTaskView"   => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton", 1) == 0,
        "CompactMode"    => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "UseCompactMode", 0) == 1,
        "ClearType"      => Dword(Registry.CurrentUser, @"Control Panel\Desktop", "FontSmoothingType", 0) == 2,
        "LeftTaskbar"    => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAl", 1) == 0,
        "ShowSeconds"    => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSecondsInSystemClock", 0) == 1,
        "HideCopilot"    => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCopilotButton", 1) == 0,
        "HideChat"       => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarMn", 1) == 0,
        "EndTask"        => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings", "TaskbarEndTask", 0) == 1,
        "ShowExtensions" => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", 1) == 0,
        "ShowHidden"     => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", 2) == 1,
        "ThisPC"         => Dword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", 2) == 1,
        "VerboseLogon"   => Dword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "VerboseStatus", 0) == 1,
        _ => false,
    };

    // ── Apply a toggle ───────────────────────────────────────────────────────
    public static async Task SetAsync(string id, bool enable)
    {
        int on = enable ? 1 : 0, off = enable ? 0 : 1;
        switch (id)
        {
            case "DarkMode":
                int dm = enable ? 0 : 1;
                await Reg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", "REG_DWORD", dm);
                await Reg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme", "REG_DWORD", dm);
                break;
            case "Transparency":
                await Reg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", "REG_DWORD", on); break;
            case "ClassicAltTab":
                await Reg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer", "AltTabSettings", "REG_DWORD", on); break;
            case "NoLoginBlur":
                if (enable) await Reg(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", "DisableAcrylicBackgroundOnLogon", "REG_DWORD", 1);
                else await RegDelete(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", "DisableAcrylicBackgroundOnLogon");
                break;
            case "OldContextMenu":
                if (enable) await CommandRunner.ExecAsync(@"reg add ""HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"" /f /ve");
                else await CommandRunner.ExecAsync(@"reg delete ""HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}"" /f");
                break;
            case "NoRoundCorners":
                await Reg(@"HKCU\Software\Microsoft\Windows\DWM", "UseWindowDarkMode", "REG_DWORD", on); break;
            case "AeroPeek":
                await Reg(@"HKCU\Software\Microsoft\Windows\DWM", "EnableAeroPeek", "REG_DWORD", on); break;
            case "AeroTitlebar":
                await Reg(@"HKCU\Software\Microsoft\Windows\DWM", "ColorPrevalence", "REG_DWORD", on);
                await Reg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "ColorPrevalence", "REG_DWORD", on);
                break;
            case "AeroGlass":
                await Reg(@"HKCU\Software\Microsoft\Windows\DWM", "ColorizationOpaqueBlend", "REG_DWORD", off);
                await Reg(@"HKCU\Software\Microsoft\Windows\DWM", "ColorizationGlassAttribute", "REG_DWORD", on);
                break;
            case "AeroAnimations":
                await Reg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAnimations", "REG_DWORD", on);
                await RegSz(@"HKCU\Control Panel\Desktop\WindowMetrics", "MinAnimate", on.ToString());
                await RegSz(@"HKCU\Control Panel\Desktop", "MenuShowDelay", enable ? "400" : "0");
                break;
            case "AeroSnapAssist":
                await Reg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "SnapAssist", "REG_DWORD", on); break;
            case "AeroDragFull":
                await RegSz(@"HKCU\Control Panel\Desktop", "DragFullWindows", enable ? "1" : "0"); break;
            case "SmallIcons":
                await Reg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarSmallIcons", "REG_DWORD", on); break;
            case "HideSearch":
                await Reg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", "REG_DWORD", enable ? 0 : 1); break;
            case "HideWidgets":
                if (enable) await Reg(@"HKLM\SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", "REG_DWORD", 0);
                else await RegDelete(@"HKLM\SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests");
                break;
            case "HideTaskView":
                await Reg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton", "REG_DWORD", enable ? 0 : 1); break;
            case "CompactMode":
                await Reg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "UseCompactMode", "REG_DWORD", on); break;
            case "ClearType":
                await Reg(@"HKCU\Control Panel\Desktop", "FontSmoothingType", "REG_DWORD", enable ? 2 : 1);
                await Reg(@"HKCU\Control Panel\Desktop", "FontSmoothing", "REG_DWORD", enable ? 2 : 0);
                break;
            case "LeftTaskbar":
                await Reg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAl", "REG_DWORD", enable ? 0 : 1); break;
            case "ShowSeconds":
                await Reg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSecondsInSystemClock", "REG_DWORD", on); break;
            case "HideCopilot":
                await Reg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCopilotButton", "REG_DWORD", enable ? 0 : 1); break;
            case "HideChat":
                await Reg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarMn", "REG_DWORD", enable ? 0 : 1); break;
            case "EndTask":
                await Reg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings", "TaskbarEndTask", "REG_DWORD", on); break;
            case "ShowExtensions":
                await Reg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", "REG_DWORD", enable ? 0 : 1); break;
            case "ShowHidden":
                await Reg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", "REG_DWORD", enable ? 1 : 2); break;
            case "ThisPC":
                await Reg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", "REG_DWORD", enable ? 1 : 2); break;
            case "VerboseLogon":
                if (enable) await Reg(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "VerboseStatus", "REG_DWORD", 1);
                else await RegDelete(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "VerboseStatus");
                break;
        }
    }

    // ── Accent color ─────────────────────────────────────────────────────────
    public static string GetCurrentAccentHex()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
            if (k?.GetValue("ColorizationColor") is int raw)
            {
                int r = (raw >> 16) & 0xFF, g = (raw >> 8) & 0xFF, b = raw & 0xFF;
                return $"#{r:X2}{g:X2}{b:X2}";
            }
        }
        catch { /* ignore */ }
        return "#0078D4";
    }

    public static async Task SetAccentColorAsync(string hex)
    {
        string h = hex.TrimStart('#');
        int r = Convert.ToInt32(h.Substring(0, 2), 16);
        int g = Convert.ToInt32(h.Substring(2, 2), 16);
        int b = Convert.ToInt32(h.Substring(4, 2), 16);
        long argb = 0xC4000000L + ((long)r << 16) + ((long)g << 8) + b;   // DWM ColorizationColor (AARRGGBB)
        long abgr = 0xFF000000L + ((long)b << 16) + ((long)g << 8) + r;   // Explorer AccentColorMenu (AABBGGRR)
        await Reg(@"HKCU\Software\Microsoft\Windows\DWM", "ColorizationColor", "REG_DWORD", argb);
        await Reg(@"HKCU\Software\Microsoft\Windows\DWM", "ColorizationAfterglow", "REG_DWORD", argb);
        await Reg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Accent", "AccentColorMenu", "REG_DWORD", abgr);
    }

    // ── Registry helpers ─────────────────────────────────────────────────────
    private static Task Reg(string key, string name, string type, long data)
        => CommandRunner.ExecAsync($"reg add \"{key}\" /v {name} /t {type} /d {data} /f");

    private static Task RegSz(string key, string name, string data)
        => CommandRunner.ExecAsync($"reg add \"{key}\" /v {name} /t REG_SZ /d {data} /f");

    private static Task RegDelete(string key, string name)
        => CommandRunner.ExecAsync($"reg delete \"{key}\" /v {name} /f");

    private static int Dword(RegistryKey root, string path, string name, int fallback)
    {
        try { using var k = root.OpenSubKey(path); return k?.GetValue(name) is int v ? v : fallback; }
        catch { return fallback; }
    }

    private static string Str(RegistryKey root, string path, string name, string fallback)
    {
        try { using var k = root.OpenSubKey(path); return k?.GetValue(name) as string ?? fallback; }
        catch { return fallback; }
    }

    private static bool KeyExists(RegistryKey root, string path)
    {
        try { using var k = root.OpenSubKey(path); return k != null; }
        catch { return false; }
    }
}
