using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VOIDTUNE.WinUI.Services;

/// <summary>Progress for a running stress series: which run, seconds left, live throughput.</summary>
public readonly record struct StressProgress(int Run, int TotalRuns, int SecondsLeft, double LiveMops);

/// <summary>One full stress series (N sustained runs) reduced to comparable numbers.</summary>
public sealed class StressResult
{
    public List<double> RunScores { get; set; } = new();        // Mops per run (total work / duration)
    public double Median { get; set; }
    /// <summary>(max − min) / median across the runs — run-to-run noise, as a fraction (0.03 = 3%).</summary>
    public double Spread { get; set; }
    /// <summary>Worst last-10s ÷ first-10s throughput ratio seen across runs. Healthy systems hold
    /// ~0.95-1.0; a thermally throttling machine sags to 0.6-0.8 as the run heats up.</summary>
    public double WorstSustainedRatio { get; set; }
    public DateTime Timestamp { get; set; }
    public string CpuName { get; set; } = "";
}

/// <summary>The A/B verdict comparing a tweaked series against the saved baseline.</summary>
public enum StressVerdict { Improved, Regressed, WithinNoise }

/// <summary>
/// Sustained multi-run stress validator: measures whether a set of tweaks actually helps THIS
/// machine, with statistics instead of single-run luck. Exists because of a confirmed field
/// regression: "max performance" power tweaks looked great in short benchmarks but drove a
/// laptop's i7-8750H to 97°C and cost it ~60% of its FPS under sustained load — a failure mode
/// only visible when the load runs long enough to heat-soak (hence 45s runs, not burst tests)
/// and only trustworthy when repeated (hence 5 runs and noise-aware comparison).
/// </summary>
public static class StressTestService
{
    public const int DefaultRuns = 5;
    public const int RunSeconds = 45;
    private const int CooldownSeconds = 5;
    private const int EdgeWindowSeconds = 10;   // first/last window compared for the sustained ratio

