using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VOIDTUNE.WinUI.Models;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class BackupPage : Page
{
    public BackupPage()
    {
        this.InitializeComponent();
        Reload();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => Reload();

    private void Reload()
    {
        var list = BackupService.List();
        BackupList.ItemsSource = list;
        CountLine.Text = $"{list.Count} REGISTRY SNAPSHOTS · {BackupService.BackupRoot}";
    }

    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        BusyRing.IsActive = true;
        Show("Exporting registry keys…", InfoBarSeverity.Informational);
        string name = await BackupService.CreateAsync("manual");
        BusyRing.IsActive = false;
        Reload();
        Show(string.IsNullOrEmpty(name) ? "Backup failed." : $"Backup created: {name}",
             string.IsNullOrEmpty(name) ? InfoBarSeverity.Error : InfoBarSeverity.Success);
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: BackupItem b }) return;

        var dlg = new ContentDialog
        {
            Title = "Restore registry backup?",
            Content = $"This re-imports {b.KeyCount} registry keys from {b.Created}. Current values for those keys will be overwritten. A reboot may be needed for some settings.",
            PrimaryButtonText = "Restore",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
        };
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

        BusyRing.IsActive = true;
        var (ok, fail) = await BackupService.RestoreAsync(b);
        BusyRing.IsActive = false;
        Show($"Restored {ok} keys" + (fail > 0 ? $", {fail} failed." : "."),
             fail > 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Success);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: BackupItem b })
        {
            bool ok = BackupService.Delete(b);
            Reload();
            Show(ok ? $"Deleted backup {b.Created}." : "Could not delete backup.",
                 ok ? InfoBarSeverity.Success : InfoBarSeverity.Error);
        }
    }

    private void Show(string msg, InfoBarSeverity sev)
    {
        StatusBar.Message = msg;
        StatusBar.Severity = sev;
        StatusBar.IsOpen = true;
    }
}
