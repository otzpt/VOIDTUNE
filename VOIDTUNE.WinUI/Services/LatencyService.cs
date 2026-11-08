using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        // Written with WriteThrough and read back through FILE_FLAG_NO_BUFFERING: a plain
        // read here would hit the Windows page cache for data this process just wrote,
        // reporting RAM-cache latency (single-digit microseconds) as disk latency. Unbuffered
        // I/O needs the seek offset 4096-aligned, hence rounding pos to a sector boundary.
        try
        {
            string tmp = Path.Combine(Path.GetTempPath(), "vt_lattest.tmp");
            const int fileSize = 10 * 1024 * 1024;
            using (var wfs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                wfs.Write(new byte[fileSize]);
                wfs.Flush(true);
            }
            using (var fs = UnbufferedIo.Open(tmp, sequential: false))
            {
                var (buf, offset) = UnbufferedIo.AllocateAligned(4096);
                var rng = new Random();
                int sectors = (fileSize - 4096) / 4096;
                double sum = 0, max = 0;
                for (int i = 0; i < 20; i++)
                {
                    long pos = rng.Next(0, sectors) * 4096L;
                    var sw = Stopwatch.StartNew();
                    fs.Seek(pos, SeekOrigin.Begin);
                    _ = fs.Read(buf, offset, 4096);
                    sw.Stop();
                    double ms = sw.Elapsed.TotalMilliseconds;
                    sum += ms; if (ms > max) max = ms;
                }
                double avg = Math.Round(sum / 20.0, 3);
                rep.Metrics.Add(new("DISK 4K READ (avg)", $"{avg} ms", avg < 1 ? "#22C55E" : "#F59E0B"));
                rep.Metrics.Add(new("DISK 4K READ (max)", $"{Math.Round(max, 3)} ms"));
                if (avg >= 1) { rep.Issues.Add($"Disk 4K reads average {avg} ms (an SSD should stay under 1 ms)."); rep.Score -= 15; }
            }
            try { File.Delete(tmp); } catch { }
        }
        catch { rep.Metrics.Add(new("DISK 4K READ", "error", "#EF4444")); }

        // ── Memory latency (randomized pointer chase) ───────────────────────
        // A straight sequential stride (the old version) is exactly the access pattern the
        // CPU's hardware prefetcher is built to detect and hide: it starts pulling future
        // lines in before the loop asks for them, so the loop measures prefetcher throughput,
        // not memory latency. A pointer chase has no such shortcut -- each load's address
        // depends on the value of the previous load, so nothing can be fetched ahead of time.
        // Sattolo's algorithm builds a single N-cycle over all lines (never a shorter sub-cycle
        // that would let the chase re-settle into a small, fully-cached loop).
        try
        {
            int size = 64 * 1024 * 1024;
            const int lineSize = 64;
            int lines = size / lineSize;

            var order = new int[lines];
            for (int i = 0; i < lines; i++) order[i] = i;
            var rng = new Random();
            for (int i = lines - 1; i > 0; i--)
            {
                int j = rng.Next(i); // [0, i) -- Sattolo, not Fisher-Yates's [0, i]
                (order[i], order[j]) = (order[j], order[i]);
            }

            var buf = new byte[size];
            for (int k = 0; k < lines; k++)
            {
                int from = order[k] * lineSize;
                int to = order[(k + 1) % lines] * lineSize;
                BitConverter.TryWriteBytes(buf.AsSpan(from, 4), to);
            }

            int pos = 0;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < lines; i++) pos = BitConverter.ToInt32(buf, pos);
            sw.Stop();
            GC.KeepAlive(pos);
            double ns = Math.Round(sw.Elapsed.TotalMilliseconds * 1_000_000.0 / lines, 2);
            rep.Metrics.Add(new("MEMORY ACCESS", $"{ns} ns/line"));
            if (ns > 150) { rep.Issues.Add($"Memory latency is {ns} ns. Check that XMP/EXPO is enabled in the BIOS."); rep.Score -= 10; }
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

        var reachable = rep.Pings.Where(p => p.Ok).ToList();
        if (reachable.Count == 0) { rep.Issues.Add("No test host responded to ping."); rep.Score -= 20; }
        else if (reachable.Min(p => p.AvgMs) > 80) { rep.Issues.Add("Network latency is above 80 ms even to the closest host."); rep.Score -= 10; }
        else if (reachable.Max(p => p.Jitter) > 30) { rep.Issues.Add("Network jitter is above 30 ms."); rep.Score -= 10; }

        rep.Score = Math.Max(0, rep.Score);
        return rep;
    });
}
