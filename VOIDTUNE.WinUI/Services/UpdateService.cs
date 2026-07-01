using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace VOIDTUNE.WinUI.Services;

public sealed record UpdateInfo(string Version, string Notes, string ZipUrl, string MsiUrl, string PageUrl);

/// <summary>
/// In-app updater. Checks the latest GitHub release, compares versions and applies the
/// update — MSI (msiexec major upgrade) for installed users, a self-extracting ZIP swap
/// for portable users.
/// </summary>
public static class UpdateService
{
    /// <summary>Current app version. Keep in sync with the .csproj &lt;Version&gt; and the release tag.</summary>
    public const string CurrentVersion = "0.8.5";

    private const string LatestApi = "https://api.github.com/repos/otzpt/VOIDTUNE/releases/latest";
    private const string RegPath = @"SOFTWARE\VOIDTUNE";

    /// <summary>The most recent update found by CheckAsync (so the Settings page can show it after navigation).</summary>
    public static UpdateInfo? Pending { get; private set; }

    /// <summary>True when launched from an MSI install (the installer writes InstallPath).</summary>
    public static bool IsInstalled
    {
        get
        {
            try
            {
                using var k = Registry.LocalMachine.OpenSubKey(RegPath);
                return k?.GetValue("InstallPath") is string p && p.Length > 0;
            }
            catch { return false; }
        }
    }

    /// <summary>Whether the startup auto-check is enabled (default on). Stored under HKCU.</summary>
    public static bool AutoCheckEnabled
    {
        get
        {
            try
            {
                using var k = Registry.CurrentUser.OpenSubKey(RegPath);
                return k?.GetValue("AutoUpdateCheck") is int v ? v != 0 : true;
            }
            catch { return true; }
        }
        set
        {
            try
            {
                using var k = Registry.CurrentUser.CreateSubKey(RegPath);
                k?.SetValue("AutoUpdateCheck", value ? 1 : 0, RegistryValueKind.DWord);
            }
            catch { /* ignore */ }
        }
    }

    /// <summary>Returns update info if the latest release is newer than the running build, else null.</summary>
    public static async Task<UpdateInfo?> CheckAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("VOIDTUNE-Updater");
            http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            string json = await http.GetStringAsync(LatestApi);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string tag = root.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "";
            string latest = tag.TrimStart('v', 'V');
            if (latest.Length == 0 || !IsNewer(latest, CurrentVersion)) return null;

            string notes = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
            string page = root.TryGetProperty("html_url", out var h) ? h.GetString() ?? "" : "";

            string zip = "", msi = "";
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in assets.EnumerateArray())
                {
                    string name = a.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    string url = a.TryGetProperty("browser_download_url", out var u) ? u.GetString() ?? "" : "";
                    if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) zip = url;
                    else if (name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase)) msi = url;
                }
            }
            Pending = new UpdateInfo(latest, notes, zip, msi, page);
            return Pending;
        }
        catch { return null; }
    }

    /// <summary>Downloads and starts applying the update. On success the caller must exit the app.</summary>
    public static async Task<(bool started, string message)> DownloadAndApplyAsync(UpdateInfo info, IProgress<string>? progress)
    {
        bool installed = IsInstalled;
        string url = installed && info.MsiUrl.Length > 0 ? info.MsiUrl : info.ZipUrl;
        if (url.Length == 0) url = info.MsiUrl.Length > 0 ? info.MsiUrl : info.ZipUrl;
        if (url.Length == 0) return (false, "No downloadable asset was found on the release.");

        bool isMsi = url.EndsWith(".msi", StringComparison.OrdinalIgnoreCase);
        string file = Path.Combine(Path.GetTempPath(), $"VOIDTUNE_update_{info.Version}{(isMsi ? ".msi" : ".zip")}");

        try
        {
            progress?.Report("Downloading update…");
            using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) })
            {
                http.DefaultRequestHeaders.UserAgent.ParseAdd("VOIDTUNE-Updater");
                var bytes = await http.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(file, bytes);
            }

            if (isMsi)
            {
                progress?.Report("Launching installer…");
                Process.Start(new ProcessStartInfo("msiexec.exe", $"/i \"{file}\"") { UseShellExecute = true });
            }
            else
            {
                progress?.Report("Preparing portable update…");
                LaunchPortableSwap(file);
            }
            return (true, "Update started. VOIDTUNE will now close to finish updating.");
        }
        catch (Exception ex)
        {
            return (false, $"Update failed: {ex.Message}");
        }
    }

    // Writes a helper script that waits for this process to exit, swaps the files and relaunches.
    private static void LaunchPortableSwap(string zipPath)
    {
        string appDir = AppContext.BaseDirectory.TrimEnd('\\');
        string exe = Path.Combine(appDir, "VOIDTUNE.exe");
        int pid = Environment.ProcessId;
        string script = Path.Combine(Path.GetTempPath(), "vt_portable_update.ps1");

        string body = string.Join("\n", new[]
        {
            "$ErrorActionPreference='SilentlyContinue'",
            $"while (Get-Process -Id {pid} -EA SilentlyContinue) {{ Start-Sleep -Milliseconds 400 }}",
            "Start-Sleep -Seconds 1",
            "$tmp = Join-Path $env:TEMP ('vt_unzip_' + [guid]::NewGuid().ToString('N'))",
            $"Expand-Archive -Path '{zipPath}' -DestinationPath $tmp -Force",
            // copy extracted tree over the app folder (handles an optional top-level folder)
            "$src = if ((Get-ChildItem $tmp -File).Count -eq 0 -and (Get-ChildItem $tmp -Directory).Count -eq 1) { (Get-ChildItem $tmp -Directory)[0].FullName } else { $tmp }",
            $"Copy-Item -Path (Join-Path $src '*') -Destination '{appDir}' -Recurse -Force",
            $"Remove-Item '{zipPath}' -Force; Remove-Item $tmp -Recurse -Force",
            $"Start-Process '{exe}'",
        });
        File.WriteAllText(script, body);

        Process.Start(new ProcessStartInfo("powershell.exe",
            $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{script}\"")
        { UseShellExecute = true });
    }

    private static bool IsNewer(string latest, string current)
    {
        var lv = Parse(latest);
        var cv = Parse(current);
        return lv != null && cv != null && lv > cv;
    }

    // Reads the leading numeric components (a.b.c.d) and ignores any pre-release suffix.
    // Uses all four fields so a four-part hotfix bump still registers as newer.
    private static Version? Parse(string v)
    {
        var nums = new List<int>();
        foreach (var part in v.Split('.', '-', '+', ' '))
        {
            if (int.TryParse(part, out int n)) nums.Add(n);
            else break;
        }
        while (nums.Count < 4) nums.Add(0);
        try { return new Version(nums[0], nums[1], nums[2], nums[3]); }
        catch { return null; }
    }
}
