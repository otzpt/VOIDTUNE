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
            // The TwoWay IsOn binding also raises Toggled when a container is (re)realized,
            // not just on user clicks. Only act when the value actually changed from the last
            // committed state — otherwise scrolling/rebuilds would re-toggle entries in a loop.
            if (it.Enabled == it.Committed) return;

            bool enable = it.Enabled;
            it.Committed = enable;
            _mgr.SetEnabled(it, enable);   // updates the item in place; does NOT rebuild the list

            StatusBar.Message = $"{it.Name}: {(enable ? "enabled" : "disabled")} at startup.";
            StatusBar.Severity = InfoBarSeverity.Success;
            StatusBar.IsOpen = true;
        }
    }
}
