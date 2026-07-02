using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using VOIDTUNE.WinUI.Models;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class TweaksPage : Page
{
    private readonly TweakEngine _engine = TweakEngine.Instance;
    private const string AllCategories = "All categories";

    /// <summary>Curated section order + look. Categories not listed fall to the end.</summary>
    private static readonly (string Key, string Display, string Glyph, string Hex)[] Sections =
    {
        ("Game",       "Gaming",           "\uE7FC", "#F472B6"),
        ("CPU",        "CPU",              "\uE950", "#A78BFA"),
        ("GPU",        "GPU",              "\uF211", "#B095F4"),
        ("RAM",        "Memory",           "\uEEA0", "#38BDF8"),
        ("Latency",    "Latency",          "\uE9D9", "#F59E0B"),
        ("Network",    "Network",          "\uE839", "#22D3EE"),
        ("Power",      "Power",            "\uE945", "#FBBF24"),
        ("Storage",    "Storage",          "\uE74E", "#34D399"),
        ("Processes",  "Processes",        "\uE9F5", "#2DD4BF"),
        ("Debloat",    "Debloat",          "\uE74D", "#FB923C"),
        ("Background", "Background",       "\uE823", "#94A3B8"),
        ("Privacy",    "Privacy",          "\uE72E", "#E879F9"),
        ("Audio",      "Audio",            "\uE767", "#4ADE80"),
        ("Restore",    "Restore defaults", "\uE90F", "#9CA3AF"),
    };

    public TweaksPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (CategoryBox.Items.Count == 0)
        {
            CategoryBox.Items.Add(AllCategories);
            foreach (var s in Sections.Where(s => _engine.Categories.Contains(s.Key)))
                CategoryBox.Items.Add(s.Display);
            CategoryBox.SelectedIndex = 0;
            TierBox.SelectedIndex = 0;   // selection-changed fires ApplyFilter
        }
        else
        {
            ApplyFilter();
        }
    }

    private void Category_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilter();
    private void Tier_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilter();

    private void Search_Changed(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput) ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (CategoryBox.Items.Count == 0) return;
        string catDisplay = CategoryBox.SelectedItem as string ?? AllCategories;
        int tierIdx = TierBox.SelectedIndex;     // 0 all, 1 safe, 2 extreme
        string q = (SearchBox.Text ?? "").Trim();

        bool Match(Tweak t) =>
            (tierIdx <= 0 || (int)t.Tier == tierIdx - 1) &&
            (q.Length == 0 ||
             t.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
             t.Description.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);

        var groups = new List<TweakGroup>();
        int shown = 0;

        foreach (var s in Sections)
        {
            if (catDisplay != AllCategories && s.Display != catDisplay) continue;
            var items = _engine.Tweaks.Where(t => t.Category == s.Key && Match(t)).ToList();
            if (items.Count == 0) continue;

            var g = new TweakGroup
            {
                Name = s.Display,
                Glyph = s.Glyph,
                Hex = s.Hex,
                BgHex = "#26" + s.Hex.TrimStart('#'),
                CountLabel = $"{items.Count} tweak{(items.Count == 1 ? "" : "s")}",
            };
            g.AddRange(items);
            groups.Add(g);
            shown += items.Count;
        }

        var cvs = new CollectionViewSource { IsSourceGrouped = true, Source = groups };
        TweakList.ItemsSource = cvs.View;
        UpdateCount(shown);
    }

    private void UpdateCount(int? shown = null)
        => CountLine.Text = $"{shown ?? _engine.Tweaks.Count} shown · {_engine.AppliedCount} applied";

    /// <summary>
    /// Toggling a tweak applies (on) or reverts (off) that single tweak immediately and persists it.
    /// The OneWay IsOn binding also raises Toggled when a list container is (re)realized or
    /// recycled, so we ignore any event where the switch already matches the tweak's applied state.
    /// </summary>
    private async void Tweak_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch ts || ts.Tag is not Tweak t) return;
        bool on = ts.IsOn;
        if (on == t.Applied) return;   // echo from binding / realization / recycling — not a user click

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
        ApplyFilter();
    }

    private async System.Threading.Tasks.Task RunApply(List<Tweak> tweaks, string label)
    {
        if (tweaks.Count == 0) { ShowStatus("Nothing to apply.", InfoBarSeverity.Warning); return; }

        var (ok, fail) = await RunWithProgress("Applying tweaks", p => _engine.ApplyAsync(tweaks, p));
        ShowStatus($"Applied {ok} {label} tweaks" + (fail > 0 ? $", {fail} failed." : "."),
                   fail > 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Success);
        ApplyFilter();
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

    private void ShowStatus(string msg, InfoBarSeverity sev)
    {
        StatusBar.Message = msg;
        StatusBar.Severity = sev;
        StatusBar.IsOpen = true;
    }
}
