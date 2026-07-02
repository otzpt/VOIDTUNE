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
        var list = new List<Tweak>(Baseline);
        list.AddRange(ArchTweaks());
        return list;
    }

    // ── Architecture-specific, gated on detected hardware ────────────────────
    private static IEnumerable<Tweak> ArchTweaks()
    {
        var arch = new List<Tweak>();
        string cpu = HardwareInfo.CpuVendor;
        string gpu = HardwareInfo.GpuVendor;

        if (cpu == "Intel")
        {
            arch.Add(new() { Id="ix1", Category="CPU", Tier=TweakTier.Safe, Name="Intel Speed Shift EPP", Description="[INTEL] Enable Speed Shift energy/performance preference.",
                ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\be337238-0d82-4146-a960-4f3749d470c7"" /v ValueMax /t REG_DWORD /d 0 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\be337238-0d82-4146-a960-4f3749d470c7"" /v ValueMax /t REG_DWORD /d 100 /f" });
            arch.Add(new() { Id="ix2", Category="CPU", Tier=TweakTier.Extreme, Name="Intel Disable C-States", Description="[INTEL] Prevent deep-sleep latency spikes.",
                ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Processor"" /v Capabilities /t REG_DWORD /d 0x0007e066 /f", RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\Processor"" /v Capabilities /f" });
            arch.Add(new() { Id="ix3", Category="CPU", Tier=TweakTier.Extreme, Name="Intel No SpeedStep", Description="[INTEL] Keep all cores at max frequency.",
                ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\be337238-0d82-4146-a960-4f3749d470c7"" /v ValueMin /t REG_DWORD /d 100 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\be337238-0d82-4146-a960-4f3749d470c7"" /v ValueMin /t REG_DWORD /d 0 /f" });
        }
        if (cpu == "AMD")
        {
            arch.Add(new() { Id="ax1", Category="CPU", Tier=TweakTier.Safe, Name="AMD CPPC Perf", Description="[AMD] Set Collaborative Processor Performance to max.",
                ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v CpuEnergyPerfPref /t REG_DWORD /d 0 /f", RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v CpuEnergyPerfPref /f" });
            arch.Add(new() { Id="ax2", Category="CPU", Tier=TweakTier.Extreme, Name="AMD Boost Max", Description="[AMD] Maximum precision boost via power plan.",
                ApplyCmd="powercfg -setacvalueindex scheme_current sub_processor PERFBOOSTPOL 100 && powercfg -setactive scheme_current", RevertCmd="powercfg -setacvalueindex scheme_current sub_processor PERFBOOSTPOL 50 && powercfg -setactive scheme_current" });
            arch.Add(new() { Id="ax3", Category="CPU", Tier=TweakTier.Extreme, Name="AMD Disable C6", Description="[AMD] Disable C6 core idle to cut latency spikes.",
                ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Processor"" /v Capabilities /t REG_DWORD /d 0x0007e066 /f", RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\Processor"" /v Capabilities /f" });
        }
        if (gpu == "NVIDIA")
        {
            arch.Add(new() { Id="nx1", Category="GPU", Tier=TweakTier.Safe, Name="NVIDIA Max Performance", Description="[NVIDIA] Force prefer-maximum-performance power mode.",
                ApplyCmd=@"reg add ""HKCU\Software\NVIDIA Corporation\Global\NvCplApi\Policies"" /v OverrideAdaptiveThreshold /t REG_DWORD /d 1 /f", RevertCmd=@"reg delete ""HKCU\Software\NVIDIA Corporation\Global\NvCplApi\Policies"" /v OverrideAdaptiveThreshold /f" });
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
            arch.Add(new() { Id="agx1", Category="GPU", Tier=TweakTier.Safe, Name="AMD GPU Deep Sleep Off", Description="[AMD GPU] Disable GPU core deep sleep for lower latency.",
                ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v PP_SclkDeepSleepDisable /t REG_DWORD /d 1 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v PP_SclkDeepSleepDisable /t REG_DWORD /d 0 /f" });
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
        }
        return arch;
    }

    // ── Baseline catalog (hardware-independent) ──────────────────────────────
    private static readonly Tweak[] Baseline =
    {
        // ── CPU ──────────────────────────────────────────────────────────────
        new() { Id="cpu2", Category="CPU", Tier=TweakTier.Safe, Name="Win32 Priority Separation", Description="Foreground apps get the largest CPU time slices.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl"" /v Win32PrioritySeparation /t REG_DWORD /d 38 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl"" /v Win32PrioritySeparation /t REG_DWORD /d 2 /f" },
        new() { Id="cpu3", Category="CPU", Tier=TweakTier.Safe, Name="High-Precision Timer", Description="Request a high-resolution global system timer.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel"" /v GlobalTimerResolutionRequests /t REG_DWORD /d 1 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel"" /v GlobalTimerResolutionRequests /t REG_DWORD /d 0 /f" },
        new() { Id="cpu4", Category="CPU", Tier=TweakTier.Safe, Name="Perf Boost Mode", Description="Maximum processor turbo boost.",
            ApplyCmd="powercfg -setacvalueindex scheme_current sub_processor PERFBOOSTMODE 2 && powercfg -setactive scheme_current", RevertCmd="powercfg -setacvalueindex scheme_current sub_processor PERFBOOSTMODE 1 && powercfg -setactive scheme_current" },
        new() { Id="cpu6", Category="CPU", Tier=TweakTier.Extreme, Name="Force All Cores", Description="Disable CPU core parking.",
            ApplyCmd="powercfg -setacvalueindex 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c 54533251-82be-4824-96c1-47b60b740d00 0cc5b647-c1df-4637-891a-dec35c318583 100 & powercfg -setactive scheme_current & exit /b 0", RevertCmd="powercfg -setacvalueindex 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c 54533251-82be-4824-96c1-47b60b740d00 0cc5b647-c1df-4637-891a-dec35c318583 0 & powercfg -setactive scheme_current & exit /b 0" },
        new() { Id="cpu7", Category="CPU", Tier=TweakTier.Extreme, Name="No Power Throttling", Description="Remove background CPU power limits.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Power\PowerThrottling"" /v PowerThrottlingOff /t REG_DWORD /d 1 /f", RevertCmd=@"reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Power\PowerThrottling"" /v PowerThrottlingOff /f" },
        new() { Id="cpu9", Category="CPU", Tier=TweakTier.Safe, Name="Distribute Timers", Description="Spread timer interrupts across all cores instead of core 0.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel"" /v DistributeTimers /t REG_DWORD /d 1 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel"" /v DistributeTimers /t REG_DWORD /d 0 /f" },
        new() { Id="cpu10", Category="CPU", Tier=TweakTier.Extreme, Name="No Lazy Timer", Description="Force always-on high-resolution timer. Lower latency, higher idle power.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel"" /v NoLazyMode /t REG_DWORD /d 1 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel"" /v NoLazyMode /t REG_DWORD /d 0 /f" },
        new() { Id="cpu11", Category="CPU", Tier=TweakTier.Safe, Name="MMCSS Responsiveness = 0", Description="Give foreground/game threads maximum scheduler priority.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v SystemResponsiveness /t REG_DWORD /d 0 /f", RevertCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v SystemResponsiveness /t REG_DWORD /d 20 /f" },

        // ── GPU ──────────────────────────────────────────────────────────────
        new() { Id="gpu1", Category="GPU", Tier=TweakTier.Safe, Name="HW GPU Scheduling", Description="Enable hardware-accelerated GPU scheduling.",
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
        new() { Id="gpu11", Category="GPU", Tier=TweakTier.Safe, Name="No VSync Latency Update", Description="Disable VSync latency update to reduce display pipeline delays.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v EnableVsyncLatencyUpdate /t REG_DWORD /d 0 /f && reg add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v DisableVsyncLatencyUpdate /t REG_DWORD /d 1 /f", RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v EnableVsyncLatencyUpdate /f & reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v DisableVsyncLatencyUpdate /f & exit /b 0" },
        new() { Id="gpu12", Category="GPU", Tier=TweakTier.Extreme, Name="No GPU Preemption", Description="Disable mid-frame GPU preemption. May cause TDR on heavy loads.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v EnableMidGfxPreemption /t REG_DWORD /d 0 /f && reg add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v EnableMidBufferPreemption /t REG_DWORD /d 0 /f", RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v EnableMidGfxPreemption /f & reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v EnableMidBufferPreemption /f & exit /b 0" },
        new() { Id="gpu13", Category="GPU", Tier=TweakTier.Extreme, Name="Max GPU Clocks", Description="Disable dynamic GPU P-states. Higher power draw.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v DisableDynamicPstate /t REG_DWORD /d 1 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v DisableDynamicPstate /t REG_DWORD /d 0 /f" },

        // ── RAM ──────────────────────────────────────────────────────────────
        new() { Id="ram1", Category="RAM", Tier=TweakTier.Safe, Name="Disable Paging Executive", Description="Keep the kernel in RAM instead of paging it to disk.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v DisablePagingExecutive /t REG_DWORD /d 1 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v DisablePagingExecutive /t REG_DWORD /d 0 /f" },
        new() { Id="ram3", Category="RAM", Tier=TweakTier.Safe, Name="Clear Temp & DNS", Description="Flush temp files and the DNS cache (one-shot).",
            ApplyCmd=@"del /q /f /s ""%TEMP%\*"" 2>nul & ipconfig /flushdns & exit /b 0", RevertCmd="" },
        new() { Id="ram4", Category="RAM", Tier=TweakTier.Extreme, Name="No Memory Compression", Description="Disable RAM compression overhead.",
            ApplyCmd="PS:try { Disable-MMAgent -MemoryCompression } catch {}; exit 0", RevertCmd="PS:try { Enable-MMAgent -MemoryCompression } catch {}; exit 0" },
        new() { Id="ram5", Category="RAM", Tier=TweakTier.Safe, Name="No Page Combining", Description="Disable page combining, reduces memory-management overhead.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v DisablePageCombining /t REG_DWORD /d 1 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v DisablePageCombining /t REG_DWORD /d 0 /f" },
        new() { Id="ram7", Category="RAM", Tier=TweakTier.Safe, Name="Optimal Page File", Description="Auto-size the pagefile to 1.5x RAM initial / 3x RAM max.",
            ApplyCmd=@"PS:$ram=[math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory/1MB); $i=[math]::Round($ram*1.5); $m=[math]::Round($ram*3); $pf=Get-WmiObject Win32_PageFileSetting; if($pf){$pf|%{$_.InitialSize=$i;$_.MaximumSize=$m;$_.Put()|Out-Null}}else{Set-WmiInstance -Class Win32_PageFileSetting -Arguments @{Name='C:\pagefile.sys';InitialSize=$i;MaximumSize=$m}|Out-Null}", RevertCmd="" },

        // ── Network ───────────────────────────────────────────────────────────
        new() { Id="net1", Category="Network", Tier=TweakTier.Safe, Name="Disable Nagle's Algorithm", Description="TCPNoDelay for lower latency.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"" /v TCPNoDelay /t REG_DWORD /d 1 /f", RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"" /v TCPNoDelay /f" },
        new() { Id="net2", Category="Network", Tier=TweakTier.Safe, Name="TCP Fast Open", Description="Reduce TCP handshake latency.",
            ApplyCmd="netsh int tcp set global fastopen=enabled fastopenfallback=enabled", RevertCmd="netsh int tcp set global fastopen=disabled fastopenfallback=disabled" },
        new() { Id="net3", Category="Network", Tier=TweakTier.Safe, Name="Enable RSS", Description="Multi-core receive-side scaling.",
            ApplyCmd="netsh int tcp set global rss=enabled", RevertCmd="netsh int tcp set global rss=disabled" },
        new() { Id="net4", Category="Network", Tier=TweakTier.Safe, Name="Flush DNS", Description="Clear the stale DNS cache (one-shot).",
            ApplyCmd="ipconfig /flushdns", RevertCmd="" },
        new() { Id="net5", Category="Network", Tier=TweakTier.Extreme, Name="No Network Throttling", Description="Remove the QoS network-throttling index.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v NetworkThrottlingIndex /t REG_DWORD /d 4294967295 /f", RevertCmd=@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v NetworkThrottlingIndex /t REG_DWORD /d 10 /f" },
        new() { Id="net6", Category="Network", Tier=TweakTier.Safe, Name="QoS Reserve Off", Description="Remove the 20% bandwidth reservation held by Psched.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Psched"" /v NonBestEffortLimit /t REG_DWORD /d 0 /f", RevertCmd=@"reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Psched"" /v NonBestEffortLimit /f" },
        new() { Id="net7", Category="Network", Tier=TweakTier.Safe, Name="No Bandwidth Throttle", Description="Disable TCP receive-side bandwidth throttling.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"" /v DisableBandwidthThrottling /t REG_DWORD /d 1 /f", RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"" /v DisableBandwidthThrottling /f" },
        new() { Id="net8", Category="Network", Tier=TweakTier.Safe, Name="AFD Fast Send", Description="Raise AFD datagram fast-send threshold to 16KB for better UDP throughput.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\AFD\Parameters"" /v FastSendDatagramThreshold /t REG_DWORD /d 16384 /f", RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Services\AFD\Parameters"" /v FastSendDatagramThreshold /f" },
        new() { Id="net9", Category="Network", Tier=TweakTier.Safe, Name="TCP Timed Wait", Description="Reduce TIME_WAIT to 30s to free ports faster.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"" /v TcpTimedWaitDelay /t REG_DWORD /d 30 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"" /v TcpTimedWaitDelay /t REG_DWORD /d 120 /f" },
        new() { Id="net10", Category="Network", Tier=TweakTier.Safe, Name="Max TCP Ports", Description="Expand ephemeral port range to 65534.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"" /v MaxUserPort /t REG_DWORD /d 65534 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"" /v MaxUserPort /t REG_DWORD /d 5000 /f" },
        new() { Id="net11", Category="Network", Tier=TweakTier.Safe, Name="Disable ECN", Description="Disable Explicit Congestion Notification for router compatibility.",
            ApplyCmd="netsh int tcp set global ecncapability=disabled", RevertCmd="netsh int tcp set global ecncapability=enabled" },
        new() { Id="net12", Category="Network", Tier=TweakTier.Safe, Name="Disable RSC", Description="Disable Receive Segment Coalescing, reduces latency spikes.",
            ApplyCmd="netsh int tcp set global rsc=disabled", RevertCmd="netsh int tcp set global rsc=enabled" },
        new() { Id="net13", Category="Network", Tier=TweakTier.Safe, Name="UDP URO", Description="Enable UDP Receive Offload for better UDP throughput.",
            ApplyCmd="netsh int udp set global uro=enabled", RevertCmd="netsh int udp set global uro=disabled" },
        new() { Id="net14", Category="Network", Tier=TweakTier.Safe, Name="CTCP", Description="Compound TCP congestion algorithm for high-bandwidth links.",
            ApplyCmd="netsh int tcp set global congestionprovider=ctcp 2>nul & exit /b 0", RevertCmd="netsh int tcp set global congestionprovider=default 2>nul & exit /b 0" },

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
        new() { Id="slim1", Category="Processes", Tier=TweakTier.Safe, Name="Disable Copilot", Description="Turn off Windows Copilot — removes its background host.",
            ApplyCmd=@"reg add ""HKCU\Software\Policies\Microsoft\Windows\WindowsCopilot"" /v TurnOffWindowsCopilot /t REG_DWORD /d 1 /f & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot"" /v TurnOffWindowsCopilot /t REG_DWORD /d 1 /f & exit /b 0", RevertCmd=@"reg delete ""HKCU\Software\Policies\Microsoft\Windows\WindowsCopilot"" /v TurnOffWindowsCopilot /f & reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot"" /v TurnOffWindowsCopilot /f & exit /b 0" },
        new() { Id="slim2", Category="Processes", Tier=TweakTier.Safe, Name="Disable Widgets", Description="Disable the Widgets board — stops Widgets.exe / WebExperience host.",
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
        new() { Id="slim12", Category="Processes", Tier=TweakTier.Safe, Name="Disable Touch Keyboard Svc", Description="Stop Tablet/Touch Keyboard service on desktops.",
            ApplyCmd="sc config TabletInputService start= disabled & sc stop TabletInputService & exit /b 0", RevertCmd="sc config TabletInputService start= demand & exit /b 0" },
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
        new() { Id="slim18", Category="Processes", Tier=TweakTier.Safe, Name="Disable UPnP / SSDP", Description="Stop SSDP Discovery + UPnP host — only needed for DLNA/media sharing.",
            ApplyCmd="sc config SSDPSRV start= disabled & sc stop SSDPSRV & sc config upnphost start= disabled & sc stop upnphost & exit /b 0", RevertCmd="sc config SSDPSRV start= demand & sc config upnphost start= demand & exit /b 0" },
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
        new() { Id="slim25", Category="Processes", Tier=TweakTier.Extreme, Name="Disable CDP User Service", Description="Disable Connected Devices Platform per-user service (CDPUserSvc) — chatty background callbacks.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\CDPUserSvc"" /v Start /t REG_DWORD /d 4 /f & exit /b 0", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\CDPUserSvc"" /v Start /t REG_DWORD /d 2 /f & exit /b 0" },
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

        // ── Third-party background updaters that run idle on a real (non-clean) install ──
        new() { Id="upd1", Category="Processes", Tier=TweakTier.Safe, Name="Disable Google Update", Description="Stop the Google Update services (gupdate/gupdatem) — Chrome's idle background updater. Update Chrome manually via its menu.",
            ApplyCmd="sc config gupdate start= disabled & sc config gupdatem start= disabled & sc stop gupdate & exit /b 0", RevertCmd="sc config gupdate start= auto & sc config gupdatem start= demand & exit /b 0" },
        new() { Id="upd2", Category="Processes", Tier=TweakTier.Safe, Name="Disable Edge Update", Description="Stop the Microsoft Edge update services (edgeupdate/edgeupdatem) and elevation service.",
            ApplyCmd="sc config edgeupdate start= disabled & sc config edgeupdatem start= disabled & sc config MicrosoftEdgeElevationService start= disabled & sc stop edgeupdate & exit /b 0", RevertCmd="sc config edgeupdate start= auto & sc config edgeupdatem start= demand & sc config MicrosoftEdgeElevationService start= demand & exit /b 0" },
        new() { Id="upd3", Category="Processes", Tier=TweakTier.Safe, Name="Disable Updater Tasks", Description="Disable common third-party auto-update scheduled tasks (Google, Edge, OneDrive) that spawn background processes.",
            ApplyCmd=@"schtasks /Change /TN ""GoogleUpdateTaskMachineCore"" /Disable 2>nul & schtasks /Change /TN ""GoogleUpdateTaskMachineUA"" /Disable 2>nul & schtasks /Change /TN ""MicrosoftEdgeUpdateTaskMachineCore"" /Disable 2>nul & schtasks /Change /TN ""MicrosoftEdgeUpdateTaskMachineUA"" /Disable 2>nul & exit /b 0", RevertCmd=@"schtasks /Change /TN ""GoogleUpdateTaskMachineCore"" /Enable 2>nul & schtasks /Change /TN ""MicrosoftEdgeUpdateTaskMachineCore"" /Enable 2>nul & exit /b 0" },

        // ── Power ─────────────────────────────────────────────────────────────
        new() { Id="pow3", Category="Power", Tier=TweakTier.Extreme, Name="Ultimate Performance", Description="Unlock AND activate the hidden Ultimate Performance power plan — max clocks, best FPS.",
            ApplyCmd=@"PS:$l=(powercfg -list | Out-String); $m=[regex]::Match($l,'([0-9a-fA-F-]{36})\s*\(Ultimate Performance\)'); if($m.Success){$g=$m.Groups[1].Value}else{$o=(powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 | Out-String);$g=[regex]::Match($o,'([0-9a-fA-F-]{36})').Value}; if($g){powercfg -setactive $g}", RevertCmd="powercfg -setactive 381b4222-f694-41f0-9685-ff5bb260df2e" },
        new() { Id="pow4", Category="Power", Tier=TweakTier.Safe, Name="Disable Sleep (AC)", Description="Never sleep while plugged in.",
            ApplyCmd="powercfg /change standby-timeout-ac 0", RevertCmd="powercfg /change standby-timeout-ac 30" },
        new() { Id="pow5", Category="Power", Tier=TweakTier.Safe, Name="Disable Hibernate", Description="Free hiberfil.sys disk space and speed up shutdown.",
            ApplyCmd="powercfg /hibernate off", RevertCmd="powercfg /hibernate on" },
        new() { Id="pow6", Category="Power", Tier=TweakTier.Safe, Name="Disable Fast Startup", Description="Ensure a clean boot, avoid stale driver state.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power"" /v HiberbootEnabled /t REG_DWORD /d 0 /f", RevertCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power"" /v HiberbootEnabled /t REG_DWORD /d 1 /f" },
        new() { Id="pow7", Category="Power", Tier=TweakTier.Safe, Name="CPU 100% Min/Max", Description="Lock CPU at 100% min/max so it never throttles under load.",
            ApplyCmd="powercfg -setacvalueindex scheme_current sub_processor PROCTHROTTLEMIN 100 && powercfg -setacvalueindex scheme_current sub_processor PROCTHROTTLEMAX 100 && powercfg -setactive scheme_current", RevertCmd="powercfg -setacvalueindex scheme_current sub_processor PROCTHROTTLEMIN 5 && powercfg -setacvalueindex scheme_current sub_processor PROCTHROTTLEMAX 100 && powercfg -setactive scheme_current" },

        // ── Latency ───────────────────────────────────────────────────────────
        new() { Id="lat1", Category="Latency", Tier=TweakTier.Safe, Name="Fast App Kill", Description="2s app-kill timeout on shutdown.",
            ApplyCmd=@"reg add ""HKCU\Control Panel\Desktop"" /v WaitToKillAppTimeout /t REG_SZ /d 2000 /f", RevertCmd=@"reg add ""HKCU\Control Panel\Desktop"" /v WaitToKillAppTimeout /t REG_SZ /d 5000 /f" },
        new() { Id="lat2", Category="Latency", Tier=TweakTier.Safe, Name="NTFS Optimization", Description="Tune NTFS memory usage and disable last-access stamps.",
            ApplyCmd="fsutil behavior set memoryusage 2 && fsutil behavior set disablelastaccess 1", RevertCmd="fsutil behavior set memoryusage 1 && fsutil behavior set disablelastaccess 0" },
        new() { Id="lat3", Category="Latency", Tier=TweakTier.Safe, Name="IRQ8 Priority", Description="Boost IRQ8 interrupt priority. May conflict on some hardware.",
            ApplyCmd=@"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl"" /v IRQ8Priority /t REG_DWORD /d 1 /f", RevertCmd=@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl"" /v IRQ8Priority /f" },
        new() { Id="lat4", Category="Latency", Tier=TweakTier.Safe, Name="MFT Zone 4", Description="Reserve maximum NTFS MFT space (zone 4).",
            ApplyCmd="fsutil behavior set mftzone 4", RevertCmd="fsutil behavior set mftzone 1" },
        new() { Id="lat5", Category="Latency", Tier=TweakTier.Safe, Name="No 8.3 Names", Description="Disable NTFS 8.3 short-name generation. Faster file creation.",
            ApplyCmd="fsutil behavior set disable8dot3 1", RevertCmd="fsutil behavior set disable8dot3 0" },

        // ── Game ──────────────────────────────────────────────────────────────
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
        new() { Id="gm9", Category="Game", Tier=TweakTier.Safe, Name="GPU MSI Mode", Description="Enable Message Signaled Interrupts on the GPU only, if supported. Safer than all-devices — applying MSI to USB/input controllers can freeze the mouse. Takes effect after reboot.",
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
        new() { Id="p2", Category="Privacy", Tier=TweakTier.Safe, Name="Block Ad Delivery", Description="Stop Microsoft advertising content delivery.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ContentDeliveryAllowed /t REG_DWORD /d 0 /f", RevertCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ContentDeliveryAllowed /t REG_DWORD /d 1 /f" },
        new() { Id="p5", Category="Privacy", Tier=TweakTier.Safe, Name="Disable Location", Description="Block location access for apps.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location"" /v Value /t REG_SZ /d Deny /f", RevertCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location"" /v Value /t REG_SZ /d Allow /f" },
        new() { Id="p6", Category="Privacy", Tier=TweakTier.Safe, Name="Disable Cortana", Description="Remove Cortana completely.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v AllowCortana /t REG_DWORD /d 0 /f", RevertCmd=@"reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v AllowCortana /f" },
        new() { Id="p7", Category="Privacy", Tier=TweakTier.Safe, Name="Disable Activity Feed", Description="Stop timeline / activity tracking.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\System"" /v EnableActivityFeed /t REG_DWORD /d 0 /f", RevertCmd=@"reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Windows\System"" /v EnableActivityFeed /f" },
        new() { Id="p8", Category="Privacy", Tier=TweakTier.Extreme, Name="Disable Windows Update", Description="Stop the Windows Update service. Re-enable to get updates.",
            ApplyCmd="sc config wuauserv start= disabled & sc stop wuauserv & exit /b 0", RevertCmd="sc config wuauserv start= demand & sc start wuauserv & exit /b 0" },

        // ── 0.8.8 quality additions ──────────────────────────────────────────
        new() { Id="deb20", Category="Debloat", Tier=TweakTier.Safe, Name="Disable Search Highlights", Description="Remove the rotating web content in the taskbar search box.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\SearchSettings"" /v IsDynamicSearchBoxEnabled /t REG_DWORD /d 0 /f", RevertCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\SearchSettings"" /v IsDynamicSearchBoxEnabled /t REG_DWORD /d 1 /f" },
        new() { Id="deb21", Category="Debloat", Tier=TweakTier.Safe, Name="No Startup App Delay", Description="Launch startup apps immediately instead of Windows' staggered delay.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize"" /v StartupDelayInMSec /t REG_DWORD /d 0 /f", RevertCmd=@"reg delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize"" /v StartupDelayInMSec /f & exit /b 0" },
        new() { Id="net20", Category="Network", Tier=TweakTier.Extreme, Name="Cloudflare DNS", Description="Use 1.1.1.1 / 1.0.0.1 — consistently among the fastest public resolvers. Revert returns to your router / DHCP DNS.",
            ApplyCmd=@"PS:try { Get-NetAdapter -Physical | Where-Object Status -eq 'Up' | ForEach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.ifIndex -ServerAddresses '1.1.1.1','1.0.0.1' } } catch {}; exit 0", RevertCmd=@"PS:try { Get-NetAdapter -Physical | ForEach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.ifIndex -ResetServerAddresses } } catch {}; ipconfig /flushdns | Out-Null; exit 0" },
        new() { Id="cpu20", Category="CPU", Tier=TweakTier.Extreme, Name="Disable Core Parking", Description="Keep all cores unparked for snappier frame pacing. Slightly higher idle power.",
            ApplyCmd="powercfg -setacvalueindex scheme_current sub_processor CPMINCORES 100 & powercfg -setactive scheme_current & exit /b 0", RevertCmd="powercfg -setacvalueindex scheme_current sub_processor CPMINCORES 10 & powercfg -setactive scheme_current & exit /b 0" },
        new() { Id="slim40", Category="Processes", Tier=TweakTier.Safe, Name="No Edge Preload", Description="Stop Microsoft Edge from pre-launching and running in the background when closed.",
            ApplyCmd=@"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Edge"" /v StartupBoostEnabled /t REG_DWORD /d 0 /f & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Edge"" /v BackgroundModeEnabled /t REG_DWORD /d 0 /f", RevertCmd=@"reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Edge"" /v StartupBoostEnabled /f & reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Edge"" /v BackgroundModeEnabled /f & exit /b 0" },
        new() { Id="stor20", Category="Storage", Tier=TweakTier.Extreme, Name="NTFS RAM Boost", Description="Let NTFS use more RAM for metadata caching (fsutil memoryusage 2). Best with 16 GB+.",
            ApplyCmd="fsutil behavior set memoryusage 2", RevertCmd="fsutil behavior set memoryusage 1" },

        // ── Restore (reset to defaults — never auto-selected) ───────────────────
        new() { Id="rst1", Category="Restore", Tier=TweakTier.Safe, Name="Restore Network", Description="Reset TCP/IP global settings to defaults.",
            ApplyCmd="netsh int tcp set global autotuninglevel=normal & netsh int tcp set global ecncapability=enabled & netsh int tcp set global rsc=enabled & netsh int tcp set global congestionprovider=default & exit /b 0", RevertCmd="" },
        new() { Id="rst2", Category="Restore", Tier=TweakTier.Safe, Name="Restore Power", Description="Switch back to the Balanced power plan.",
            ApplyCmd="powercfg -setactive 381b4222-f694-41f0-9685-ff5bb260df2e", RevertCmd="" },
        new() { Id="rst3", Category="Restore", Tier=TweakTier.Safe, Name="Re-enable Services", Description="Restore SysMain and DiagTrack to automatic.",
            ApplyCmd="sc config SysMain start= auto & sc start SysMain & sc config DiagTrack start= auto & sc start DiagTrack & exit /b 0", RevertCmd="" },
        new() { Id="rst4", Category="Restore", Tier=TweakTier.Safe, Name="Restore Visual FX", Description="Re-enable Windows animations and effects.",
            ApplyCmd=@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects"" /v VisualFXSetting /t REG_DWORD /d 0 /f", RevertCmd="" },

    };
}
