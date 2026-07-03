using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VOIDTUNE.WinUI.Models;

namespace VOIDTUNE.WinUI.Services;

/// <summary>
/// Live "who's eating my machine" sampler for the DevTools Process Monitor. Takes two CPU-time
/// samples ~500 ms apart to compute real per-process CPU%, blends it with memory into an impact
/// score, and returns the heaviest processes. All reads; the only mutation is KillProcess.
/// </summary>
public static class ProcessMonitorService
{
    // Never offer to kill these — killing them logs you out or bugchecks.
    private static readonly HashSet<string> Critical = new(StringComparer.OrdinalIgnoreCase)
    {
        "system", "idle", "registry", "smss", "csrss", "wininit", "winlogon",
        "services", "lsass", "fontdrvhost", "dwm", "explorer", "audiodg",
        "voidtune", "memory compression", "secure system",
    };

    public static Task<List<ProcMonRow>> TopAsync(int count = 18) => Task.Run(() =>
    {
        int cores = Math.Max(1, Environment.ProcessorCount);
        int me = Environment.ProcessId;

        // sample 1
        var first = new Dictionary<int, TimeSpan>();
        foreach (var p in Process.GetProcesses())
        {
            try { first[p.Id] = p.TotalProcessorTime; } catch { /* access denied / exited */ }
            finally { p.Dispose(); }
        }

        var sw = Stopwatch.StartNew();
        Thread.Sleep(500);
        sw.Stop();
        double elapsedMs = Math.Max(1, sw.Elapsed.TotalMilliseconds);

        // sample 2 + build rows
        var raw = new List<(string Name, int Pid, double Cpu, double RamMb, double Score, bool Killable)>();
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                if (!first.TryGetValue(p.Id, out var t0)) continue;
                double cpu = (p.TotalProcessorTime - t0).TotalMilliseconds / (elapsedMs * cores) * 100.0;
                if (cpu < 0) cpu = 0;
                double ramMb = p.WorkingSet64 / 1024.0 / 1024.0;
                double score = cpu + ramMb / 40.0;   // 400 MB ≈ 10 pts, 50% CPU ≈ 50 pts
                bool killable = p.Id != me && !Critical.Contains(p.ProcessName);
                raw.Add((p.ProcessName, p.Id, cpu, ramMb, score, killable));
            }
            catch { /* exited mid-sample */ }
            finally { p.Dispose(); }
        }

        var top = raw.OrderByDescending(r => r.Score).Take(count).ToList();
        double max = top.Count > 0 ? Math.Max(1, top[0].Score) : 1;

        return top.Select(r => new ProcMonRow
        {
            Name = r.Name,
            Pid = r.Pid,
            Cpu = $"{r.Cpu:0}%",
            Ram = r.RamMb >= 1024 ? $"{r.RamMb / 1024:0.0} GB" : $"{r.RamMb:0} MB",
            Bar = Math.Round(r.Score / max * 100.0, 1),
            Hex = r.Cpu >= 25 ? "#F472B6" : r.Cpu >= 8 ? "#F59E0B" : "#A78BFA",
            Killable = r.Killable,
        }).ToList();
    });

    /// <summary>Force-terminates a process tree. Returns a human-readable result.</summary>
    public static (bool ok, string msg) KillProcess(int pid, string name)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            p.Kill(entireProcessTree: true);
            return (true, $"Ended {name} (pid {pid}).");
        }
        catch (Exception ex) { return (false, $"Couldn't end {name}: {ex.Message}"); }
    }
}
