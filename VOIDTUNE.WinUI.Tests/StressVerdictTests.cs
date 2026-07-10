using System.Collections.Generic;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Tests;

/// <summary>
/// The Tweak Validator's verdict math is what tells a user "these tweaks helped / hurt / did
/// nothing" — a wrong verdict is worse than no verdict, so the thresholds are pinned here.
/// </summary>
public class StressVerdictTests
{
    private static StressResult Result(double median, double spread = 0.02, double sustained = 0.97) => new()
    {
        Median = median,
        Spread = spread,
        WorstSustainedRatio = sustained,
        RunScores = new List<double> { median },
    };

    [Fact]
    public void Clear_gain_outside_noise_is_improved()
    {
        var (v, delta, _) = StressTestService.Compare(Result(100), Result(110));
        Assert.Equal(StressVerdict.Improved, v);
        Assert.Equal(10, delta, 1);
    }

    [Fact]
    public void Clear_loss_outside_noise_is_regressed()
    {
        var (v, _, summary) = StressTestService.Compare(Result(100), Result(85));
        Assert.Equal(StressVerdict.Regressed, v);
        Assert.Contains("Revert", summary);
    }

    [Fact]
    public void Small_delta_inside_noise_is_within_noise()
    {
        // 1% delta with 2% noise floor — indistinguishable from luck.
        var (v, _, summary) = StressTestService.Compare(Result(100), Result(101));
        Assert.Equal(StressVerdict.WithinNoise, v);
        Assert.Contains("no measurable difference", summary);
    }

    [Fact]
    public void Noisy_series_widens_the_noise_band()
    {
        // A 6% gain would normally be "improved", but the baseline itself swung ±8%
        // run-to-run — the gain is inside the machine's own variation.
        var (v, _, _) = StressTestService.Compare(Result(100, spread: 0.08), Result(106));
        Assert.Equal(StressVerdict.WithinNoise, v);
    }

    [Fact]
    public void New_thermal_sag_adds_throttle_warning_even_when_median_improved()
    {
        // The pow7 signature: better burst numbers, but throughput now collapses within a run.
        var baseline = Result(100, sustained: 0.96);
        var current = Result(107, sustained: 0.70);
        var (v, _, summary) = StressTestService.Compare(baseline, current);
        Assert.Equal(StressVerdict.Improved, v);
        Assert.Contains("THROTTLE WARNING", summary);
    }

    [Fact]
    public void No_throttle_warning_when_baseline_already_sagged()
    {
        // A machine that always throttles (dusty laptop) shouldn't blame the tweaks for it.
        var (_, _, summary) = StressTestService.Compare(Result(100, sustained: 0.75), Result(103, sustained: 0.72));
        Assert.DoesNotContain("THROTTLE WARNING", summary);
    }

    [Fact]
    public async System.Threading.Tasks.Task Engine_smoke_run_produces_sane_numbers()
    {
        // One real 45s all-core run — CI-gated (same env var as the apply/revert round-trips)
        // so local test runs stay fast. Validates the engine end-to-end: nonzero score, a
        // physically plausible sustained ratio, and one score per run.
        if (System.Environment.GetEnvironmentVariable("VOIDTUNE_DESTRUCTIVE_TESTS") != "1") return;

        var r = await StressTestService.RunSeriesAsync(runs: 1);
        Assert.Single(r.RunScores);
        Assert.True(r.Median > 0, "score should be positive");
        Assert.InRange(r.WorstSustainedRatio, 0.1, 1.5);
        Assert.True(r.Spread >= 0);
    }
}
