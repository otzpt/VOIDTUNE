using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;
using VOIDTUNE.WinUI.Models;
using VOIDTUNE.WinUI.Services;
using Windows.UI;
using Microsoft.UI.Xaml.Media;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class PersonalizePage : Page
{
    private bool _loading;

    public PersonalizePage()
    {
        this.InitializeComponent();
        AccentGrid.ItemsSource = PersonalizeService.AccentColors;
        Refresh();
    }

    private void Refresh()
    {
        _loading = true;
        foreach (var t in PersonalizeService.Toggles)
            t.Enabled = PersonalizeService.GetState(t.Id);

        // group by Group, preserving definition order
        var groups = PersonalizeService.Toggles
            .GroupBy(t => t.Group)
            .Select(g => new GroupInfo(g.Key, g.ToList()));
        GroupedToggles.Source = groups;

        UpdateAccentLabel();
        _loading = false;
    }

    private void UpdateAccentLabel()
    {
        string hex = PersonalizeService.GetCurrentAccentHex();
        CurrentAccentLabel.Text = PersonalizeService.AccentColors.FirstOrDefault(c => string.Equals(c.Hex, hex, System.StringComparison.OrdinalIgnoreCase))?.Name ?? hex;
        try
        {
            byte r = System.Convert.ToByte(hex.Substring(1, 2), 16);
            byte g = System.Convert.ToByte(hex.Substring(3, 2), 16);
            byte b = System.Convert.ToByte(hex.Substring(5, 2), 16);
            CurrentSwatch.Background = new SolidColorBrush(Color.FromArgb(255, r, g, b));
        }
        catch { /* ignore */ }
    }

    private async void Toggle_Changed(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_loading) return;
        if (sender is ToggleSwitch { Tag: PersonalizeToggle t })
        {
            await PersonalizeService.SetAsync(t.Id, t.Enabled);
            Show($"{t.Name}: {(t.Enabled ? "ON" : "OFF")} — some changes need Explorer restart / sign-out.", InfoBarSeverity.Success);
        }
    }

    private async void Accent_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is AccentColor c)
        {
            await PersonalizeService.SetAccentColorAsync(c.Hex);
            UpdateAccentLabel();
            Show($"Accent set to {c.Name} ({c.Hex}). Restart Explorer to apply fully.", InfoBarSeverity.Success);
        }
    }

    private void Show(string msg, InfoBarSeverity sev)
    {
        StatusBar.Message = msg;
        StatusBar.Severity = sev;
        StatusBar.IsOpen = true;
    }

    /// <summary>Grouping shim for CollectionViewSource (needs a Key + items).</summary>
    private sealed class GroupInfo : List<PersonalizeToggle>
    {
        public GroupInfo(string key, IEnumerable<PersonalizeToggle> items) : base(items) => Key = key;
        public string Key { get; }
    }
}
