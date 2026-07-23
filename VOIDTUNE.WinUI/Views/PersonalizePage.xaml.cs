using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using VOIDTUNE.WinUI.Models;
using VOIDTUNE.WinUI.Services;
using Windows.UI;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class PersonalizePage : Page
{
    /// <summary>Mod cards shown in the grid (filtered by search).</summary>
    public ObservableCollection<PersonalizeToggle> Mods { get; } = new();

    public PersonalizePage()
    {
        this.InitializeComponent();
        AccentGrid.ItemsSource = PersonalizeService.AccentColors;
        Refresh();
    }

    private void Refresh()
    {
        // Read the live state of every mod, then (re)build the visible list.
        foreach (var t in PersonalizeService.Toggles)
            t.Enabled = PersonalizeService.GetState(t.Id);
        ApplyFilter();
        UpdateAccentLabel();
    }

    private void Search_Changed(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput) ApplyFilter();
    }

    private void ApplyFilter()
    {
        string q = (SearchBox?.Text ?? "").Trim();
        Mods.Clear();
        foreach (var t in PersonalizeService.Toggles)
        {
            if (q.Length > 0 &&
                t.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 &&
                t.Description.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 &&
                t.Group.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
            Mods.Add(t);
        }
        int on = PersonalizeService.Toggles.Count(t => t.Enabled);
        CountLine.Text = $"{Mods.Count} shown · {on} on";
    }

    // Toggling a mod applies it immediately. The IsOn binding also fires Toggled when a card is
    // (re)realized while scrolling, so we ignore events where the switch already matches state.
    private async void Toggle_Changed(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // DataContext, not Tag: during virtualized-grid container recycling, WinUI can re-fire
        // Toggled (from the IsOn x:Bind refreshing) before a same-container Tag="{x:Bind}" has
        // been re-evaluated for the new item — Tag briefly points at the PREVIOUS mod while IsOn
        // already reflects the new one, so the echo-guard below compares mismatched objects and
        // silently flips the WRONG mod while scrolling. DataContext updates atomically for the
        // whole container before any child bindings re-evaluate, so it can't go stale like a peer
        // property binding can (same bug class fixed on the Tweaks/Startup pages).
        if (sender is not ToggleSwitch ts || ts.DataContext is not PersonalizeToggle t) return;
        bool on = ts.IsOn;
        if (on == t.Enabled) return;   // echo from binding / realization — not a user action

        // Debounce: even keyed off DataContext, a fast scroll can still catch a container
        // mid-recycle where IsOn reflects the new item but the rest of its bindings haven't
        // settled yet. A genuine click's mismatch is stable; a recycling artifact's resolves
        // within a frame or two on its own — re-check before acting so scrolling alone can't
        // flip a mod.
        await System.Threading.Tasks.Task.Delay(120);
        if (ts.DataContext != t || ts.IsOn != on || on == t.Enabled) return;

        ts.IsEnabled = false;
        await PersonalizeService.SetAsync(t.Id, on);
        t.Enabled = on;
        ts.IsEnabled = true;

        int cnt = PersonalizeService.Toggles.Count(x => x.Enabled);
        CountLine.Text = $"{Mods.Count} shown · {cnt} on";
        Show($"{t.Name}: {(on ? "ON" : "OFF")} — some changes need an Explorer restart or sign-out.", InfoBarSeverity.Success);
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

    private void UpdateAccentLabel()
    {
        string hex = PersonalizeService.GetCurrentAccentHex();
        CurrentAccentLabel.Text = PersonalizeService.AccentColors
            .FirstOrDefault(c => string.Equals(c.Hex, hex, StringComparison.OrdinalIgnoreCase))?.Name ?? hex;
        try
        {
            byte r = Convert.ToByte(hex.Substring(1, 2), 16);
            byte g = Convert.ToByte(hex.Substring(3, 2), 16);
            byte b = Convert.ToByte(hex.Substring(5, 2), 16);
            CurrentSwatch.Background = new SolidColorBrush(Color.FromArgb(255, r, g, b));
        }
        catch { /* ignore */ }
    }

    private void Show(string msg, InfoBarSeverity sev)
    {
        StatusBar.Message = msg;
        StatusBar.Severity = sev;
        StatusBar.IsOpen = true;
    }
}
