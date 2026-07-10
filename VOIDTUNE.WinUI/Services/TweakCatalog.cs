using System.Collections.Generic;
using System.Linq;
using VOIDTUNE.WinUI.Models;

namespace VOIDTUNE.WinUI.Services;

/// <summary>
/// Full tweak catalog, ported 1:1 from modules/data.ps1 (base + architecture-specific).
/// Verbatim strings keep registry backslashes literal; "" is an embedded quote.
/// "PS:" prefix routes a command through PowerShell; otherwise it runs in cmd.exe.
/// Architecture tweaks (Intel/AMD/NVIDIA/laptop) are appended only on matching hardware.
/// </summary>
public static class TweakCatalog
{
    public static IReadOnlyList<Tweak> All { get; }

    // Static ctor runs after every static field initializer (incl. Baseline),
    // so Build() always sees a fully-populated Baseline array.
    static TweakCatalog() => All = Build();

    private static IReadOnlyList<Tweak> Build()
    {
        int build = HardwareInfo.WinBuild;
        // Software gate: drop tweaks whose feature only exists on a newer Windows than this one
        // (e.g. Windows 11-only tweaks on a Windows 10 machine). Hardware gating lives in ArchTweaks().
        var list = new List<Tweak>(Baseline.Where(t => build >= t.MinBuild));
        list.AddRange(ArchTweaks());
        return list;
    }

    private const int Win11 = 22000;   // first Windows 11 build

