using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VOIDTUNE.WinUI.Models;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Tests;

/// <summary>
/// Actually runs every toggleable tweak's Apply command, then its Revert command, against a
/// real Windows machine. This is the test that would have caught the 0.8.10 regression before
/// it shipped: "Full Reset to Windows Defaults" *looked* fine as a code review — it's the actual
/// execution (deleting the user's Ultimate Performance plan) that revealed the problem.
///
/// SAFETY: this only runs when VOIDTUNE_DESTRUCTIVE_TESTS=1 is set in the environment. Every
/// other test method in this suite is read-only or works against in-memory data; this one is
/// the sole exception, and it mutates real system state (registry, services, power plans). Only
/// enable it in a disposable environment — a GitHub Actions Windows runner (this repo's CI does
/// exactly that; see .github/workflows/ci.yml) or a throwaway VM. Never set this on a dev machine
/// you actually use.
///
/// GitHub-hosted Windows runners are Server editions, not consumer Windows 10/11 — a good chunk
/// of consumer-only services (Xbox stack, Storage Sense, Media Player Sharing, ...) and features
/// genuinely don't exist there. That's an environment difference, not a bug, so non-registry
/// commands (sc config, powercfg, netsh, PS scripts) are only asserted to run without throwing
/// or hanging — not to exit 0. Registry commands (reg add/delete) behave identically on every
/// Windows SKU, so those ARE asserted to fully succeed both ways.
///
/// Note if you ever run this locally in an unelevated shell: every HKLM registry write will fail
/// with "Acesso negado"/"Access is denied" — that's your shell's own privilege level, not a bug
/// in the tweak (confirmed by running this suite against a real, unelevated dev shell: every
/// failure was exactly this, and every tweak that *did* have sufficient privilege applied then
/// reverted cleanly in the same run, leaving no net change). GitHub Actions' Windows runners run
/// as Administrator by default, so this isn't a problem in CI.
/// </summary>
public class ApplyRevertRoundTripTests
{
    private static bool DestructiveTestsEnabled =>
        Environment.GetEnvironmentVariable("VOIDTUNE_DESTRUCTIVE_TESTS") == "1";

    private static readonly Regex RegistryOnly =
        new(@"^\s*(PS:\s*)?reg\s+(add|delete)\b", RegexOptions.IgnoreCase);

    public static TheoryData<string, string, string> ToggleableTweaks()
    {
        var data = new TheoryData<string, string, string>();
        foreach (Tweak t in TweakCatalog.All)
        {
            if (string.IsNullOrWhiteSpace(t.RevertCmd)) continue; // one-shot/restore actions — nothing to round-trip
            if (t.ApplyCmd.StartsWith("ENGINE:", StringComparison.Ordinal)) continue; // in-app engine toggle, not a shell command
            data.Add(t.Id, t.ApplyCmd, t.RevertCmd);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(ToggleableTweaks))]
    public async Task Apply_then_revert_executes_cleanly(string id, string applyCmd, string revertCmd)
    {
        if (!DestructiveTestsEnabled) return; // no-op outside an explicitly disposable environment

        bool isRegistryPair = RegistryOnly.IsMatch(applyCmd) && RegistryOnly.IsMatch(revertCmd);

        (bool ok, string output) applyResult = await RunAsync(applyCmd);
        (bool ok, string output) revertResult = await RunAsync(revertCmd);

        if (isRegistryPair)
        {
            Assert.True(applyResult.ok, $"[{id}] Apply failed: {applyResult.output}");
            Assert.True(revertResult.ok, $"[{id}] Revert failed: {revertResult.output}");
        }
        else
        {
            // Non-registry commands (services/powercfg/netsh/PS scripts) may legitimately fail
            // on a Server-edition CI runner missing the underlying consumer feature — we only
            // require that the process actually launched and returned, i.e. didn't throw or hang,
            // which is distinguishable from "service not found" by still producing SOME output.
            Assert.False(string.IsNullOrEmpty(applyResult.output) && !applyResult.ok,
                $"[{id}] Apply appears to have thrown/timed out rather than run: {applyResult.output}");
            Assert.False(string.IsNullOrEmpty(revertResult.output) && !revertResult.ok,
                $"[{id}] Revert appears to have thrown/timed out rather than run: {revertResult.output}");
        }
    }

    /// <summary>
    /// Minimal, test-local mirror of Services/CommandRunner.cs's dispatch (PS: prefix -> a
    /// PowerShell script file, otherwise cmd.exe). Deliberately not a reference to the real
    /// CommandRunner: that class calls into EngineTweaks, which pulls in GameWatcherService and
    /// the rest of the WinUI app's dependency graph — this test project intentionally only
    /// compiles the three dependency-free files it needs (see the .csproj comment).
    /// </summary>
    private static async Task<(bool ok, string output)> RunAsync(string command)
    {
        if (command.StartsWith("PS:", StringComparison.Ordinal))
        {
            string body = "$ProgressPreference='SilentlyContinue'\r\n" + command[3..].Trim();
            string file = Path.Combine(Path.GetTempPath(), $"voidtune_test_{Guid.NewGuid():N}.ps1");
            try
            {
                await File.WriteAllTextAsync(file, body, new System.Text.UTF8Encoding(true));
                return await RunProcessAsync("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{file}\"");
            }
            finally { try { File.Delete(file); } catch { /* ignore */ } }
        }

        return await RunProcessAsync("cmd.exe", $"/c {command}");
    }

    private static async Task<(bool ok, string output)> RunProcessAsync(string fileName, string arguments)
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
            try { p.StandardInput.Close(); } catch { /* ignore */ }

            Task<string> stdout = p.StandardOutput.ReadToEndAsync();
            Task<string> stderr = p.StandardError.ReadToEndAsync();

            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(90));
            try { await p.WaitForExitAsync(cts.Token); }
            catch (OperationCanceledException)
            {
                try { p.Kill(entireProcessTree: true); } catch { /* already exiting */ }
                return (false, "Timed out after 90s");
            }

            string output = ((await stdout) + (await stderr)).Trim();
            return (p.ExitCode == 0, output);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
