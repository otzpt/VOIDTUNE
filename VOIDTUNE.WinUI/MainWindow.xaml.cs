using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using VOIDTUNE.WinUI.Services;
using VOIDTUNE.WinUI.Views;
using Windows.Graphics;

namespace VOIDTUNE.WinUI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        // Mica backdrop + extend content into the title bar (Windows 11 look)
        this.SystemBackdrop = new MicaBackdrop();
        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(AppTitleBar);
        this.Title = "VOIDTUNE";

        AppWindow.Resize(new SizeInt32(1240, 820));

        if (UpdateService.AutoCheckEnabled) _ = CheckForUpdatesAsync();
    }

    private async System.Threading.Tasks.Task CheckForUpdatesAsync()
    {
        try
        {
            var info = await UpdateService.CheckAsync();
            if (info != null)
            {
                UpdateBar.Title = $"Update available — v{info.Version}";
                UpdateBar.Message = "A newer version of VOIDTUNE is on GitHub. Open Settings to install it.";
                UpdateBar.IsOpen = true;
            }
        }
        catch { /* offline / API down — stay quiet */ }
    }

    private void UpdateBar_View(object sender, RoutedEventArgs e)
    {
        UpdateBar.IsOpen = false;
        Nav.SelectedItem = Nav.SettingsItem;   // routes to SettingsPage, which shows the update card
    }

    private void Nav_Loaded(object sender, RoutedEventArgs e)
    {
        Nav.SelectedItem = Nav.MenuItems[0];
        ContentFrame.Navigate(typeof(DashboardPage), null, new EntranceNavigationTransitionInfo());
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo());
            return;
        }

        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            Type target = tag switch
            {
                "dash"        => typeof(DashboardPage),
                "tweaks"      => typeof(TweaksPage),
                "services"    => typeof(ServicesPage),
                "startup"     => typeof(StartupPage),
                "privacy"     => typeof(PrivacyPage),
                "personalize" => typeof(PersonalizePage),
                "drivers"     => typeof(DriversPage),
                "gpu"         => typeof(GpuHealthPage),
                "latency"     => typeof(LatencyPage),
                "bench"       => typeof(BenchmarksPage),
                "apps"        => typeof(AppsPage),
                "backup"      => typeof(BackupPage),
                "script"      => typeof(ScriptPage),
                _             => typeof(DashboardPage),
            };
            ContentFrame.Navigate(target, tag, new DrillInNavigationTransitionInfo());
        }
    }
}
