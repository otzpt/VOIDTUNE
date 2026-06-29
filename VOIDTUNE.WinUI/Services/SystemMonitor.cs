using System;
using System.Runtime.InteropServices;

namespace VOIDTUNE.WinUI.Services;

/// <summary>Lightweight live CPU / RAM sampling via Win32 — no extra NuGet dependencies.</summary>
public sealed class SystemMonitor
{
    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FILETIME idle, out FILETIME kernel, out FILETIME user);

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME { public uint Low; public uint High; }

    private static ulong ToU64(FILETIME ft) => ((ulong)ft.High << 32) | ft.Low;

    private ulong _prevIdle, _prevKernel, _prevUser;

    /// <summary>Returns total CPU utilisation 0–100 since the previous call.</summary>
    public double GetCpuUsage()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user)) return 0;
        ulong i = ToU64(idle), k = ToU64(kernel), u = ToU64(user);

        ulong idleDelta = i - _prevIdle;
        ulong kernelDelta = k - _prevKernel;
        ulong userDelta = u - _prevUser;
        _prevIdle = i; _prevKernel = k; _prevUser = u;

        ulong total = kernelDelta + userDelta; // kernel already includes idle
        if (total == 0) return 0;
        double usage = (total - idleDelta) * 100.0 / total;
        return Math.Clamp(usage, 0, 100);
    }

    /// <summary>(usedGB, totalGB, percent)</summary>
    public (double usedGb, double totalGb, double percent) GetMemory()
    {
        var m = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!GlobalMemoryStatusEx(ref m)) return (0, 0, 0);
        double total = m.ullTotalPhys / 1024.0 / 1024 / 1024;
        double avail = m.ullAvailPhys / 1024.0 / 1024 / 1024;
        double used = total - avail;
        return (Math.Round(used, 1), Math.Round(total, 1), m.dwMemoryLoad);
    }
}
