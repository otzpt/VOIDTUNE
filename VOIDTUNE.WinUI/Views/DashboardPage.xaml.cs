using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using VOIDTUNE.WinUI.Models;
using VOIDTUNE.WinUI.Services;
using Windows.UI;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class DashboardPage : Page
{
    private readonly SystemMonitor _monitor = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1.5) };
    private readonly TweakEngine _engine = TweakEngine.Instance;

    public DashboardPage()
    {
        this.InitializeComponent();
        _monitor.GetCpuUsage(); // prime the first delta
        _timer.Tick += (_, _) => Refresh();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        Refresh();
        _timer.Start();
        HwLine.Text = $"CPU: {HardwareInfo.CpuVendor} [{HardwareInfo.CpuName}]  ·  GPU: {HardwareInfo.GpuName}  ·  " +
                      $"RAM: {HardwareInfo.TotalRamGb:0} GB  ·  Build: {HardwareInfo.WinBuild}  ·  " +
                      $"{(HardwareInfo.IsLaptop ? "LAPTOP" : "DESKTOP")}  ·  {_engine.Tweaks.Count} tweaks · running as Administrator";
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e) => _timer.Stop();

    private void Refresh()
    {
        double cpu = _monitor.GetCpuUsage();
        CpuValue.Text = $"{cpu:0}%";
        CpuBar.Value = cpu;

        var (used, total, pct) = _monitor.GetMemory();
        RamValue.Text = $"{used:0.0}GB";
        RamBar.Value = pct;

        ProcValue.Text = System.Diagnostics.Process.GetProcesses().Length.ToString();

        int applied = _engine.AppliedCount;
        AppliedValue.Text = applied.ToString();

        int score = (int)Math.Clamp(100 - cpu * 0.4 - pct * 0.4 + applied * 1.5, 0, 100);
        HealthValue.Text = score.ToString();
        HealthLabel.Text = score >= 80 ? "GOOD" : score >= 50 ? "FAIR" : "NEEDS WORK";
        HealthValue.Foreground = new SolidColorBrush(
            score >= 80 ? Color.FromArgb(255, 0x22, 0xC5, 0x5E) :
            score >= 50 ? Color.FromArgb(255, 0xF5, 0x9E, 0x0B) :
                          Color.FromArgb(255, 0xEF, 0x44, 0x44));
    }

    private async void ApplySafe_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true);
        var (ok, fail) = await _engine.ApplyTierAsync(TweakTier.Safe);
        SetBusy(false);
        ShowStatus($"Applied {ok} SAFE tweaks" + (fail > 0 ? $", {fail} failed." : "."),
                   fail > 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Success);
        Refresh();
    }

    private async void RevertAll_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true);
        var (ok, fail) = await _engine.RevertAllAsync();
        SetBusy(false);
        ShowStatus($"Reverted {ok} tweaks" + (fail > 0 ? $", {fail} failed." : "."), InfoBarSeverity.Informational);
        Refresh();
    }

    private void SetBusy(bool busy)
    {
        BusyRing.IsActive = busy;
        ApplySafeBtn.IsEnabled = !busy;
        RevertAllBtn.IsEnabled = !busy;
    }

    private void ShowStatus(string msg, InfoBarSeverity sev)
    {
        StatusBar.Message = msg;
        StatusBar.Severity = sev;
        StatusBar.IsOpen = true;
    }
}
