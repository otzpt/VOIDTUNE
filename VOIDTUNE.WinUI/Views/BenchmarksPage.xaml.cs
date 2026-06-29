using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class BenchmarksPage : Page
{
    public BenchmarksPage()
    {
        this.InitializeComponent();
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
}
