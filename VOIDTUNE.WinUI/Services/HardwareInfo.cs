using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace VOIDTUNE.WinUI.Services;

/// <summary>Lightweight hardware detection for the dashboard banner and architecture-gated tweaks.</summary>
public static class HardwareInfo
{
    public static string CpuName { get; } = ReadCpuName();
    public static string CpuVendor { get; } = DetectCpuVendor(ReadCpuName(), ReadCpuVendorId());
    public static int WinBuild { get; } = Environment.OSVersion.Version.Build;
    public static bool IsLaptop { get; } = DetectLaptop();

    private static readonly IReadOnlyList<GpuEntry> _gpus = ReadGpus();
    public static IReadOnlyList<string> AllGpus { get; } = _gpus.Select(g => g.Name).ToList();
    /// <summary>The primary GPU — the discrete adapter is preferred over an integrated one.</summary>
    public static string GpuName { get; } = _gpus.Count > 0 ? _gpus[0].Name : "Unknown GPU";
    public static string GpuVendor { get; } = DetectGpuVendor(_gpus.Count > 0 ? _gpus[0].Name : "");
    /// <summary>VRAM of the primary GPU in bytes (from the registry, which avoids WMI's 4 GB cap).</summary>
    public static long GpuVramBytes { get; } = _gpus.Count > 0 ? _gpus[0].VramBytes : 0;

    public static double TotalRamGb
    {
        get
        {
            var (_, total, _) = new SystemMonitor().GetMemory();
            return total;
        }
    }

    private static string ReadCpuName()
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            return (k?.GetValue("ProcessorNameString") as string)?.Trim() ?? "Unknown CPU";
        }
        catch { return "Unknown CPU"; }
    }

    private static string ReadCpuVendorId()
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            return (k?.GetValue("VendorIdentifier") as string)?.Trim() ?? "";
        }
        catch { return ""; }
    }

    private static string DetectCpuVendor(string name, string vendorId)
    {
        string s = (name + " " + vendorId).ToUpperInvariant();
        if (s.Contains("INTEL")) return "Intel";
        if (s.Contains("AMD") || s.Contains("AUTHENTICAMD")) return "AMD";
        return "Unknown";
    }

    private sealed record GpuEntry(string Name, long VramBytes, int Rank);

    // Enumerate display adapters; sort discrete-first, then by VRAM. The registry
    // qwMemorySize avoids WMI's 4 GB AdapterRAM cap on big cards.
    private static IReadOnlyList<GpuEntry> ReadGpus()
    {
        var gpus = new List<GpuEntry>();
        try
        {
            using var cls = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
            if (cls != null)
            {
                foreach (var sub in cls.GetSubKeyNames().Where(n => n.Length == 4 && int.TryParse(n, out _)))
                {
                    try
                    {
                        using var k = cls.OpenSubKey(sub);
                        if (k?.GetValue("DriverDesc") is not string descRaw) continue;
                        string desc = descRaw.Trim();
                        if (desc.Length == 0 || gpus.Any(g => g.Name == desc)) continue;
                        gpus.Add(new GpuEntry(desc, ReadVram(k), Rank(desc)));
                    }
                    catch { /* ignore */ }
                }
            }
        }
        catch { /* ignore */ }
        // Highest rank (discrete) first, then most VRAM.
        return gpus.OrderByDescending(g => g.Rank).ThenByDescending(g => g.VramBytes).ToList();
    }

    private static long ReadVram(RegistryKey k)
    {
        try
        {
            object? v = k.GetValue("HardwareInformation.qwMemorySize");
            return v switch
            {
                long l => l,
                int i => i,
                byte[] b when b.Length >= 8 => BitConverter.ToInt64(b, 0),
                _ => 0,
            };
        }
        catch { return 0; }
    }

    // 2 = discrete, 1 = unknown, 0 = integrated, -1 = software/basic adapter.
    private static int Rank(string name)
    {
        string s = name.ToUpperInvariant();
        if (s.Contains("BASIC DISPLAY") || s.Contains("REMOTE DISPLAY") || s.Contains("MIRACAST")) return -1;
        if (s.Contains("NVIDIA") || s.Contains("GEFORCE") || s.Contains("RTX") || s.Contains("GTX") ||
            s.Contains("QUADRO") || s.Contains("TITAN") || s.Contains("RADEON RX") || s.Contains("RADEON PRO") ||
            s.Contains("ARC ")) return 2;
        if (s.Contains("RADEON(TM) GRAPHICS") || s.Contains("VEGA") || s.Contains("UHD") || s.Contains("IRIS") ||
            s.Contains("HD GRAPHICS") || s.Contains("INTEGRATED")) return 0;
        return 1;
    }

    private static string DetectGpuVendor(string name)
    {
        string s = name.ToUpperInvariant();
        if (s.Contains("NVIDIA") || s.Contains("GEFORCE") || s.Contains("RTX") || s.Contains("GTX")) return "NVIDIA";
        if (s.Contains("AMD") || s.Contains("RADEON")) return "AMD";
        if (s.Contains("INTEL")) return "Intel";
        return "Unknown";
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);

    // A real battery (not just the CmBatt driver) means this is a laptop.
    // BatteryFlag 128 = "no system battery", 255 = "unknown".
    private static bool DetectLaptop()
    {
        try
        {
            if (GetSystemPowerStatus(out var s))
                return s.BatteryFlag != 128 && s.BatteryFlag != 255;
        }
        catch { /* ignore */ }
        return false;
    }
}
