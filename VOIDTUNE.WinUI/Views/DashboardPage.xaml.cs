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
using System.Linq;
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

        // 1) Nebula blooms — big soft colored clouds that slowly breathe + drift. Added first
        //    so they sit behind the stars. This is what makes the galaxy read as deep, not flat.
        AddNebula(w * 0.14, h * 0.16, 300, Color.FromArgb(0x00, 0x8B, 0x5C, 0xF6), 0x52); // violet
        AddNebula(w * 0.82, h * 0.78, 340, Color.FromArgb(0x00, 0xEC, 0x48, 0x99), 0x3E); // magenta
        AddNebula(w * 0.62, h * 0.30, 240, Color.FromArgb(0x00, 0x38, 0xBD, 0xF8), 0x30); // cyan

        // 2) Starfield — mostly faint, a few bright glowing anchors.
        for (int i = 0; i < 96; i++)
        {
            bool bright = _rng.NextDouble() < 0.14;
            double size = bright ? _rng.NextDouble() * 1.6 + 2.4 : _rng.NextDouble() * 1.7 + 0.6;
            double roll = _rng.NextDouble();
            Color c = roll < 0.30 ? Color.FromArgb(255, 0xC4, 0xB5, 0xFD)   // violet
                    : roll < 0.46 ? Color.FromArgb(255, 0x7D, 0xD3, 0xFC)   // cyan
                                  : Color.FromArgb(255, 0xF5, 0xF3, 0xFF);  // white
            var star = new Ellipse { Width = size, Height = size, Fill = new SolidColorBrush(c) };
            double peak = bright ? _rng.NextDouble() * 0.35 + 0.6 : _rng.NextDouble() * 0.5 + 0.15;
            star.Opacity = peak;

            // bright stars get a soft glow halo behind them
            if (bright)
            {
                var glow = new Ellipse
                {
                    Width = size * 4.5,
                    Height = size * 4.5,
                    Fill = RadialFill(Color.FromArgb(0x00, c.R, c.G, c.B), 0x66, c),
                    Opacity = 0.7,
                };
                Canvas.SetLeft(glow, _rng.NextDouble() * (w - 12) + 6 - size * 1.75);
                double gy = _rng.NextDouble() * (h - 12) + 6;
                Canvas.SetTop(glow, gy - size * 1.75);
                StarCanvas.Children.Add(glow);
            }

            Canvas.SetLeft(star, _rng.NextDouble() * (w - 12) + 6);
            Canvas.SetTop(star, _rng.NextDouble() * (h - 12) + 6);
            StarCanvas.Children.Add(star);

            Animate(star, "Opacity", peak, _rng.NextDouble() * 0.22 + 0.04,
                    _rng.NextDouble() * 2.6 + 1.4, _rng.NextDouble() * 3);
        }
    }

    // A soft radial-gradient bloom that slowly pulses (opacity) and drifts (translate).
    private void AddNebula(double cx, double cy, double diameter, Color edge, byte coreAlpha)
    {
        Color core = Color.FromArgb(coreAlpha, edge.R, edge.G, edge.B);
        var bloom = new Ellipse
        {
            Width = diameter,
            Height = diameter,
            Fill = RadialFill(edge, coreAlpha, core),
            Opacity = 0.85,
        };
        Canvas.SetLeft(bloom, cx - diameter / 2);
        Canvas.SetTop(bloom, cy - diameter / 2);
        StarCanvas.Children.Add(bloom);

        // Opacity-only pulse — runs on the composition (GPU) thread, so it stays smooth and
        // never competes with the UI thread. No positional drift (that was UI-thread work).
        Animate(bloom, "Opacity", 0.55, 0.95, _rng.NextDouble() * 4 + 6, _rng.NextDouble() * 2);
    }

    private static RadialGradientBrush RadialFill(Color edge, byte coreAlpha, Color coreColor)
    {
        var b = new RadialGradientBrush();
        b.GradientStops.Add(new GradientStop { Color = Color.FromArgb(coreAlpha, coreColor.R, coreColor.G, coreColor.B), Offset = 0 });
        b.GradientStops.Add(new GradientStop { Color = Color.FromArgb(0x00, edge.R, edge.G, edge.B), Offset = 1 });
        return b;
    }

    private void Animate(DependencyObject target, string prop, double from, double to, double seconds, double delay)
    {
        var anim = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = new Duration(TimeSpan.FromSeconds(seconds)),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            BeginTime = TimeSpan.FromSeconds(delay),
            EnableDependentAnimation = true,
        };
        Storyboard.SetTarget(anim, target);
        Storyboard.SetTargetProperty(anim, prop);
        var sb = new Storyboard();
        sb.Children.Add(anim);
        sb.Begin();
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
        var safe = _engine.Tweaks.Where(t => t.Tier == TweakTier.Safe).ToList();
        var confirmed = await Views.ApplyPreviewDialog.ShowAsync(this.XamlRoot, "Apply SAFE tweaks",
            $"VOIDTUNE is about to apply {safe.Count} SAFE tweaks. Uncheck anything you don't want — nothing runs until you hit Apply.", safe);
        if (confirmed is null) return;                                   // cancelled
        if (confirmed.Count == 0) { ShowStatus("Nothing selected.", InfoBarSeverity.Warning); return; }

        SetBusy(true, "applying SAFE tweaks…");
        var (ok, fail) = await _engine.ApplyAsync(confirmed);
        SetBusy(false);
        ShowStatus($"Applied {ok} SAFE tweaks" + (fail > 0 ? $", {fail} failed." : "."),
                   fail > 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Success);
        Refresh();

        int rebootCount = confirmed.Count(t => t.NeedsReboot && t.Applied);
        if (rebootCount > 0) await Views.RebootPrompt.ShowAsync(this.XamlRoot, rebootCount);
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
