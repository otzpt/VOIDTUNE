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

        if (command.StartsWith("PS:", StringComparison.Ordinal))
        {
            // Pass the script as a base64 -EncodedCommand instead of inline -Command "...".
            // Inline quoting breaks whenever the script itself contains double quotes (e.g. the
            // MSI-mode and write-cache tweaks), which silently corrupted those commands and made
            // them fail. EncodedCommand is quote-proof.
            string ps = command[3..].Trim();
            string b64 = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(ps));
            return RunAsync("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {b64}");
        }

        return RunAsync("cmd.exe", $"/c {command}");
    }

    /// <summary>Runs a multi-line script by writing it to a temp file (PowerShell .ps1 or cmd .bat).</summary>
    public static async Task<CommandResult> RunScriptAsync(string body, bool powershell)
    {
        string ext = powershell ? ".ps1" : ".bat";
        string file = Path.Combine(Path.GetTempPath(), $"vt_script_{Guid.NewGuid():N}{ext}");
        try
        {
            await File.WriteAllTextAsync(file, body);
            return powershell
                ? await RunAsync("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{file}\"")
                : await RunAsync("cmd.exe", $"/c \"{file}\"");
        }
        catch (Exception ex) { return new CommandResult(false, ex.Message); }
        finally { try { File.Delete(file); } catch { /* ignore */ } }
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
