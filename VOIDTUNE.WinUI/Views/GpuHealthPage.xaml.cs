using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class GpuHealthPage : Page
{
    private bool _loaded;

    public GpuHealthPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        if (!_loaded) { _loaded = true; await LoadAsync(); }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async System.Threading.Tasks.Task LoadAsync()
    {
        BusyBar.Visibility = Visibility.Visible;
        SubLine.Text = "READING GPU…";
        var d = await GpuHealthService.GetAsync();
        BusyBar.Visibility = Visibility.Collapsed;

        GpuNameText.Text = d.Name;
        GpuVendorText.Text = $"VENDOR: {d.Vendor.ToUpperInvariant()}";
        SubLine.Text = "LIVE GPU TELEMETRY";

        StatsRepeater.ItemsSource = new List<GpuStat>
        {
            new("DRIVER VERSION", d.DriverVer),
            new("DRIVER DATE",    d.DriverDate),
            new("VRAM TOTAL",     d.VramTotal),
            new("VRAM FREE",      d.VramFree),
            new("TEMPERATURE",    d.TempC),
            new("CORE CLOCK",     d.CoreClock),
            new("MEMORY CLOCK",   d.MemClock),
            new("FAN SPEED",      d.FanSpeed),
            new("POWER DRAW",     d.PowerDraw),
        };
    }
}
