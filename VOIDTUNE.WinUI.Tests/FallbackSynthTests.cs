using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Tests;

/// <summary>
/// The synthesized fallback is a second write-path to system state — a wrong synthesis would
/// silently set the wrong service to the wrong start type, which is worse than no fallback.
/// </summary>
public class FallbackSynthTests
{
    [Fact]
    public void Single_service_disable_maps_to_registry_start_4()
    {
        string fb = FallbackSynth.ForCommand("sc config Spooler start= disabled & sc stop Spooler & exit /b 0");
        Assert.Contains(@"HKLM\SYSTEM\CurrentControlSet\Services\Spooler", fb);
        Assert.Contains("/d 4", fb);
        Assert.EndsWith("exit /b 0", fb);
    }

    [Fact]
    public void Demand_maps_to_3_and_auto_maps_to_2()
    {
        Assert.Contains("/d 3", FallbackSynth.ForCommand("sc config lfsvc start= demand & exit /b 0"));
        Assert.Contains("/d 2", FallbackSynth.ForCommand("sc config SysMain start= auto & exit /b 0"));
    }

    [Fact]
    public void Multi_service_command_synthesizes_one_write_per_service()
    {
        string cmd = "sc config XblAuthManager start= disabled & sc config XblGameSave start= disabled & sc stop XblAuthManager & exit /b 0";
        string fb = FallbackSynth.ForCommand(cmd);
        Assert.Contains(@"Services\XblAuthManager", fb);
        Assert.Contains(@"Services\XblGameSave", fb);
    }

    [Fact]
    public void Delayed_auto_also_sets_the_delayed_flag()
    {
        string fb = FallbackSynth.ForCommand("sc config WSearch start= delayed-auto & sc start WSearch & exit /b 0");
        Assert.Contains("/d 2", fb);
        Assert.Contains("DelayedAutostart", fb);
    }

    [Fact]
    public void Non_service_commands_get_no_fallback()
    {
        Assert.Equal("", FallbackSynth.ForCommand(@"reg add ""HKLM\X"" /v Y /t REG_DWORD /d 1 /f"));
        Assert.Equal("", FallbackSynth.ForCommand("powercfg /hibernate off"));
        Assert.Equal("", FallbackSynth.ForCommand("PS:Get-Service | Out-Null"));
        Assert.Equal("", FallbackSynth.ForCommand("ENGINE:autoboost:on"));
        Assert.Equal("", FallbackSynth.ForCommand(""));
    }

    [Fact]
    public void Every_catalog_service_tweak_synthesizes_cleanly()
    {
        // The synthesizer must handle every real sc-config command in the shipped catalog
        // without producing malformed registry paths (no quotes, no spaces-only names).
        foreach (var t in TweakCatalog.All)
        {
            foreach (string cmd in new[] { t.ApplyCmd, t.RevertCmd })
            {
                string fb = FallbackSynth.ForCommand(cmd);
                if (fb.Length == 0) continue;
                Assert.DoesNotContain(@"Services\""", fb);
                Assert.DoesNotContain(@"Services\ ", fb);
                Assert.Contains("/v Start", fb);
            }
        }
    }
}
