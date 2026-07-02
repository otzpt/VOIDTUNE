using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.Win32;
using VOIDTUNE.WinUI.Models;

namespace VOIDTUNE.WinUI.Services;

/// <summary>
/// Backend for the DevTools hub. Read-only system diagnostics (System Probe) plus helpers the
/// other DevTools tabs build on. Everything here only *reads* state — nothing is mutated.
/// </summary>
public static class DevToolsService
{
    /// <summary>Gathers a read-only snapshot of the settings VOIDTUNE cares about.</summary>
    public static async Task<List<ProbeItem>> ProbeAsync()
    {
        var items = new List<ProbeItem>();

        // ── Registry-backed checks (current value vs the optimized target) ──────────
        RegDword(items, "CPU", "Win32 Priority Separation",
            RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\PriorityControl",
            "Win32PrioritySeparation", 38, "foreground boost");
        RegDword(items, "CPU", "Global Timer Resolution",
            RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            "GlobalTimerResolutionRequests", 1, "high-resolution");
        RegDword(items, "GPU", "Hardware GPU Scheduling",
            RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
            "HwSchMode", 2, "enabled");
        RegDword(items, "RAM", "Disable Paging Executive",
            RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            "DisablePagingExecutive", 1, "kernel kept in RAM");
        RegDword(items, "Network", "Network Throttling",
            RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
            "NetworkThrottlingIndex", -1, "disabled (0xFFFFFFFF)");
        RegDword(items, "Game", "Game DVR",
            RegistryHive.CurrentUser, @"System\GameConfigStore",
            "GameDVR_Enabled", 0, "off");
        RegDword(items, "System", "MMCSS System Responsiveness",
            RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
            "SystemResponsiveness", 0, "0 = max foreground");

        // ── Active power scheme ─────────────────────────────────────────────────────
        var scheme = await CommandRunner.ExecAsync("powercfg /getactivescheme");
        string schemeName = ParsePowerScheme(scheme.Output);
        bool ultimate = schemeName.Contains("Ultimate", StringComparison.OrdinalIgnoreCase);
        bool high = schemeName.Contains("High performance", StringComparison.OrdinalIgnoreCase);
        items.Add(new ProbeItem
        {
            Category = "Power",
            Name = "Active power plan",
            Value = string.IsNullOrWhiteSpace(schemeName) ? "unknown" : schemeName,
            State = ultimate || high ? "OPTIMIZED" : "DEFAULT",
            StateHex = ultimate || high ? "#22C55E" : "#9a93a8",
        });

        // ── Curated service states ──────────────────────────────────────────────────
        ServiceState(items, "DiagTrack", "Telemetry (DiagTrack)", stoppedIsGood: true);
        ServiceState(items, "SysMain", "Superfetch (SysMain)", stoppedIsGood: true);
        ServiceState(items, "WSearch", "Windows Search", stoppedIsGood: false);

        return items;
    }

    // ── helpers ─────────────────────────────────────────────────────────────────────
    private static void RegDword(List<ProbeItem> items, string cat, string name,
        RegistryHive hive, string path, string valueName, int optimized, string optLabel)
    {
        object? raw = ReadReg(hive, path, valueName);
        if (raw is null)
        {
            items.Add(new ProbeItem { Category = cat, Name = name, Value = "not set", State = "DEFAULT", StateHex = "#9a93a8" });
            return;
        }
        int cur;
        try { cur = Convert.ToInt32(raw); }
        catch { items.Add(new ProbeItem { Category = cat, Name = name, Value = raw.ToString() ?? "?", State = "INFO", StateHex = "#a78bfa" }); return; }

        bool ok = cur == optimized;
        items.Add(new ProbeItem
        {
            Category = cat,
            Name = name,
            Value = ok ? $"{optLabel}" : $"{cur}",
            State = ok ? "OPTIMIZED" : "DEFAULT",
            StateHex = ok ? "#22C55E" : "#9a93a8",
        });
    }

    private static void ServiceState(List<ProbeItem> items, string svc, string name, bool stoppedIsGood)
    {
        string status;
        bool stopped;
        try
        {
            using var sc = new ServiceController(svc);
            var s = sc.Status;
            stopped = s != ServiceControllerStatus.Running && s != ServiceControllerStatus.StartPending;
            status = s.ToString();
        }
        catch { status = "not present"; stopped = true; }

        bool good = stoppedIsGood ? stopped : !stopped;
        items.Add(new ProbeItem
        {
            Category = "Services",
            Name = name,
            Value = status,
            State = good ? "OPTIMIZED" : "INFO",
            StateHex = good ? "#22C55E" : "#a78bfa",
        });
    }

    private static object? ReadReg(RegistryHive hive, string path, string name)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
            using var k = baseKey.OpenSubKey(path);
            return k?.GetValue(name);
        }
        catch { return null; }
    }

    // powercfg output: "Power Scheme GUID: <guid>  (Balanced)"
    private static string ParsePowerScheme(string output)
    {
        if (string.IsNullOrEmpty(output)) return "";
        int open = output.LastIndexOf('(');
        int close = output.LastIndexOf(')');
        return open >= 0 && close > open ? output[(open + 1)..close].Trim() : "";
    }
}
