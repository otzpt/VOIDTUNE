using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using VOIDTUNE.WinUI.Models;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class TweaksPage : Page
{
    private readonly TweakEngine _engine = TweakEngine.Instance;
    private readonly ObservableCollection<Tweak> _filtered = new();
    private const string AllCategories = "All categories";

    public TweaksPage()
    {
        this.InitializeComponent();
        TweakList.ItemsSource = _filtered;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (CategoryBox.Items.Count == 0)
        {
            CategoryBox.Items.Add(AllCategories);
            foreach (var c in _engine.Categories) CategoryBox.Items.Add(c);
            CategoryBox.SelectedIndex = 0;
            TierBox.SelectedIndex = 0;
        }
        else
        {
            ApplyFilter();
        }
    }

    private bool _suppressNuclearToggle;

    private void Category_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilter();
    private void Tier_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilter();

    private void Search_Changed(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput) ApplyFilter();
    }

    private async void Nuclear_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressNuclearToggle) return;

        var dlg = new ContentDialog
        {
            Title = "Reveal NUCLEAR tweaks?",
            Content = "NUCLEAR tweaks disable core Windows security — Defender real-time protection, " +
                      "SmartScreen, UAC, Core Isolation and the Firewall.\n\n" +
                      "These are for isolated machines you fully control. Everything stays revertible, " +
                      "but only enable this if you understand the risk.\n\nShow them anyway?",
            PrimaryButtonText = "Yes, I understand",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
        };

        if (await dlg.ShowAsync() == ContentDialogResult.Primary)
        {
            ApplyFilter();
        }
        else
        {
            // User backed out — untick without re-triggering the dialog.
            _suppressNuclearToggle = true;
            NuclearCheck.IsChecked = false;
            _suppressNuclearToggle = false;
        }
    }

    private void Nuclear_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_suppressNuclearToggle) return;
        // Deselect any nuclear tweaks so they can't be applied while hidden.
        foreach (var t in _engine.Tweaks.Where(t => t.Tier == TweakTier.Nuclear)) t.Selected = false;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (CategoryBox.Items.Count == 0) return;
        string cat = CategoryBox.SelectedItem as string ?? AllCategories;
        int tierIdx = TierBox.SelectedIndex;     // 0 all, 1 safe, 2 extreme, 3 nuclear
        string q = (SearchBox.Text ?? "").Trim();
        bool showNuclear = NuclearCheck.IsChecked == true;

        _filtered.Clear();
        foreach (var t in _engine.Tweaks)
        {
            if (t.Tier == TweakTier.Nuclear && !showNuclear) continue;   // hidden until explicitly revealed
            if (cat != AllCategories && t.Category != cat) continue;
            if (tierIdx > 0 && (int)t.Tier != tierIdx - 1) continue;
            if (q.Length > 0 &&
                t.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 &&
                t.Description.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
            _filtered.Add(t);
        }
        UpdateCount();
    }

    private void UpdateCount()
        => CountLine.Text = $"{_filtered.Count} shown · {_engine.AppliedCount} applied";

    /// <summary>
    /// Toggling a tweak applies (on) or reverts (off) that single tweak immediately and persists it.
    /// The TwoWay/OneWay IsOn binding also raises Toggled when a list container is (re)realized or
    /// recycled, so we ignore any event where the switch already matches the tweak's applied state.
    /// </summary>
    private async void Tweak_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch ts || ts.Tag is not Tweak t) return;
        bool on = ts.IsOn;
        if (on == t.Applied) return;   // echo from binding / realization / recycling — not a user click

        if (on && t.Tier == TweakTier.Nuclear && !await ConfirmNuclear(1))
        {
            ts.IsOn = false;           // user declined — undo the visual (guard skips the echo)
            return;
        }

        ts.IsEnabled = false;
        var (ok, fail) = on
            ? await _engine.ApplyAsync(new[] { t }, backup: false)   // single toggle: skip the heavy backup
            : await _engine.RevertAsync(new[] { t });
        ts.IsEnabled = true;

        ts.IsOn = t.Applied;           // sync switch to the real result (engine already saved state)
        UpdateCount();
        ShowStatus(fail > 0
            ? $"{t.Name}: couldn't {(on ? "apply" : "revert")}."
            : $"{t.Name} {(t.Applied ? "applied" : "reverted")} · saved.",
            fail > 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Success);
    }

    private async void ApplySafe_Click(object sender, RoutedEventArgs e)
    {
        var safe = _engine.Tweaks.Where(t => t.Tier == TweakTier.Safe).ToList();
        await RunApply(safe, "SAFE");
    }

    private async void RevertAll_Click(object sender, RoutedEventArgs e)
    {
        var applied = _engine.Tweaks.Where(t => t.Applied).ToList();
        if (applied.Count == 0) { ShowStatus("No applied tweaks to revert.", InfoBarSeverity.Informational); return; }

        var (ok, fail) = await RunWithProgress("Reverting tweaks", p => _engine.RevertAsync(applied, p));
        ShowStatus($"Reverted {ok} tweaks" + (fail > 0 ? $", {fail} failed." : "."), InfoBarSeverity.Informational);
        UpdateCount();
    }

    private async System.Threading.Tasks.Task RunApply(List<Tweak> tweaks, string label)
    {
        if (tweaks.Count == 0) { ShowStatus("Nothing to apply.", InfoBarSeverity.Warning); return; }

        var (ok, fail) = await RunWithProgress("Applying tweaks", p => _engine.ApplyAsync(tweaks, p));
        ShowStatus($"Applied {ok} {label} tweaks" + (fail > 0 ? $", {fail} failed." : "."),
                   fail > 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Success);
        UpdateCount();
    }

    /// <summary>
    /// Runs an apply/revert operation behind a modal dialog with a live progress bar, so the
    /// user gets clear "Applying 12 / 40" feedback instead of a tiny spinner. The op reports
    /// progress through the passed <see cref="IProgress{T}"/>.
    /// </summary>
    private async System.Threading.Tasks.Task<(int ok, int fail)> RunWithProgress(
        string title, Func<IProgress<TweakProgress>, System.Threading.Tasks.Task<(int ok, int fail)>> op)
    {
        var status = new TextBlock { Text = "Preparing…", Opacity = 0.85, TextWrapping = TextWrapping.Wrap };
        var bar = new ProgressBar { Minimum = 0, Maximum = 1, Value = 0, IsIndeterminate = true, Width = 340 };
        var panel = new StackPanel { Spacing = 14, MinWidth = 360 };
        panel.Children.Add(status);
        panel.Children.Add(bar);

        var dlg = new ContentDialog
        {
            Title = title,
            Content = panel,
            XamlRoot = this.XamlRoot,
        };

        var progress = new Progress<TweakProgress>(p =>
        {
            if (p.Total <= 0)
            {
                bar.IsIndeterminate = true;
                status.Text = p.Phase;
            }
            else
            {
                bar.IsIndeterminate = false;
                bar.Maximum = p.Total;
                bar.Value = p.Done;
                status.Text = $"{p.Phase}  {p.Done} / {p.Total}";
            }
        });

        _ = dlg.ShowAsync();
        try
        {
            return await op(progress);
        }
        finally
        {
            dlg.Hide();
        }
    }

    private async System.Threading.Tasks.Task<bool> ConfirmNuclear(int count)
    {
        var dlg = new ContentDialog
        {
            Title = "NUCLEAR tweaks selected",
            Content = $"You have {count} NUCLEAR tweak(s) selected. These disable core Windows security " +
                      "(SmartScreen, UAC, Core Isolation, Firewall). Everything is revertible. Continue?",
            PrimaryButtonText = "Apply anyway",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
        };
        return await dlg.ShowAsync() == ContentDialogResult.Primary;
    }

    private void ShowStatus(string msg, InfoBarSeverity sev)
    {
        StatusBar.Message = msg;
        StatusBar.Severity = sev;
        StatusBar.IsOpen = true;
    }
}
