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

    private async void Nav_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyDevMode();
        Services.AppSettingsStore.DevModeChanged += OnDevModeChanged;

        // Reconcile applied-tweak state against the live system behind the loading screen.
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await TweakEngine.Instance.InitializeAsync();
        int active = TweakEngine.Instance.AppliedCount;
        LoadingStatus.Text = $"Found {active} active tweak{(active == 1 ? "" : "s")}.";

        // Let that result read for a beat (and avoid a jarring flash on very fast machines).
        int shown = (int)sw.ElapsedMilliseconds;
        if (shown < 900) await System.Threading.Tasks.Task.Delay(900 - shown);

        Nav.SelectedItem = Nav.MenuItems[0];
        ContentFrame.Navigate(typeof(DashboardPage), null, new EntranceNavigationTransitionInfo());

        await HideLoadingOverlayAsync();
    }

    // Fades the startup loading screen out, then collapses it so it can't catch input.
    private async System.Threading.Tasks.Task HideLoadingOverlayAsync()
    {
        var fade = new DoubleAnimation
        {
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(320)),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        };
        Storyboard.SetTarget(fade, LoadingOverlay);
        Storyboard.SetTargetProperty(fade, "Opacity");
        var sb = new Storyboard();
        sb.Children.Add(fade);
        var tcs = new System.Threading.Tasks.TaskCompletionSource();
        sb.Completed += (_, _) => tcs.SetResult();
        sb.Begin();
        await tcs.Task;
        LoadingOverlay.Visibility = Visibility.Collapsed;
    }

    private void OnDevModeChanged() => DispatcherQueue.TryEnqueue(ApplyDevMode);

    // DevTools is a WIP category, shown only when Developer mode is enabled in Settings.
    private void ApplyDevMode()
    {
        var vis = Services.AppSettingsStore.DevMode ? Visibility.Visible : Visibility.Collapsed;
        DevToolsNav.Visibility = vis;
        DevToolsHeader.Visibility = vis;
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
                "restore"     => typeof(RestorePage),
                "services"    => typeof(ServicesPage),
                "startup"     => typeof(StartupPage),
                "personalize" => typeof(PersonalizePage),
                "drivers"     => typeof(DriversPage),
                "gpu"         => typeof(GpuHealthPage),
                "latency"     => typeof(LatencyPage),
                "bench"       => typeof(BenchmarksPage),
                "apps"        => typeof(AppsPage),
                "backup"      => typeof(BackupPage),
                "script"      => typeof(ScriptPage),
                "devtools"    => typeof(DevToolsPage),
                _             => typeof(DashboardPage),
            };
            ContentFrame.Navigate(target, tag, new DrillInNavigationTransitionInfo());
        }
    }
}
