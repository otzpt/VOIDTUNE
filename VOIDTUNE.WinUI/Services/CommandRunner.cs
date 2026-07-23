using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VOIDTUNE.WinUI.Services;

public readonly record struct CommandResult(bool Ok, string Output);

/// <summary>
/// Runs system commands. Mirrors RunC / RunPS / Exec-Cmd from the PowerShell edition:
/// commands prefixed "PS:" run through PowerShell, everything else through cmd.exe.
/// </summary>
public static class CommandRunner
{
    public static Task<CommandResult> ExecAsync(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return Task.FromResult(new CommandResult(true, "no-op"));

        // In-app engine tweaks (e.g. "ENGINE:autoboost:on") — no process, just a dispatch.
        if (command.StartsWith("ENGINE:", StringComparison.Ordinal))
            return Task.FromResult(EngineTweaks.Exec(command[7..]));

        if (command.StartsWith("PS:", StringComparison.Ordinal))
            return RunScriptAsync(command[3..].Trim(), powershell: true);

        return RunAsync("cmd.exe", $"/c {command}");
    }

    /// <summary>
    /// Runs a script by writing it to a readable temp file and executing it with -File.
    ///
    /// We deliberately do NOT use `powershell -EncodedCommand &lt;base64&gt;`: an app spawning
    /// base64-encoded PowerShell is one of the strongest antivirus heuristics (MITRE T1027 /
    /// T1059.001 — obfuscated script execution) and was getting VOIDTUNE flagged. A plain,
    /// human-readable .ps1 run with -File is just as quote-proof (the body lives in a file, so
    /// there's no shell-escaping to corrupt double quotes) but reads as an ordinary script.
    /// </summary>
    public static async Task<CommandResult> RunScriptAsync(string body, bool powershell)
    {
        string ext = powershell ? ".ps1" : ".bat";
        string file = Path.Combine(Path.GetTempPath(), $"voidtune_{Guid.NewGuid():N}{ext}");
        try
        {
            if (powershell)
                // Silence module-autoload progress (it lands on stderr as CLIXML and would
                // corrupt any output we parse as JSON, e.g. the Drivers page).
                body = "$ProgressPreference='SilentlyContinue'\r\n" + body;

            // UTF-8 with BOM so PowerShell/cmd decode non-ASCII in the script correctly.
            await File.WriteAllTextAsync(file, body, new System.Text.UTF8Encoding(true));
            return powershell
                ? await RunAsync(PowerShellPath, $"-NoProfile -ExecutionPolicy Bypass -File \"{file}\"")
                : await RunAsync("cmd.exe", $"/c \"{file}\"");
        }
        catch (Exception ex) { return new CommandResult(false, ex.Message); }
        finally { try { File.Delete(file); } catch { /* ignore */ } }
    }

    /// <summary>
    /// Fully-qualified path to Windows PowerShell. Field-confirmed real bug: invoking the bare
    /// "powershell.exe" name and relying on PATH search can resolve to something that isn't
    /// real PowerShell on some systems (App Execution Alias shadowing, third-party PATH entries
    /// ahead of System32) — the result still launches but then rejects -File with "the term
    /// '...ps1' is not recognized", which real PowerShell never does since -File is a core
    /// parameter it always understands. The System32 path can't be shadowed this way.
    /// </summary>
    private static readonly string PowerShellPath = ResolvePowerShellPath();

    private static string ResolvePowerShellPath()
    {
        string full = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell", "v1.0", "powershell.exe");
        return File.Exists(full) ? full : "powershell.exe"; // defensive fallback, should never trigger
    }

    /// <summary>
    /// Per-command ceiling. A few tweaks (WMI enumeration, fsutil usn) are legitimately slow,
    /// but nothing may block the batch forever — past this the process tree is killed and the
    /// command is reported as failed so the apply always finishes.
    /// </summary>
    private const int TimeoutSeconds = 90;

    private static async Task<CommandResult> RunAsync(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi)!;
            // Give the child an EOF on stdin so a command that unexpectedly prompts can't hang.
            try { p.StandardInput.Close(); } catch { /* ignore */ }

            // Read both pipes concurrently. Reading one to the end before the other deadlocks
            // the moment a command fills the other pipe's ~4 KB buffer — this was the "stuck at
            // N-of-M" hang: a couple of commands with chatty stderr never returned.
            Task<string> stdout = p.StandardOutput.ReadToEndAsync();
            Task<string> stderr = p.StandardError.ReadToEndAsync();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
            try
            {
                await p.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { p.Kill(entireProcessTree: true); } catch { /* already exiting */ }
                return new CommandResult(false, $"Timed out after {TimeoutSeconds}s");
            }

            string output = ((await stdout) + (await stderr)).Trim();
            return new CommandResult(p.ExitCode == 0, output);
        }
        catch (Exception ex)
        {
            return new CommandResult(false, ex.Message);
        }
    }
}
