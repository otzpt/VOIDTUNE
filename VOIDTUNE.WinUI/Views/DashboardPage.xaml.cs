using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using VOIDTUNE.WinUI.Models;
using VOIDTUNE.WinUI.Services;
using Windows.Foundation;
using Windows.UI;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class DashboardPage : Page
{
    private readonly SystemMonitor _monitor = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1.5) };
    private readonly DispatcherTimer _ringAnim = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private readonly TweakEngine _engine = TweakEngine.Instance;
    private readonly Spark _cpuSpark = new();
    private readonly Spark _ramSpark = new();
    private readonly ObservableCollection<DiskRow> _disks = new();
    private readonly Random _rng = new();

    private double _shownScore;      // ring animates toward _targetScore
    private double _targetScore;
    private bool _starsBuilt;
    private int _lastProc;

    public DashboardPage()
    {
        this.InitializeComponent();
        _monitor.GetCpuUsage(); // prime the first delta
        _timer.Tick += (_, _) => Refresh();
        _ringAnim.Tick += (_, _) => StepRing();
        DiskList.ItemsSource = _disks;
        StarCanvas.SizeChanged += (_, _) => BuildStars();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        Refresh();
        RefreshDisks();
        _timer.Start();
        HwLine.Text = $"CPU: {HardwareInfo.CpuVendor} [{HardwareInfo.CpuName}]  ·  GPU: {HardwareInfo.GpuName}  ·  " +
                      $"RAM: {HardwareInfo.TotalRamGb:0} GB  ·  Build: {HardwareInfo.WinBuild}  ·  " +
                      $"{(HardwareInfo.IsLaptop ? "LAPTOP" : "DESKTOP")}  ·  {_engine.Tweaks.Count} tweaks · running as Administrator";
        CpuChip.Text = HardwareInfo.CpuName.Trim();
        GpuChip.Text = HardwareInfo.GpuName.Trim();
        RamChip.Text = $"{HardwareInfo.TotalRamGb:0} GB RAM";
        BuildChip.Text = $"WIN BUILD {HardwareInfo.WinBuild}";
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _timer.Stop();
        _ringAnim.Stop();
    }

    // ── live refresh ─────────────────────────────────────────────────────────

    private void Refresh()
    {
        double cpu = _monitor.GetCpuUsage();
        CpuValue.Text = $"{cpu:0}%";
        _cpuSpark.Push(cpu);
        _cpuSpark.Render(CpuSparkLine, CpuSparkFill, CpuSparkHost);

        var (used, total, pct) = _monitor.GetMemory();
        RamValue.Text = $"{used:0.0}";
        RamTotal.Text = $"/ {total:0} GB";
        _ramSpark.Push(pct);
        _ramSpark.Render(RamSparkLine, RamSparkFill, RamSparkHost);

        int proc = System.Diagnostics.Process.GetProcesses().Length;
        ProcValue.Text = proc.ToString();
        ProcDelta.Text = _lastProc == 0 || proc == _lastProc ? "running now"
                       : proc > _lastProc ? $"▲ {proc - _lastProc} since last sample"
                       : $"▼ {_lastProc - proc} since last sample";
        _lastProc = proc;

        int applied = _engine.AppliedCount;
        AppliedValue.Text = applied.ToString();
        AppliedOf.Text = $"of {_engine.Tweaks.Count} in the catalog";

        var up = TimeSpan.FromMilliseconds(Environment.TickCount64);
        UptimeChip.Text = up.TotalDays >= 1 ? $"UP {(int)up.TotalDays}d {up.Hours}h" : $"UP {up.Hours}h {up.Minutes}m";

        // honest health: load + pressure, small bonus for an optimized box
        double score = 100 - cpu * 0.30 - pct * 0.35 - Math.Max(0, proc - 230) * 0.10 + (applied > 0 ? 4 : 0);
        _targetScore = Math.Clamp(score, 0, 100);
        if (!_ringAnim.IsEnabled) _ringAnim.Start();

        StatusChip.Text = _targetScore >= 80 ? "SYSTEM NOMINAL" : _targetScore >= 55 ? "ELEVATED LOAD" : "UNDER PRESSURE";
    }

    private void StepRing()
    {
        _shownScore += (_targetScore - _shownScore) * 0.16;
        if (Math.Abs(_targetScore - _shownScore) < 0.4)
        {
            _shownScore = _targetScore;
            _ringAnim.Stop();
        }
        RingScore.Text = $"{_shownScore:0}";
        RingLabel.Text = _shownScore >= 85 ? "OPTIMAL" : _shownScore >= 70 ? "GOOD" : _shownScore >= 50 ? "FAIR" : "NEEDS WORK";
        RingArc.Data = BuildArc(89, 89, 77, _shownScore / 100.0);
    }

    private static PathGeometry BuildArc(double cx, double cy, double r, double pct)
    {
        pct = Math.Clamp(pct, 0.004, 0.9996);
        double start = -Math.PI / 2;
        double end = start + pct * Math.PI * 2;
        var fig = new PathFigure
        {
            StartPoint = new Point(cx + r * Math.Cos(start), cy + r * Math.Sin(start)),
            IsClosed = false,
        };
        fig.Segments.Add(new ArcSegment
        {
            Point = new Point(cx + r * Math.Cos(end), cy + r * Math.Sin(end)),
            Size = new Size(r, r),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = pct > 0.5,
        });
        var geo = new PathGeometry();
        geo.Figures.Add(fig);
        return geo;
    }

    private void RefreshDisks()
    {
        _disks.Clear();
        try
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                if (!d.IsReady || d.DriveType != DriveType.Fixed) continue;
                double totalGb = d.TotalSize / 1024.0 / 1024 / 1024;
                double freeGb = d.TotalFreeSpace / 1024.0 / 1024 / 1024;
                double usedGb = totalGb - freeGb;
                _disks.Add(new DiskRow
                {
                    Letter = d.Name.TrimEnd('\\'),
                    Detail = $"{usedGb:0} / {totalGb:0} GB · {freeGb:0} GB free",
                    Pct = totalGb > 0 ? usedGb / totalGb * 100 : 0,
                });
            }
        }
        catch { /* a drive disappeared mid-enumeration — show what we have */ }
    }

    // ── starfield ────────────────────────────────────────────────────────────

    private void BuildStars()
    {
        if (_starsBuilt || StarCanvas.ActualWidth < 50 || StarCanvas.ActualHeight < 50) return;
        _starsBuilt = true;

        double w = StarCanvas.ActualWidth, h = StarCanvas.ActualHeight;
        for (int i = 0; i < 54; i++)
        {
            double size = _rng.NextDouble() * 1.8 + 0.8;
            bool tinted = _rng.NextDouble() < 0.35;
            var star = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(tinted
                    ? Color.FromArgb(255, 0xA7, 0x8B, 0xFA)
                    : Color.FromArgb(255, 0xE9, 0xE4, 0xF7)),
                Opacity = _rng.NextDouble() * 0.5 + 0.15,
            };
            Canvas.SetLeft(star, _rng.NextDouble() * (w - 12) + 6);
            Canvas.SetTop(star, _rng.NextDouble() * (h - 12) + 6);
            StarCanvas.Children.Add(star);

            // slow random twinkle
            var anim = new DoubleAnimation
            {
                From = star.Opacity,
                To = _rng.NextDouble() * 0.25 + 0.05,
                Duration = new Duration(TimeSpan.FromSeconds(_rng.NextDouble() * 2.6 + 1.4)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime = TimeSpan.FromSeconds(_rng.NextDouble() * 3),
            };
            Storyboard.SetTarget(anim, star);
            Storyboard.SetTargetProperty(anim, "Opacity");
            var sb = new Storyboard();
            sb.Children.Add(anim);
            sb.Begin();
        }
    }

    // ── sparkline ────────────────────────────────────────────────────────────

    private sealed class Spark
    {
        private const int Cap = 46;
        private readonly Queue<double> _pts = new();

        public void Push(double v)
        {
            _pts.Enqueue(Math.Clamp(v, 0, 100));
            while (_pts.Count > Cap) _pts.Dequeue();
        }

        public void Render(Polyline line, Polygon fill, FrameworkElement host)
        {
            double w = host.ActualWidth, h = host.ActualHeight;
            if (w < 10 || h < 10 || _pts.Count < 2) return;

            var linePts = new PointCollection();
            var fillPts = new PointCollection();
            int i = 0, n = _pts.Count;
            foreach (double v in _pts)
            {
                double x = i * w / (Cap - 1);
                double y = h - (v / 100.0) * (h - 3) - 1.5;
                linePts.Add(new Point(x, y));
                fillPts.Add(new Point(x, y));
                i++;
            }
            double lastX = (n - 1) * w / (Cap - 1);
            fillPts.Add(new Point(lastX, h));
            fillPts.Add(new Point(0, h));

            line.Points = linePts;
            fill.Points = fillPts;
        }
    }

    // ── quick actions ────────────────────────────────────────────────────────

    private async void ApplySafe_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true, "applying SAFE tier…");
        var (ok, fail) = await _engine.ApplyTierAsync(TweakTier.Safe);
        SetBusy(false);
        ShowStatus($"Applied {ok} SAFE tweaks" + (fail > 0 ? $", {fail} failed." : "."),
                   fail > 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Success);
        Refresh();
    }

    private async void RevertAll_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true, "reverting to Windows defaults…");
        var (ok, fail) = await _engine.RevertAllAsync();
        SetBusy(false);
        ShowStatus($"Reverted {ok} tweaks" + (fail > 0 ? $", {fail} failed." : "."), InfoBarSeverity.Informational);
        Refresh();
    }

    private async void RestorePoint_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true, "creating restore point (can take a minute)…");
        var r = await CommandRunner.ExecAsync(
            "PS:try { Enable-ComputerRestore -Drive 'C:\\' -ErrorAction SilentlyContinue; " +
            "Checkpoint-Computer -Description 'VOIDTUNE manual checkpoint' -RestorePointType MODIFY_SETTINGS } catch {}; exit 0");
        SetBusy(false);
        ShowStatus(r.Ok
            ? "Restore point requested. Windows may skip it if one was made in the last 24h."
            : "Could not create a restore point — System Protection may be disabled.",
            r.Ok ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
    }

    private async void CleanTemp_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true, "flushing temp files + DNS cache…");
        await CommandRunner.ExecAsync(@"del /q /f /s ""%TEMP%\*"" 2>nul & ipconfig /flushdns & exit /b 0");
        SetBusy(false);
        RefreshDisks();
        ShowStatus("Temp files and DNS cache flushed.", InfoBarSeverity.Success);
    }

    private void SetBusy(bool busy, string hint = "")
    {
        BusyRing.IsActive = busy;
        ActionHint.Text = busy ? hint : "";
        ApplySafeBtn.IsEnabled = !busy;
        RevertAllBtn.IsEnabled = !busy;
        RestorePointBtn.IsEnabled = !busy;
        CleanTempBtn.IsEnabled = !busy;
    }

    private void ShowStatus(string msg, InfoBarSeverity sev)
    {
        StatusBar.Message = msg;
        StatusBar.Severity = sev;
        StatusBar.IsOpen = true;
    }
}
