using System;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class BenchmarksPage : Page
{
    private StressResult? _baseline;
    private CancellationTokenSource? _stressCts;

    public BenchmarksPage()
    {
        this.InitializeComponent();
        _baseline = StressTestService.LoadBaseline();
        if (_baseline is not null) ShowBaseline();
    }

    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        RunBtn.IsEnabled = false;
        BusyBar.Visibility = Visibility.Visible;
        StatusBar.IsOpen = true;
        StatusBar.Severity = InfoBarSeverity.Informational;

        var progress = new Progress<string>(msg => StatusBar.Message = $"Benchmarking: {msg}");
        var metrics = await BenchmarkService.RunAsync(progress);

        StatsRepeater.ItemsSource = metrics;
        BusyBar.Visibility = Visibility.Collapsed;
        RunBtn.IsEnabled = true;
        StatusBar.Severity = InfoBarSeverity.Success;
        StatusBar.Message = "Benchmark complete. Re-run after applying tweaks to compare.";
    }

    // ── Tweak Validator ──────────────────────────────────────────────────────

    private async void Baseline_Click(object sender, RoutedEventArgs e)
    {
        var result = await RunStressSeries("baseline");
        if (result is null) return;

        _baseline = result;
        StressTestService.SaveBaseline(result);
        ShowBaseline();
        VerdictBar.IsOpen = true;
        VerdictBar.Severity = InfoBarSeverity.Success;
        VerdictBar.Title = "Baseline saved";
        VerdictBar.Message = "Now apply the tweaks you want to test (reboot if the app asks), then come back and hit \"Run comparison\". The baseline survives reboots.";
    }

    private async void Compare_Click(object sender, RoutedEventArgs e)
    {
        if (_baseline is null) return;
        var result = await RunStressSeries("comparison");
        if (result is null) return;

        var (verdict, _, summary) = StressTestService.Compare(_baseline, result);
        VerdictBar.IsOpen = true;
        VerdictBar.Title = verdict switch
        {
            StressVerdict.Improved => "Tweaks helped",
            StressVerdict.Regressed => "Tweaks hurt this machine",
            _ => "No measurable difference",
        };
        VerdictBar.Severity = verdict switch
        {
            StressVerdict.Improved => InfoBarSeverity.Success,
            StressVerdict.Regressed => InfoBarSeverity.Error,
            _ => InfoBarSeverity.Warning,
        };
        VerdictBar.Message = summary;
    }

    private void CancelStress_Click(object sender, RoutedEventArgs e) => _stressCts?.Cancel();

    private void ClearBaseline_Click(object sender, RoutedEventArgs e)
    {
        _baseline = null;
        StressTestService.ClearBaseline();
        BaselineInfo.Visibility = Visibility.Collapsed;
        CompareBtn.IsEnabled = false;
        ClearBaselineBtn.IsEnabled = false;
        VerdictBar.IsOpen = false;
    }

    private async System.Threading.Tasks.Task<StressResult?> RunStressSeries(string label)
    {
        SetStressBusy(true);
        StressStatus.Text = $"Starting {label} series…";
        _stressCts = new CancellationTokenSource();

        var progress = new Progress<StressProgress>(p =>
        {
            int totalSecs = StressTestService.DefaultRuns * StressTestService.RunSeconds;
            int doneSecs = (p.Run - 1) * StressTestService.RunSeconds + (StressTestService.RunSeconds - p.SecondsLeft);
            StressBar.Value = doneSecs * 100.0 / totalSecs;
            StressStatus.Text = $"Run {p.Run}/{p.TotalRuns} — {p.SecondsLeft}s left — {p.LiveMops:F1} Mops/s live";
        });

        try
        {
            return await StressTestService.RunSeriesAsync(progress, ct: _stressCts.Token);
        }
        catch (OperationCanceledException)
        {
            StressStatus.Text = "Cancelled.";
            return null;
        }
        finally
        {
            SetStressBusy(false);
            _stressCts.Dispose();
            _stressCts = null;
        }
    }

    private void SetStressBusy(bool busy)
    {
        BaselineBtn.IsEnabled = !busy;
        CompareBtn.IsEnabled = !busy && _baseline is not null;
        ClearBaselineBtn.IsEnabled = !busy && _baseline is not null;
        CancelStressBtn.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        StressBar.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        StressStatus.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        RunBtn.IsEnabled = !busy;
    }

    private void ShowBaseline()
    {
        if (_baseline is null) return;
        BaselineInfo.Visibility = Visibility.Visible;
        BaselineInfo.Text =
            $"BASELINE · {_baseline.Timestamp:g} · {_baseline.CpuName}\n" +
            $"median {_baseline.Median:F1} Mops/s · runs [{string.Join(", ", _baseline.RunScores)}] · " +
            $"spread ±{_baseline.Spread:P1} · sustained {_baseline.WorstSustainedRatio:P0}";
        CompareBtn.IsEnabled = true;
        ClearBaselineBtn.IsEnabled = true;
    }
}
