using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class SettingsPage : Page
{
    private UpdateInfo? _pending;
    private bool _initialising = true;

    public SettingsPage()
    {
        this.InitializeComponent();
        VersionText.Text = $"v{UpdateService.CurrentVersion} · WinUI 3 edition";
        AutoCheckToggle.IsOn = UpdateService.AutoCheckEnabled;
        InstallModeText.Text = UpdateService.IsInstalled
            ? "Install mode: installed (MSI) — updates run the installer."
            : "Install mode: portable — updates replace files in place.";
        InstallBtnText.Text = UpdateService.IsInstalled ? "Download & run installer" : "Download & install";
        DevModeToggle.IsOn = AppSettingsStore.DevMode;
        _initialising = false;

        // If a startup check already found an update, surface it here.
        if (UpdateService.Pending != null) ShowUpdate(UpdateService.Pending);
    }

    private void AutoCheck_Toggled(object sender, RoutedEventArgs e)
    {
        if (_initialising) return;
        UpdateService.AutoCheckEnabled = AutoCheckToggle.IsOn;
    }

    private async void DevMode_Toggled(object sender, RoutedEventArgs e)
    {
        if (_initialising) return;

        if (DevModeToggle.IsOn)
        {
            var dlg = new ContentDialog
            {
                Title = "Enable Developer mode?",
                Content = "Developer mode unlocks the DevTools category — a live system probe, an elevated " +
                          "console, and a block-based tweak builder.\n\nThese are work-in-progress features. " +
                          "Everything stays reversible, but expect rough edges. Continue?",
                PrimaryButtonText = "Enable WIP features",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
            };
            if (await dlg.ShowAsync() == ContentDialogResult.Primary)
                AppSettingsStore.DevMode = true;
            else
                DevModeToggle.IsOn = false;   // user backed out
        }
        else
        {
            AppSettingsStore.DevMode = false;
        }
    }

    private async void Check_Click(object sender, RoutedEventArgs e)
    {
        CheckBtn.IsEnabled = false;
        CheckRing.IsActive = true;
        UpdateCard.Visibility = Visibility.Collapsed;
        Show("Checking GitHub for the latest release…", InfoBarSeverity.Informational);

        var info = await UpdateService.CheckAsync();

        CheckRing.IsActive = false;
        CheckBtn.IsEnabled = true;

        if (info == null)
        {
            Show($"You're up to date (v{UpdateService.CurrentVersion}).", InfoBarSeverity.Success);
        }
        else
        {
            ShowUpdate(info);
        }
    }

    public void ShowUpdate(UpdateInfo info)
    {
        _pending = info;
        UpdateInfoBar.IsOpen = false;
        UpdateTitle.Text = $"Update available — v{info.Version}";
        UpdateNotes.Text = string.IsNullOrWhiteSpace(info.Notes) ? "(no release notes)" : info.Notes;
        ReleaseLink.NavigateUri = string.IsNullOrEmpty(info.PageUrl) ? null : new Uri(info.PageUrl);
        UpdateCard.Visibility = Visibility.Visible;
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        if (_pending == null) return;

        InstallBtn.IsEnabled = false;
        InstallRing.IsActive = true;
        var progress = new Progress<string>(m => Show(m, InfoBarSeverity.Informational));

        var (started, message) = await UpdateService.DownloadAndApplyAsync(_pending, progress);

        if (started)
        {
            Show(message, InfoBarSeverity.Success);
            await System.Threading.Tasks.Task.Delay(1500);   // let the user read it
            Application.Current.Exit();                       // exit so the update can finish
        }
        else
        {
            InstallRing.IsActive = false;
            InstallBtn.IsEnabled = true;
            Show(message, InfoBarSeverity.Error);
        }
    }

    private void Show(string msg, InfoBarSeverity sev)
    {
        UpdateInfoBar.Message = msg;
        UpdateInfoBar.Severity = sev;
        UpdateInfoBar.IsOpen = true;
    }
}
