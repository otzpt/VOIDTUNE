using System;
using System.Threading.Tasks;
using VOIDTUNE.WinUI.Models;

namespace VOIDTUNE.WinUI.Services;

/// <summary>Installs / uninstalls apps through winget. Mirrors the App installer page of the PS edition.</summary>
public static class AppInstaller
{
    public static async Task<bool> IsWingetAvailableAsync()
    {
        var r = await CommandRunner.ExecAsync("where winget");
        return r.Ok && r.Output.Length > 0;
    }

    public static async Task<bool> InstallAsync(AppItem app)
    {
        app.Busy = true; app.Status = "installing…"; app.StatusHex = "#38BDF8";
        var r = await CommandRunner.ExecAsync(
            $"winget install --id {app.Id} -e --silent --accept-package-agreements --accept-source-agreements --disable-interactivity");
        bool ok = r.Ok || r.Output.IndexOf("already installed", StringComparison.OrdinalIgnoreCase) >= 0;
        app.Installed = ok;
        app.Status = ok ? "installed" : "failed";
        app.StatusHex = ok ? "#22C55E" : "#EF4444";
        app.Busy = false;
        return ok;
    }

    public static async Task<bool> UninstallAsync(AppItem app)
    {
        app.Busy = true; app.Status = "removing…"; app.StatusHex = "#F59E0B";
        var r = await CommandRunner.ExecAsync($"winget uninstall --id {app.Id} -e --silent --disable-interactivity");
        bool ok = r.Ok;
        app.Installed = !ok && app.Installed;
        app.Status = ok ? "removed" : "failed";
        app.StatusHex = ok ? "#3A3A3A" : "#EF4444";
        app.Busy = false;
        return ok;
    }

    /// <summary>Marks which catalog apps are already installed (single winget list call).</summary>
    public static async Task DetectInstalledAsync(System.Collections.Generic.IEnumerable<AppItem> apps)
    {
        var r = await CommandRunner.ExecAsync("winget list --disable-interactivity");
        if (!r.Ok) return;
        string list = r.Output;
        foreach (var a in apps)
        {
            if (list.IndexOf(a.Id, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                a.Installed = true;
                a.Status = "installed";
                a.StatusHex = "#22C55E";
            }
        }
    }
}
