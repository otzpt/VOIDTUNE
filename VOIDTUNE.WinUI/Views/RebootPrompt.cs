using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace VOIDTUNE.WinUI.Views;

/// <summary>
/// Shown after applying tweaks that only take full effect on restart (timers, GPU scheduling,
/// MSI mode, kernel settings). Applying those live leaves interrupt/timer handling half-applied,
/// which shows up as audio pops, DPC-latency stutter and small FPS drops until a reboot.
/// </summary>
public static class RebootPrompt
{
    /// <summary>Offers Reboot now / Reboot later. Returns true if a restart was started.</summary>
    public static async Task<bool> ShowAsync(XamlRoot root, int rebootTweakCount = 0)
    {
        string count = rebootTweakCount > 0
            ? $"{rebootTweakCount} of the tweaks you just applied "
            : "Some of the tweaks you applied ";

        var body = new StackPanel { Spacing = 10 };
        body.Children.Add(new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Text = count + "only take full effect after a restart — timers, GPU scheduling, " +
                   "MSI mode and kernel settings.",
        });
        body.Children.Add(new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.75,
            Text = "Rebooting later can cause audio pops, lag spikes and small FPS drops until you do, " +
                   "because the system is running half-applied.",
        });

        var dlg = new ContentDialog
        {
            Title = "Restart to finish applying",
            Content = body,
            PrimaryButtonText = "Reboot now",
            CloseButtonText = "Reboot later",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = root,
        };

        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return false;
        return Reboot();
    }

    private static bool Reboot()
    {
        try
        {
            // 8s grace so the user can close work; /c gives the shutdown reason text.
            Process.Start(new ProcessStartInfo("shutdown", "/r /t 8 /c \"VOIDTUNE: finishing tweak apply\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            return true;
        }
        catch { return false; }
    }
}
