using System.Collections.Generic;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Tests;

public class TweakVerificationTests
{
    private const string TwoValues =
        @"reg add ""HKCU\A"" /v One /t REG_DWORD /d 1 /f & reg add ""HKCU\B"" /v Two /t REG_DWORD /d 1 /f & exit /b 0";

    private static SystemProbes Probes(
        Dictionary<string, object>? values = null,
        Dictionary<string, int>? services = null,
        Dictionary<string, bool>? tasks = null) =>
        new(
            (key, name) => values is not null && values.TryGetValue($"{key}|{name}", out var v) ? v : null,
            name => services is not null && services.TryGetValue(name, out int s) ? s : null,
            name => tasks is not null && tasks.TryGetValue(name, out bool e) ? e : null);

    [Fact]
    public void A_chain_with_exit_zero_fails_when_a_later_step_did_not_apply()
    {
        var probes = Probes(new() { [@"HKCU\A|One"] = 1 });
        Assert.False(TweakVerifier.Verify(TwoValues, probes));
    }

    [Fact]
    public void A_chain_passes_only_when_every_step_applied()
    {
        var probes = Probes(new() { [@"HKCU\A|One"] = 1, [@"HKCU\B|Two"] = 1 });
        Assert.True(TweakVerifier.Verify(TwoValues, probes));
    }

    [Fact]
    public void A_deleted_value_counts_as_applied_only_when_gone()
    {
        const string cmd = @"reg delete ""HKCU\A"" /v One /f";
        Assert.True(TweakVerifier.Verify(cmd, Probes()));
        Assert.False(TweakVerifier.Verify(cmd, Probes(new() { [@"HKCU\A|One"] = 1 })));
    }

    [Fact]
    public void Service_start_type_is_compared()
    {
        const string cmd = "sc config Foo start= disabled & sc stop Foo & exit /b 0";
        Assert.True(TweakVerifier.Verify(cmd, Probes(services: new() { ["Foo"] = 4 })));
        Assert.False(TweakVerifier.Verify(cmd, Probes(services: new() { ["Foo"] = 2 })));
    }

    [Fact]
    public void A_service_this_windows_lacks_is_not_a_failure()
    {
        const string cmd = "sc config Gone start= disabled & sc config Foo start= disabled & exit /b 0";
        Assert.True(TweakVerifier.Verify(cmd, Probes(services: new() { ["Foo"] = 4 })));
    }

    [Fact]
    public void Task_state_is_compared()
    {
        const string cmd = @"schtasks /Change /TN ""Microsoft\T"" /Disable 2>nul & exit /b 0";
        Assert.True(TweakVerifier.Verify(cmd, Probes(tasks: new() { [@"Microsoft\T"] = false })));
        Assert.False(TweakVerifier.Verify(cmd, Probes(tasks: new() { [@"Microsoft\T"] = true })));
    }

    [Theory]
    [InlineData("PS:Set-Thing; exit 0")]
    [InlineData("powercfg /hibernate off")]
    [InlineData("netsh int tcp set global rsc=disabled")]
    public void Commands_with_nothing_checkable_return_null(string cmd)
    {
        Assert.Null(TweakVerifier.Verify(cmd, Probes()));
    }

    [Fact]
    public void A_tweak_is_obsolete_only_when_all_its_targets_are_gone()
    {
        const string cmd = "sc config Gone start= disabled & sc config Foo start= disabled & exit /b 0";
        Assert.True(TweakVerifier.IsObsolete(cmd, Probes(), canReadTasks: true));
        Assert.False(TweakVerifier.IsObsolete(cmd, Probes(services: new() { ["Foo"] = 3 }), canReadTasks: true));
    }

    [Fact]
    public void Registry_tweaks_are_never_obsolete()
    {
        Assert.False(TweakVerifier.IsObsolete(TwoValues, Probes(), canReadTasks: true));
    }

    [Fact]
    public void Tasks_are_not_called_obsolete_without_permission_to_read_them()
    {
        const string cmd = @"schtasks /Change /TN ""Microsoft\T"" /Disable";
        Assert.False(TweakVerifier.IsObsolete(cmd, Probes(), canReadTasks: false));
    }

    [Theory]
    [InlineData("AMD Ryzen 5 7600X 6-Core Processor", "Zen 4", false, false)]
    [InlineData("AMD Ryzen 7 9800X3D 8-Core Processor", "Zen 5", false, false)]
    [InlineData("AMD Ryzen 9 7950X3D 16-Core Processor", "Zen 4", true, false)]
    [InlineData("AMD Ryzen 9 9950X3D 16-Core Processor", "Zen 5", true, false)]
    [InlineData("13th Gen Intel(R) Core(TM) i9-13900K", "Intel 13th gen", false, true)]
    [InlineData("14th Gen Intel(R) Core(TM) i7-14700KF", "Intel 14th gen", false, true)]
    [InlineData("13th Gen Intel(R) Core(TM) i7-13700H", "Intel 13th gen", false, false)]
    [InlineData("Intel(R) Core(TM) i7-8750H CPU @ 2.20GHz", "Intel 8th gen", false, false)]
    [InlineData("Intel(R) Core(TM) Ultra 7 265K", "Core Ultra series 2", false, false)]
    public void Cpu_names_map_to_generation_and_constraints(string name, string generation, bool dualX3D, bool raptor)
    {
        var profile = CpuProfile.Parse(name);
        Assert.Equal(generation, profile.Generation);
        Assert.Equal(dualX3D, profile.IsDualCcdX3D);
        Assert.Equal(raptor, profile.IsRaptorLakeDesktop);
    }

    [Fact]
    public void Get_AppxPackage_is_never_given_a_list_of_names()
    {
        // -Name takes one string. A list throws, and the tweak's try/catch used to hide it.
        var array = new System.Text.RegularExpressions.Regex(@"Get-AppxPackage[^|;]*-Name\s+'[^']*'\s*,");
        foreach (var t in TweakCatalog.All)
        {
            Assert.False(array.IsMatch(t.ApplyCmd), t.Id);
            Assert.False(array.IsMatch(t.RevertCmd), t.Id);
        }
    }

    [Fact]
    public void Normal_ram_use_costs_no_health_points()
    {
        Assert.Equal(100, HealthScore.Compute(0, 48, 40, 190, rebootPending: false));
    }

    [Fact]
    public void Full_disk_and_pending_reboot_lower_health()
    {
        double score = HealthScore.Compute(0, 0, 95, 100, rebootPending: true);
        Assert.Equal(75, score);
    }

    [Theory]
    [InlineData("rundll32.exe shell32.dll,Foo", true)]
    [InlineData(@"""C:\Definitely\Not\Here\app.exe"" --flag", false)]
    [InlineData(@"C:\Definitely Not\Here\app.exe --flag", false)]
    [InlineData(@"C:\Windows\System32\notepad.exe --flag", true)]
    [InlineData(@"""C:\Windows\System32\notepad.exe"" --flag", true)]
    [InlineData("%windir%\\System32\\notepad.exe", true)]
    public void Startup_targets_are_judged_only_when_the_path_is_absolute(string command, bool expected)
    {
        Assert.Equal(expected, StartupTarget.Exists(command));
    }
}
