using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VOIDTUNE.WinUI.Models;

namespace VOIDTUNE.WinUI.Services;

/// <summary>
/// Persists apply/revert results to disk so a failure can actually be diagnosed after the fact —
/// previously the only trace of a failed tweak was a transient "N failed" InfoBar with no detail,
/// which made remote bug reports (different hardware, different Windows build) unactionable.
/// </summary>
public static class ApplyLogService
{
    public static string LogDir { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VOIDTUNE", "logs");

    /// <summary>Writes a full result log (every tweak, ok or not) and returns the file path, or "" on failure.</summary>
    public static string WriteLog(string action, IReadOnlyList<(Tweak Tweak, bool Ok, string Output)> results)
    {
        try
        {
            Directory.CreateDirectory(LogDir);
            string file = Path.Combine(LogDir, $"{action}-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

            var sb = new StringBuilder();
            sb.AppendLine($"VOIDTUNE {UpdateService.CurrentVersion} — {action}");
            sb.AppendLine($"Timestamp: {DateTime.Now:O}");
            sb.AppendLine($"Windows: {Environment.OSVersion.Version}");
            sb.AppendLine($"CPU: {HardwareInfo.CpuName} ({HardwareInfo.CpuVendor})");
            sb.AppendLine($"GPU: {HardwareInfo.GpuName}");
            sb.AppendLine(new string('-', 60));
            foreach (var (t, ok, output) in results)
            {
                sb.AppendLine($"[{(ok ? "OK" : "FAILED")}] {t.Id} ({t.TierLabel}) — {t.Name}");
                if (!ok && !string.IsNullOrWhiteSpace(output))
                    sb.AppendLine("    " + output.Replace("\n", "\n    "));
            }

            File.WriteAllText(file, sb.ToString(), new UTF8Encoding(true));
            return file;
        }
        catch { return ""; }
    }

    /// <summary>
    /// Builds a pre-filled GitHub "New Issue" URL, meant to be opened in the user's own browser
    /// with their own GitHub account — deliberately NOT an automatic silent upload. A shipped app
    /// must never embed a personal access token to post on the maintainer's behalf; that token
    /// would be extractable from the binary by anyone. This keeps the report opt-in and
    /// attributable, at the cost of one extra click from the user.
    /// </summary>
    public static string BuildGitHubIssueUrl(IReadOnlyList<(Tweak Tweak, string Output)> failures, string logPath)
    {
        string title = $"[Apply failure] {failures.Count} tweak(s) failed on {HardwareInfo.CpuVendor}/{HardwareInfo.GpuName}";

        string Body(bool includeDetails)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"**VOIDTUNE** {UpdateService.CurrentVersion} — Windows {Environment.OSVersion.Version}");
            sb.AppendLine($"**CPU:** {HardwareInfo.CpuName} ({HardwareInfo.CpuVendor})");
            sb.AppendLine($"**GPU:** {HardwareInfo.GpuName}");
            sb.AppendLine();
            if (includeDetails)
            {
                sb.AppendLine("**Failed tweaks:**");
                foreach (var (t, output) in failures.Take(20))
                {
                    string snippet = string.IsNullOrWhiteSpace(output) ? "(no output)" : output.Split('\n')[0].Trim();
                    if (snippet.Length > 200) snippet = snippet[..200] + "…";
                    sb.AppendLine($"- `{t.Id}` **{t.Name}** — {snippet}");
                }
                if (failures.Count > 20) sb.AppendLine($"- …and {failures.Count - 20} more (see attached log)");
            }
            else
            {
                sb.AppendLine($"{failures.Count} tweak(s) failed — details truncated, please attach the log file below.");
            }
            sb.AppendLine();
            if (!string.IsNullOrEmpty(logPath))
                sb.AppendLine($"Full log saved locally at `{logPath}` — please attach it here if you can.");
            return sb.ToString();
        }

        string BuildUrl(string body) =>
            "https://github.com/otzpt/VOIDTUNE/issues/new" +
            $"?title={Uri.EscapeDataString(title)}" +
            $"&labels={Uri.EscapeDataString("bug,apply-failure")}" +
            $"&body={Uri.EscapeDataString(body)}";

        string url = BuildUrl(Body(includeDetails: true));

        // Browsers reliably handle a few KB of URL; stay well under to avoid a broken/truncated link.
        const int MaxUrlLength = 7000;
        if (url.Length > MaxUrlLength) url = BuildUrl(Body(includeDetails: false));
        return url;
    }
}
