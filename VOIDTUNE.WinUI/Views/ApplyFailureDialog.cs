using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VOIDTUNE.WinUI.Models;

namespace VOIDTUNE.WinUI.Views;

/// <summary>
/// Shown after an apply/revert with failures — lists exactly which tweaks failed and why
/// (previously only a "N failed" count was ever visible), and offers to open a pre-filled
/// GitHub issue with the details plus a pointer to the full log file on disk.
/// </summary>
public static class ApplyFailureDialog
{
    public static async Task ShowAsync(XamlRoot root, IReadOnlyList<(Tweak Tweak, string Output)> failures, string logPath)
    {
        if (failures.Count == 0) return;

        var body = new StackPanel { Spacing = 10 };
        body.Children.Add(new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Text = $"{failures.Count} tweak{(failures.Count == 1 ? "" : "s")} didn't apply:",
        });

        var list = new StackPanel { Spacing = 4, Margin = new Thickness(0, 4, 0, 4) };
        foreach (var (t, output) in failures)
        {
            string snippet = string.IsNullOrWhiteSpace(output) ? "(no output)" : output.Split('\n')[0].Trim();
            if (snippet.Length > 140) snippet = snippet[..140] + "…";
            list.Children.Add(new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Text = $"• {t.Name} — {snippet}",
            });
        }
        var scroller = new ScrollViewer { MaxHeight = 220, Content = list };
        body.Children.Add(scroller);

        if (!string.IsNullOrEmpty(logPath))
        {
            body.Children.Add(new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.7,
                FontSize = 12,
                Text = $"Full log saved to: {logPath}",
            });
        }

        var dlg = new ContentDialog
        {
            Title = "Some tweaks failed",
            Content = body,
            PrimaryButtonText = "Report on GitHub",
            SecondaryButtonText = "Open log folder",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = root,
        };

        var result = await dlg.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            OpenUrl(Services.ApplyLogService.BuildGitHubIssueUrl(failures, logPath));
        }
        else if (result == ContentDialogResult.Secondary && !string.IsNullOrEmpty(logPath))
        {
            OpenFolder(logPath);
        }
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* best effort — user can still find the log manually */ }
    }

    private static void OpenFolder(string filePath)
    {
        try { Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"") { UseShellExecute = true }); }
        catch { /* best effort */ }
    }
}
