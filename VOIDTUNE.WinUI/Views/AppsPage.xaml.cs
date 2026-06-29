using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;
using VOIDTUNE.WinUI.Models;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class AppsPage : Page
{
    private readonly List<AppItem> _all = AppCatalog.All.ToList();

    public AppsPage()
    {
        this.InitializeComponent();
        CategoryBox.Items.Add("All categories");
        foreach (var c in _all.Select(a => a.Category).Distinct()) CategoryBox.Items.Add(c);
        CategoryBox.SelectedIndex = 0;
        Apply();
    }

    private void Category_Changed(object sender, SelectionChangedEventArgs e) => Apply();

    private void Apply()
    {
        string cat = CategoryBox.SelectedItem as string ?? "All categories";
        var items = cat == "All categories" ? _all : _all.Where(a => a.Category == cat).ToList();
        AppList.ItemsSource = items;
        CountLine.Text = $"{items.Count} apps · {_all.Count(a => a.Installed)} installed · winget required";
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        foreach (var a in _all) a.Selected = false;
    }

    private async void Detect_Click(object sender, RoutedEventArgs e)
    {
        if (!await EnsureWinget()) return;
        BusyRing.IsActive = true;
        Show("Scanning installed apps via winget…", InfoBarSeverity.Informational);
        await AppInstaller.DetectInstalledAsync(_all);
        BusyRing.IsActive = false;
        Apply();
        Show($"{_all.Count(a => a.Installed)} of {_all.Count} catalog apps already installed.", InfoBarSeverity.Success);
    }

    private async void InstallOne_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: AppItem app })
        {
            if (!await EnsureWinget()) return;
            bool ok = await AppInstaller.InstallAsync(app);
            Show(ok ? $"{app.Name} installed." : $"{app.Name} failed to install.",
                 ok ? InfoBarSeverity.Success : InfoBarSeverity.Error);
            Apply();
        }
    }

    private async void InstallSelected_Click(object sender, RoutedEventArgs e)
    {
        var picked = _all.Where(a => a.Selected).ToList();
        if (picked.Count == 0) { Show("Select one or more apps first.", InfoBarSeverity.Warning); return; }
        if (!await EnsureWinget()) return;

        BusyRing.IsActive = true;
        int ok = 0;
        foreach (var a in picked)
        {
            Show($"Installing {a.Name}… ({ok + 1}/{picked.Count})", InfoBarSeverity.Informational);
            if (await AppInstaller.InstallAsync(a)) ok++;
            a.Selected = false;
        }
        BusyRing.IsActive = false;
        Apply();
        Show($"Installed {ok}/{picked.Count} apps.", ok == picked.Count ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
    }

    private async System.Threading.Tasks.Task<bool> EnsureWinget()
    {
        if (await AppInstaller.IsWingetAvailableAsync()) return true;
        Show("winget (App Installer) not found. Install it from the Microsoft Store first.", InfoBarSeverity.Error);
        return false;
    }

    private void Show(string msg, InfoBarSeverity sev)
    {
        StatusBar.Message = msg;
        StatusBar.Severity = sev;
        StatusBar.IsOpen = true;
    }
}
