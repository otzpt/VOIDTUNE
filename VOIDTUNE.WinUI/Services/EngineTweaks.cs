using System;

namespace VOIDTUNE.WinUI.Services;

/// <summary>
/// Tweaks that toggle in-app engines instead of running shell commands. Their ApplyCmd /
/// RevertCmd use an "ENGINE:name:on|off" pseudo-command that CommandRunner routes here,
/// so they flow through the normal apply/revert/verify pipeline like any other tweak.
/// </summary>
public static class EngineTweaks
{
    /// <summary>Handles "name:on|off" (the part after the ENGINE: prefix).</summary>
    public static CommandResult Exec(string spec)
    {
        try
        {
            var parts = spec.Split(':', 2);
            string name = parts[0].Trim().ToLowerInvariant();
            bool on = parts.Length > 1 && parts[1].Trim().Equals("on", StringComparison.OrdinalIgnoreCase);

            switch (name)
            {
                case "autoboost":
                    AppSettingsStore.AutoGameBoost = on;
                    if (on) GameWatcherService.Start();
                    else GameWatcherService.Stop();   // also restores any active boost
                    return new CommandResult(true, on
                        ? "Auto Game Boost enabled — fullscreen games are boosted automatically."
                        : "Auto Game Boost disabled — everything restored.");

                default:
                    return new CommandResult(false, $"Unknown engine tweak '{name}'.");
            }
        }
        catch (Exception ex)
        {
            return new CommandResult(false, "Engine tweak failed: " + ex.Message);
        }
    }
}
