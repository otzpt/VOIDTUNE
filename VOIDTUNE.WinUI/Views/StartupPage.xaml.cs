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
        // DataContext, not Tag: during virtualized-list container recycling, WinUI can re-fire
        // Toggled (from the IsOn x:Bind refreshing) before a same-container Tag="{x:Bind}" has
        // been re-evaluated for the new item — Tag briefly points at the PREVIOUS row's item
        // while IsOn already reflects the new one, so the echo-guard below compares mismatched
        // objects and misfires on the wrong entry while scrolling. DataContext is set atomically
        // for the whole container before any child bindings re-evaluate, so it can't go stale.
        if (sender is not ToggleSwitch ts || ts.DataContext is not StartupItem it) return;

        // IsOn is bound OneWay, so the switch's value reflects a real user flip here; the model
        // (it.Enabled) still holds the previous state. When they already match, this Toggled came
        // from binding/container realization during scroll — ignore it. Same guard the Tweaks page
        // uses; TwoWay binding is what made this toggle behave inverted/randomly before.
        bool on = ts.IsOn;
        if (on == it.Enabled) return;

        _mgr.SetEnabled(it, on);   // apply to registry/folder, updates the item's label in place
        it.Enabled = on;           // sync the model; the OneWay binding re-reads it without looping
        it.Committed = on;

        StatusBar.Message = $"{it.Name}: {(on ? "enabled" : "disabled")} at startup.";
        StatusBar.Severity = InfoBarSeverity.Success;
        StatusBar.IsOpen = true;
    }
}
