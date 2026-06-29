using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class LatencyPage : Page
{
    public LatencyPage()
    {
        this.InitializeComponent();
    }

    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        RunBtn.IsEnabled = false;
        BusyBar.Visibility = Visibility.Visible;
        StatusBar.IsOpen = true;
        StatusBar.Severity = InfoBarSeverity.Informational;
        StatusBar.Message = "Running latency probes (timer, disk, memory, network)…";

        var rep = await LatencyService.RunAsync();

        StatsRepeater.ItemsSource = rep.Metrics;
        PingList.ItemsSource = rep.Pings;

        if (rep.Issues.Count > 0)
        {
            IssuesCard.Visibility = Visibility.Visible;
            ScoreText.Text = $"Latency health score: {rep.Score}/100";
            IssuesList.ItemsSource = rep.Issues;
        }
        else
        {
            IssuesCard.Visibility = Visibility.Collapsed;
        }

        BusyBar.Visibility = Visibility.Collapsed;
        RunBtn.IsEnabled = true;
        StatusBar.Severity = rep.Score >= 80 ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
        StatusBar.Message = $"Done. Latency health score: {rep.Score}/100.";
    }
}
