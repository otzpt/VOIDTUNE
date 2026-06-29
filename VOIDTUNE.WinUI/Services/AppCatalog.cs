using System.Collections.Generic;
using VOIDTUNE.WinUI.Models;

namespace VOIDTUNE.WinUI.Services;

/// <summary>Curated winget app catalog, ported from $APPS_DATA in modules/data.ps1.</summary>
public static class AppCatalog
{
    public static IReadOnlyList<AppItem> All { get; } = new List<AppItem>
    {
        // Browsers
        new() { Id="Google.Chrome",                   Name="Chrome",              Category="Browser" },
        new() { Id="Mozilla.Firefox",                 Name="Firefox",             Category="Browser" },
        new() { Id="Brave.Brave",                     Name="Brave",               Category="Browser" },
        new() { Id="Opera.OperaGX",                   Name="Opera GX",            Category="Browser" },
        // Communication
        new() { Id="Discord.Discord",                 Name="Discord",             Category="Comm" },
        new() { Id="Telegram.TelegramDesktop",        Name="Telegram",            Category="Comm" },
        new() { Id="WhatsApp.WhatsApp",               Name="WhatsApp",            Category="Comm" },
        // Gaming
        new() { Id="Valve.Steam",                     Name="Steam",               Category="Gaming" },
        new() { Id="EpicGames.EpicGamesLauncher",     Name="Epic Games",          Category="Gaming" },
        new() { Id="Ubisoft.Connect",                 Name="Ubisoft Connect",     Category="Gaming" },
        new() { Id="ElectronicArts.EADesktop",        Name="EA Desktop",          Category="Gaming" },
        new() { Id="Parsec.Parsec",                   Name="Parsec",              Category="Gaming" },
        // HW Monitor
        new() { Id="TechPowerUp.GPU-Z",               Name="GPU-Z",               Category="HW Monitor" },
        new() { Id="CPUID.CPU-Z",                     Name="CPU-Z",               Category="HW Monitor" },
        new() { Id="REALiX.HWiNFO",                   Name="HWiNFO64",            Category="HW Monitor" },
        new() { Id="MSI.MSIAfterburner",              Name="MSI Afterburner",     Category="HW Monitor" },
        new() { Id="CrystalDewWorld.CrystalDiskInfo", Name="CrystalDiskInfo",     Category="HW Monitor" },
        // Tools
        new() { Id="7zip.7zip",                       Name="7-Zip",               Category="Tools" },
        new() { Id="RARLab.WinRAR",                   Name="WinRAR",              Category="Tools" },
        new() { Id="VideoLAN.VLC",                    Name="VLC",                 Category="Tools" },
        new() { Id="OBSProject.OBSStudio",            Name="OBS Studio",          Category="Tools" },
        new() { Id="Microsoft.PowerToys",             Name="PowerToys",           Category="Tools" },
        new() { Id="voidtools.Everything",            Name="Everything",          Category="Tools" },
        new() { Id="HandBrake.HandBrake",             Name="HandBrake",           Category="Tools" },
        new() { Id="Rufus.Rufus",                     Name="Rufus",               Category="Tools" },
        new() { Id="ShareX.ShareX",                   Name="ShareX",              Category="Tools" },
        new() { Id="AutoHotkey.AutoHotkey",           Name="AutoHotkey",          Category="Tools" },
        new() { Id="JAMSoftware.TreeSize.Free",       Name="TreeSize Free",       Category="Tools" },
        new() { Id="WinSCP.WinSCP",                   Name="WinSCP",              Category="Tools" },
        // Media / Creative
        new() { Id="GIMP.GIMP",                       Name="GIMP",                Category="Media" },
        new() { Id="Audacity.Audacity",               Name="Audacity",            Category="Media" },
        new() { Id="Spotify.Spotify",                 Name="Spotify",             Category="Media" },
        new() { Id="Inkscape.Inkscape",               Name="Inkscape",            Category="Media" },
        new() { Id="BlenderFoundation.Blender",       Name="Blender",             Category="Media" },
        new() { Id="DaVinciResolve.DaVinciResolve",   Name="DaVinci Resolve",     Category="Media" },
        new() { Id="KDE.Kdenlive",                    Name="Kdenlive",            Category="Media" },
        // Dev
        new() { Id="SublimeHQ.SublimeText.4",         Name="Sublime Text",        Category="Dev" },
        new() { Id="Microsoft.VisualStudioCode",      Name="VS Code",             Category="Dev" },
        new() { Id="Notepad++.Notepad++",             Name="Notepad++",           Category="Dev" },
        new() { Id="Git.Git",                         Name="Git",                 Category="Dev" },
        new() { Id="GitHub.GitHubDesktop",            Name="GitHub Desktop",      Category="Dev" },
        new() { Id="OpenJS.NodeJS",                   Name="Node.js",             Category="Dev" },
        new() { Id="Python.Python.3",                 Name="Python 3",            Category="Dev" },
        new() { Id="Postman.Postman",                 Name="Postman",             Category="Dev" },
        new() { Id="Neovim.Neovim",                   Name="Neovim",              Category="Dev" },
        new() { Id="Microsoft.WindowsTerminal",       Name="Windows Terminal",    Category="Dev" },
        new() { Id="TimKosse.FileZilla.Client",       Name="FileZilla",           Category="Dev" },
        // Security
        new() { Id="Malwarebytes.Malwarebytes",       Name="Malwarebytes",        Category="Security" },
        new() { Id="Bitwarden.Bitwarden",             Name="Bitwarden",           Category="Security" },
        new() { Id="KeePass.KeePass",                 Name="KeePass",             Category="Security" },
        new() { Id="Wireshark.Wireshark",             Name="Wireshark",           Category="Security" },
        // System / Utils
        new() { Id="Microsoft.PowerShell",            Name="PowerShell 7",        Category="System" },
        new() { Id="Chocolatey.Chocolatey",           Name="Chocolatey",          Category="System" },
        new() { Id="CrystalDewWorld.CrystalDiskMark", Name="CrystalDiskMark",     Category="System" },
        new() { Id="Ventoy.Ventoy",                   Name="Ventoy",              Category="System" },
        new() { Id="WizTree.WizTree",                 Name="WizTree",             Category="System" },
        new() { Id="Microsoft.Sysinternals.Suite",    Name="Sysinternals Suite",  Category="System" },
        new() { Id="ProcessHacker.ProcessHacker",     Name="Process Hacker",      Category="System" },
        new() { Id="Wagnardsoft.DDU",                 Name="Display Driver Uninstaller", Category="System" },
        new() { Id="lostindark.DriverStoreExplorer",  Name="DriverStoreExplorer", Category="System" },
        new() { Id="Geeks3D.FurMark",                 Name="FurMark",             Category="System" },
        new() { Id="CPUID.HWMonitor",                 Name="HWMonitor",           Category="System" },
        new() { Id="Maxon.CinebenchR23",              Name="Cinebench R23",       Category="System" },
        new() { Id="PuTTY.PuTTY",                     Name="PuTTY",               Category="System" },
    };
}
