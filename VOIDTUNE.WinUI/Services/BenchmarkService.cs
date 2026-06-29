using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace VOIDTUNE.WinUI.Services;

/// <summary>Lightweight in-process CPU / RAM / disk benchmarks (pure .NET, no external tools).</summary>
public static class BenchmarkService
{
    public static Task<List<Metric>> RunAsync(IProgress<string>? progress = null) => Task.Run(() =>
    {
        var m = new List<Metric>();

        // ── CPU single-thread ───────────────────────────────────────────────
        progress?.Report("CPU single-thread…");
        const long iters = 120_000_000;
        var sw = Stopwatch.StartNew();
        double acc = CpuWork(iters);
        sw.Stop();
        GC.KeepAlive(acc);
        double stMs = sw.Elapsed.TotalMilliseconds;
        double stScore = Math.Round(iters / stMs / 1000.0, 0); // kops/ms
        m.Add(new("CPU SINGLE-THREAD", $"{stScore} pts", "#22C55E"));
        m.Add(new("CPU 1T TIME", $"{Math.Round(stMs)} ms"));

        // ── CPU multi-thread ────────────────────────────────────────────────
        int cores = Environment.ProcessorCount;
        progress?.Report($"CPU multi-thread ({cores} cores)…");
        sw.Restart();
        Parallel.For(0, cores, _ => { var r = CpuWork(iters); GC.KeepAlive(r); });
        sw.Stop();
        double mtMs = sw.Elapsed.TotalMilliseconds;
        double mtScore = Math.Round(iters * cores / mtMs / 1000.0, 0);
        m.Add(new("CPU MULTI-THREAD", $"{mtScore} pts", "#22C55E"));
        m.Add(new("CPU THREADS", $"{cores} cores"));

        // ── RAM bandwidth ───────────────────────────────────────────────────
        progress?.Report("RAM bandwidth…");
        try
        {
            int size = 256 * 1024 * 1024;
            var src = new byte[size];
            var dst = new byte[size];
            sw.Restart();
            int passes = 6;
            for (int i = 0; i < passes; i++) Buffer.BlockCopy(src, 0, dst, 0, size);
            sw.Stop();
            double gb = size / 1073741824.0 * passes;
            double gbps = Math.Round(gb / sw.Elapsed.TotalSeconds, 1);
            m.Add(new("RAM BANDWIDTH", $"{gbps} GB/s", "#22C55E"));
        }
        catch { m.Add(new("RAM BANDWIDTH", "error", "#EF4444")); }

        // ── Disk sequential ─────────────────────────────────────────────────
        progress?.Report("Disk sequential write/read…");
        try
        {
            string tmp = Path.Combine(Path.GetTempPath(), "vt_bench.tmp");
            int chunk = 4 * 1024 * 1024;
            int chunks = 32; // 128 MB
            var buf = new byte[chunk];
            new Random().NextBytes(buf);

            sw.Restart();
            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, chunk))
            {
                for (int i = 0; i < chunks; i++) fs.Write(buf, 0, chunk);
                fs.Flush(true);
            }
            sw.Stop();
            double mb = chunk * (double)chunks / 1048576.0;
            m.Add(new("DISK WRITE", $"{Math.Round(mb / sw.Elapsed.TotalSeconds)} MB/s", "#22C55E"));

            sw.Restart();
            using (var fs = new FileStream(tmp, FileMode.Open, FileAccess.Read, FileShare.None, chunk))
            {
                int read; long total = 0;
                while ((read = fs.Read(buf, 0, chunk)) > 0) total += read;
                GC.KeepAlive(total);
            }
            sw.Stop();
            m.Add(new("DISK READ", $"{Math.Round(mb / sw.Elapsed.TotalSeconds)} MB/s", "#22C55E"));
            try { File.Delete(tmp); } catch { }
        }
        catch { m.Add(new("DISK", "error", "#EF4444")); }

        progress?.Report("Done.");
        return m;
    });

    private static double CpuWork(long iters)
    {
        double acc = 0;
        for (long i = 1; i <= iters; i++)
            acc += Math.Sqrt(i) * 0.5 + (i & 0xFF);
        return acc;
    }
}
