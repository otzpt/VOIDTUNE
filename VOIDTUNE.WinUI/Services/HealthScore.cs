using System;
using Microsoft.Win32;

namespace VOIDTUNE.WinUI.Services;

/// <summary>Dashboard score: current load plus the conditions that actually degrade a PC.</summary>
public static class HealthScore
{
    /// <summary>RAM below 60% and disk below 85% are normal use and cost nothing.</summary>
    public static double Compute(double cpuPercent, double ramPercent, double systemDiskUsedPercent, int processes, bool rebootPending)
    {
        double score = 100
            - cpuPercent * 0.30
            - Math.Max(0, ramPercent - 60) * 0.8
            - Math.Max(0, systemDiskUsedPercent - 85) * 2
            - Math.Max(0, processes - 230) * 0.10
            - (rebootPending ? 5 : 0);
        return Math.Clamp(score, 0, 100);
    }

    public static bool IsRebootPending()
    {
        try
        {
            using var cbs = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending");
            using var wu = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");
            return cbs is not null || wu is not null;
        }
        catch { return false; }
    }
}
