using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using VOIDTUNE.WinUI.Converters;
using VOIDTUNE.WinUI.Models;

namespace VOIDTUNE.WinUI.Views;

/// <summary>
/// Shows exactly which tweaks a bulk action (Apply SAFE, a tier apply, etc.) is about to run,
/// each with its own checkbox, before anything touches the system. Nothing is applied until the
/// user confirms — and anything unchecked here is simply skipped. This exists because a one-click
/// "apply everything" action was, until now, invisible: users had no way to see or trim the list
/// without opening the Tweaks page and reading 150+ entries one by one.
/// </summary>
public static class ApplyPreviewDialog
{
    private static readonly HexToBrushConverter HexToBrush = new();

    /// <summary>Returns the subset the user left checked, or null if they cancelled.</summary>
    public static async Task<List<Tweak>?> ShowAsync(XamlRoot root, string title, string intro, IReadOnlyList<Tweak> tweaks)
    {
        var checks = new List<(CheckBox Box, Tweak Tweak)>();
        var rows = new StackPanel { Spacing = 2 };

        foreach (var t in tweaks.OrderBy(t => t.Category).ThenBy(t => t.Name))
        {
            var badge = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x22, 0, 0, 0)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 1, 6, 1),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = t.TierLabel,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = (SolidColorBrush)HexToBrush.Convert(t.TierHex, typeof(SolidColorBrush), null!, ""),
                },
            };

            var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            header.Children.Add(new TextBlock { Text = t.Name, FontWeight = FontWeights.SemiBold, FontSize = 13.5, VerticalAlignment = VerticalAlignment.Center });
            header.Children.Add(badge);

            var content = new StackPanel { Spacing = 2, MaxWidth = 460 };
            content.Children.Add(header);
            content.Children.Add(new TextBlock
            {
                Text = t.Description,
                Opacity = 0.62,
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap,
            });

            var cb = new CheckBox { IsChecked = true, Content = content, Margin = new Thickness(0, 3, 0, 3) };
            rows.Children.Add(cb);
            checks.Add((cb, t));
        }

        var scroller = new ScrollViewer
        {
            Content = rows,
            MaxHeight = 420,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };

        var countLabel = new TextBlock { FontFamily = new FontFamily("Consolas"), FontSize = 11, Opacity = 0.55 };
        void UpdateCount()
        {
            int n = checks.Count(c => c.Box.IsChecked == true);
            countLabel.Text = $"{n} of {checks.Count} selected";
        }
        UpdateCount();
        foreach (var (box, _) in checks) { box.Checked += (_, _) => UpdateCount(); box.Unchecked += (_, _) => UpdateCount(); }

        var selectAll = new CheckBox { Content = "Select all", IsChecked = true };
        bool suppressBulk = false;
        selectAll.Checked += (_, _) => { if (suppressBulk) return; foreach (var (box, _) in checks) box.IsChecked = true; };
        selectAll.Unchecked += (_, _) => { if (suppressBulk) return; foreach (var (box, _) in checks) box.IsChecked = false; };

        var topRow = new Grid();
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(selectAll, 0);
        Grid.SetColumn(countLabel, 1);
        countLabel.VerticalAlignment = VerticalAlignment.Center;
        topRow.Children.Add(selectAll);
        topRow.Children.Add(countLabel);

        var body = new StackPanel { Spacing = 12, MinWidth = 440 };
        if (!string.IsNullOrEmpty(intro))
            body.Children.Add(new TextBlock { Text = intro, TextWrapping = TextWrapping.Wrap, Opacity = 0.78, FontSize = 12.5 });
        body.Children.Add(topRow);
        body.Children.Add(scroller);

        var dlg = new ContentDialog
        {
            Title = title,
            Content = body,
            PrimaryButtonText = "Apply selected",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = root,
        };

        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return null;
        return checks.Where(c => c.Box.IsChecked == true).Select(c => c.Tweak).ToList();
    }

    /// <summary>Lightweight single-tweak confirmation for turning on an individual EXTREME tweak.</summary>
    public static async Task<bool> ConfirmExtremeAsync(XamlRoot root, Tweak t)
    {
        var body = new StackPanel { Spacing = 8, MaxWidth = 400 };
        body.Children.Add(new TextBlock
        {
            Text = "This is an EXTREME tweak — it's opt-in because its effect is more aggressive or system-dependent than the SAFE set.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.8,
            FontSize = 12.5,
        });
        body.Children.Add(new TextBlock { Text = t.Description, TextWrapping = TextWrapping.Wrap, FontSize = 12.5 });

        var dlg = new ContentDialog
        {
            Title = $"Apply \"{t.Name}\"?",
            Content = body,
            PrimaryButtonText = "Apply",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = root,
        };
        return await dlg.ShowAsync() == ContentDialogResult.Primary;
    }
}