    private static string BaselinePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "VOIDTUNE", "stress-baseline.json");

    // ── persistence (baseline must survive a reboot: NeedsReboot tweaks sit between A and B) ──
    public static StressResult? LoadBaseline()
    {
        try
        {
            if (!File.Exists(BaselinePath)) return null;
            return JsonSerializer.Deserialize<StressResult>(File.ReadAllText(BaselinePath));
        }
        catch { return null; }
    }

    public static void SaveBaseline(StressResult r)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BaselinePath)!);
            File.WriteAllText(BaselinePath, JsonSerializer.Serialize(r));
        }
        catch { /* validator still works in-memory for this session */ }
    }

    public static void ClearBaseline()
    {
        try { File.Delete(BaselinePath); } catch { /* already gone */ }
    }

    // ── the series ──────────────────────────────────────────────────────────
    public static async Task<StressResult> RunSeriesAsync(
        IProgress<StressProgress>? progress = null,
        int runs = DefaultRuns,
        CancellationToken ct = default)
    {
        var result = new StressResult { Timestamp = DateTime.Now, CpuName = HardwareInfo.CpuName };
        double worstRatio = double.MaxValue;

        for (int run = 1; run <= runs; run++)
        {
            ct.ThrowIfCancellationRequested();
            var (score, ratio) = await Task.Run(() => RunOnce(run, runs, progress, ct), ct);
            result.RunScores.Add(score);
            worstRatio = Math.Min(worstRatio, ratio);

            if (run < runs)
                await Task.Delay(TimeSpan.FromSeconds(CooldownSeconds), ct);
        }

        var sorted = result.RunScores.OrderBy(x => x).ToList();
        result.Median = sorted[sorted.Count / 2];
        result.Spread = result.Median > 0 ? (sorted[^1] - sorted[0]) / result.Median : 0;
        result.WorstSustainedRatio = worstRatio == double.MaxValue ? 1 : worstRatio;
        return result;
    }

    /// <summary>One 45-second all-core run. Returns (score in Mops, last10s/first10s ratio).</summary>
    private static (double Score, double SustainedRatio) RunOnce(
        int run, int totalRuns, IProgress<StressProgress>? progress, CancellationToken ct)
    {
        int threads = Environment.ProcessorCount;
        long opsCounter = 0;
        using var stop = new ManualResetEventSlim(false);

        var workers = new Thread[threads];
        for (int t = 0; t < threads; t++)
        {
            workers[t] = new Thread(seed =>
            {
                // Mixed integer / float / memory workload over a fixed 1 MB slab per thread —
                // deliberately larger than L1/L2 so the memory subsystem is exercised too, and
                // fixed-size so the GC contributes zero noise between runs.
                var slab = new long[131_072]; // 1 MB of longs
                var rnd = new Random((int)seed! * 40_503 + 17);
                for (int i = 0; i < slab.Length; i++) slab[i] = rnd.Next();

                double facc = 1.0;
                uint h = (uint)(int)seed * 2_654_435_761u + 1;
                while (!stop.IsSet)
                {
                    // one chunk ≈ 64k mixed ops between counter updates
                    for (int i = 0; i < 65_536; i++)
                    {
                        h = h * 2_654_435_761u + 12_345u;                 // Weyl-style index hash
                        int idx = (int)(h & (uint)(slab.Length - 1));
                        slab[idx] = slab[idx] * 6_364_136_223_846_793_005L + 1_442_695_040_888_963_407L;
                        facc += Math.Sqrt(Math.Abs((double)(slab[idx] & 0xFFFF)) + 1.0) * 1e-9;
                    }
                    Interlocked.Add(ref opsCounter, 65_536);
                }
                GC.KeepAlive(facc);
            })
            { IsBackground = true, Priority = ThreadPriority.Normal };
            workers[t].Start(t);
        }

        // Sample completed ops once a second so we can compare the run's first and last windows.
        var perSecond = new List<long>(RunSeconds);
        long prev = 0;
        var sw = Stopwatch.StartNew();
        for (int s = 1; s <= RunSeconds; s++)
        {
            while (sw.Elapsed.TotalSeconds < s)
            {
                if (ct.IsCancellationRequested) break;
                Thread.Sleep(25);
            }
            long now = Interlocked.Read(ref opsCounter);
            perSecond.Add(now - prev);
            prev = now;
            progress?.Report(new StressProgress(run, totalRuns, RunSeconds - s,
                Math.Round(perSecond[^1] / 1e6, 1)));
            if (ct.IsCancellationRequested) break;
        }
        stop.Set();
        foreach (var w in workers) w.Join(2000);
        ct.ThrowIfCancellationRequested();

        double totalMops = perSecond.Sum() / 1e6;
        double score = Math.Round(totalMops / RunSeconds, 1); // Mops/s averaged over the run

        double first = perSecond.Take(EdgeWindowSeconds).Average();
        double last = perSecond.Skip(Math.Max(0, perSecond.Count - EdgeWindowSeconds)).Average();
        double ratio = first > 0 ? Math.Round(last / first, 3) : 1;

        return (score, ratio);
    }

    // ── the verdict ─────────────────────────────────────────────────────────
    public static (StressVerdict Verdict, double DeltaPercent, string Summary) Compare(
        StressResult baseline, StressResult current)
    {
        double delta = baseline.Median > 0
            ? (current.Median - baseline.Median) / baseline.Median * 100.0
            : 0;

        // Noise threshold: the worse of the two series' own run-to-run spread, floored at 2% —
        // a delta inside this band is indistinguishable from luck, which is the whole reason
        // this runs 5 times instead of once.
        double noise = Math.Max(Math.Max(baseline.Spread, current.Spread) * 100.0, 2.0);

        StressVerdict v = Math.Abs(delta) <= noise
            ? StressVerdict.WithinNoise
            : delta > 0 ? StressVerdict.Improved : StressVerdict.Regressed;

        string summary = v switch
        {
            StressVerdict.Improved => $"IMPROVED — median {current.Median:F1} vs {baseline.Median:F1} Mops/s ({delta:+0.0;-0.0}%), outside the ±{noise:F1}% noise band.",
            StressVerdict.Regressed => $"REGRESSED — median {current.Median:F1} vs {baseline.Median:F1} Mops/s ({delta:+0.0;-0.0}%), outside the ±{noise:F1}% noise band. Revert the tweaks you just applied.",
            _ => $"WITHIN NOISE — median {current.Median:F1} vs {baseline.Median:F1} Mops/s ({delta:+0.0;-0.0}%) is inside the ±{noise:F1}% run-to-run variation. These tweaks made no measurable difference on this machine.",
        };

        // Independent throttle check: even an "improved" median can hide a new sustained-load
        // problem (short-run gains, long-run sag) — exactly the field regression's signature.
        if (current.WorstSustainedRatio < 0.85 && baseline.WorstSustainedRatio >= 0.90)
            summary += $" ⚠ THROTTLE WARNING: throughput now sags to {current.WorstSustainedRatio:P0} of its starting pace within a run " +
                       $"(baseline held {baseline.WorstSustainedRatio:P0}) — the applied tweaks are likely causing thermal throttling.";

        return (v, delta, summary);
    }
}
