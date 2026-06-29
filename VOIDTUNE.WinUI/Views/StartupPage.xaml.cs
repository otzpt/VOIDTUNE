using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VOIDTUNE.WinUI.Models;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class StartupPage : Page
{
    private readonly StartupManager _mgr = new();
    private bool _loading;

    public StartupPage()
    {
        this.InitializeComponent();
        Reload();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => Reload();

    private void Reload()
    {
        _loading = true;
        _mgr.Refresh();
        _loading = false;
        CountLine.Text = $"{_mgr.Items.Count} STARTUP ENTRIES";
    }

    private void Toggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        if (sender is ToggleSwitch { Tag: StartupItem it })
        {
            _loading = true;
            _mgr.SetEnabled(it, it.Enabled);
            _loading = false;
            CountLine.Text = $"{_mgr.Items.Count} STARTUP ENTRIES";
            StatusBar.Message = $"{it.Name}: {(it.Enabled ? "enabled" : "disabled")} at startup.";
            StatusBar.Severity = InfoBarSeverity.Success;
            StatusBar.IsOpen = true;
        }
    }
}
