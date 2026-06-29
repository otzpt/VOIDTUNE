using System;
using System.Diagnostics;
using System.IO;
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
            return RunAsync("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{command[3..].Trim()}\"");

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

    private static Task<CommandResult> RunAsync(string fileName, string arguments)
    {
        return Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var p = Process.Start(psi)!;
                string output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                p.WaitForExit();
                return new CommandResult(p.ExitCode == 0, output.Trim());
            }
            catch (Exception ex)
            {
                return new CommandResult(false, ex.Message);
            }
        });
    }
}