    // ── Architecture-specific, gated on detected hardware ────────────────────
    private static IEnumerable<Tweak> ArchTweaks()
    {
        var arch = new List<Tweak>();
        string cpu = HardwareInfo.CpuVendor;
        string gpu = HardwareInfo.GpuVendor;

        // ix1 "Intel Speed Shift EPP" and ix3 "Intel No SpeedStep" were REMOVED in 0.8.17 —
        // both wrote ValueMax/ValueMin onto the machine-wide *definition* of the PERFBOOSTMODE
        // setting (GUID be337238…, which is boost mode, not EPP — the tweak was mislabeled from
        // the start). ix1's ValueMax=0 clamped turbo boost OFF for every plan on every Intel
        // machine as a SAFE-tier default; field-confirmed as a plan-independent FPS deficit on
        // an i7-8750H (Balanced only recovered half the lost FPS until this override was
        // deleted). rst7 below removes the stale overrides for anyone who ever applied them.
        // Proper boost/EPP handling now lives in the VOIDTUNE Power Plan (pow10).
        if (cpu == "AMD")
        {
            arch.Add(new() { Id="ax1", Category="CPU", Tier=TweakTier.Safe, Name="AMD CPPC Perf", Description="[AMD] Set Collaborative Processor Performance to max.",
                ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v CpuEnergyPerfPref /t REG_DWORD /d 0 /f", RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v CpuEnergyPerfPref /f" });
            arch.Add(new() { Id="ax2", Category="CPU", Tier=TweakTier.Extreme, Name="AMD Boost Max", Description="[AMD] Maximum precision boost via power plan.",
                ApplyCmd="powercfg -setacvalueindex scheme_current sub_processor PERFBOOSTPOL 100 && powercfg -setactive scheme_current", RevertCmd="powercfg -setacvalueindex scheme_current sub_processor PERFBOOSTPOL 50 && powercfg -setactive scheme_current" });
        }
        if (gpu == "NVIDIA")
        {
            if (!HardwareInfo.IsLaptop)
            {
                // Desktop-only: this overrides NVIDIA's adaptive power state so the GPU sits at
                // max P-state continuously instead of backing off between demanding frames/scenes.
                // Confirmed same failure mode as pow7 (CPU): on a laptop's shared/limited thermal
                // budget this removes the GPU's own recovery window, so it throttles *harder* under
                // sustained load — a friend's GTX 1050 laptop hit 97C (throttle territory) running
                // hot, and switching the whole system to the Windows-recommended Balanced power plan
                // (letting the OS manage thermals again) gained ~100 FPS back over a "max performance" plan.
                arch.Add(new() { Id="nx1", Category="GPU", Tier=TweakTier.Safe, Name="NVIDIA Max Performance", Description="[DESKTOP] Force prefer-maximum-performance power mode. Desktop-only — on a laptop this keeps the GPU at max power state with no thermal recovery window, causing worse throttling under sustained load, not better FPS.",
                    ApplyCmd=@"reg add ""HKCU\Software\NVIDIA Corporation\Global\NvCplApi\Policies"" /v OverrideAdaptiveThreshold /t REG_DWORD /d 1 /f", RevertCmd=@"reg delete ""HKCU\Software\NVIDIA Corporation\Global\NvCplApi\Policies"" /v OverrideAdaptiveThreshold /f" });
            }
            arch.Add(new() { Id="nx2", Category="GPU", Tier=TweakTier.Safe, Name="NVIDIA No Telemetry", Description="[NVIDIA] Disable NVIDIA telemetry service and reporting.",
                ApplyCmd=@"sc config NvTelemetryContainer start= disabled & sc stop NvTelemetryContainer & reg add ""HKLM\SOFTWARE\NVIDIA Corporation\NvControlPanel2\Client"" /v OptInOrOutPreference /t REG_DWORD /d 0 /f & exit /b 0", RevertCmd="sc config NvTelemetryContainer start= auto & exit /b 0" });
            arch.Add(new() { Id="nx3", Category="GPU", Tier=TweakTier.Safe, Name="NVIDIA Shader Cache", Description="[NVIDIA] Ensure shader disk cache is on for faster loads.",
                ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v DisableShaderCache /t REG_DWORD /d 0 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v DisableShaderCache /t REG_DWORD /d 1 /f" });
            arch.Add(new() { Id="nx4", Category="GPU", Tier=TweakTier.Extreme, Name="NVIDIA HDCP Off", Description="[NVIDIA] Disable HDCP. Non-DRM displays only.",
                ApplyCmd=@"reg add ""HKCU\Software\NVIDIA Corporation\Global\NvCplApi\Policies"" /v HDCPEnabled /t REG_DWORD /d 0 /f", RevertCmd=@"reg add ""HKCU\Software\NVIDIA Corporation\Global\NvCplApi\Policies"" /v HDCPEnabled /t REG_DWORD /d 1 /f" });
            arch.Add(new() { Id="nx5", Category="GPU", Tier=TweakTier.Safe, Name="NVIDIA PCIe Max Link", Description="[NVIDIA] Force PCIe link to max speed, prevent throttling.",
                ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v NvCplEnableLinkedAdaptersAutoPowerDown /t REG_DWORD /d 0 /f", RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v NvCplEnableLinkedAdaptersAutoPowerDown /f" });
            arch.Add(new() { Id="nx6", Category="GPU", Tier=TweakTier.Extreme, Name="NVIDIA No Power Gating", Description="[NVIDIA] Disable GPU power gating, eliminate clock-up spikes.",
                ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v DisablePowerGating /t REG_DWORD /d 1 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v DisablePowerGating /t REG_DWORD /d 0 /f" });
        }
        if (gpu == "AMD")
        {
            if (!HardwareInfo.IsLaptop)
            {
                // Desktop-only — same reasoning as nx1 above: disabling the GPU core's deep-sleep
                // state removes its thermal recovery window, which backfires on a laptop's shared
                // CPU/GPU thermal budget.
                arch.Add(new() { Id="agx1", Category="GPU", Tier=TweakTier.Safe, Name="AMD GPU Deep Sleep Off", Description="[DESKTOP] Disable GPU core deep sleep for lower latency. Desktop-only — on a laptop this removes the GPU's thermal recovery window and causes worse throttling under sustained load.",
                    ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v PP_SclkDeepSleepDisable /t REG_DWORD /d 1 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v PP_SclkDeepSleepDisable /t REG_DWORD /d 0 /f" });
            }
            arch.Add(new() { Id="agx2", Category="GPU", Tier=TweakTier.Safe, Name="AMD Chill Off", Description="[AMD GPU] Disable Radeon Chill frame limiter.",
                ApplyCmd=@"reg add ""HKCU\Software\ATI\ACE\Settings\ADL\CWDDEPM"" /v ChillEnabled /t REG_DWORD /d 0 /f", RevertCmd=@"reg add ""HKCU\Software\ATI\ACE\Settings\ADL\CWDDEPM"" /v ChillEnabled /t REG_DWORD /d 1 /f" });
            arch.Add(new() { Id="agx3", Category="GPU", Tier=TweakTier.Extreme, Name="AMD ULPS Off", Description="[AMD GPU] Disable Ultra Low Power State, reduces stutter.",
                ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v EnableUlps /t REG_DWORD /d 0 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v EnableUlps /t REG_DWORD /d 1 /f" });
            arch.Add(new() { Id="agx4", Category="GPU", Tier=TweakTier.Extreme, Name="AMD No Energy Driver", Description="[AMD GPU] Disable energy driver power management.",
                ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v DisableDrmdmaPowerGating /t REG_DWORD /d 1 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v DisableDrmdmaPowerGating /t REG_DWORD /d 0 /f" });
        }
        if (HardwareInfo.IsLaptop)
        {
            arch.Add(new() { Id="lx1", Category="Power", Tier=TweakTier.Safe, Name="Laptop AC Performance", Description="[LAPTOP] Maximum performance on AC power.",
                ApplyCmd="powercfg -setacvalueindex scheme_current sub_processor PERFBOOSTPOL 100 && powercfg -setactive scheme_current", RevertCmd="powercfg -setacvalueindex scheme_current sub_processor PERFBOOSTPOL 50 && powercfg -setactive scheme_current" });

            // VOIDTUNE Power Plan — laptop profile. Built from real evidence, not folklore:
            // a field test on an i7-8750H Legion showed Balanced BEAT a max-performance plan by
            // ~100 FPS (thermal headroom > pinned clocks on cooling-limited hardware), so the
            // laptop plan starts from a BALANCED duplicate and adds only the settings that
            // reduce latency without generating sustained heat: core parking off (wake latency;
            // thermally neutral — best-effort: many systems declare CPMINCORES range 0-0 because
            // parking is already OS-managed there, and the write failing is benign, verified on
            // real hardware), USB selective suspend off, PCIe ASPM off. All writes are
            // AC-only (setacvalueindex) so battery behavior is untouched. Thermal governors
            // (min processor state, boost mode, EPP) are deliberately LEFT AT BALANCED DEFAULTS.
            // The plan is detected by the name we set ourselves, so it's locale-independent and
            // never duplicated on re-apply (the pow3 lesson).
            arch.Add(new() { Id="pow10", Category="Power", Tier=TweakTier.Safe, Name="VOIDTUNE Power Plan", Description="[LAPTOP] Create + activate the VOIDTUNE plan: Balanced's proven thermal behavior (a real laptop gained ~100 FPS going back to Balanced from max-performance) plus the latency fixes that don't generate heat — core parking off, USB selective suspend off, PCIe link power off. AC-only; battery behavior untouched.",
                ApplyCmd=@"PS:$ErrorActionPreference='SilentlyContinue'; $list=(powercfg -list | Out-String); $m=[regex]::Match($list,'([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})[^\r\n]*\(VOIDTUNE Performance\)'); if($m.Success){$g=$m.Groups[1].Value}else{ $o=(powercfg -duplicatescheme 381b4222-f694-41f0-9685-ff5bb260df2e | Out-String); $g=[regex]::Match($o,'[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}').Value; if($g){ powercfg -changename $g 'VOIDTUNE Performance' 'Hardware-aware plan by VOIDTUNE (laptop profile: Balanced thermals + latency fixes)' | Out-Null } }; if(-not $g){exit 1}; powercfg -setacvalueindex $g SUB_PROCESSOR CPMINCORES 100 | Out-Null; powercfg -setacvalueindex $g 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0 | Out-Null; powercfg -setacvalueindex $g SUB_PCIEXPRESS ASPM 0 | Out-Null; powercfg -setactive $g; exit 0",
                RevertCmd=@"PS:$ErrorActionPreference='SilentlyContinue'; powercfg -setactive 381b4222-f694-41f0-9685-ff5bb260df2e | Out-Null; $list=(powercfg -list | Out-String); $m=[regex]::Match($list,'([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})[^\r\n]*\(VOIDTUNE Performance\)'); if($m.Success){ powercfg -delete $m.Groups[1].Value | Out-Null }; exit 0" });
        }
        else
        {
            // Desktop-only: forcing PROCTHROTTLEMIN to 100 means the CPU never idles down, which
            // is fine with real case/cooler airflow but actively harmful on a laptop — it removes
            // the chip's thermal recovery window, so it soaks hotter and throttles *harder* under
            // sustained load than it would otherwise. Confirmed as a real regression (350->140 FPS
            // in Minecraft on an Intel 8th-gen + GTX 1050 laptop) before this gate was added.
            arch.Add(new() { Id="pow7", Category="Power", Tier=TweakTier.Safe, Name="CPU 100% Min/Max", Description="[DESKTOP] Lock CPU at 100% min/max so it never throttles under load. Desktop-only — on a laptop this removes thermal recovery time and causes worse throttling, not less.",
                ApplyCmd="powercfg -setacvalueindex scheme_current sub_processor PROCTHROTTLEMIN 100 && powercfg -setacvalueindex scheme_current sub_processor PROCTHROTTLEMAX 100 && powercfg -setactive scheme_current",
                RevertCmd="powercfg -setacvalueindex scheme_current sub_processor PROCTHROTTLEMIN 5 && powercfg -setacvalueindex scheme_current sub_processor PROCTHROTTLEMAX 100 && powercfg -setactive scheme_current" });
            arch.Add(new() { Id="cpu4", Category="CPU", Tier=TweakTier.Safe, Name="Perf Boost Mode", Description="[DESKTOP] Aggressive processor turbo boost. Desktop-only — \"Aggressive\" mode ramps to turbo clocks faster/more readily under any load, which brings a laptop to its thermal limit sooner under sustained gaming than the default \"Enabled\" boost mode.",
                ApplyCmd="powercfg -setacvalueindex scheme_current sub_processor PERFBOOSTMODE 2 && powercfg -setactive scheme_current", RevertCmd="powercfg -setacvalueindex scheme_current sub_processor PERFBOOSTMODE 1 && powercfg -setactive scheme_current" });

            // VOIDTUNE Power Plan — desktop profile. Duplicates Ultimate Performance (which
            // already ships min-state 100 / parking off) and layers the settings with the
            // strongest evidence from Microsoft's own power-tuning docs + community plans
            // (Bitsum, Calypto, WinUtil): boost mode Aggressive, EPP 0 (the lever modern
            // Speed-Shift/HWP silicon actually respects — no alias, hence the raw GUID),
            // parking off explicitly (best-effort: some systems lock CPMINCORES to range 0-0
            // because parking is already OS-managed; that write failing is benign, verified on
            // real hardware), USB selective suspend + PCIe ASPM off. IDLEDISABLE is
            // deliberately NOT here: community consensus is it adds ~30W idle heat, degrades
            // frame pacing, and its only defensible use is benchmark runs on serious cooling.
            // Locale-safe: detected/deduped by the name we set ourselves (the pow3 lesson).
            arch.Add(new() { Id="pow10", Category="Power", Tier=TweakTier.Safe, Name="VOIDTUNE Power Plan", Description="[DESKTOP] Create + activate the VOIDTUNE plan: Ultimate Performance base with Aggressive boost, max energy-performance preference, core parking off, USB selective suspend off, and PCIe link power off — every setting evidence-backed (Microsoft power-tuning docs, Bitsum, Calypto), and none of the folklore ones that hurt frame pacing (C-state/idle disable is deliberately excluded).",
                ApplyCmd=@"PS:$ErrorActionPreference='SilentlyContinue'; $list=(powercfg -list | Out-String); $m=[regex]::Match($list,'([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})[^\r\n]*\(VOIDTUNE Performance\)'); if($m.Success){$g=$m.Groups[1].Value}else{ $o=(powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 | Out-String); $g=[regex]::Match($o,'[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}').Value; if($g){ powercfg -changename $g 'VOIDTUNE Performance' 'Hardware-aware plan by VOIDTUNE (desktop profile: Ultimate base + evidence-backed latency settings)' | Out-Null } }; if(-not $g){exit 1}; powercfg -setacvalueindex $g SUB_PROCESSOR PERFBOOSTMODE 2 | Out-Null; powercfg -setacvalueindex $g SUB_PROCESSOR 36687f9e-e3a5-4dbf-b1dc-15eb381c6863 0 | Out-Null; powercfg -setacvalueindex $g SUB_PROCESSOR CPMINCORES 100 | Out-Null; powercfg -setacvalueindex $g SUB_PROCESSOR PROCTHROTTLEMIN 100 | Out-Null; powercfg -setacvalueindex $g 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0 | Out-Null; powercfg -setacvalueindex $g SUB_PCIEXPRESS ASPM 0 | Out-Null; powercfg -setactive $g; exit 0",
                RevertCmd=@"PS:$ErrorActionPreference='SilentlyContinue'; powercfg -setactive 381b4222-f694-41f0-9685-ff5bb260df2e | Out-Null; $list=(powercfg -list | Out-String); $m=[regex]::Match($list,'([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})[^\r\n]*\(VOIDTUNE Performance\)'); if($m.Success){ powercfg -delete $m.Groups[1].Value | Out-Null }; exit 0" });
        }

        // RAM-gated: caching tweaks that only help (and are only safe) with plenty of memory.
        double ram = 0;
        try { ram = HardwareInfo.TotalRamGb; } catch { /* leave 0 → skip */ }
        if (ram >= 16)
        {
        }
        if (ram >= 32)
        {
        }

        // Hybrid-CPU-gated: only meaningful when P-cores and E-cores coexist.
        if (VoidSchedulerService.IsHybridCpu)
        {
            arch.Add(new() { Id="hy1", NeedsReboot=true, Category="Game", Tier=TweakTier.Safe, Name="Prefer P-Cores for Foreground", Description="[HYBRID CPU] Bias the scheduler toward the performance cores for the app you're using. Pairs with Auto Game Boost.",
                ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel"" /v HeteroPolicy /t REG_DWORD /d 4 /f",
                RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel"" /v HeteroPolicy /f & exit /b 0" });
        }

        return arch;
    }

    // ── Baseline catalog (hardware-independent) ──────────────────────────────
    private static readonly Tweak[] Baseline =
    {
        // ── CPU ──────────────────────────────────────────────────────────────
        new() { Id="cpu2", Category="CPU", Tier=TweakTier.Safe, Name="Win32 Priority Separation", Description="Foreground apps get the largest CPU time slices.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl"" /v Win32PrioritySeparation /t REG_DWORD /d 38 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl"" /v Win32PrioritySeparation /t REG_DWORD /d 2 /f" },
        new() { Id="cpu7", Category="CPU", Tier=TweakTier.Extreme, Name="No Power Throttling", Description="Remove background CPU power limits.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Power\PowerThrottling"" /v PowerThrottlingOff /t REG_DWORD /d 1 /f", RevertCmd=@"reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Power\PowerThrottling"" /v PowerThrottlingOff /f" },
        new() { Id="cpu9", NeedsReboot=true, Category="CPU", Tier=TweakTier.Safe, Name="Distribute Timers", Description="Spread timer interrupts across all cores instead of core 0.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel"" /v DistributeTimers /t REG_DWORD /d 1 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel"" /v DistributeTimers /t REG_DWORD /d 0 /f" },
        new() { Id="cpu11", Category="CPU", Tier=TweakTier.Safe, Name="MMCSS Responsiveness", Description="Reserve only 10% of CPU for background so games/audio get the rest — without fully starving the audio engine (SystemResponsiveness=0 causes pops).",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v SystemResponsiveness /t REG_DWORD /d 10 /f", RevertCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v SystemResponsiveness /t REG_DWORD /d 20 /f" },

        // ── GPU ──────────────────────────────────────────────────────────────
        new() { Id="gpu1", NeedsReboot=true, Category="GPU", Tier=TweakTier.Extreme, Name="HW GPU Scheduling", Description="Enable hardware-accelerated GPU scheduling (HAGS). Helps on some systems, but is a coin-flip — on others it causes stutter and alt-tab lag. Opt-in; toggle off and reboot if you get hitching.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v HwSchMode /t REG_DWORD /d 2 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v HwSchMode /t REG_DWORD /d 1 /f" },
        new() { Id="gpu2", Category="GPU", Tier=TweakTier.Safe, Name="GPU Priority 8", Description="Raise GPU scheduling priority for games.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""GPU Priority"" /t REG_DWORD /d 8 /f", RevertCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""GPU Priority"" /t REG_DWORD /d 1 /f" },
        new() { Id="gpu3", Category="GPU", Tier=TweakTier.Safe, Name="TDR Delay", Description="Raise GPU timeout delay to prevent driver-recovery crashes.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v TdrDelay /t REG_DWORD /d 10 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v TdrDelay /t REG_DWORD /d 2 /f" },
        new() { Id="gpu4", Category="GPU", Tier=TweakTier.Safe, Name="Direct Flip", Description="Reduce display latency via forced direct flip.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v ForceDirectFlip /t REG_DWORD /d 1 /f", RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v ForceDirectFlip /f" },
        new() { Id="gpu5", Category="GPU", Tier=TweakTier.Extreme, Name="Disable MPO", Description="Fix Multi-Plane Overlay stutter. May flicker on some GPUs.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows\Dwm"" /v OverlayTestMode /t REG_DWORD /d 5 /f", RevertCmd=@"reg delete ""HKLM\SOFTWARE\Microsoft\Windows\Dwm"" /v OverlayTestMode /f" },
        new() { Id="gpu6", Category="GPU", Tier=TweakTier.Safe, Name="Async Present", Description="Enable asynchronous GPU presentation for lower frame latency.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v EnableAsyncPresentation /t REG_DWORD /d 1 /f", RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v EnableAsyncPresentation /f" },
        new() { Id="gpu7", Category="GPU", Tier=TweakTier.Safe, Name="D3D12 Independent Flip", Description="Force D3D12 independent flip, reduces DWM overhead.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v D3D12ForceIndependentFlip /t REG_DWORD /d 1 /f", RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v D3D12ForceIndependentFlip /f" },
        new() { Id="gpu8", Category="GPU", Tier=TweakTier.Safe, Name="Async Compute", Description="Enable asynchronous GPU compute for better utilisation.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v EnableAsyncCompute /t REG_DWORD /d 1 /f", RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v EnableAsyncCompute /f" },
        new() { Id="gpu9", Category="GPU", Tier=TweakTier.Safe, Name="No Framebuffer Compression", Description="Disable GPU framebuffer compression, reduces latency on some hardware.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v EnableFrameBufferCompression /t REG_DWORD /d 0 /f", RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v EnableFrameBufferCompression /f" },
        new() { Id="gpu10", Category="GPU", Tier=TweakTier.Safe, Name="DWM Queue Size", Description="Limit DWM queued buffers to 2 for lower display latency.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows\Dwm"" /v MaxQueuedBuffers /t REG_DWORD /d 2 /f", RevertCmd=@"reg delete ""HKLM\SOFTWARE\Microsoft\Windows\Dwm"" /v MaxQueuedBuffers /f" },

        // ── RAM ──────────────────────────────────────────────────────────────
        new() { Id="ram3", Category="RAM", Tier=TweakTier.Safe, Name="Clear Temp & DNS", Description="Flush temp files and the DNS cache (one-shot).",
            ApplyCmd=@"del /q /f /s ""%TEMP%\*"" 2>nul & ipconfig /flushdns & exit /b 0", RevertCmd="" },
        new() { Id="ram7", Category="RAM", Tier=TweakTier.Safe, Name="Optimal Page File", Description="Auto-size the pagefile to 1.5x RAM initial / 3x RAM max.",
            ApplyCmd=@"PS:$ram=[math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory/1MB); $i=[math]::Round($ram*1.5); $m=[math]::Round($ram*3); $pf=Get-WmiObject Win32_PageFileSetting; if($pf){$pf|%{$_.InitialSize=$i;$_.MaximumSize=$m;$_.Put()|Out-Null}}else{Set-WmiInstance -Class Win32_PageFileSetting -Arguments @{Name='C:\pagefile.sys';InitialSize=$i;MaximumSize=$m}|Out-Null}", RevertCmd="" },

        // ── Network ───────────────────────────────────────────────────────────
        new() { Id="net5", Category="Network", Tier=TweakTier.Extreme, Name="No Network Throttling", Description="Remove the QoS network-throttling index.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v NetworkThrottlingIndex /t REG_DWORD /d 4294967295 /f", RevertCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v NetworkThrottlingIndex /t REG_DWORD /d 10 /f" },
        new() { Id="net12", Category="Network", Tier=TweakTier.Safe, Name="Disable RSC", Description="Disable Receive Segment Coalescing, reduces latency spikes.",
            ApplyCmd="netsh int tcp set global rsc=disabled", RevertCmd="netsh int tcp set global rsc=enabled" },
        new() { Id="net13", Category="Network", Tier=TweakTier.Safe, Name="Low-Latency UDP", Description="Disable UDP Receive Offload — URO coalesces incoming UDP packets for throughput, which adds a little latency. Games (UDP) want them delivered immediately, so this keeps them uncoalesced.",
            ApplyCmd="netsh int udp set global uro=disabled", RevertCmd="netsh int udp set global uro=enabled" },

        // ── Debloat ───────────────────────────────────────────────────────────
        new() { Id="deb1", Category="Debloat", Tier=TweakTier.Safe, Name="Disable Telemetry", Description="Stop and disable the DiagTrack service.",
            ApplyCmd="sc config DiagTrack start= disabled & sc stop DiagTrack & exit /b 0", RevertCmd="sc config DiagTrack start= auto & sc start DiagTrack & exit /b 0" },
        new() { Id="deb2", Category="Debloat", Tier=TweakTier.Safe, Name="Block Ads", Description="Stop ContentDeliveryManager suggested content.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ContentDeliveryAllowed /t REG_DWORD /d 0 /f", RevertCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ContentDeliveryAllowed /t REG_DWORD /d 1 /f" },
        new() { Id="deb3", Category="Debloat", Tier=TweakTier.Safe, Name="Disable Cortana", Description="Remove Cortana and Bing search.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v AllowCortana /t REG_DWORD /d 0 /f", RevertCmd=@"reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v AllowCortana /f" },
        new() { Id="deb4", Category="Debloat", Tier=TweakTier.Safe, Name="Disable Game Bar", Description="Remove Xbox Game Bar capture overhead.",
            ApplyCmd=@"reg add ""HKCU\System\GameConfigStore"" /v GameDVR_Enabled /t REG_DWORD /d 0 /f", RevertCmd=@"reg add ""HKCU\System\GameConfigStore"" /v GameDVR_Enabled /t REG_DWORD /d 1 /f" },
        new() { Id="deb5", Category="Debloat", Tier=TweakTier.Safe, Name="Disable Animations", Description="Set visual effects to best performance.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects"" /v VisualFXSetting /t REG_DWORD /d 2 /f", RevertCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects"" /v VisualFXSetting /t REG_DWORD /d 0 /f" },
        new() { Id="deb7", Category="Debloat", Tier=TweakTier.Extreme, Name="Disable Superfetch", Description="Stop the SysMain preloading service.",
            ApplyCmd="sc config SysMain start= disabled & sc stop SysMain & exit /b 0", RevertCmd="sc config SysMain start= auto & sc start SysMain & exit /b 0" },
        new() { Id="deb8", Category="Debloat", Tier=TweakTier.Safe, Name="Disable Aero Peek", Description="Remove the hover-to-peek desktop effect.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\DWM"" /v EnableAeroPeek /t REG_DWORD /d 0 /f", RevertCmd=@"reg add ""HKCU\Software\Microsoft\Windows\DWM"" /v EnableAeroPeek /t REG_DWORD /d 1 /f" },
        new() { Id="deb9", Category="Debloat", Tier=TweakTier.Safe, Name="Disable Aero Shake", Description="Stop shake-to-minimize all windows.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v DisallowShaking /t REG_DWORD /d 1 /f", RevertCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v DisallowShaking /t REG_DWORD /d 0 /f" },
        new() { Id="deb10", Category="Debloat", Tier=TweakTier.Safe, Name="Disable Snap Assist", Description="Remove the snap-layout popup when dragging windows.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v SnapAssist /t REG_DWORD /d 0 /f", RevertCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v SnapAssist /t REG_DWORD /d 1 /f" },
        new() { Id="deb11", Category="Debloat", Tier=TweakTier.Safe, Name="Menu Delay = 0", Description="Instant menu open, no hover delay.",
            ApplyCmd=@"reg add ""HKCU\Control Panel\Desktop"" /v MenuShowDelay /t REG_SZ /d 0 /f", RevertCmd=@"reg add ""HKCU\Control Panel\Desktop"" /v MenuShowDelay /t REG_SZ /d 400 /f" },
        new() { Id="deb12", Category="Debloat", Tier=TweakTier.Safe, Name="No Thumbnail Cache", Description="Skip building thumbcache.db on network/temp drives.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v DisableThumbnailCache /t REG_DWORD /d 1 /f", RevertCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v DisableThumbnailCache /t REG_DWORD /d 0 /f" },
        new() { Id="deb13", Category="Debloat", Tier=TweakTier.Safe, Name="Disable Sticky Keys", Description="Turn off sticky/filter/toggle keys popups.",
            ApplyCmd=@"reg add ""HKCU\Control Panel\Accessibility\StickyKeys"" /v Flags /t REG_SZ /d 506 /f && reg add ""HKCU\Control Panel\Accessibility\ToggleKeys"" /v Flags /t REG_SZ /d 58 /f && reg add ""HKCU\Control Panel\Accessibility\Keyboard Response"" /v Flags /t REG_SZ /d 122 /f", RevertCmd=@"reg add ""HKCU\Control Panel\Accessibility\StickyKeys"" /v Flags /t REG_SZ /d 510 /f" },
        new() { Id="deb14", Category="Debloat", Tier=TweakTier.Safe, Name="Disable Startup Sound", Description="Mute the Windows boot/logon sound.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\BootAnimation"" /v DisableStartupSound /t REG_DWORD /d 1 /f & reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"" /v DisableStartupSound /t REG_DWORD /d 1 /f & exit /b 0", RevertCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\BootAnimation"" /v DisableStartupSound /t REG_DWORD /d 0 /f & exit /b 0" },
        new() { Id="deb15", Category="Debloat", Tier=TweakTier.Safe, Name="Disable Suggested Content", Description="Stop Settings-app suggestions and 'ways to finish setup' nags.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SubscribedContent-338393Enabled /t REG_DWORD /d 0 /f & reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SubscribedContent-353694Enabled /t REG_DWORD /d 0 /f & reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SubscribedContent-353696Enabled /t REG_DWORD /d 0 /f & reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\UserProfileEngagement"" /v ScoobeSystemSettingEnabled /t REG_DWORD /d 0 /f & exit /b 0", RevertCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SubscribedContent-338393Enabled /t REG_DWORD /d 1 /f & exit /b 0" },

        // ── Processes (background-process reduction) ────────────────────────────
        new() { Id="slim1", Category="Processes", Tier=TweakTier.Safe, MinBuild=Win11, Name="Disable Copilot", Description="Turn off Windows Copilot — removes its background host. (Windows 11)",
            ApplyCmd=@"reg add ""HKCU\Software\Policies\Microsoft\Windows\WindowsCopilot"" /v TurnOffWindowsCopilot /t REG_DWORD /d 1 /f & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot"" /v TurnOffWindowsCopilot /t REG_DWORD /d 1 /f & exit /b 0", RevertCmd=@"reg delete ""HKCU\Software\Policies\Microsoft\Windows\WindowsCopilot"" /v TurnOffWindowsCopilot /f & reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot"" /v TurnOffWindowsCopilot /f & exit /b 0" },
        new() { Id="slim2", Category="Processes", Tier=TweakTier.Safe, MinBuild=Win11, Name="Disable Widgets", Description="Disable the Widgets board — stops Widgets.exe / WebExperience host. (Windows 11)",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Dsh"" /v AllowNewsAndInterests /t REG_DWORD /d 0 /f & reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v TaskbarDa /t REG_DWORD /d 0 /f & exit /b 0", RevertCmd=@"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Dsh"" /v AllowNewsAndInterests /t REG_DWORD /d 1 /f & exit /b 0" },
        new() { Id="slim3", Category="Processes", Tier=TweakTier.Safe, Name="Edge Background Off", Description="Disable Edge startup boost and background mode — no idle msedge.exe.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Edge"" /v StartupBoostEnabled /t REG_DWORD /d 0 /f & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Edge"" /v BackgroundModeEnabled /t REG_DWORD /d 0 /f & exit /b 0", RevertCmd=@"reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Edge"" /v StartupBoostEnabled /f & reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Edge"" /v BackgroundModeEnabled /f & exit /b 0" },
        new() { Id="slim4", Category="Processes", Tier=TweakTier.Safe, Name="Disable OneDrive Sync", Description="Block OneDrive sync engine from auto-running.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Policies\Microsoft\OneDrive"" /v DisableFileSyncNGSC /t REG_DWORD /d 1 /f", RevertCmd=@"reg delete ""HKLM\SOFTWARE\Policies\Microsoft\OneDrive"" /v DisableFileSyncNGSC /f" },
        new() { Id="slim5", Category="Processes", Tier=TweakTier.Safe, Name="Disable Web Search", Description="Kill Bing/web results in Start — trims SearchHost web calls.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Search"" /v BingSearchEnabled /t REG_DWORD /d 0 /f & reg add ""HKCU\Software\Policies\Microsoft\Windows\Explorer"" /v DisableSearchBoxSuggestions /t REG_DWORD /d 1 /f & exit /b 0", RevertCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Search"" /v BingSearchEnabled /t REG_DWORD /d 1 /f & reg delete ""HKCU\Software\Policies\Microsoft\Windows\Explorer"" /v DisableSearchBoxSuggestions /f & exit /b 0" },
        new() { Id="slim6", Category="Processes", Tier=TweakTier.Safe, Name="Disable Clipboard History", Description="Turn off clipboard history sync service.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\System"" /v AllowClipboardHistory /t REG_DWORD /d 0 /f", RevertCmd=@"reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Windows\System"" /v AllowClipboardHistory /f" },
        new() { Id="slim7", Category="Processes", Tier=TweakTier.Safe, Name="Disable Spotlight & Tips", Description="Stop lockscreen Spotlight, tips and suggestion content jobs.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v RotatingLockScreenEnabled /t REG_DWORD /d 0 /f & reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SoftLandingEnabled /t REG_DWORD /d 0 /f & reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SystemPaneSuggestionsEnabled /t REG_DWORD /d 0 /f & reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SubscribedContent-338389Enabled /t REG_DWORD /d 0 /f & exit /b 0", RevertCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SystemPaneSuggestionsEnabled /t REG_DWORD /d 1 /f & exit /b 0" },
        new() { Id="slim8", Category="Processes", Tier=TweakTier.Safe, Name="Disable Storage Sense", Description="Stop the automatic Storage Sense cleanup task.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\StorageSense"" /v AllowStorageSenseGlobal /t REG_DWORD /d 0 /f", RevertCmd=@"reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Windows\StorageSense"" /v AllowStorageSenseGlobal /f" },
        new() { Id="slim9", Category="Processes", Tier=TweakTier.Safe, Name="Disable Geolocation Svc", Description="Stop the location framework service (lfsvc).",
            ApplyCmd="sc config lfsvc start= disabled & sc stop lfsvc & exit /b 0", RevertCmd="sc config lfsvc start= demand & exit /b 0" },
        new() { Id="slim10", Category="Processes", Tier=TweakTier.Safe, Name="Disable AllJoyn Router", Description="Stop the AllJoyn IoT router service (AJRouter).",
            ApplyCmd="sc config AJRouter start= disabled & sc stop AJRouter & exit /b 0", RevertCmd="sc config AJRouter start= demand & exit /b 0" },
        new() { Id="slim11", Category="Processes", Tier=TweakTier.Safe, Name="Disable NFC/Payments Svc", Description="Stop the Payments and NFC/SE manager (SEMgrSvc).",
            ApplyCmd="sc config SEMgrSvc start= disabled & sc stop SEMgrSvc & exit /b 0", RevertCmd="sc config SEMgrSvc start= demand & exit /b 0" },
        new() { Id="slim13", Category="Processes", Tier=TweakTier.Extreme, Name="Disable Compat Assistant", Description="Stop Program Compatibility Assistant service (PcaSvc).",
            ApplyCmd="sc config PcaSvc start= disabled & sc stop PcaSvc & exit /b 0", RevertCmd="sc config PcaSvc start= auto & sc start PcaSvc & exit /b 0" },
        new() { Id="slim14", Category="Processes", Tier=TweakTier.Extreme, Name="Disable Connected Devices", Description="Stop Connected Devices Platform service (CDPSvc) — many background callbacks.",
            ApplyCmd="sc config CDPSvc start= disabled & sc stop CDPSvc & exit /b 0", RevertCmd="sc config CDPSvc start= auto & sc start CDPSvc & exit /b 0" },
        new() { Id="slim15", Category="Processes", Tier=TweakTier.Extreme, Name="Disable Diagnostic Policy", Description="Stop the Diagnostic Policy Service (DPS) — heavy WdiServiceHost spawns.",
            ApplyCmd="sc config DPS start= disabled & sc stop DPS & exit /b 0", RevertCmd="sc config DPS start= auto & sc start DPS & exit /b 0" },
        new() { Id="slim16", Category="Processes", Tier=TweakTier.Safe, Name="Disable Error Reporting Svc", Description="Stop Windows Error Reporting service (WerSvc) — removes WerFault background work.",
            ApplyCmd="sc config WerSvc start= disabled & sc stop WerSvc & exit /b 0", RevertCmd="sc config WerSvc start= demand & exit /b 0" },
        new() { Id="slim17", Category="Processes", Tier=TweakTier.Safe, Name="Disable Link Tracking", Description="Stop Distributed Link Tracking (TrkWks) — tracks moved shortcuts, rarely needed.",
            ApplyCmd="sc config TrkWks start= disabled & sc stop TrkWks & exit /b 0", RevertCmd="sc config TrkWks start= auto & exit /b 0" },
        new() { Id="slim18", Category="Processes", Tier=TweakTier.Safe, Name="Disable UPnP / Network Discovery", Description="Stop SSDP Discovery, UPnP host, and Function Discovery (Resource Publication + Provider Host) — only needed for DLNA/media sharing and browsing other devices in Explorer's Network tab.",
            ApplyCmd="sc config SSDPSRV start= disabled & sc stop SSDPSRV & sc config upnphost start= disabled & sc stop upnphost & sc config FDResPub start= disabled & sc stop FDResPub & sc config fdPHost start= disabled & sc stop fdPHost & exit /b 0",
            RevertCmd="sc config SSDPSRV start= demand & sc config upnphost start= demand & sc config FDResPub start= demand & sc config fdPHost start= demand & exit /b 0" },
        new() { Id="slim19", Category="Processes", Tier=TweakTier.Safe, Name="Disable Smart Card Stack", Description="Stop SCardSvr / ScDeviceEnum / SCPolicySvc — no smart-card reader needed.",
            ApplyCmd="sc config SCardSvr start= disabled & sc stop SCardSvr & sc config ScDeviceEnum start= disabled & sc stop ScDeviceEnum & sc config SCPolicySvc start= disabled & sc stop SCPolicySvc & exit /b 0", RevertCmd="sc config SCardSvr start= demand & sc config ScDeviceEnum start= demand & sc config SCPolicySvc start= demand & exit /b 0" },
        new() { Id="slim20", Category="Processes", Tier=TweakTier.Safe, Name="Disable Image Acquisition", Description="Stop Windows Image Acquisition (stisvc) — only for scanners/old cameras.",
            ApplyCmd="sc config stisvc start= disabled & sc stop stisvc & exit /b 0", RevertCmd="sc config stisvc start= demand & exit /b 0" },
        new() { Id="slim21", Category="Processes", Tier=TweakTier.Safe, Name="Disable Phone Service", Description="Stop the Phone Service (PhoneSvc) — phone-call integration, usually idle.",
            ApplyCmd="sc config PhoneSvc start= disabled & sc stop PhoneSvc & exit /b 0", RevertCmd="sc config PhoneSvc start= demand & exit /b 0" },
        new() { Id="slim22", Category="Processes", Tier=TweakTier.Safe, Name="Disable Hotspot & Parental", Description="Stop Mobile Hotspot (icssvc) and Parental Controls (WpcMonSvc).",
            ApplyCmd="sc config icssvc start= disabled & sc stop icssvc & sc config WpcMonSvc start= disabled & sc stop WpcMonSvc & exit /b 0", RevertCmd="sc config icssvc start= demand & sc config WpcMonSvc start= demand & exit /b 0" },
        new() { Id="slim23", Category="Processes", Tier=TweakTier.Safe, Name="Disable Insider Service", Description="Stop the Windows Insider service (wisvc).",
            ApplyCmd="sc config wisvc start= disabled & sc stop wisvc & exit /b 0", RevertCmd="sc config wisvc start= demand & exit /b 0" },
        new() { Id="slim24", Category="Processes", Tier=TweakTier.Extreme, Name="Disable User Sync Services", Description="Disable per-user data services (OneSync, Messaging, Contacts/Unistore/UserData) — breaks Mail/Calendar/People sync.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\OneSyncSvc"" /v Start /t REG_DWORD /d 4 /f & reg add ""HKLM\SYSTEM\CurrentControlSet\Services\MessagingService"" /v Start /t REG_DWORD /d 4 /f & reg add ""HKLM\SYSTEM\CurrentControlSet\Services\PimIndexMaintenanceSvc"" /v Start /t REG_DWORD /d 4 /f & reg add ""HKLM\SYSTEM\CurrentControlSet\Services\UnistoreSvc"" /v Start /t REG_DWORD /d 4 /f & reg add ""HKLM\SYSTEM\CurrentControlSet\Services\UserDataSvc"" /v Start /t REG_DWORD /d 4 /f & exit /b 0", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\OneSyncSvc"" /v Start /t REG_DWORD /d 3 /f & reg add ""HKLM\SYSTEM\CurrentControlSet\Services\MessagingService"" /v Start /t REG_DWORD /d 3 /f & reg add ""HKLM\SYSTEM\CurrentControlSet\Services\PimIndexMaintenanceSvc"" /v Start /t REG_DWORD /d 3 /f & reg add ""HKLM\SYSTEM\CurrentControlSet\Services\UnistoreSvc"" /v Start /t REG_DWORD /d 3 /f & reg add ""HKLM\SYSTEM\CurrentControlSet\Services\UserDataSvc"" /v Start /t REG_DWORD /d 3 /f & exit /b 0" },
        new() { Id="slim25", Category="Processes", Tier=TweakTier.Extreme, Name="Disable CDP (Connected Devices Platform)", Description="Disable the base Connected Devices Platform service (CDPSvc) and its per-user instance (CDPUserSvc) — chatty background callbacks behind Nearby Sharing / Continue on PC.",
            ApplyCmd=@"sc config CDPSvc start= disabled & sc stop CDPSvc & reg add ""HKLM\SYSTEM\CurrentControlSet\Services\CDPUserSvc"" /v Start /t REG_DWORD /d 4 /f & exit /b 0",
            RevertCmd=@"sc config CDPSvc start= demand & reg add ""HKLM\SYSTEM\CurrentControlSet\Services\CDPUserSvc"" /v Start /t REG_DWORD /d 2 /f & exit /b 0" },
        new() { Id="slim26", Category="Processes", Tier=TweakTier.Extreme, Name="Disable Push Notifications", Description="Stop WpnService + per-user WpnUserService — disables toast notifications, removes their hosts.",
            ApplyCmd=@"sc config WpnService start= disabled & sc stop WpnService & reg add ""HKLM\SYSTEM\CurrentControlSet\Services\WpnUserService"" /v Start /t REG_DWORD /d 4 /f & exit /b 0", RevertCmd=@"sc config WpnService start= auto & sc start WpnService & reg add ""HKLM\SYSTEM\CurrentControlSet\Services\WpnUserService"" /v Start /t REG_DWORD /d 2 /f & exit /b 0" },
        new() { Id="slim27", Category="Processes", Tier=TweakTier.Extreme, Name="Disable Update Orchestrator", Description="Stop UsoSvc + wuauserv — no Windows Updates until reverted. Removes update background hosts.",
            ApplyCmd="sc config UsoSvc start= disabled & sc stop UsoSvc & sc config wuauserv start= disabled & sc stop wuauserv & exit /b 0", RevertCmd="sc config UsoSvc start= demand & sc config wuauserv start= demand & sc start wuauserv & exit /b 0" },

        // ── Debloat: remove bloatware UWP apps (no security impact; Xbox left intact for gamers) ──
        new() { Id="appx1", Category="Processes", Tier=TweakTier.Safe, Name="Hide Security Tray Icon", Description="Stop SecurityHealthSystray.exe at logon. Windows Defender protection stays fully ON — only the tray icon process is removed.",
            ApplyCmd=@"reg delete ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"" /v SecurityHealth /f 2>nul & exit /b 0", RevertCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"" /v SecurityHealth /t REG_EXPAND_SZ /d ""%windir%\system32\SecurityHealthSystray.exe"" /f" },
        new() { Id="appx2", Category="Processes", Tier=TweakTier.Extreme, Name="Remove Bing News & Weather", Description="Uninstall the Bing News and Weather apps — kills their background tile/feed updaters.",
            ApplyCmd="PS:try { Get-AppxPackage -Name '*Microsoft.BingNews*','*Microsoft.BingWeather*' | Remove-AppxPackage -EA SilentlyContinue } catch {}; exit 0", RevertCmd="PS:try { Get-AppxPackage -AllUsers -Name '*Microsoft.BingNews*','*Microsoft.BingWeather*' | ForEach-Object { Add-AppxPackage -DisableDevelopmentMode -Register (Join-Path $_.InstallLocation 'AppXManifest.xml') -EA SilentlyContinue } } catch {}; exit 0" },
        new() { Id="appx3", Category="Processes", Tier=TweakTier.Extreme, Name="Remove Phone Link", Description="Uninstall Phone Link (YourPhone) — removes PhoneExperienceHost background process.",
            ApplyCmd="PS:try { Get-AppxPackage -Name '*Microsoft.YourPhone*' | Remove-AppxPackage -EA SilentlyContinue } catch {}; exit 0", RevertCmd="PS:try { Get-AppxPackage -AllUsers -Name '*Microsoft.YourPhone*' | ForEach-Object { Add-AppxPackage -DisableDevelopmentMode -Register (Join-Path $_.InstallLocation 'AppXManifest.xml') -EA SilentlyContinue } } catch {}; exit 0" },
        new() { Id="appx4", Category="Processes", Tier=TweakTier.Extreme, Name="Remove Groove & Movies", Description="Uninstall Groove Music (ZuneMusic) and Movies & TV (ZuneVideo).",
            ApplyCmd="PS:try { Get-AppxPackage -Name '*Microsoft.ZuneMusic*','*Microsoft.ZuneVideo*' | Remove-AppxPackage -EA SilentlyContinue } catch {}; exit 0", RevertCmd="PS:try { Get-AppxPackage -AllUsers -Name '*Microsoft.ZuneMusic*','*Microsoft.ZuneVideo*' | ForEach-Object { Add-AppxPackage -DisableDevelopmentMode -Register (Join-Path $_.InstallLocation 'AppXManifest.xml') -EA SilentlyContinue } } catch {}; exit 0" },
        new() { Id="appx5", Category="Processes", Tier=TweakTier.Extreme, Name="Remove Teams (Chat)", Description="Uninstall the consumer Teams / Chat app — removes its background host.",
            ApplyCmd="PS:try { Get-AppxPackage -Name '*MicrosoftTeams*','*MSTeams*' | Remove-AppxPackage -EA SilentlyContinue } catch {}; exit 0", RevertCmd="PS:try { Get-AppxPackage -AllUsers -Name '*MicrosoftTeams*','*MSTeams*' | ForEach-Object { Add-AppxPackage -DisableDevelopmentMode -Register (Join-Path $_.InstallLocation 'AppXManifest.xml') -EA SilentlyContinue } } catch {}; exit 0" },
        new() { Id="appx6", Category="Processes", Tier=TweakTier.Extreme, Name="Remove Clipchamp", Description="Uninstall the Clipchamp video editor app.",
            ApplyCmd="PS:try { Get-AppxPackage -Name '*Clipchamp.Clipchamp*' | Remove-AppxPackage -EA SilentlyContinue } catch {}; exit 0", RevertCmd="PS:try { Get-AppxPackage -AllUsers -Name '*Clipchamp.Clipchamp*' | ForEach-Object { Add-AppxPackage -DisableDevelopmentMode -Register (Join-Path $_.InstallLocation 'AppXManifest.xml') -EA SilentlyContinue } } catch {}; exit 0" },
        new() { Id="appx7", Category="Processes", Tier=TweakTier.Extreme, Name="Remove Help & Get Started", Description="Uninstall the Get Help and Tips/Get Started apps.",
            ApplyCmd="PS:try { Get-AppxPackage -Name '*Microsoft.GetHelp*','*Microsoft.Getstarted*' | Remove-AppxPackage -EA SilentlyContinue } catch {}; exit 0", RevertCmd="PS:try { Get-AppxPackage -AllUsers -Name '*Microsoft.GetHelp*','*Microsoft.Getstarted*' | ForEach-Object { Add-AppxPackage -DisableDevelopmentMode -Register (Join-Path $_.InstallLocation 'AppXManifest.xml') -EA SilentlyContinue } } catch {}; exit 0" },
        new() { Id="appx8", Category="Processes", Tier=TweakTier.Extreme, Name="Remove People & Feedback", Description="Uninstall the People app and the Feedback Hub.",
            ApplyCmd="PS:try { Get-AppxPackage -Name '*Microsoft.People*','*Microsoft.WindowsFeedbackHub*' | Remove-AppxPackage -EA SilentlyContinue } catch {}; exit 0", RevertCmd="PS:try { Get-AppxPackage -AllUsers -Name '*Microsoft.People*','*Microsoft.WindowsFeedbackHub*' | ForEach-Object { Add-AppxPackage -DisableDevelopmentMode -Register (Join-Path $_.InstallLocation 'AppXManifest.xml') -EA SilentlyContinue } } catch {}; exit 0" },
        new() { Id="appx9", Category="Processes", Tier=TweakTier.Extreme, Name="Remove Maps, To Do, Office Hub", Description="Uninstall Windows Maps, Microsoft To Do and the Office hub stub.",
            ApplyCmd="PS:try { Get-AppxPackage -Name '*Microsoft.WindowsMaps*','*Microsoft.Todos*','*Microsoft.MicrosoftOfficeHub*' | Remove-AppxPackage -EA SilentlyContinue } catch {}; exit 0", RevertCmd="PS:try { Get-AppxPackage -AllUsers -Name '*Microsoft.WindowsMaps*','*Microsoft.Todos*','*Microsoft.MicrosoftOfficeHub*' | ForEach-Object { Add-AppxPackage -DisableDevelopmentMode -Register (Join-Path $_.InstallLocation 'AppXManifest.xml') -EA SilentlyContinue } } catch {}; exit 0" },
        new() { Id="appx10", Category="Processes", Tier=TweakTier.Extreme, Name="Remove Cortana & Mixed Reality", Description="Uninstall the Cortana app and the Mixed Reality Portal.",
            ApplyCmd="PS:try { Get-AppxPackage -Name '*549981C3F5F10*','*Microsoft.MixedReality.Portal*' | Remove-AppxPackage -EA SilentlyContinue } catch {}; exit 0", RevertCmd="PS:try { Get-AppxPackage -AllUsers -Name '*549981C3F5F10*','*Microsoft.MixedReality.Portal*' | ForEach-Object { Add-AppxPackage -DisableDevelopmentMode -Register (Join-Path $_.InstallLocation 'AppXManifest.xml') -EA SilentlyContinue } } catch {}; exit 0" },
        new() { Id="appx11", Category="Processes", Tier=TweakTier.Extreme, Name="Remove Promoted Junk (Candy Crush etc.)", Description="Uninstall the auto-installed promoted apps Windows drops on you — Candy Crush & other King games, Disney+, TikTok, Facebook, Instagram, Netflix, Prime Video, Spotify stub, LinkedIn, Solitaire, Duolingo and friends. One toggle. Reinstall any from the Store if you actually want it.",
            ApplyCmd="PS:try { Get-AppxPackage -Name '*king.com*','*CandyCrush*','*BubbleWitch*','*FarmHeroes*','*Disney*','*BytedancePte.TikTok*','*Facebook*','*Instagram*','*4DF9E0F8.Netflix*','*Netflix*','*AmazonVideo.PrimeVideo*','*SpotifyAB.SpotifyMusic*','*7EE7776C.LinkedInforWindows*','*Microsoft.MicrosoftSolitaireCollection*','*Duolingo*','*PandoraMediaInc*','*Flipboard*','*Twitter*','*PicsArt*' | Remove-AppxPackage -EA SilentlyContinue } catch {}; exit 0",
            RevertCmd="PS:try { Get-AppxPackage -AllUsers -Name '*king.com*','*Disney*','*TikTok*','*Facebook*','*Instagram*','*Netflix*','*PrimeVideo*','*SpotifyMusic*','*LinkedIn*','*SolitaireCollection*' | ForEach-Object { Add-AppxPackage -DisableDevelopmentMode -Register (Join-Path $_.InstallLocation 'AppXManifest.xml') -EA SilentlyContinue } } catch {}; exit 0" },
        new() { Id="appx12", Category="Processes", Tier=TweakTier.Extreme, Name="Remove Dev Home, Family & Journal", Description="Uninstall Dev Home, Family Safety, Journal and Whiteboard — preinstalled apps with background components most people never open.",
            ApplyCmd="PS:try { Get-AppxPackage -Name '*Microsoft.Windows.DevHome*','*MicrosoftCorporationII.MicrosoftFamily*','*Microsoft.MicrosoftJournal*','*Microsoft.Whiteboard*' | Remove-AppxPackage -EA SilentlyContinue } catch {}; exit 0",
            RevertCmd="PS:try { Get-AppxPackage -AllUsers -Name '*Microsoft.Windows.DevHome*','*MicrosoftFamily*','*Microsoft.Whiteboard*' | ForEach-Object { Add-AppxPackage -DisableDevelopmentMode -Register (Join-Path $_.InstallLocation 'AppXManifest.xml') -EA SilentlyContinue } } catch {}; exit 0" },
        new() { Id="appx13", Category="Processes", Tier=TweakTier.Extreme, Name="Remove Quick Assist & New Outlook", Description="Uninstall the Quick Assist remote-help app and the promoted 'new Outlook' web-wrapper stub.",
            ApplyCmd="PS:try { Get-AppxPackage -Name '*MicrosoftCorporationII.QuickAssist*','*Microsoft.OutlookForWindows*' | Remove-AppxPackage -EA SilentlyContinue } catch {}; exit 0",
            RevertCmd="PS:try { Get-AppxPackage -AllUsers -Name '*QuickAssist*','*OutlookForWindows*' | ForEach-Object { Add-AppxPackage -DisableDevelopmentMode -Register (Join-Path $_.InstallLocation 'AppXManifest.xml') -EA SilentlyContinue } } catch {}; exit 0" },

        // ── Third-party background updaters that run idle on a real (non-clean) install ──
        new() { Id="upd1", Category="Processes", Tier=TweakTier.Safe, Name="Disable Google Update", Description="Stop the Google Update services (gupdate/gupdatem) — Chrome's idle background updater. Update Chrome manually via its menu.",
            ApplyCmd="sc config gupdate start= disabled & sc config gupdatem start= disabled & sc stop gupdate & exit /b 0", RevertCmd="sc config gupdate start= auto & sc config gupdatem start= demand & exit /b 0" },
        new() { Id="upd2", Category="Processes", Tier=TweakTier.Safe, Name="Disable Edge Update", Description="Stop the Microsoft Edge update services (edgeupdate/edgeupdatem) and elevation service.",
            ApplyCmd="sc config edgeupdate start= disabled & sc config edgeupdatem start= disabled & sc config MicrosoftEdgeElevationService start= disabled & sc stop edgeupdate & exit /b 0", RevertCmd="sc config edgeupdate start= auto & sc config edgeupdatem start= demand & sc config MicrosoftEdgeElevationService start= demand & exit /b 0" },
        new() { Id="upd3", Category="Processes", Tier=TweakTier.Safe, Name="Disable Updater Tasks", Description="Disable common third-party auto-update scheduled tasks (Google, Edge, OneDrive) that spawn background processes.",
            ApplyCmd=@"schtasks /Change /TN ""GoogleUpdateTaskMachineCore"" /Disable 2>nul & schtasks /Change /TN ""GoogleUpdateTaskMachineUA"" /Disable 2>nul & schtasks /Change /TN ""MicrosoftEdgeUpdateTaskMachineCore"" /Disable 2>nul & schtasks /Change /TN ""MicrosoftEdgeUpdateTaskMachineUA"" /Disable 2>nul & exit /b 0", RevertCmd=@"schtasks /Change /TN ""GoogleUpdateTaskMachineCore"" /Enable 2>nul & schtasks /Change /TN ""MicrosoftEdgeUpdateTaskMachineCore"" /Enable 2>nul & exit /b 0" },

        // ── Power ─────────────────────────────────────────────────────────────
        new() { Id="pow3", Category="Power", Tier=TweakTier.Extreme, Name="Ultimate Performance", Description="Unlock AND activate the hidden Ultimate Performance power plan — max clocks, best sustained FPS on a desktop with real cooling. On a thermally-limited laptop this can do the opposite: confirmed case of a laptop gaining ~100 FPS by switching back to the Windows-recommended Balanced plan, because \"always max power\" removed its thermal recovery window and made it throttle harder. If you're on a laptop, test FPS before/after — don't assume this helps.",
            // Detects an already-unlocked Ultimate Performance plan by GUID, not by name: powercfg's
            // scheme names are shown in the OS display language ("Ultimate Performance" in English,
            // "Desempenho Máximo" in Portuguese, etc.), so matching the English string only worked on
            // English Windows — everywhere else this silently duplicated a brand-new scheme on every
            // single apply, leaving orphaned plans behind. Any scheme GUID beyond the three built-in
            // ones (Balanced/High performance/Power saver) is treated as an already-unlocked Ultimate
            // Performance plan, regardless of locale.
            ApplyCmd=@"PS:$known=@('381b4222-f694-41f0-9685-ff5bb260df2e','8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c','a1841308-3541-4fab-bc81-f71556f20b4a'); $ids=([regex]::Matches((powercfg -list | Out-String),'[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}') | ForEach-Object { $_.Value }); $g=$ids | Where-Object { $known -notcontains $_ } | Select-Object -First 1; if(-not $g){$o=(powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 | Out-String);$g=[regex]::Match($o,'[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}').Value}; if($g){powercfg -setactive $g}", RevertCmd="powercfg -setactive 381b4222-f694-41f0-9685-ff5bb260df2e" },
        new() { Id="pow4", Category="Power", Tier=TweakTier.Safe, Name="Disable Sleep (AC)", Description="Never sleep while plugged in.",
            ApplyCmd="powercfg /change standby-timeout-ac 0", RevertCmd="powercfg /change standby-timeout-ac 30" },
        new() { Id="pow5", Category="Power", Tier=TweakTier.Safe, Name="Disable Hibernate", Description="Free hiberfil.sys disk space, stop hibernate bookkeeping, and speed up shutdown. Also disables Fast Startup.",
            ApplyCmd="powercfg /hibernate off", RevertCmd="powercfg /hibernate on" },
        new() { Id="pow6", Category="Power", Tier=TweakTier.Safe, Name="Disable Fast Startup", Description="Ensure a clean boot, avoid stale driver state.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power"" /v HiberbootEnabled /t REG_DWORD /d 0 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power"" /v HiberbootEnabled /t REG_DWORD /d 1 /f" },

        // ── Latency ───────────────────────────────────────────────────────────
        new() { Id="lat1", Category="Latency", Tier=TweakTier.Safe, Name="Fast App Kill", Description="2s app-kill timeout on shutdown.",
            ApplyCmd=@"reg add ""HKCU\Control Panel\Desktop"" /v WaitToKillAppTimeout /t REG_SZ /d 2000 /f", RevertCmd=@"reg add ""HKCU\Control Panel\Desktop"" /v WaitToKillAppTimeout /t REG_SZ /d 5000 /f" },
        new() { Id="lat2", Category="Latency", Tier=TweakTier.Safe, Name="NTFS Optimization", Description="Tune NTFS memory usage and disable last-access stamps.",
            ApplyCmd="fsutil behavior set memoryusage 2 && fsutil behavior set disablelastaccess 1", RevertCmd="fsutil behavior set memoryusage 1 && fsutil behavior set disablelastaccess 0" },
        new() { Id="lat3", Category="Latency", Tier=TweakTier.Safe, Name="IRQ8 Priority", Description="Boost IRQ8 interrupt priority. May conflict on some hardware.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl"" /v IRQ8Priority /t REG_DWORD /d 1 /f", RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl"" /v IRQ8Priority /f" },
        new() { Id="lat5", Category="Latency", Tier=TweakTier.Safe, Name="No 8.3 Names", Description="Disable NTFS 8.3 short-name generation. Faster file creation.",
            ApplyCmd="fsutil behavior set disable8dot3 1", RevertCmd="fsutil behavior set disable8dot3 0" },

        // ── Game ──────────────────────────────────────────────────────────────
        new() { Id="gm0", Category="Game", Tier=TweakTier.Safe, Name="Auto Game Boost", Description="Automatically detects when a fullscreen game is running and pins it to your fastest cores (Intel P-cores / AMD CCD-0), pushes Discord/browsers/background apps onto the rest, raises the game's priority and turns off power throttling. Everything restores instantly when the game closes. No per-game setup.",
            ApplyCmd="ENGINE:autoboost:on", RevertCmd="ENGINE:autoboost:off" },
        new() { Id="gm10", Category="Game", Tier=TweakTier.Safe, Name="Game-Time Power Plan", Description="Switch to the VOIDTUNE Performance power plan the moment a fullscreen game starts, and back to your normal plan when it closes — max clocks during play, normal power/heat/noise the rest of the time (the pattern Bitsum recommends instead of running an aggressive plan 24/7). Needs the \"VOIDTUNE Power Plan\" tweak applied so there's a plan to switch to.",
            ApplyCmd="ENGINE:gametimepower:on", RevertCmd="ENGINE:gametimepower:off" },
        new() { Id="gm1", Category="Game", Tier=TweakTier.Safe, Name="Game Mode", Description="Enable Windows Game Mode.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\GameBar"" /v AllowAutoGameMode /t REG_DWORD /d 1 /f", RevertCmd=@"reg add ""HKCU\Software\Microsoft\GameBar"" /v AllowAutoGameMode /t REG_DWORD /d 0 /f" },
        new() { Id="gm2", Category="Game", Tier=TweakTier.Safe, Name="MMCSS Games = High", Description="High scheduling category for game tasks.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""Scheduling Category"" /t REG_SZ /d High /f", RevertCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""Scheduling Category"" /t REG_SZ /d Normal /f" },
        new() { Id="gm3", Category="Game", Tier=TweakTier.Extreme, Name="No Overlay Hooks", Description="Disable fullscreen-exclusive overlay hooks.",
            ApplyCmd=@"reg add ""HKCU\System\GameConfigStore"" /v GameDVR_FSEBehavior /t REG_DWORD /d 2 /f", RevertCmd=@"reg delete ""HKCU\System\GameConfigStore"" /v GameDVR_FSEBehavior /f" },
        new() { Id="gm4", Category="Game", Tier=TweakTier.Safe, Name="Fullscreen Optim Off", Description="Disable DWM fullscreen optimizations for lower input lag.",
            ApplyCmd=@"reg add ""HKCU\System\GameConfigStore"" /v GameDVR_DXGIHonorFSEWindowsCompatible /t REG_DWORD /d 1 /f && reg add ""HKCU\System\GameConfigStore"" /v GameDVR_FSEBehaviorMode /t REG_DWORD /d 2 /f", RevertCmd=@"reg delete ""HKCU\System\GameConfigStore"" /v GameDVR_DXGIHonorFSEWindowsCompatible /f" },
        new() { Id="gm5", Category="Game", Tier=TweakTier.Safe, Name="DWM Flush Rate", Description="Force DWM to flush every frame, reduces microstutter.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\DWM"" /v Flush /t REG_DWORD /d 1 /f", RevertCmd=@"reg delete ""HKCU\Software\Microsoft\Windows\DWM"" /v Flush /f" },
        new() { Id="gm6", Category="Game", Tier=TweakTier.Safe, Name="CPU Priority Games", Description="Boost game thread priority via MMCSS.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v Priority /t REG_DWORD /d 6 /f && reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""SFIO Priority"" /t REG_SZ /d High /f", RevertCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v Priority /t REG_DWORD /d 2 /f" },
        new() { Id="gm8", Category="Game", Tier=TweakTier.Safe, Name="No USB Suspend", Description="Prevent USB devices powering off mid-game (input stutter).",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\USB"" /v DisableSelectiveSuspend /t REG_DWORD /d 1 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\USB"" /v DisableSelectiveSuspend /t REG_DWORD /d 0 /f" },
        new() { Id="gm9", NeedsReboot=true, Category="Game", Tier=TweakTier.Extreme, Name="GPU MSI Mode", Description="Enable Message Signaled Interrupts on the GPU only, if supported. Can lower DPC latency, but on some GPU/driver combos it does the opposite and adds stutter. Opt-in; takes effect after reboot.",
            ApplyCmd=@"PS:try { Get-WmiObject Win32_PnPSignedDriver -EA SilentlyContinue | Where-Object { $_.DeviceClass -match 'Display' } | ForEach-Object { $p = ""HKLM:\SYSTEM\CurrentControlSet\Enum\$($_.DeviceID)\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties""; if (Test-Path (Split-Path $p)) { New-Item $p -Force | Out-Null; Set-ItemProperty $p -Name MSISupported -Value 1 } } } catch {}; exit 0", RevertCmd=@"PS:try { Get-WmiObject Win32_PnPSignedDriver -EA SilentlyContinue | Where-Object { $_.DeviceClass -match 'Display' } | ForEach-Object { $p = ""HKLM:\SYSTEM\CurrentControlSet\Enum\$($_.DeviceID)\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties""; if (Test-Path $p) { Set-ItemProperty $p -Name MSISupported -Value 0 } } } catch {}; exit 0" },

        // ── Background / Scheduled tasks ────────────────────────────────────────
        new() { Id="bg1", Category="Background", Tier=TweakTier.Safe, Name="Disable UWP Background", Description="Revoke background execution for all UWP/modern apps globally.",
            ApplyCmd=@"PS:$k='HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications'; if(!(Test-Path $k)){New-Item $k -Force|Out-Null}; Set-ItemProperty $k GlobalUserDisabled 1; Get-ChildItem $k -EA SilentlyContinue | ForEach-Object { Set-ItemProperty $_.PSPath Disabled 1 -EA SilentlyContinue }", RevertCmd=@"PS:$k='HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications'; Set-ItemProperty $k GlobalUserDisabled 0 -EA SilentlyContinue; Get-ChildItem $k -EA SilentlyContinue | ForEach-Object { Set-ItemProperty $_.PSPath Disabled 0 -EA SilentlyContinue }" },
        new() { Id="bg2", Category="Background", Tier=TweakTier.Safe, Name="Disable Compat Tasks", Description="Stop Application Experience / Appraiser background tasks.",
            ApplyCmd=@"schtasks /Change /TN ""Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"" /Disable 2>nul & schtasks /Change /TN ""Microsoft\Windows\Application Experience\ProgramDataUpdater"" /Disable 2>nul & schtasks /Change /TN ""Microsoft\Windows\Application Experience\StartupAppTask"" /Disable 2>nul & exit /b 0", RevertCmd=@"schtasks /Change /TN ""Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"" /Enable 2>nul & schtasks /Change /TN ""Microsoft\Windows\Application Experience\ProgramDataUpdater"" /Enable 2>nul & exit /b 0" },
        new() { Id="bg3", Category="Background", Tier=TweakTier.Safe, Name="Disable CEIP Tasks", Description="Stop Customer Experience Improvement Program tasks.",
            ApplyCmd=@"schtasks /Change /TN ""Microsoft\Windows\Customer Experience Improvement Program\Consolidator"" /Disable 2>nul & schtasks /Change /TN ""Microsoft\Windows\Customer Experience Improvement Program\UsbCeip"" /Disable 2>nul & exit /b 0", RevertCmd=@"schtasks /Change /TN ""Microsoft\Windows\Customer Experience Improvement Program\Consolidator"" /Enable 2>nul & exit /b 0" },
        new() { Id="bg4", Category="Background", Tier=TweakTier.Safe, Name="Disable Feedback Tasks", Description="Stop Windows Feedback / Siuf telemetry collection tasks.",
            ApplyCmd=@"schtasks /Change /TN ""Microsoft\Windows\Feedback\Siuf\DmClient"" /Disable 2>nul & schtasks /Change /TN ""Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload"" /Disable 2>nul & exit /b 0", RevertCmd=@"schtasks /Change /TN ""Microsoft\Windows\Feedback\Siuf\DmClient"" /Enable 2>nul & exit /b 0" },
        new() { Id="bg5", Category="Background", Tier=TweakTier.Safe, Name="Disable Disk Diag Task", Description="Stop the Microsoft Disk Diagnostic data collector.",
            ApplyCmd=@"schtasks /Change /TN ""Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector"" /Disable 2>nul & exit /b 0", RevertCmd=@"schtasks /Change /TN ""Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector"" /Enable 2>nul & exit /b 0" },
        new() { Id="bg6", Category="Background", Tier=TweakTier.Safe, Name="Disable WER Tasks", Description="Stop Windows Error Reporting background queue tasks.",
            ApplyCmd=@"schtasks /Change /TN ""Microsoft\Windows\Windows Error Reporting\QueueReporting"" /Disable 2>nul & exit /b 0", RevertCmd=@"schtasks /Change /TN ""Microsoft\Windows\Windows Error Reporting\QueueReporting"" /Enable 2>nul & exit /b 0" },
        new() { Id="bg7", Category="Background", Tier=TweakTier.Safe, Name="Startup Delay = 0", Description="Startup apps open instantly instead of staggered.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize"" /v StartupDelayInMSec /t REG_DWORD /d 0 /f", RevertCmd=@"reg delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize"" /v StartupDelayInMSec /f" },
        new() { Id="bg8", Category="Background", Tier=TweakTier.Safe, Name="Disable Delivery Opt", Description="Stop Delivery Optimization background upload tasks.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization"" /v DODownloadMode /t REG_DWORD /d 0 /f & exit /b 0", RevertCmd=@"reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization"" /v DODownloadMode /f 2>nul & exit /b 0" },

        // ── Storage ───────────────────────────────────────────────────────────
        new() { Id="stor1", Category="Storage", Tier=TweakTier.Safe, Name="StorAHCI MSI Mode", Description="Enable MSI for storage controllers (NVMe/SATA AHCI).",
            ApplyCmd=@"PS:Get-ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Enum\PCI' -EA SilentlyContinue | Get-ChildItem -EA SilentlyContinue | ForEach-Object { $p=Get-ItemProperty $_.PSPath -EA SilentlyContinue; if($p.Class -match 'SCSIAdapter|HDC'){ $msi=Join-Path $_.PSPath 'Device Parameters\Interrupt Management\MessageSignaledInterruptProperties'; if(!(Test-Path $msi)){New-Item $msi -Force -EA SilentlyContinue|Out-Null}; Set-ItemProperty $msi MSISupported 1 -EA SilentlyContinue } }", RevertCmd=@"PS:Get-ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Enum\PCI' -EA SilentlyContinue | Get-ChildItem -EA SilentlyContinue | ForEach-Object { $p=Get-ItemProperty $_.PSPath -EA SilentlyContinue; if($p.Class -match 'SCSIAdapter|HDC'){ $msi=Join-Path $_.PSPath 'Device Parameters\Interrupt Management\MessageSignaledInterruptProperties'; Set-ItemProperty $msi MSISupported 0 -EA SilentlyContinue } }" },
        new() { Id="stor2", Category="Storage", Tier=TweakTier.Safe, Name="Enable Write Cache", Description="Enable disk write caching for better sequential write performance.",
            ApplyCmd=@"PS:Get-WmiObject Win32_DiskDrive -EA SilentlyContinue | ForEach-Object { $id=$_.PNPDeviceID; $k=""HKLM:\SYSTEM\CurrentControlSet\Enum\$id\Device Parameters\Disk""; if(!(Test-Path $k)){New-Item $k -Force -EA SilentlyContinue|Out-Null}; Set-ItemProperty $k UserWriteCacheSetting 1 -EA SilentlyContinue }", RevertCmd=@"PS:Get-WmiObject Win32_DiskDrive -EA SilentlyContinue | ForEach-Object { $id=$_.PNPDeviceID; $k=""HKLM:\SYSTEM\CurrentControlSet\Enum\$id\Device Parameters\Disk""; Set-ItemProperty $k UserWriteCacheSetting 0 -EA SilentlyContinue }" },
        new() { Id="stor3", Category="Storage", Tier=TweakTier.Safe, Name="TRIM All SSDs", Description="Run TRIM optimisation on all fixed drives now (one-shot).",
            ApplyCmd=@"PS:Get-Volume | Where-Object {$_.DriveType -eq 'Fixed' -and $_.DriveLetter} | ForEach-Object { try { Optimize-Volume -DriveLetter $_.DriveLetter -ReTrim -EA SilentlyContinue } catch {} }", RevertCmd="" },
        new() { Id="stor4", Category="Storage", Tier=TweakTier.Safe, Name="NTFS Disable Encrypt", Description="Disable NTFS on-disk encryption overhead (not BitLocker).",
            ApplyCmd="fsutil behavior set disableencryption 1", RevertCmd="fsutil behavior set disableencryption 0" },
        new() { Id="stor5", Category="Storage", Tier=TweakTier.Safe, Name="NTFS Large MFT", Description="Reserve maximum MFT space (zone 4) to reduce fragmentation.",
            ApplyCmd="fsutil behavior set mftzone 4", RevertCmd="fsutil behavior set mftzone 1" },
        new() { Id="stor6", Category="Storage", Tier=TweakTier.Extreme, Name="Disable NTFS Journal", Description="Delete the NTFS change journal on C:. Breaks VSS shadow copies.",
            ApplyCmd="fsutil usn queryjournal C: >nul 2>&1 && fsutil usn deletejournal /D C: & exit /b 0", RevertCmd="fsutil usn createjournal m=0x10000000 a=0x800000 C:" },

        // ── Audio ─────────────────────────────────────────────────────────────
        new() { Id="aud1", Category="Audio", Tier=TweakTier.Safe, Name="Disable Audio Enhancements", Description="Disable system-wide audio DSP effects that add buffer latency.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Audio"" /v DisableProtectedAudioDG /t REG_DWORD /d 1 /f", RevertCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Audio"" /v DisableProtectedAudioDG /t REG_DWORD /d 0 /f" },
        new() { Id="aud2", Category="Audio", Tier=TweakTier.Safe, Name="MMCSS Audio = High", Description="High scheduling for the audio thread. Lower underrun risk.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio"" /v Priority /t REG_DWORD /d 6 /f & reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio"" /v ""Scheduling Category"" /t REG_SZ /d High /f & reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio"" /v ""SFIO Priority"" /t REG_SZ /d High /f & exit /b 0", RevertCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio"" /v Priority /t REG_DWORD /d 2 /f" },
        new() { Id="aud3", Category="Audio", Tier=TweakTier.Safe, Name="Exclusive Audio Mode", Description="Let apps (DAW, games) claim exclusive device control for lowest buffers.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Audio"" /v AllowExclusiveModeOverride /t REG_DWORD /d 1 /f", RevertCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Audio"" /v AllowExclusiveModeOverride /t REG_DWORD /d 0 /f" },
        new() { Id="aud4", Category="Audio", Tier=TweakTier.Extreme, Name="Disable Auto Ducking", Description="Stop Windows lowering other audio when comms apps are active.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Audio"" /v AutoDucking /t REG_DWORD /d 0 /f", RevertCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Audio"" /v AutoDucking /t REG_DWORD /d 1 /f" },
        new() { Id="aud5", Category="Audio", Tier=TweakTier.Safe, Name="Disable Audio Logging", Description="Disable audio engine debug/event logging.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\Audiosrv"" /v PerfDisableAllLogging /t REG_DWORD /d 1 /f", RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Services\Audiosrv"" /v PerfDisableAllLogging /f" },

        // ── Privacy ───────────────────────────────────────────────────────────
        new() { Id="p1", Category="Privacy", Tier=TweakTier.Safe, Name="Disable Telemetry Services", Description="Kill DiagTrack and dmwappushservice.",
            ApplyCmd="sc config DiagTrack start= disabled & sc stop DiagTrack & sc config dmwappushservice start= disabled & sc stop dmwappushservice & exit /b 0", RevertCmd="sc config DiagTrack start= auto & sc start DiagTrack & exit /b 0" },
        new() { Id="p5", Category="Privacy", Tier=TweakTier.Safe, Name="Disable Location", Description="Block location access for apps.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location"" /v Value /t REG_SZ /d Deny /f", RevertCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location"" /v Value /t REG_SZ /d Allow /f" },
        new() { Id="p7", Category="Privacy", Tier=TweakTier.Safe, Name="Disable Activity Feed", Description="Stop timeline / activity tracking.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\System"" /v EnableActivityFeed /t REG_DWORD /d 0 /f", RevertCmd=@"reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Windows\System"" /v EnableActivityFeed /f" },
        new() { Id="p8", Category="Privacy", Tier=TweakTier.Extreme, Name="Disable Windows Update", Description="Stop the Windows Update service. Re-enable to get updates.",
            ApplyCmd="sc config wuauserv start= disabled & sc stop wuauserv & exit /b 0", RevertCmd="sc config wuauserv start= demand & sc start wuauserv & exit /b 0" },

        // ── 0.8.8 quality additions ──────────────────────────────────────────
        new() { Id="deb20", Category="Debloat", Tier=TweakTier.Safe, MinBuild=Win11, Name="Disable Search Highlights", Description="Remove the rotating web content in the taskbar search box. (Windows 11)",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\SearchSettings"" /v IsDynamicSearchBoxEnabled /t REG_DWORD /d 0 /f", RevertCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\SearchSettings"" /v IsDynamicSearchBoxEnabled /t REG_DWORD /d 1 /f" },
        new() { Id="deb21", Category="Debloat", Tier=TweakTier.Safe, Name="No Startup App Delay", Description="Launch startup apps immediately instead of Windows' staggered delay.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize"" /v StartupDelayInMSec /t REG_DWORD /d 0 /f", RevertCmd=@"reg delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize"" /v StartupDelayInMSec /f & exit /b 0" },
        new() { Id="net20", Category="Network", Tier=TweakTier.Extreme, Name="Cloudflare DNS", Description="Use 1.1.1.1 / 1.0.0.1 — consistently among the fastest public resolvers. Revert returns to your router / DHCP DNS.",
            ApplyCmd=@"PS:try { Get-NetAdapter -Physical | Where-Object Status -eq 'Up' | ForEach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.ifIndex -ServerAddresses '1.1.1.1','1.0.0.1' } } catch {}; exit 0", RevertCmd=@"PS:try { Get-NetAdapter -Physical | ForEach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.ifIndex -ResetServerAddresses } } catch {}; ipconfig /flushdns | Out-Null; exit 0" },
        new() { Id="slim40", Category="Processes", Tier=TweakTier.Safe, Name="No Edge Preload", Description="Stop Microsoft Edge from pre-launching and running in the background when closed.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Edge"" /v StartupBoostEnabled /t REG_DWORD /d 0 /f & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Edge"" /v BackgroundModeEnabled /t REG_DWORD /d 0 /f", RevertCmd=@"reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Edge"" /v StartupBoostEnabled /f & reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Edge"" /v BackgroundModeEnabled /f & exit /b 0" },
        new() { Id="deb22", Category="Debloat", Tier=TweakTier.Safe, Name="Disable Windows Recall", Description="Turn off Windows Recall / AI screen-snapshotting (Copilot+ PCs) and its background data analysis. Frees NPU/CPU cycles and disk; harmless on PCs that don't have it.",
            ApplyCmd=@"reg add ""HKCU\Software\Policies\Microsoft\Windows\WindowsAI"" /v DisableAIDataAnalysis /t REG_DWORD /d 1 /f & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI"" /v DisableAIDataAnalysis /t REG_DWORD /d 1 /f & exit /b 0", RevertCmd=@"reg delete ""HKCU\Software\Policies\Microsoft\Windows\WindowsAI"" /v DisableAIDataAnalysis /f & reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI"" /v DisableAIDataAnalysis /f & exit /b 0" },

        // ── 0.8.10 process-reduction additions (vetted safe, inspired by WinUtil) ─
        new() { Id="deb30", Category="Debloat", Tier=TweakTier.Safe, Name="Disable Consumer Features", Description="Stop Windows silently auto-installing suggested/promoted apps (Candy Crush, etc.) and their background installers.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent"" /v DisableWindowsConsumerFeatures /t REG_DWORD /d 1 /f", RevertCmd=@"reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent"" /v DisableWindowsConsumerFeatures /f & exit /b 0" },
        new() { Id="slim41", Category="Processes", Tier=TweakTier.Safe, Name="Disable Background Apps", Description="Globally stop UWP/Store apps running in the background — a big cut to idle process count. Win32 apps (games, Discord, browsers) are unaffected.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications"" /v GlobalUserDisabled /t REG_DWORD /d 1 /f & reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Search"" /v BackgroundAppGlobalToggle /t REG_DWORD /d 0 /f & exit /b 0", RevertCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications"" /v GlobalUserDisabled /t REG_DWORD /d 0 /f & reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Search"" /v BackgroundAppGlobalToggle /t REG_DWORD /d 1 /f & exit /b 0" },
        new() { Id="deb31", Category="Debloat", Tier=TweakTier.Safe, Name="Disable Notifications", Description="Turn off toast notifications and the notification center — stops their background push work.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\PushNotifications"" /v ToastEnabled /t REG_DWORD /d 0 /f", RevertCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\PushNotifications"" /v ToastEnabled /t REG_DWORD /d 1 /f" },
        new() { Id="deb32", Category="Debloat", Tier=TweakTier.Safe, Name="Disable Storage Sense (Per-User)", Description="Stop the automatic background disk-cleanup service from waking periodically.",
            ApplyCmd=@"reg add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy"" /v 01 /t REG_DWORD /d 0 /f", RevertCmd=@"reg add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy"" /v 01 /t REG_DWORD /d 1 /f" },
        new() { Id="net21", Category="Network", Tier=TweakTier.Safe, Name="Prefer IPv4 + No Teredo", Description="Prefer IPv4 over IPv6 and disable the Teredo tunneling adapter — removes an idle network component. IPv6 itself stays on.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters"" /v DisabledComponents /t REG_DWORD /d 0x20 /f & netsh interface teredo set state disabled & exit /b 0", RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters"" /v DisabledComponents /f & netsh interface teredo set state default & exit /b 0" },
        new() { Id="stor31", Category="Storage", Tier=TweakTier.Safe, Name="Enable Long Paths", Description="Allow file paths longer than 260 characters — prevents deep-folder errors for games/dev tools. Pure win, no downside.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\FileSystem"" /v LongPathsEnabled /t REG_DWORD /d 1 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\FileSystem"" /v LongPathsEnabled /t REG_DWORD /d 0 /f" },
        new() { Id="sec1", Category="Debloat", Tier=TweakTier.Extreme, Name="Block WPBT Binaries", Description="Block the Windows Platform Binary Table — stops motherboard firmware from silently injecting OEM executables at boot. Safe on most systems; revert if a vendor tool stops working.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager"" /v DisableWpbtExecution /t REG_DWORD /d 1 /f", RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager"" /v DisableWpbtExecution /f & exit /b 0" },

        // ── 0.8.10 high-value additions (FPS / fewer processes / snappier) ───────
        new() { Id="deb40", Category="Processes", Tier=TweakTier.Safe, Name="Disable Telemetry Tasks", Description="Disable the scheduled tasks that wake periodically to collect telemetry (Compatibility Appraiser, Customer Experience Improvement, disk diagnostics). Fewer background wake-ups.",
            ApplyCmd=@"schtasks /Change /TN ""\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"" /Disable & schtasks /Change /TN ""\Microsoft\Windows\Application Experience\ProgramDataUpdater"" /Disable & schtasks /Change /TN ""\Microsoft\Windows\Customer Experience Improvement Program\Consolidator"" /Disable & schtasks /Change /TN ""\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip"" /Disable & schtasks /Change /TN ""\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector"" /Disable & exit /b 0",
            RevertCmd=@"schtasks /Change /TN ""\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"" /Enable & schtasks /Change /TN ""\Microsoft\Windows\Application Experience\ProgramDataUpdater"" /Enable & schtasks /Change /TN ""\Microsoft\Windows\Customer Experience Improvement Program\Consolidator"" /Enable & schtasks /Change /TN ""\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip"" /Enable & schtasks /Change /TN ""\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector"" /Enable & exit /b 0" },
        new() { Id="slim50", Category="Processes", Tier=TweakTier.Safe, NeedsReboot=true, Name="Group svchost Processes", Description="Merge the many split svchost.exe hosts into a few grouped ones — a big cut to the process count on systems with plenty of RAM. Sets the split threshold to your installed memory. (Needs 8 GB+; takes effect after reboot.)",
            ApplyCmd=@"PS:try { $kb=[int]((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory/1KB); reg add 'HKLM\SYSTEM\CurrentControlSet\Control' /v SvcHostSplitThresholdInKB /t REG_DWORD /d $kb /f | Out-Null } catch {}; exit 0",
            RevertCmd=@"PS:reg add 'HKLM\SYSTEM\CurrentControlSet\Control' /v SvcHostSplitThresholdInKB /t REG_DWORD /d 380000 /f | Out-Null; exit 0" },
        new() { Id="upd10", Category="Debloat", Tier=TweakTier.Safe, Name="Block Driver Updates in WU", Description="Stop Windows Update from silently replacing your GPU/chipset drivers with its own (often older) versions. You still get security updates; just not driver overwrites.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"" /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 1 /f & reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching"" /v SearchOrderConfig /t REG_DWORD /d 0 /f & exit /b 0",
            RevertCmd=@"reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"" /v ExcludeWUDriversInQualityUpdate /f & reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching"" /v SearchOrderConfig /t REG_DWORD /d 1 /f & exit /b 0" },
        // 0.8.17: ux1 no longer touches HungAppTimeout or AutoEndTasks. HungAppTimeout=2000 +
        // AutoEndTasks=1 made Windows auto-kill ANY app it considered hung for 2 seconds —
        // including Explorer during routine stalls (thumbnails, network folders, slow disks),
        // which showed up in the field as "Explorer randomly restarts itself". Same failure
        // class was reported by another optimizer's users (WinUtil issue #122). The remaining
        // two values only act during shutdown, which is all the description ever promised.
        new() { Id="ux1", Category="Debloat", Tier=TweakTier.Safe, Name="Faster Shutdown", Description="Cut the timeouts Windows waits for services/apps during shutdown — snappier shutdown, no multi-second hang on close. Doesn't touch running apps: the aggressive auto-kill-hung-apps variant of this tweak made Windows restart Explorer mid-session and was removed.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control"" /v WaitToKillServiceTimeout /t REG_SZ /d 2000 /f & reg add ""HKCU\Control Panel\Desktop"" /v WaitToKillAppTimeout /t REG_SZ /d 2000 /f & reg delete ""HKCU\Control Panel\Desktop"" /v HungAppTimeout /f 2>nul & reg delete ""HKCU\Control Panel\Desktop"" /v AutoEndTasks /f 2>nul & exit /b 0",
            RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control"" /v WaitToKillServiceTimeout /t REG_SZ /d 5000 /f & reg delete ""HKCU\Control Panel\Desktop"" /v WaitToKillAppTimeout /f & reg delete ""HKCU\Control Panel\Desktop"" /v HungAppTimeout /f 2>nul & reg delete ""HKCU\Control Panel\Desktop"" /v AutoEndTasks /f 2>nul & exit /b 0" },
        new() { Id="slim51", Category="Processes", Tier=TweakTier.Safe, Name="Disable Error Reporting", Description="Turn off Windows Error Reporting — removes the WerFault / WerSvc background work that fires on every app hiccup.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting"" /v Disabled /t REG_DWORD /d 1 /f & sc config WerSvc start= disabled & sc stop WerSvc & exit /b 0",
            RevertCmd=@"reg delete ""HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting"" /v Disabled /f & sc config WerSvc start= demand & exit /b 0" },

        // ── 0.8.10 round 2: services that were only in the Services tab, mirrored ──
        // into the one-click SAFE flow. Each is a service almost nobody actively uses
        // and disabling it has no real downside — the "free wins" other optimizers'
        // safe profiles include that ours didn't yet.
        new() { Id="slim52", Category="Processes", Tier=TweakTier.Safe, Name="Disable Fax Service", Description="Stop the Fax service — virtually unused on a modern PC.",
            ApplyCmd="sc config Fax start= disabled & sc stop Fax & exit /b 0", RevertCmd="sc config Fax start= demand & exit /b 0" },
        new() { Id="slim53", Category="Processes", Tier=TweakTier.Safe, Name="Disable Offline Maps Svc", Description="Stop the Downloaded Maps Manager (MapsBroker) — only needed if you use the Windows Maps app offline.",
            ApplyCmd="sc config MapsBroker start= disabled & sc stop MapsBroker & exit /b 0", RevertCmd="sc config MapsBroker start= demand & exit /b 0" },
        new() { Id="slim54", Category="Processes", Tier=TweakTier.Safe, Name="Disable Media Player Sharing", Description="Stop Windows Media Player Network Sharing (WMPNetworkSvc) — legacy DLNA streaming, rarely used.",
            ApplyCmd="sc config WMPNetworkSvc start= disabled & sc stop WMPNetworkSvc & exit /b 0", RevertCmd="sc config WMPNetworkSvc start= demand & exit /b 0" },
        new() { Id="slim55", Category="Processes", Tier=TweakTier.Safe, Name="Disable Remote Registry", Description="Stop Remote Registry — lets other machines edit your registry over the network. No legitimate use on a home PC, and disabling it closes a real attack surface.",
            ApplyCmd="sc config RemoteRegistry start= disabled & sc stop RemoteRegistry & exit /b 0", RevertCmd="sc config RemoteRegistry start= demand & exit /b 0" },
        new() { Id="slim56", Category="Processes", Tier=TweakTier.Safe, Name="Disable Internet Connection Sharing", Description="Stop the Internet Connection Sharing service (SharedAccess) — only needed if you're sharing this PC's connection with other devices.",
            ApplyCmd="sc config SharedAccess start= disabled & sc stop SharedAccess & exit /b 0", RevertCmd="sc config SharedAccess start= demand & exit /b 0" },
        new() { Id="slim57", Category="Processes", Tier=TweakTier.Safe, Name="Disable Retail Demo Mode", Description="Stop Retail Demo service — exists only for store-floor kiosk PCs.",
            ApplyCmd="sc config RetailDemo start= disabled & sc stop RetailDemo & exit /b 0", RevertCmd="sc config RetailDemo start= demand & exit /b 0" },
        new() { Id="slim58", Category="Processes", Tier=TweakTier.Safe, Name="Disable Xbox Background Services", Description="Stop the Xbox Live Auth Manager, Game Save, Networking and Accessory services — background processes for the Xbox app / Game Pass. Steam, Epic, Riot and FiveM don't need any of these; only disable if you don't use the Xbox app or Xbox cloud saves.",
            ApplyCmd="sc config XblAuthManager start= disabled & sc config XblGameSave start= disabled & sc config XboxNetApiSvc start= disabled & sc config XboxGipSvc start= disabled & sc stop XblAuthManager & sc stop XblGameSave & sc stop XboxNetApiSvc & sc stop XboxGipSvc & exit /b 0",
            RevertCmd="sc config XblAuthManager start= demand & sc config XblGameSave start= demand & sc config XboxNetApiSvc start= demand & sc config XboxGipSvc start= demand & exit /b 0" },

        // ── New in 0.8.17 — sourced from AtlasOS/ReviOS/Microsoft docs, cross-checked for
        //    real mechanism + no known regressions before inclusion (see CHANGELOG).
        new() { Id="slim59", Category="Processes", Tier=TweakTier.Safe, Name="De-prioritize Background Processes (IFEO)", Description="Lower the CPU/I-O scheduling priority of SearchIndexer, ctfmon, fontdrvhost and sihost via Image File Execution Options — the same documented mechanism Windows itself uses for foreground-app boosting, just aimed the other way. These keep running (nothing is disabled) but yield CPU/disk to your game the moment it needs it.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\SearchIndexer.exe\PerfOptions"" /v CpuPriorityClass /t REG_DWORD /d 5 /f & reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\ctfmon.exe\PerfOptions"" /v CpuPriorityClass /t REG_DWORD /d 1 /f & reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\ctfmon.exe\PerfOptions"" /v IoPriority /t REG_DWORD /d 1 /f & reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\fontdrvhost.exe\PerfOptions"" /v CpuPriorityClass /t REG_DWORD /d 1 /f & reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\fontdrvhost.exe\PerfOptions"" /v IoPriority /t REG_DWORD /d 1 /f & reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\sihost.exe\PerfOptions"" /v CpuPriorityClass /t REG_DWORD /d 1 /f & reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\sihost.exe\PerfOptions"" /v IoPriority /t REG_DWORD /d 1 /f & exit /b 0",
            RevertCmd=@"reg delete ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\SearchIndexer.exe\PerfOptions"" /v CpuPriorityClass /f & reg delete ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\ctfmon.exe\PerfOptions"" /v CpuPriorityClass /f & reg delete ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\ctfmon.exe\PerfOptions"" /v IoPriority /f & reg delete ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\fontdrvhost.exe\PerfOptions"" /v CpuPriorityClass /f & reg delete ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\fontdrvhost.exe\PerfOptions"" /v IoPriority /f & reg delete ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\sihost.exe\PerfOptions"" /v CpuPriorityClass /f & reg delete ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\sihost.exe\PerfOptions"" /v IoPriority /f & exit /b 0" },
        new() { Id="slim60", Category="Processes", Tier=TweakTier.Safe, Name="Disable Automatic Maintenance Wake", Description="Stop Automatic Maintenance from waking your PC from sleep to run its nightly task swarm (defrag, telemetry, .NET NGEN, disk diagnostics) — a common cause of \"why is my PC busy when I just sat down\". Maintenance still runs when the PC is already awake; this only removes the wake-up.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance"" /v WakeUp /t REG_DWORD /d 0 /f", RevertCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance"" /v WakeUp /t REG_DWORD /d 1 /f" },
        new() { Id="slim61", Category="Processes", Tier=TweakTier.Safe, Name="Disable Fault Tolerant Heap", Description="Turns off the Fault Tolerant Heap subsystem, which silently applies per-app memory-allocation shims to anything that has crashed before — a documented, measurable slowdown for apps with lots of small allocations (Microsoft's own docs note the perf cost; multiple game/render-software vendors tell users to disable it for exactly this reason).",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\FTH"" /v Enabled /t REG_DWORD /d 0 /f", RevertCmd=@"reg delete ""HKLM\SOFTWARE\Microsoft\FTH"" /v Enabled /f" },
        new() { Id="slim62", Category="Processes", Tier=TweakTier.Safe, Name="Disable Explorer Auto Folder-Type Discovery", Description="Stops Explorer from inspecting every folder's contents to guess whether it's Pictures/Music/Videos/General and forcing a single generic template instead — removes the disk churn and open-lag on large or networked folders. Cosmetic-only downside: folder view columns become uniform.",
            ApplyCmd=@"reg add ""HKCU\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags\AllFolders\Shell"" /v FolderType /t REG_SZ /d NotSpecified /f", RevertCmd=@"reg delete ""HKCU\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags\AllFolders\Shell"" /v FolderType /f" },

        new() { Id="ext1", Category="Processes", Tier=TweakTier.Extreme, Name="Disable Bluetooth Stack", Description="Stop the Bluetooth Support Service, Audio Gateway and per-user Bluetooth service template — removes their persistent processes entirely. Only apply if you have no Bluetooth radio or don't use Bluetooth mice/headsets/controllers, since this breaks all of them.",
            ApplyCmd="sc config bthserv start= disabled & sc config BTAGService start= disabled & sc config BluetoothUserService start= disabled & sc stop bthserv & sc stop BTAGService & exit /b 0",
            RevertCmd="sc config bthserv start= demand & sc config BTAGService start= demand & sc config BluetoothUserService start= demand & exit /b 0" },
        new() { Id="ext2", Category="Processes", Tier=TweakTier.Extreme, Name="Disable Print Spooler", Description="Stop the Print Spooler service and its notification helper (PrintNotify), which run even with no printer attached — Spooler is also a notorious attack surface (PrintNightmare). Breaks ALL printing and some \"print to PDF\" flows — only apply if you never print from this PC.",
            ApplyCmd="sc config Spooler start= disabled & sc stop Spooler & sc config PrintNotify start= disabled & sc stop PrintNotify & exit /b 0",
            RevertCmd="sc config Spooler start= demand & sc start Spooler & sc config PrintNotify start= demand & exit /b 0" },
        new() { Id="ext3", Category="Processes", Tier=TweakTier.Extreme, Name="Disable Sensor & Biometric Services", Description="Stop the ambient-light/orientation sensor services and Windows Biometric Service — idle overhead on any desktop with no sensors. Breaks auto-brightness, auto-rotate, and Windows Hello fingerprint/face login where present.",
            ApplyCmd="sc config SensorService start= disabled & sc config SensrSvc start= disabled & sc config SensorDataService start= disabled & sc config WbioSrvc start= disabled & sc stop SensorService & sc stop SensrSvc & sc stop SensorDataService & sc stop WbioSrvc & exit /b 0",
            RevertCmd="sc config SensorService start= demand & sc config SensrSvc start= demand & sc config SensorDataService start= demand & sc config WbioSrvc start= demand & exit /b 0" },
        new() { Id="ext4", Category="Processes", Tier=TweakTier.Extreme, Name="Disable Remote Desktop Services", Description="Stop Remote Desktop Services, its user-mode port redirector and session-env helper — removes idle processes and a real remote-access attack surface. Breaks inbound Remote Desktop and Remote Assistance/Quick Assist; skip if you ever connect to this PC remotely.",
            ApplyCmd="sc config TermService start= disabled & sc config SessionEnv start= disabled & sc config UmRdpService start= disabled & sc stop TermService & sc stop SessionEnv & sc stop UmRdpService & exit /b 0",
            RevertCmd="sc config TermService start= demand & sc config SessionEnv start= demand & sc config UmRdpService start= demand & exit /b 0" },

        // ── New in 0.8.17 — closing the process-count gap vs. Platinum+ and similar
        //    community optimizers: read their actual scripts, verified which of their
        //    service disables are genuinely Automatic-by-default (real footprint) vs.
        //    already Manual/trigger-start (would be a no-op, the slim12 lesson) before adding.
        new() { Id="slim63", Category="Processes", Tier=TweakTier.Safe, Name="Disable Shell Hardware Detection", Description="Stop AutoPlay's hardware-detection service — Automatic by default, real background footprint. Only loses the AutoPlay prompt when inserting USB drives/discs; manually opening them in Explorer still works fine.",
            ApplyCmd="sc config ShellHWDetection start= disabled & sc stop ShellHWDetection & exit /b 0", RevertCmd="sc config ShellHWDetection start= auto & sc start ShellHWDetection & exit /b 0" },

        new() { Id="ext5", Category="Processes", Tier=TweakTier.Extreme, Name="Disable Windows Search", Description="Stop the Windows Search indexer (WSearch) — one of the heaviest background services on a stock install (Automatic-Delayed by default). Breaks Start Menu app/file search and Explorer's search box (still works, just does a slow live scan instead of using the index).",
            ApplyCmd="sc config WSearch start= disabled & sc stop WSearch & exit /b 0", RevertCmd="sc config WSearch start= delayed-auto & sc start WSearch & exit /b 0" },
        new() { Id="ext6", Category="Processes", Tier=TweakTier.Extreme, Name="Disable File/Printer Sharing (Server Service)", Description="Stop the Server service (LanmanServer) — Automatic by default even though most single-PC gaming setups never share a folder or printer from this machine. Breaks SMB file sharing, printer sharing, and being visible to other PCs on the network as a share target.",
            ApplyCmd="sc config LanmanServer start= disabled & sc stop LanmanServer & exit /b 0", RevertCmd="sc config LanmanServer start= auto & sc start LanmanServer & exit /b 0" },
        new() { Id="ext7", Category="Network", Tier=TweakTier.Extreme, Name="Disable IP Helper", Description="Stop IP Helper (iphlpsvc) — Automatic by default, handles IPv6 transition tunneling (Teredo/6to4/ISATAP). Real background service, but disabling can break IPv6 connectivity or specific VPN clients that depend on it — only apply if you're on IPv4-only networking and don't use an affected VPN.",
            ApplyCmd="sc config iphlpsvc start= disabled & sc stop iphlpsvc & exit /b 0", RevertCmd="sc config iphlpsvc start= auto & sc start iphlpsvc & exit /b 0" },

        // ── Restore (reset to defaults — never auto-selected) ───────────────────
        new() { Id="rst1", Category="Restore", Tier=TweakTier.Safe, Name="Restore Network", Description="Reset TCP/IP global settings to defaults.",
            ApplyCmd="netsh int tcp set global autotuninglevel=normal & netsh int tcp set global ecncapability=enabled & netsh int tcp set global rsc=enabled & netsh int tcp set global congestionprovider=default & exit /b 0", RevertCmd="" },
        new() { Id="rst2", Category="Restore", Tier=TweakTier.Safe, Name="Restore Power", Description="Switch back to the Balanced power plan.",
            ApplyCmd="powercfg -setactive 381b4222-f694-41f0-9685-ff5bb260df2e", RevertCmd="" },
        new() { Id="rst3", Category="Restore", Tier=TweakTier.Safe, Name="Re-enable Services", Description="Restore SysMain and DiagTrack to automatic.",
            ApplyCmd="sc config SysMain start= auto & sc start SysMain & sc config DiagTrack start= auto & sc start DiagTrack & exit /b 0", RevertCmd="" },
        new() { Id="rst4", Category="Restore", Tier=TweakTier.Safe, Name="Restore Visual FX", Description="Re-enable Windows animations and effects.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects"" /v VisualFXSetting /t REG_DWORD /d 0 /f", RevertCmd="" },
        new() { Id="rst5", Category="Restore", Tier=TweakTier.Safe, Name="Fix Stutter — Restore Memory Defaults", Description="Undoes the memory tweaks that stall a DRAM-less SSD under disk load: re-enables RAM compression, resets NTFS memory usage, re-enables page combining, and clears any leftover DPC-watchdog override. Use this if games/audio freeze for a few seconds during big file transfers or Windows Update.",
            ApplyCmd="PS:try { Enable-MMAgent -MemoryCompression -EA SilentlyContinue } catch {}; fsutil behavior set memoryusage 1 | Out-Null; " +
                     @"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v DisablePageCombining /t REG_DWORD /d 0 /f | Out-Null; " +
                     @"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel"" /v DPCTimeout /f 2>$null | Out-Null; " +
                     @"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v LargeSystemCache /f 2>$null | Out-Null; exit 0", RevertCmd="" },
        new() { Id="rst6", Category="Restore", Tier=TweakTier.Safe, Name="Full Reset to Windows Defaults", Description="The clean slate. Resets power plans (unparks cores), timers, GPU scheduling, memory management, and the whole TCP/IP stack back to stock — including tweaks that were removed from the catalog and can no longer be toggled off. Reboot after. Use this to get a known-good baseline before applying a fresh set.",
            ApplyCmd="PS:$ErrorActionPreference='SilentlyContinue'; " +
                     // NOTE: deliberately NOT using `powercfg -restoredefaultschemes` — it doesn't just
                     // reset values, it DELETES any unlocked custom scheme (including Ultimate
                     // Performance) and force-switches to Balanced. That silently downgraded a real
                     // user's power plan and tanked their benchmarks. Switching the active scheme to
                     // Balanced is the actual "known-good baseline" intent, without the collateral damage.
                     "try { Enable-MMAgent -MemoryCompression } catch {}; fsutil behavior set memoryusage 1 | Out-Null; powercfg -setactive 381b4222-f694-41f0-9685-ff5bb260df2e | Out-Null; " +
                     "$mm='HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management'; 'DisablePagingExecutive','DisablePageCombining','LargeSystemCache','IoPageLockLimit','PoolUsageMaximum' | ForEach-Object { Remove-ItemProperty $mm -Name $_ -EA SilentlyContinue }; " +
                     "$k='HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel'; 'NoLazyMode','DistributeTimers','GlobalTimerResolutionRequests','DPCTimeout' | ForEach-Object { Remove-ItemProperty $k -Name $_ -EA SilentlyContinue }; " +
                     "$g='HKLM:\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers'; 'HwSchMode','EnableVsyncLatencyUpdate','DisableVsyncLatencyUpdate','EnableMidGfxPreemption','EnableMidBufferPreemption','DisableDynamicPstate' | ForEach-Object { Remove-ItemProperty $g -Name $_ -EA SilentlyContinue }; " +
                     // Remove the PERFBOOSTMODE definition overrides written by the old ix1/ix3 tweaks
                     // (removed in 0.8.17) — ValueMax=0 clamped turbo boost OFF machine-wide on Intel.
                     "$pb='HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\be337238-0d82-4146-a960-4f3749d470c7'; 'ValueMax','ValueMin' | ForEach-Object { Remove-ItemProperty $pb -Name $_ -EA SilentlyContinue }; " +
                     // Remove the hung-app auto-kill pair written by pre-0.8.17 ux1 — it made Windows
                     // restart Explorer whenever it stalled 2s (thumbnails, network folders).
                     "$dk='HKCU:\\Control Panel\\Desktop'; 'HungAppTimeout','AutoEndTasks' | ForEach-Object { Remove-ItemProperty $dk -Name $_ -EA SilentlyContinue }; " +
                     "$tp='HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters'; 'TCPNoDelay','TcpTimedWaitDelay','MaxUserPort','DisableBandwidthThrottling' | ForEach-Object { Remove-ItemProperty $tp -Name $_ -EA SilentlyContinue }; " +
                     // `netsh int tcp/ip reset` fully reinitializes the stack incl. the Winsock LSP
                     // catalog — it can drop live connections and crash/kill any process holding an
                     // open socket or a custom network filter (VPNs, anti-cheat, cloud sync, voice
                     // chat, game launchers). A "restore defaults" tool has no business doing that.
                     // Same safe, targeted settings reset the rst1 "Restore Network" tweak already uses.
                     "netsh int tcp set global autotuninglevel=normal | Out-Null; netsh int tcp set global ecncapability=enabled | Out-Null; netsh int tcp set global rsc=enabled | Out-Null; netsh int tcp set global congestionprovider=default | Out-Null; ipconfig /flushdns | Out-Null; exit 0", RevertCmd="" },
        new() { Id="rst7", Category="Restore", Tier=TweakTier.Safe, NeedsReboot=true, Name="Restore CPU Turbo Boost", Description="Removes a leftover from older VOIDTUNE versions (\"Intel Speed Shift EPP\" / \"Intel No SpeedStep\", both removed in 0.8.17) that clamped the turbo-boost setting's allowed range machine-wide — capable of disabling turbo on every power plan. If your CPU never clocks above its base frequency under load (check the multiplier in CPU-Z), run this and reboot.",
            ApplyCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\be337238-0d82-4146-a960-4f3749d470c7"" /v ValueMax /f 2>nul & reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\be337238-0d82-4146-a960-4f3749d470c7"" /v ValueMin /f 2>nul & powercfg -setacvalueindex scheme_current sub_processor PERFBOOSTMODE 1 & powercfg -setactive scheme_current & exit /b 0", RevertCmd="" },

    };
}
