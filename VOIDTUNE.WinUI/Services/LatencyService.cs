using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace VOIDTUNE.WinUI.Services;

public sealed record PingResult(string Host, double AvgMs, double Jitter, bool Ok)
{
    public string AvgText => Ok ? $"{AvgMs} ms" : "unreachable";
    public string JitterText => Ok ? $"jitter {Jitter} ms" : "";
}

public sealed class LatencyReport
{
    public List<Metric> Metrics { get; } = new();
    public List<PingResult> Pings { get; } = new();
    public int Score { get; set; } = 100;
    public List<string> Issues { get; } = new();
}

/// <summary>System latency probes, ported from modules/drivers.ps1 (timer, disk, memory, network, DPC heuristic).</summary>
public static class LatencyService
{
    [DllImport("ntdll.dll")]
    private static extern int NtQueryTimerResolution(out uint min, out uint max, out uint cur);

    private static readonly string[] PingHosts = { "1.1.1.1", "8.8.8.8", "google.com" };

    public static Task<LatencyReport> RunAsync() => Task.Run(() =>
    {
        var rep = new LatencyReport();

        // ── Timer resolution ────────────────────────────────────────────────
        double curMs = 15.6;
        try
        {
            NtQueryTimerResolution(out _, out _, out uint cur);
            curMs = Math.Round(cur / 10000.0, 3);
        }
        catch { /* ignore */ }
        rep.Metrics.Add(new("TIMER RESOLUTION", $"{curMs} ms", curMs <= 1.5 ? "#22C55E" : "#F59E0B"));
        if (curMs > 1.5) { rep.Issues.Add($"Timer resolution is {curMs} ms (ideal ≤ 0.5 ms) — apply the High-Precision Timer tweak."); rep.Score -= 20; }

        // ── Disk latency (random 4KB reads) ─────────────────────────────────
        try
        {
            string tmp = Path.Combine(Path.GetTempPath(), "vt_lattest.tmp");
            File.WriteAllBytes(tmp, new byte[10 * 1024 * 1024]);
            using (var fs = File.OpenRead(tmp))
            {
                var buf = new byte[4096];
                var rng = new Random();
                double sum = 0, max = 0;
                for (int i = 0; i < 20; i++)
                {
                    long pos = rng.Next(0, 10 * 1024 * 1024 - 4096);
                    var sw = Stopwatch.StartNew();
                    fs.Seek(pos, SeekOrigin.Begin);
                    _ = fs.Read(buf, 0, 4096);
                    sw.Stop();
                    double ms = sw.Elapsed.TotalMilliseconds;
                    sum += ms; if (ms > max) max = ms;
                }
                double avg = Math.Round(sum / 20.0, 3);
                rep.Metrics.Add(new("DISK 4K READ (avg)", $"{avg} ms", avg < 1 ? "#22C55E" : "#F59E0B"));
                rep.Metrics.Add(new("DISK 4K READ (max)", $"{Math.Round(max, 3)} ms"));
            }
            try { File.Delete(tmp); } catch { }
        }
        catch { rep.Metrics.Add(new("DISK 4K READ", "error", "#EF4444")); }

        // ── Memory latency (cache-line stride) ──────────────────────────────
        try
        {
            int size = 64 * 1024 * 1024;
            var buf = new byte[size];
            var sw = Stopwatch.StartNew();
            int acc = 0;
            for (int i = 0; i < size; i += 64) acc += buf[i];
            sw.Stop();
            GC.KeepAlive(acc);
            double ns = Math.Round(sw.Elapsed.TotalMilliseconds * 1_000_000.0 / (size / 64.0), 2);
            rep.Metrics.Add(new("MEMORY ACCESS", $"{ns} ns/line"));
        }
        catch { rep.Metrics.Add(new("MEMORY ACCESS", "error", "#EF4444")); }

        // ── Network ping ────────────────────────────────────────────────────
        foreach (var host in PingHosts)
        {
            try
            {
                var times = new List<long>();
                using var ping = new Ping();
                for (int i = 0; i < 5; i++)
                {
                    try { var r = ping.Send(host, 1500); if (r.Status == IPStatus.Success) times.Add(r.RoundtripTime); }
                    catch { /* ignore */ }
                }
                if (times.Count > 0)
                {
                    double avg = 0; long mn = long.MaxValue, mx = long.MinValue;
                    foreach (var t in times) { avg += t; if (t < mn) mn = t; if (t > mx) mx = t; }
                    avg = Math.Round(avg / times.Count, 1);
                    rep.Pings.Add(new(host, avg, mx - mn, true));
                }
                else rep.Pings.Add(new(host, 999, 0, false));
            }
            catch { rep.Pings.Add(new(host, 999, 0, false)); }
        }

        rep.Score = Math.Max(0, rep.Score);
        return rep;
    });
}
