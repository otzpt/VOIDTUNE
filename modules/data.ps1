# VOIDTUNE - modules/data.ps1
# Copyright (C) 2026 @OTZPT - GPL v3
# All data definitions: tweaks, apps, services, privacy

# ── TWEAKS ────────────────────────────────────────────────────────────────────
$script:TWEAKS = @(
    # CPU
    [TI]@{Id='cpu1';Cat='cpu';Badge='SAFE';BC='#22C55E';Name='HIGH PERF PLAN';Desc='Set power scheme to high performance';Cmd='powercfg -setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c';RevertCmd='powercfg -setactive 381b4222-f694-41f0-9685-ff5bb260df2e'},
    [TI]@{Id='cpu2';Cat='cpu';Badge='SAFE';BC='#22C55E';Name='WIN32 PRIORITY';Desc='Foreground app gets max CPU slices';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl" /v Win32PrioritySeparation /t REG_DWORD /d 38 /f';RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl" /v Win32PrioritySeparation /t REG_DWORD /d 2 /f'},
    [TI]@{Id='cpu3';Cat='cpu';Badge='SAFE';BC='#22C55E';Name='TIMER RESOLUTION';Desc='High-precision system timer (GlobalTimerResolutionRequests)';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel" /v GlobalTimerResolutionRequests /t REG_DWORD /d 1 /f';RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel" /v GlobalTimerResolutionRequests /t REG_DWORD /d 0 /f'},
    [TI]@{Id='cpu4';Cat='cpu';Badge='SAFE';BC='#22C55E';Name='PERF BOOST MODE';Desc='Max processor turbo boost';Cmd='powercfg -setacvalueindex scheme_current sub_processor PERFBOOSTMODE 2 && powercfg -setactive scheme_current';RevertCmd='powercfg -setacvalueindex scheme_current sub_processor PERFBOOSTMODE 1 && powercfg -setactive scheme_current'},
    [TI]@{Id='cpu5';Cat='cpu';Badge='EXTREME';BC='#7c3aed';Name='ULTIMATE PERF';Desc='Hidden ultra performance plan';Cmd='powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61';RevertCmd='powercfg -setactive 381b4222-f694-41f0-9685-ff5bb260df2e'},
    [TI]@{Id='cpu6';Cat='cpu';Badge='EXTREME';BC='#7c3aed';Name='FORCE ALL CORES';Desc='Disable core parking';Cmd='powercfg -setacvalueindex 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c 54533251-82be-4824-96c1-47b60b740d00 0cc5b647-c1df-4637-891a-dec35c318583 100 & powercfg -setacvalueindex 381b4222-f694-41f0-9685-ff5bb260df2e 54533251-82be-4824-96c1-47b60b740d00 0cc5b647-c1df-4637-891a-dec35c318583 100 & powercfg -setactive scheme_current & exit /b 0';RevertCmd='powercfg -setacvalueindex 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c 54533251-82be-4824-96c1-47b60b740d00 0cc5b647-c1df-4637-891a-dec35c318583 0 & powercfg -setacvalueindex 381b4222-f694-41f0-9685-ff5bb260df2e 54533251-82be-4824-96c1-47b60b740d00 0cc5b647-c1df-4637-891a-dec35c318583 0 & powercfg -setactive scheme_current & exit /b 0'},
    [TI]@{Id='cpu7';Cat='cpu';Badge='EXTREME';BC='#7c3aed';Name='NO PWR THROTTLE';Desc='Remove background CPU limits';Cmd='reg add "HKLM\SOFTWARE\Policies\Microsoft\Power\PowerThrottling" /v PowerThrottlingOff /t REG_DWORD /d 1 /f';RevertCmd='reg delete "HKLM\SOFTWARE\Policies\Microsoft\Power\PowerThrottling" /v PowerThrottlingOff /f'},
    [TI]@{Id='cpu8';Cat='cpu';Badge='SAFE';BC='#22C55E';Name='DPC WATCHDOG';Desc='Tune DPC watchdog and timeout to reduce deferred procedure call delays';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel" /v DpcWatchdogProfileOffset /t REG_DWORD /d 0 /f && reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel" /v DpcTimeout /t REG_DWORD /d 0 /f';RevertCmd='reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel" /v DpcWatchdogProfileOffset /f & reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel" /v DpcTimeout /f'},
    [TI]@{Id='cpu9';Cat='cpu';Badge='SAFE';BC='#22C55E';Name='DISTRIBUTE TIMERS';Desc='Spread timer interrupts across all CPU cores instead of routing to core 0';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel" /v DistributeTimers /t REG_DWORD /d 1 /f';RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel" /v DistributeTimers /t REG_DWORD /d 0 /f'},
    [TI]@{Id='cpu10';Cat='cpu';Badge='EXTREME';BC='#7c3aed';Name='NO LAZY TIMER';Desc='Force always-on high-resolution timer — lower latency, higher idle power';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel" /v NoLazyMode /t REG_DWORD /d 1 /f';RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel" /v NoLazyMode /t REG_DWORD /d 0 /f'},
    [TI]@{Id='cpu11';Cat='cpu';Badge='SAFE';BC='#22C55E';Name='MMCSS RESPONSIVE=0';Desc='SystemResponsiveness=0 — gives foreground/game threads maximum CPU scheduler slices';Cmd='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v SystemResponsiveness /t REG_DWORD /d 0 /f';RevertCmd='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v SystemResponsiveness /t REG_DWORD /d 20 /f'},
    [TI]@{Id='cpu12';Cat='cpu';Badge='EXTREME';BC='#7c3aed';Name='NO SPECTRE MITIG';Desc='Disable Spectre/Meltdown mitigations — recovers 5-15% on pre-Zen3/pre-10th-gen. SECURITY RISK on shared machines.';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v FeatureSettingsOverride /t REG_DWORD /d 3 /f && reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v FeatureSettingsOverrideMask /t REG_DWORD /d 3 /f';RevertCmd='reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v FeatureSettingsOverride /f & reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v FeatureSettingsOverrideMask /f'},
    # GPU
    [TI]@{Id='gpu1';Cat='gpu';Badge='SAFE';BC='#22C55E';Name='HW GPU SCHEDULING';Desc='Hardware-accelerated GPU scheduling';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v HwSchMode /t REG_DWORD /d 2 /f';RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v HwSchMode /t REG_DWORD /d 1 /f'},
    [TI]@{Id='gpu2';Cat='gpu';Badge='SAFE';BC='#22C55E';Name='GPU PRIORITY';Desc='Priority 8 for games';Cmd='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "GPU Priority" /t REG_DWORD /d 8 /f';RevertCmd='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "GPU Priority" /t REG_DWORD /d 1 /f'},
    [TI]@{Id='gpu3';Cat='gpu';Badge='SAFE';BC='#22C55E';Name='TDR DELAY';Desc='Prevent GPU timeout crashes';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v TdrDelay /t REG_DWORD /d 10 /f';RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v TdrDelay /t REG_DWORD /d 2 /f'},
    [TI]@{Id='gpu4';Cat='gpu';Badge='SAFE';BC='#22C55E';Name='DIRECT FLIP';Desc='Reduce display latency via direct flip';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v ForceDirectFlip /t REG_DWORD /d 1 /f';RevertCmd='reg delete "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v ForceDirectFlip /f'},
    [TI]@{Id='gpu5';Cat='gpu';Badge='EXTREME';BC='#7c3aed';Name='DISABLE MPO';Desc='Fix MPO stutters. May cause flickering on some GPUs - revertible';Cmd='reg add "HKLM\SOFTWARE\Microsoft\Windows\Dwm" /v OverlayTestMode /t REG_DWORD /d 5 /f';RevertCmd='reg delete "HKLM\SOFTWARE\Microsoft\Windows\Dwm" /v OverlayTestMode /f'},
    [TI]@{Id='gpu6';Cat='gpu';Badge='SAFE';BC='#22C55E';Name='ASYNC PRESENT';Desc='Enable asynchronous GPU presentation for lower frame latency';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v EnableAsyncPresentation /t REG_DWORD /d 1 /f';RevertCmd='reg delete "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v EnableAsyncPresentation /f'},
    [TI]@{Id='gpu7';Cat='gpu';Badge='SAFE';BC='#22C55E';Name='D3D12 INDEP FLIP';Desc='Force D3D12 independent flip — reduces DWM overhead for fullscreen apps';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v D3D12ForceIndependentFlip /t REG_DWORD /d 1 /f';RevertCmd='reg delete "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v D3D12ForceIndependentFlip /f'},
    [TI]@{Id='gpu8';Cat='gpu';Badge='SAFE';BC='#22C55E';Name='ASYNC COMPUTE';Desc='Enable asynchronous GPU compute for better utilisation';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v EnableAsyncCompute /t REG_DWORD /d 1 /f';RevertCmd='reg delete "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v EnableAsyncCompute /f'},
    [TI]@{Id='gpu9';Cat='gpu';Badge='SAFE';BC='#22C55E';Name='NO FRAME BUF COMPRESS';Desc='Disable GPU framebuffer compression — reduces latency on some hardware';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v EnableFrameBufferCompression /t REG_DWORD /d 0 /f';RevertCmd='reg delete "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v EnableFrameBufferCompression /f'},
    [TI]@{Id='gpu10';Cat='gpu';Badge='SAFE';BC='#22C55E';Name='DWM QUEUE SIZE';Desc='Limit DWM queued buffers to 2 for lower display latency';Cmd='reg add "HKLM\SOFTWARE\Microsoft\Windows\Dwm" /v MaxQueuedBuffers /t REG_DWORD /d 2 /f';RevertCmd='reg delete "HKLM\SOFTWARE\Microsoft\Windows\Dwm" /v MaxQueuedBuffers /f'},
    [TI]@{Id='gpu11';Cat='gpu';Badge='SAFE';BC='#22C55E';Name='NO VSYNC LAT UPDATE';Desc='Disable VSync latency update mechanism to reduce display pipeline delays';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v EnableVsyncLatencyUpdate /t REG_DWORD /d 0 /f && reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v DisableVsyncLatencyUpdate /t REG_DWORD /d 1 /f';RevertCmd='reg delete "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v EnableVsyncLatencyUpdate /f & reg delete "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v DisableVsyncLatencyUpdate /f'},
    [TI]@{Id='gpu12';Cat='gpu';Badge='EXTREME';BC='#7c3aed';Name='NO GPU PREEMPT';Desc='Disable mid-frame GPU preemption — lower latency but may cause TDR on heavy loads';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v EnableMidGfxPreemption /t REG_DWORD /d 0 /f && reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v EnableMidBufferPreemption /t REG_DWORD /d 0 /f';RevertCmd='reg delete "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v EnableMidGfxPreemption /f & reg delete "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v EnableMidBufferPreemption /f'},
    [TI]@{Id='gpu13';Cat='gpu';Badge='EXTREME';BC='#7c3aed';Name='MAX GPU CLOCKS';Desc='Disable dynamic GPU P-states — GPU stays at max clocks, higher power draw';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v DisableDynamicPstate /t REG_DWORD /d 1 /f';RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v DisableDynamicPstate /t REG_DWORD /d 0 /f'},
    # RAM
    [TI]@{Id='ram1';Cat='ram';Badge='SAFE';BC='#22C55E';Name='DISABLE PAGING';Desc='Keep kernel in RAM';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v DisablePagingExecutive /t REG_DWORD /d 1 /f';RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v DisablePagingExecutive /t REG_DWORD /d 0 /f'},
    [TI]@{Id='ram2';Cat='ram';Badge='SAFE';BC='#22C55E';Name='LARGE SYS CACHE';Desc='Increase RAM cache';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v LargeSystemCache /t REG_DWORD /d 1 /f';RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v LargeSystemCache /t REG_DWORD /d 0 /f'},
    [TI]@{Id='ram3';Cat='ram';Badge='SAFE';BC='#22C55E';Name='CLEAR TEMP';Desc='Flush temp files and DNS';Cmd='del /q /f /s "%TEMP%\*" 2>nul & ipconfig /flushdns';RevertCmd=''},
    [TI]@{Id='ram4';Cat='ram';Badge='EXTREME';BC='#7c3aed';Name='NO MEM COMPRESS';Desc='Remove compression overhead';Cmd='PS:Disable-MMAgent -MemoryCompression';RevertCmd='PS:Enable-MMAgent -MemoryCompression'},
    [TI]@{Id='ram5';Cat='ram';Badge='SAFE';BC='#22C55E';Name='NO PAGE COMBINE';Desc='Disable page combining — reduces memory management overhead at the cost of slightly higher RAM usage';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v DisablePageCombining /t REG_DWORD /d 1 /f';RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v DisablePageCombining /t REG_DWORD /d 0 /f'},
    [TI]@{Id='ram6';Cat='ram';Badge='SAFE';BC='#22C55E';Name='NO WS AGING';Desc='Disable working set trimming so active process pages stay in RAM longer';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v DisableWorkingSetTrimming /t REG_DWORD /d 1 /f';RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v DisableWorkingSetTrimming /t REG_DWORD /d 0 /f'},
    [TI]@{Id='ram7';Cat='ram';Badge='SAFE';BC='#22C55E';Name='OPTIMAL PAGE FILE';Desc='Set pagefile to 1.5x RAM initial size and 3x RAM max (auto-calculated from your installed RAM)';Cmd='PS:$ram=[math]::Round((Get-CimInstance Win32_ComputerSystem -EA SilentlyContinue).TotalPhysicalMemory/1MB); $init=[math]::Round($ram*1.5); $max=[math]::Round($ram*3); $pf=Get-WmiObject Win32_PageFileSetting -EA SilentlyContinue; if($pf){ $pf | ForEach-Object { $_.InitialSize=$init; $_.MaximumSize=$max; $_.Put() | Out-Null } } else { Set-WmiInstance -Class Win32_PageFileSetting -Arguments @{Name="C:\pagefile.sys";InitialSize=$init;MaximumSize=$max} | Out-Null }; Write-Output "PageFile set: initial=${init}MB max=${max}MB"';RevertCmd=''},
    # Network
    [TI]@{Id='net1';Cat='net';Badge='SAFE';BC='#22C55E';Name='DISABLE NAGLES';Desc='TCPNoDelay lower latency';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TCPNoDelay /t REG_DWORD /d 1 /f';RevertCmd='reg delete "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TCPNoDelay /f'},
    [TI]@{Id='net2';Cat='net';Badge='SAFE';BC='#22C55E';Name='TCP FAST OPEN';Desc='Reduce handshake latency';Cmd='netsh int tcp set global fastopen=enabled fastopenfallback=enabled';RevertCmd='netsh int tcp set global fastopen=disabled fastopenfallback=disabled'},
    [TI]@{Id='net3';Cat='net';Badge='SAFE';BC='#22C55E';Name='ENABLE RSS';Desc='Multi-core receive scaling';Cmd='netsh int tcp set global rss=enabled';RevertCmd='netsh int tcp set global rss=disabled'},
    [TI]@{Id='net4';Cat='net';Badge='SAFE';BC='#22C55E';Name='FLUSH DNS';Desc='Clear stale DNS cache';Cmd='ipconfig /flushdns';RevertCmd=''},
    [TI]@{Id='net5';Cat='net';Badge='EXTREME';BC='#7c3aed';Name='NO NET THROTTLE';Desc='Remove QoS bandwidth limits (NetworkThrottlingIndex)';Cmd='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v NetworkThrottlingIndex /t REG_DWORD /d 4294967295 /f';RevertCmd='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v NetworkThrottlingIndex /t REG_DWORD /d 10 /f'},
    [TI]@{Id='net6';Cat='net';Badge='SAFE';BC='#22C55E';Name='QOS RESERVE OFF';Desc='Remove the 20% bandwidth reservation that Psched holds back by default';Cmd='reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\Psched" /v NonBestEffortLimit /t REG_DWORD /d 0 /f';RevertCmd='reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\Psched" /v NonBestEffortLimit /f'},
    [TI]@{Id='net7';Cat='net';Badge='SAFE';BC='#22C55E';Name='NO BW THROTTLE';Desc='Disable TCP receive-side bandwidth throttling';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v DisableBandwidthThrottling /t REG_DWORD /d 1 /f';RevertCmd='reg delete "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v DisableBandwidthThrottling /f'},
    [TI]@{Id='net8';Cat='net';Badge='SAFE';BC='#22C55E';Name='AFD FAST SEND';Desc='Raise AFD datagram fast-send threshold to 16KB for better UDP throughput';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Services\AFD\Parameters" /v FastSendDatagramThreshold /t REG_DWORD /d 16384 /f';RevertCmd='reg delete "HKLM\SYSTEM\CurrentControlSet\Services\AFD\Parameters" /v FastSendDatagramThreshold /f'},
    [TI]@{Id='net9';Cat='net';Badge='SAFE';BC='#22C55E';Name='TCP TIMED WAIT';Desc='Reduce TIME_WAIT to 30s to free ports faster after connections close';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpTimedWaitDelay /t REG_DWORD /d 30 /f';RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpTimedWaitDelay /t REG_DWORD /d 120 /f'},
    [TI]@{Id='net10';Cat='net';Badge='SAFE';BC='#22C55E';Name='MAX TCP PORTS';Desc='Expand ephemeral port range to 65534 — useful under heavy concurrent connections';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v MaxUserPort /t REG_DWORD /d 65534 /f';RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v MaxUserPort /t REG_DWORD /d 5000 /f'},
    [TI]@{Id='net11';Cat='net';Badge='SAFE';BC='#22C55E';Name='DISABLE ECN';Desc='Disable Explicit Congestion Notification — improves compatibility with some routers/ISPs';Cmd='netsh int tcp set global ecncapability=disabled';RevertCmd='netsh int tcp set global ecncapability=enabled'},
    [TI]@{Id='net12';Cat='net';Badge='SAFE';BC='#22C55E';Name='DISABLE RSC';Desc='Disable Receive Segment Coalescing — reduces latency spikes on some NICs';Cmd='netsh int tcp set global rsc=disabled';RevertCmd='netsh int tcp set global rsc=enabled'},
    [TI]@{Id='net13';Cat='net';Badge='SAFE';BC='#22C55E';Name='UDP URO';Desc='Enable UDP URO (UDP Receive Offload) for better UDP throughput';Cmd='netsh int udp set global uro=enabled';RevertCmd='netsh int udp set global uro=disabled'},
    [TI]@{Id='net14';Cat='net';Badge='SAFE';BC='#22C55E';Name='CTCP';Desc='Compound TCP congestion algorithm — better throughput on high-bandwidth links';Cmd='netsh int tcp set global congestionprovider=ctcp';RevertCmd='netsh int tcp set global congestionprovider=default'},
    # Debloat
    [TI]@{Id='deb1';Cat='deb';Badge='SAFE';BC='#22C55E';Name='DISABLE TELEMETRY';Desc='Kill DiagTrack service';Cmd='sc config DiagTrack start= disabled & sc stop DiagTrack & exit /b 0';RevertCmd='sc config DiagTrack start= auto & sc start DiagTrack & exit /b 0'},
    [TI]@{Id='deb2';Cat='deb';Badge='SAFE';BC='#22C55E';Name='BLOCK ADS';Desc='Stop ContentDelivery ads';Cmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v ContentDeliveryAllowed /t REG_DWORD /d 0 /f';RevertCmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v ContentDeliveryAllowed /t REG_DWORD /d 1 /f'},
    [TI]@{Id='deb3';Cat='deb';Badge='SAFE';BC='#22C55E';Name='DISABLE CORTANA';Desc='Remove Cortana and Bing search';Cmd='reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search" /v AllowCortana /t REG_DWORD /d 0 /f';RevertCmd='reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search" /v AllowCortana /f'},
    [TI]@{Id='deb4';Cat='deb';Badge='SAFE';BC='#22C55E';Name='DISABLE GAME BAR';Desc='Remove Xbox Game Bar overhead';Cmd='reg add "HKCU\System\GameConfigStore" /v GameDVR_Enabled /t REG_DWORD /d 0 /f';RevertCmd='reg add "HKCU\System\GameConfigStore" /v GameDVR_Enabled /t REG_DWORD /d 1 /f'},
    [TI]@{Id='deb5';Cat='deb';Badge='SAFE';BC='#22C55E';Name='DISABLE ANIM';Desc='Disable all visual effects';Cmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects" /v VisualFXSetting /t REG_DWORD /d 2 /f';RevertCmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects" /v VisualFXSetting /t REG_DWORD /d 0 /f'},
    [TI]@{Id='deb6';Cat='deb';Badge='SAFE';BC='#22C55E';Name='RAW MOUSE';Desc='Disable pointer acceleration';Cmd='reg add "HKCU\Control Panel\Mouse" /v MouseSpeed /t REG_SZ /d 0 /f';RevertCmd='reg add "HKCU\Control Panel\Mouse" /v MouseSpeed /t REG_SZ /d 1 /f'},
    [TI]@{Id='deb7';Cat='deb';Badge='EXTREME';BC='#7c3aed';Name='DISABLE SUPERFETCH';Desc='Stop SysMain preloading';Cmd='sc config SysMain start= disabled & sc stop SysMain & exit /b 0';RevertCmd='sc config SysMain start= auto & sc start SysMain & exit /b 0'},
    [TI]@{Id='deb8';Cat='deb';Badge='SAFE';BC='#22C55E';Name='DISABLE AERO PEEK';Desc='Remove hover-to-peek desktop effect';Cmd='reg add "HKCU\Software\Microsoft\Windows\DWM" /v EnableAeroPeek /t REG_DWORD /d 0 /f';RevertCmd='reg add "HKCU\Software\Microsoft\Windows\DWM" /v EnableAeroPeek /t REG_DWORD /d 1 /f'},
    [TI]@{Id='deb9';Cat='deb';Badge='SAFE';BC='#22C55E';Name='DISABLE AERO SHAKE';Desc='Stop shake-to-minimize all windows';Cmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v DisallowShaking /t REG_DWORD /d 1 /f';RevertCmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v DisallowShaking /t REG_DWORD /d 0 /f'},
    [TI]@{Id='deb10';Cat='deb';Badge='SAFE';BC='#22C55E';Name='DISABLE SNAP ASSIST';Desc='Remove snap layout popup when dragging windows';Cmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v SnapAssist /t REG_DWORD /d 0 /f';RevertCmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v SnapAssist /t REG_DWORD /d 1 /f'},
    [TI]@{Id='deb11';Cat='deb';Badge='SAFE';BC='#22C55E';Name='MENU DELAY 0';Desc='Instant menu open, no hover delay';Cmd='reg add "HKCU\Control Panel\Desktop" /v MenuShowDelay /t REG_SZ /d 0 /f';RevertCmd='reg add "HKCU\Control Panel\Desktop" /v MenuShowDelay /t REG_SZ /d 400 /f'},
    [TI]@{Id='deb12';Cat='deb';Badge='SAFE';BC='#22C55E';Name='NO THUMBNAIL CACHE';Desc='Skip building thumbcache.db on network/temp drives';Cmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v DisableThumbnailCache /t REG_DWORD /d 1 /f';RevertCmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v DisableThumbnailCache /t REG_DWORD /d 0 /f'},
    [TI]@{Id='deb13';Cat='deb';Badge='SAFE';BC='#22C55E';Name='DISABLE STICKINESS';Desc='Turn off sticky/filter/toggle keys popups';Cmd='reg add "HKCU\Control Panel\Accessibility\StickyKeys" /v Flags /t REG_SZ /d 506 /f && reg add "HKCU\Control Panel\Accessibility\ToggleKeys" /v Flags /t REG_SZ /d 58 /f && reg add "HKCU\Control Panel\Accessibility\Keyboard Response" /v Flags /t REG_SZ /d 122 /f';RevertCmd='reg add "HKCU\Control Panel\Accessibility\StickyKeys" /v Flags /t REG_SZ /d 510 /f'},
    # Power
    [TI]@{Id='pow1';Cat='pow';Badge='SAFE';BC='#22C55E';Name='BALANCED';Desc='Standard balanced plan';Cmd='powercfg -setactive 381b4222-f694-41f0-9685-ff5bb260df2e';RevertCmd=''},
    [TI]@{Id='pow2';Cat='pow';Badge='SAFE';BC='#22C55E';Name='HIGH PERFORMANCE';Desc='Max clocks no saving';Cmd='powercfg -setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c';RevertCmd='powercfg -setactive 381b4222-f694-41f0-9685-ff5bb260df2e'},
    [TI]@{Id='pow3';Cat='pow';Badge='EXTREME';BC='#7c3aed';Name='ULTIMATE PERF';Desc='No micro-latency pauses';Cmd='powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61';RevertCmd='powercfg -setactive 381b4222-f694-41f0-9685-ff5bb260df2e'},
    [TI]@{Id='pow4';Cat='pow';Badge='SAFE';BC='#22C55E';Name='DISABLE SLEEP';Desc='No sleep while plugged in';Cmd='powercfg /change standby-timeout-ac 0';RevertCmd='powercfg /change standby-timeout-ac 30'},
    [TI]@{Id='pow5';Cat='pow';Badge='SAFE';BC='#22C55E';Name='DISABLE HIBERNATE';Desc='Free hiberfil.sys disk space and speed up shutdown';Cmd='powercfg /hibernate off';RevertCmd='powercfg /hibernate on'},
    [TI]@{Id='pow6';Cat='pow';Badge='SAFE';BC='#22C55E';Name='DISABLE FAST STARTUP';Desc='Disable fast startup — ensures clean boot and avoids stale driver state';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power" /v HiberbootEnabled /t REG_DWORD /d 0 /f';RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power" /v HiberbootEnabled /t REG_DWORD /d 1 /f'},
    [TI]@{Id='pow7';Cat='pow';Badge='SAFE';BC='#22C55E';Name='CPU 100% MIN/MAX';Desc='Lock CPU at 100% min/max so it never throttles under load';Cmd='powercfg -setacvalueindex scheme_current sub_processor PROCTHROTTLEMIN 100 && powercfg -setacvalueindex scheme_current sub_processor PROCTHROTTLEMAX 100 && powercfg -setactive scheme_current';RevertCmd='powercfg -setacvalueindex scheme_current sub_processor PROCTHROTTLEMIN 5 && powercfg -setacvalueindex scheme_current sub_processor PROCTHROTTLEMAX 100 && powercfg -setactive scheme_current'},
    # Latency
    [TI]@{Id='lat1';Cat='lat';Badge='SAFE';BC='#22C55E';Name='FAST APP KILL';Desc='2s app kill timeout';Cmd='reg add "HKCU\Control Panel\Desktop" /v WaitToKillAppTimeout /t REG_SZ /d 2000 /f';RevertCmd='reg add "HKCU\Control Panel\Desktop" /v WaitToKillAppTimeout /t REG_SZ /d 5000 /f'},
    [TI]@{Id='lat2';Cat='lat';Badge='SAFE';BC='#22C55E';Name='FSUTIL OPT';Desc='Optimize NTFS memory usage and disable last-access timestamps';Cmd='fsutil behavior set memoryusage 2 && fsutil behavior set disablelastaccess 1';RevertCmd='fsutil behavior set memoryusage 1 && fsutil behavior set disablelastaccess 0'},
    [TI]@{Id='lat3';Cat='lat';Badge='SAFE';BC='#22C55E';Name='IRQ PRIORITY';Desc='Boost IRQ8 interrupt priority. May cause device conflicts on some hardware';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl" /v IRQ8Priority /t REG_DWORD /d 1 /f';RevertCmd='reg delete "HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl" /v IRQ8Priority /f'},
    [TI]@{Id='lat4';Cat='lat';Badge='SAFE';BC='#22C55E';Name='MFT ZONE 4';Desc='Reserve maximum NTFS MFT space (zone 4) — reduces fragmentation on busy drives';Cmd='fsutil behavior set mftzone 4';RevertCmd='fsutil behavior set mftzone 1'},
    [TI]@{Id='lat5';Cat='lat';Badge='SAFE';BC='#22C55E';Name='NO 8DOT3 NAMES';Desc='Disable NTFS 8.3 short filename generation — speeds up file creation on large dirs';Cmd='fsutil behavior set disable8dot3 1';RevertCmd='fsutil behavior set disable8dot3 0'},
    # Game
    [TI]@{Id='gm1';Cat='game';Badge='SAFE';BC='#22C55E';Name='GAME MODE';Desc='Enable Windows Game Mode';Cmd='reg add "HKCU\Software\Microsoft\GameBar" /v AllowAutoGameMode /t REG_DWORD /d 1 /f';RevertCmd='reg add "HKCU\Software\Microsoft\GameBar" /v AllowAutoGameMode /t REG_DWORD /d 0 /f'},
    [TI]@{Id='gm2';Cat='game';Badge='SAFE';BC='#22C55E';Name='MMCSS HIGH';Desc='High scheduling for games';Cmd='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Scheduling Category" /t REG_SZ /d High /f';RevertCmd='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Scheduling Category" /t REG_SZ /d Normal /f'},
    [TI]@{Id='gm3';Cat='game';Badge='EXTREME';BC='#7c3aed';Name='NO OVERLAY HOOKS';Desc='Disable FSE overlay hooks';Cmd='reg add "HKCU\System\GameConfigStore" /v GameDVR_FSEBehavior /t REG_DWORD /d 2 /f';RevertCmd='reg delete "HKCU\System\GameConfigStore" /v GameDVR_FSEBehavior /f'},
    [TI]@{Id='gm4';Cat='game';Badge='SAFE';BC='#22C55E';Name='FULLSCREEN OPTIM OFF';Desc='Disable DWM fullscreen optimizations for lower input lag';Cmd='reg add "HKCU\System\GameConfigStore" /v GameDVR_DXGIHonorFSEWindowsCompatible /t REG_DWORD /d 1 /f && reg add "HKCU\System\GameConfigStore" /v GameDVR_FSEBehaviorMode /t REG_DWORD /d 2 /f';RevertCmd='reg delete "HKCU\System\GameConfigStore" /v GameDVR_DXGIHonorFSEWindowsCompatible /f'},
    [TI]@{Id='gm5';Cat='game';Badge='SAFE';BC='#22C55E';Name='DWM FLUSH RATE';Desc='Force DWM to flush every frame, reduces microstutter';Cmd='reg add "HKCU\Software\Microsoft\Windows\DWM" /v Flush /t REG_DWORD /d 1 /f';RevertCmd='reg delete "HKCU\Software\Microsoft\Windows\DWM" /v Flush /f'},
    [TI]@{Id='gm6';Cat='game';Badge='SAFE';BC='#22C55E';Name='CPU PRIORITY GAMES';Desc='Boost game thread priority via MMCSS';Cmd='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v Priority /t REG_DWORD /d 6 /f && reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "SFIO Priority" /t REG_SZ /d High /f';RevertCmd='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v Priority /t REG_DWORD /d 2 /f'},
    [TI]@{Id='gm7';Cat='game';Badge='EXTREME';BC='#7c3aed';Name='DISABLE HPET';Desc='Disable High Precision Event Timer - can lower latency on some CPUs';Cmd='bcdedit /set disabledynamictick yes & bcdedit /deletevalue useplatformclock & exit /b 0';RevertCmd='bcdedit /set disabledynamictick no & bcdedit /set useplatformclock true & exit /b 0'},
    [TI]@{Id='gm8';Cat='game';Badge='SAFE';BC='#22C55E';Name='DISABLE USB SUSPEND';Desc='Prevent USB devices from being powered off mid-game causing input stutter';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Services\USB" /v DisableSelectiveSuspend /t REG_DWORD /d 1 /f';RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Services\USB" /v DisableSelectiveSuspend /t REG_DWORD /d 0 /f'},
    [TI]@{Id='gm9';Cat='game';Badge='SAFE';BC='#22C55E';Name='MSI MODE ALL DEVICES';Desc='Enable Message Signaled Interrupts on all capable PCI/USB devices for lower IRQ latency';Cmd='PS:Get-WmiObject Win32_PnPSignedDriver | Where-Object { $_.DeviceClass -match "Display|Net|USB|Media" } | ForEach-Object { $path = "HKLM:\SYSTEM\CurrentControlSet\Enum\$($_.DeviceID)\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties"; if (Test-Path (Split-Path $path)) { New-Item $path -Force | Out-Null; Set-ItemProperty $path -Name MSISupported -Value 1 } }';RevertCmd=''},
    # Restore
    [TI]@{Id='rst1';Cat='rst';Badge='RESTORE';BC='#38BDF8';Name='RESTORE NETWORK';Desc='Reset TCP/IP to defaults';Cmd='netsh int tcp set global autotuninglevel=normal & netsh int tcp set global ecncapability=enabled & netsh int tcp set global rsc=enabled & netsh int tcp set global congestionprovider=default';RevertCmd=''},
    [TI]@{Id='rst2';Cat='rst';Badge='RESTORE';BC='#38BDF8';Name='RESTORE POWER';Desc='Back to Balanced plan';Cmd='powercfg -setactive 381b4222-f694-41f0-9685-ff5bb260df2e';RevertCmd=''},
    [TI]@{Id='rst3';Cat='rst';Badge='RESTORE';BC='#38BDF8';Name='RE-ENABLE SVCS';Desc='Restore SysMain and DiagTrack';Cmd='sc config SysMain start= auto & sc start SysMain & sc config DiagTrack start= auto & sc start DiagTrack & exit /b 0';RevertCmd=''},
    [TI]@{Id='rst4';Cat='rst';Badge='RESTORE';BC='#38BDF8';Name='RESTORE VISUAL FX';Desc='Re-enable animations';Cmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects" /v VisualFXSetting /t REG_DWORD /d 0 /f';RevertCmd=''},
    [TI]@{Id='rst5';Cat='rst';Badge='RESTORE';BC='#38BDF8';Name='RESTORE MOUSE';Desc='Re-enable pointer acceleration';Cmd='reg add "HKCU\Control Panel\Mouse" /v MouseSpeed /t REG_SZ /d 1 /f';RevertCmd=''},
    # Background / Scheduled Tasks
    [TI]@{Id='bg1';Cat='bg';Badge='SAFE';BC='#22C55E';Name='DISABLE UWP BACKGROUND';Desc='Revoke background execution for all UWP/modern apps globally — drops 8-20 RuntimeBroker processes';Cmd='PS:$k="HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications"; if(!(Test-Path $k)){New-Item $k -Force|Out-Null}; Set-ItemProperty $k GlobalUserDisabled 1; Get-ChildItem $k -EA SilentlyContinue | ForEach-Object { Set-ItemProperty $_.PSPath Disabled 1 -EA SilentlyContinue; Set-ItemProperty $_.PSPath DisabledByUser 1 -EA SilentlyContinue }';RevertCmd='PS:$k="HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications"; Set-ItemProperty $k GlobalUserDisabled 0 -EA SilentlyContinue; Get-ChildItem $k -EA SilentlyContinue | ForEach-Object { Set-ItemProperty $_.PSPath Disabled 0 -EA SilentlyContinue; Set-ItemProperty $_.PSPath DisabledByUser 0 -EA SilentlyContinue }'},
    [TI]@{Id='bg2';Cat='bg';Badge='SAFE';BC='#22C55E';Name='DISABLE COMPAT TASKS';Desc='Stop Application Experience/Appraiser scheduled tasks that run CompatTelRunner.exe in background';Cmd='schtasks /Change /TN "Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser" /Disable 2>nul & schtasks /Change /TN "Microsoft\Windows\Application Experience\ProgramDataUpdater" /Disable 2>nul & schtasks /Change /TN "Microsoft\Windows\Application Experience\StartupAppTask" /Disable 2>nul & exit /b 0';RevertCmd='schtasks /Change /TN "Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser" /Enable 2>nul & schtasks /Change /TN "Microsoft\Windows\Application Experience\ProgramDataUpdater" /Enable 2>nul & exit /b 0'},
    [TI]@{Id='bg3';Cat='bg';Badge='SAFE';BC='#22C55E';Name='DISABLE CEIP TASKS';Desc='Stop Customer Experience Improvement Program scheduled tasks';Cmd='schtasks /Change /TN "Microsoft\Windows\Customer Experience Improvement Program\Consolidator" /Disable 2>nul & schtasks /Change /TN "Microsoft\Windows\Customer Experience Improvement Program\UsbCeip" /Disable 2>nul & exit /b 0';RevertCmd='schtasks /Change /TN "Microsoft\Windows\Customer Experience Improvement Program\Consolidator" /Enable 2>nul & exit /b 0'},
    [TI]@{Id='bg4';Cat='bg';Badge='SAFE';BC='#22C55E';Name='DISABLE FEEDBACK TASKS';Desc='Stop Windows Feedback/Siuf telemetry collection tasks';Cmd='schtasks /Change /TN "Microsoft\Windows\Feedback\Siuf\DmClient" /Disable 2>nul & schtasks /Change /TN "Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload" /Disable 2>nul & exit /b 0';RevertCmd='schtasks /Change /TN "Microsoft\Windows\Feedback\Siuf\DmClient" /Enable 2>nul & exit /b 0'},
    [TI]@{Id='bg5';Cat='bg';Badge='SAFE';BC='#22C55E';Name='DISABLE DISK DIAG TASK';Desc='Stop Microsoft Disk Diagnostic data collector background task';Cmd='schtasks /Change /TN "Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector" /Disable 2>nul & exit /b 0';RevertCmd='schtasks /Change /TN "Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector" /Enable 2>nul & exit /b 0'},
    [TI]@{Id='bg6';Cat='bg';Badge='SAFE';BC='#22C55E';Name='DISABLE WER TASKS';Desc='Stop Windows Error Reporting background queue tasks — eliminates WerFault.exe background spawns';Cmd='schtasks /Change /TN "Microsoft\Windows\Windows Error Reporting\QueueReporting" /Disable 2>nul & exit /b 0';RevertCmd='schtasks /Change /TN "Microsoft\Windows\Windows Error Reporting\QueueReporting" /Enable 2>nul & exit /b 0'},
    [TI]@{Id='bg7';Cat='bg';Badge='SAFE';BC='#22C55E';Name='STARTUP DELAY ZERO';Desc='Remove startup delay for Explorer-launched items — startup apps open instantly, not staggered';Cmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize" /v StartupDelayInMSec /t REG_DWORD /d 0 /f';RevertCmd='reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize" /v StartupDelayInMSec /f'},
    [TI]@{Id='bg8';Cat='bg';Badge='SAFE';BC='#22C55E';Name='DISABLE DO TASKS';Desc='Stop Delivery Optimization background upload tasks';Cmd='schtasks /Change /TN "Microsoft\Windows\WindowsUpdate\Scheduled Start" /Disable 2>nul & reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization" /v DODownloadMode /t REG_DWORD /d 0 /f & exit /b 0';RevertCmd='reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization" /v DODownloadMode /f 2>nul & exit /b 0'},
    # Storage
    [TI]@{Id='stor1';Cat='stor';Badge='SAFE';BC='#22C55E';Name='STORAHCI MSI MODE';Desc='Enable Message Signalled Interrupts for storage controllers (NVMe/SATA AHCI) — reduces storage latency';Cmd='PS:Get-ChildItem "HKLM:\SYSTEM\CurrentControlSet\Enum\PCI" -EA SilentlyContinue | Get-ChildItem -EA SilentlyContinue | ForEach-Object { $p=Get-ItemProperty $_.PSPath -EA SilentlyContinue; if($p.Class -match "SCSIAdapter|HDC"){ $msi=Join-Path $_.PSPath "Device Parameters\Interrupt Management\MessageSignaledInterruptProperties"; if(!(Test-Path $msi)){New-Item $msi -Force -EA SilentlyContinue|Out-Null}; Set-ItemProperty $msi MSISupported 1 -EA SilentlyContinue } }';RevertCmd='PS:Get-ChildItem "HKLM:\SYSTEM\CurrentControlSet\Enum\PCI" -EA SilentlyContinue | Get-ChildItem -EA SilentlyContinue | ForEach-Object { $p=Get-ItemProperty $_.PSPath -EA SilentlyContinue; if($p.Class -match "SCSIAdapter|HDC"){ $msi=Join-Path $_.PSPath "Device Parameters\Interrupt Management\MessageSignaledInterruptProperties"; Set-ItemProperty $msi MSISupported 0 -EA SilentlyContinue } }'},
    [TI]@{Id='stor2';Cat='stor';Badge='SAFE';BC='#22C55E';Name='ENABLE WRITE CACHE';Desc='Enable disk write caching per-disk for better sequential write performance';Cmd='PS:Get-WmiObject Win32_DiskDrive -EA SilentlyContinue | ForEach-Object { $id=$_.PNPDeviceID; $k="HKLM:\SYSTEM\CurrentControlSet\Enum\$id\Device Parameters\Disk"; if(!(Test-Path $k)){New-Item $k -Force -EA SilentlyContinue|Out-Null}; Set-ItemProperty $k UserWriteCacheSetting 1 -EA SilentlyContinue }';RevertCmd='PS:Get-WmiObject Win32_DiskDrive -EA SilentlyContinue | ForEach-Object { $id=$_.PNPDeviceID; $k="HKLM:\SYSTEM\CurrentControlSet\Enum\$id\Device Parameters\Disk"; Set-ItemProperty $k UserWriteCacheSetting 0 -EA SilentlyContinue }'},
    [TI]@{Id='stor3';Cat='stor';Badge='SAFE';BC='#22C55E';Name='TRIM ALL SSDS';Desc='Run TRIM optimisation on all SSDs right now — one-time operation';Cmd='PS:Get-Volume | Where-Object {$_.DriveType -eq "Fixed"} | ForEach-Object { try { Optimize-Volume -DriveLetter $_.DriveLetter -ReTrim -EA SilentlyContinue } catch {} }';RevertCmd=''},
    [TI]@{Id='stor4';Cat='stor';Badge='SAFE';BC='#22C55E';Name='NTFS DISABLE ENCRYPT';Desc='Disable NTFS on-disk encryption overhead (not BitLocker — NTFS attribute flag only)';Cmd='fsutil behavior set disableencryption 1';RevertCmd='fsutil behavior set disableencryption 0'},
    [TI]@{Id='stor5';Cat='stor';Badge='SAFE';BC='#22C55E';Name='NTFS LARGE MFT';Desc='Reserve maximum MFT space (zone 4) — already in Latency tab but grouped here for storage focus';Cmd='fsutil behavior set mftzone 4';RevertCmd='fsutil behavior set mftzone 1'},
    [TI]@{Id='stor6';Cat='stor';Badge='EXTREME';BC='#7c3aed';Name='DISABLE NTFS JOURNAL';Desc='Delete NTFS change journal on C: — reduces I/O overhead but breaks VSS shadow copies';Cmd='fsutil usn deletejournal /D C:';RevertCmd='fsutil usn createjournal m=0x10000000 a=0x800000 C:'},
    # Audio
    [TI]@{Id='aud1';Cat='audio';Badge='SAFE';BC='#22C55E';Name='DISABLE AUDIO ENHANCE';Desc='Disable system-wide audio enhancements/DSP effects that add buffer latency';Cmd='reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Audio" /v DisableProtectedAudioDG /t REG_DWORD /d 1 /f';RevertCmd='reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Audio" /v DisableProtectedAudioDG /t REG_DWORD /d 0 /f'},
    [TI]@{Id='aud2';Cat='audio';Badge='SAFE';BC='#22C55E';Name='MMCSS AUDIO HIGH';Desc='Set MMCSS audio thread to High scheduling category — lower buffer underrun risk';Cmd='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio" /v Priority /t REG_DWORD /d 6 /f && reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio" /v "Scheduling Category" /t REG_SZ /d High /f && reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio" /v "SFIO Priority" /t REG_SZ /d High /f';RevertCmd='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio" /v Priority /t REG_DWORD /d 2 /f'},
    [TI]@{Id='aud3';Cat='audio';Badge='SAFE';BC='#22C55E';Name='EXCLUSIVE AUDIO MODE';Desc='Allow apps (DAW, games) to claim exclusive device control for lowest possible buffer size';Cmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Audio" /v AllowExclusiveModeOverride /t REG_DWORD /d 1 /f';RevertCmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Audio" /v AllowExclusiveModeOverride /t REG_DWORD /d 0 /f'},
    [TI]@{Id='aud4';Cat='audio';Badge='EXTREME';BC='#7c3aed';Name='DISABLE AUTO DUCKING';Desc='Stop Windows automatically lowering other audio when communication apps are active';Cmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Audio" /v AutoDucking /t REG_DWORD /d 0 /f';RevertCmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Audio" /v AutoDucking /t REG_DWORD /d 1 /f'},
    [TI]@{Id='aud5';Cat='audio';Badge='SAFE';BC='#22C55E';Name='DISABLE AUDIO LOGGING';Desc='Disable audio engine debug/event logging - minor CPU overhead reduction';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Services\Audiosrv" /v PerfDisableAllLogging /t REG_DWORD /d 1 /f';RevertCmd='reg delete "HKLM\SYSTEM\CurrentControlSet\Services\Audiosrv" /v PerfDisableAllLogging /f'},
    # NUCLEAR - security-disabling tweaks. ALL revertible. Use only on an isolated machine you fully control.
    [TI]@{Id='nuke1';Cat='nuke';Badge='NUCLEAR';BC='#EF4444';Name='DISABLE DEFENDER RT';Desc='Disable Windows Defender real-time protection (Tamper Protection must be OFF in Windows Security first). MAJOR security risk.';Cmd='reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender" /v DisableAntiSpyware /t REG_DWORD /d 1 /f & reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection" /v DisableRealtimeMonitoring /t REG_DWORD /d 1 /f & exit /b 0';RevertCmd='reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender" /v DisableAntiSpyware /f & reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection" /v DisableRealtimeMonitoring /f & exit /b 0'},
    [TI]@{Id='nuke2';Cat='nuke';Badge='NUCLEAR';BC='#EF4444';Name='DISABLE SMARTSCREEN';Desc='Disable SmartScreen app/file reputation checks. Removes a download/malware safety net.';Cmd='reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v EnableSmartScreen /t REG_DWORD /d 0 /f';RevertCmd='reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v EnableSmartScreen /t REG_DWORD /d 1 /f'},
    [TI]@{Id='nuke3';Cat='nuke';Badge='NUCLEAR';BC='#EF4444';Name='DISABLE UAC';Desc='Disable User Account Control elevation prompts (EnableLUA=0). Requires reboot. Apps run elevated silently.';Cmd='reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v EnableLUA /t REG_DWORD /d 0 /f';RevertCmd='reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v EnableLUA /t REG_DWORD /d 1 /f'},
    [TI]@{Id='nuke4';Cat='nuke';Badge='NUCLEAR';BC='#EF4444';Name='DISABLE CORE ISOLATION';Desc='Disable memory integrity / HVCI virtualization-based security. Recovers CPU overhead, lowers exploit protection. Requires reboot.';Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity" /v Enabled /t REG_DWORD /d 0 /f';RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity" /v Enabled /t REG_DWORD /d 1 /f'},
    [TI]@{Id='nuke5';Cat='nuke';Badge='NUCLEAR';BC='#EF4444';Name='DISABLE FIREWALL';Desc='Turn off Windows Firewall on all profiles. Only safe behind a trusted router/hardware firewall.';Cmd='netsh advfirewall set allprofiles state off';RevertCmd='netsh advfirewall set allprofiles state on'}
)

# ── ARCHITECTURE TWEAKS (built at runtime after HW detection) ─────────────────
$script:ARCH_TWEAKS = @()

# INTEL CPU
if ($script:HW.CpuVendor -eq 'Intel') {
    $script:ARCH_TWEAKS += [TI]@{
        Id='ix1';Cat='cpu';Badge='SAFE';BC='#22C55E';
        Name='SPEED SHIFT EPP';
        Desc='[INTEL CPU] Enable Speed Shift energy perf preference';
        Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\be337238-0d82-4146-a960-4f3749d470c7" /v ValueMax /t REG_DWORD /d 0 /f';
        RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\be337238-0d82-4146-a960-4f3749d470c7" /v ValueMax /t REG_DWORD /d 100 /f'
    }

    $script:ARCH_TWEAKS += [TI]@{
        Id='ix2';Cat='cpu';Badge='EXTREME';BC='#7c3aed';
        Name='INTEL DISABLE C-STATES';
        Desc='[INTEL CPU] Prevent deep sleep latency spikes';
        Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Processor" /v Capabilities /t REG_DWORD /d 0x0007e066 /f';
        RevertCmd='reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Processor" /v Capabilities /f'
    }

    $script:ARCH_TWEAKS += [TI]@{
        Id='ix3';Cat='cpu';Badge='EXTREME';BC='#7c3aed';
        Name='INTEL NO SPEEDSTEP';
        Desc='[INTEL CPU] Disable SpeedStep/SpeedShift — keeps all cores at max frequency';
        Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\be337238-0d82-4146-a960-4f3749d470c7" /v ValueMin /t REG_DWORD /d 100 /f';
        RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\be337238-0d82-4146-a960-4f3749d470c7" /v ValueMin /t REG_DWORD /d 0 /f'
    }
}

# AMD CPU
if ($script:HW.CpuVendor -eq 'AMD') {
    $script:ARCH_TWEAKS += [TI]@{
        Id='ax1';Cat='cpu';Badge='SAFE';BC='#22C55E';
        Name='AMD CPPC PERF';
        Desc='[AMD CPU] Set Collaborative Processor Performance to max';
        Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Power" /v CpuEnergyPerfPref /t REG_DWORD /d 0 /f';
        RevertCmd='reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Power" /v CpuEnergyPerfPref /f'
    }

    $script:ARCH_TWEAKS += [TI]@{
        Id='ax2';Cat='cpu';Badge='EXTREME';BC='#7c3aed';
        Name='AMD BOOST MAX';
        Desc='[AMD CPU] Max precision boost via power plan';
        Cmd='powercfg -setacvalueindex scheme_current sub_processor PERFBOOSTPOL 100 && powercfg -setactive scheme_current';
        RevertCmd='powercfg -setacvalueindex scheme_current sub_processor PERFBOOSTPOL 50 && powercfg -setactive scheme_current'
    }

    $script:ARCH_TWEAKS += [TI]@{
        Id='ax3';Cat='cpu';Badge='EXTREME';BC='#7c3aed';
        Name='AMD DISABLE C6';
        Desc='[AMD CPU] Disable C6 core idle to cut latency spikes';
        Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Processor" /v Capabilities /t REG_DWORD /d 0x0007e066 /f';
        RevertCmd='reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Processor" /v Capabilities /f'
    }
}

# NVIDIA GPU
if ($script:HW.GpuVendor -eq 'NVIDIA') {
    $script:ARCH_TWEAKS += [TI]@{
        Id='nx1';Cat='gpu';Badge='SAFE';BC='#22C55E';
        Name='NVIDIA MAX PERF';
        Desc='[NVIDIA GPU] Force prefer max performance power mode';
        Cmd='reg add "HKCU\Software\NVIDIA Corporation\Global\NvCplApi\Policies" /v OverrideAdaptiveThreshold /t REG_DWORD /d 1 /f';
        RevertCmd='reg delete "HKCU\Software\NVIDIA Corporation\Global\NvCplApi\Policies" /v OverrideAdaptiveThreshold /f'
    }

    $script:ARCH_TWEAKS += [TI]@{
        Id='nx2';Cat='gpu';Badge='SAFE';BC='#22C55E';
        Name='NVIDIA NO TELEMETRY';
        Desc='[NVIDIA GPU] Disable NVIDIA telemetry service and reporting';
        Cmd='sc config NvTelemetryContainer start= disabled & sc stop NvTelemetryContainer & reg add "HKLM\SOFTWARE\NVIDIA Corporation\NvControlPanel2\Client" /v OptInOrOutPreference /t REG_DWORD /d 0 /f';
        RevertCmd='sc config NvTelemetryContainer start= auto'
    }

    $script:ARCH_TWEAKS += [TI]@{
        Id='nx3';Cat='gpu';Badge='SAFE';BC='#22C55E';
        Name='NVIDIA SHADER CACHE';
        Desc='[NVIDIA GPU] Ensure shader disk cache is on for faster game loads';
        Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v DisableShaderCache /t REG_DWORD /d 0 /f';
        RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v DisableShaderCache /t REG_DWORD /d 1 /f'
    }

    $script:ARCH_TWEAKS += [TI]@{
        Id='nx4';Cat='gpu';Badge='EXTREME';BC='#7c3aed';
        Name='NVIDIA HDCP OFF';
        Desc='[NVIDIA GPU] Disable HDCP - small latency gain, non-DRM displays only';
        Cmd='reg add "HKCU\Software\NVIDIA Corporation\Global\NvCplApi\Policies" /v HDCPEnabled /t REG_DWORD /d 0 /f';
        RevertCmd='reg add "HKCU\Software\NVIDIA Corporation\Global\NvCplApi\Policies" /v HDCPEnabled /t REG_DWORD /d 1 /f'
    }

    $script:ARCH_TWEAKS += [TI]@{
        Id='nx5';Cat='gpu';Badge='SAFE';BC='#22C55E';
        Name='NVIDIA PCIe MAX LINK';
        Desc='[NVIDIA GPU] Force PCIe link to max speed — prevents bandwidth throttling';
        Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000" /v NvCplEnableLinkedAdaptersAutoPowerDown /t REG_DWORD /d 0 /f';
        RevertCmd='reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000" /v NvCplEnableLinkedAdaptersAutoPowerDown /f'
    }

    $script:ARCH_TWEAKS += [TI]@{
        Id='nx6';Cat='gpu';Badge='EXTREME';BC='#7c3aed';
        Name='NVIDIA NO PWR GATING';
        Desc='[NVIDIA GPU] Disable GPU power gating — eliminates clock-up latency spikes';
        Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000" /v DisablePowerGating /t REG_DWORD /d 1 /f';
        RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000" /v DisablePowerGating /t REG_DWORD /d 0 /f'
    }
}

# AMD GPU
if ($script:HW.GpuVendor -eq 'AMD') {
    $script:ARCH_TWEAKS += [TI]@{
        Id='agx1';Cat='gpu';Badge='SAFE';BC='#22C55E';
        Name='AMD GPU DEEP SLEEP OFF';
        Desc='[AMD GPU] Disable GPU core deep sleep for lower latency';
        Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000" /v PP_SclkDeepSleepDisable /t REG_DWORD /d 1 /f';
        RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000" /v PP_SclkDeepSleepDisable /t REG_DWORD /d 0 /f'
    }

    $script:ARCH_TWEAKS += [TI]@{
        Id='agx2';Cat='gpu';Badge='SAFE';BC='#22C55E';
        Name='AMD CHILL OFF';
        Desc='[AMD GPU] Disable Radeon Chill frame limiter';
        Cmd='reg add "HKCU\Software\ATI\ACE\Settings\ADL\CWDDEPM" /v ChillEnabled /t REG_DWORD /d 0 /f';
        RevertCmd='reg add "HKCU\Software\ATI\ACE\Settings\ADL\CWDDEPM" /v ChillEnabled /t REG_DWORD /d 1 /f'
    }

    $script:ARCH_TWEAKS += [TI]@{
        Id='agx3';Cat='gpu';Badge='EXTREME';BC='#7c3aed';
        Name='AMD ULPS OFF';
        Desc='[AMD GPU] Disable Ultra Low Power State - reduces stutter on hybrid systems';
        Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000" /v EnableUlps /t REG_DWORD /d 0 /f';
        RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000" /v EnableUlps /t REG_DWORD /d 1 /f'
    }

    $script:ARCH_TWEAKS += [TI]@{
        Id='agx4';Cat='gpu';Badge='EXTREME';BC='#7c3aed';
        Name='AMD NO ENERGY DRIVER';
        Desc='[AMD GPU] Disable AMD energy driver power management — keeps GPU at higher clocks';
        Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000" /v DisableDrmdmaPowerGating /t REG_DWORD /d 1 /f';
        RevertCmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000" /v DisableDrmdmaPowerGating /t REG_DWORD /d 0 /f'
    }
}

# LAPTOP
if ($script:HW.IsLaptop) {
    $script:ARCH_TWEAKS += [TI]@{
        Id='lx1';Cat='pow';Badge='SAFE';BC='#22C55E';
        Name='LAPTOP AC PERF';
        Desc='[LAPTOP] Max performance on AC power only';
        Cmd='powercfg -setacvalueindex scheme_current sub_processor PERFBOOSTPOL 100 && powercfg -setactive scheme_current';
        RevertCmd='powercfg -setacvalueindex scheme_current sub_processor PERFBOOSTPOL 50 && powercfg -setactive scheme_current'
    }
}

# ── PRIVACY TWEAKS ────────────────────────────────────────────────────────────
$script:PRIVTWEAKS = @(
    [TI]@{Id='p1';Cat='priv';Badge='SAFE';BC='#22C55E';Name='DISABLE TELEMETRY';Desc='Kill DiagTrack and dmwappushservice';Cmd='sc config DiagTrack start= disabled & sc stop DiagTrack & sc config dmwappushservice start= disabled & sc stop dmwappushservice & exit /b 0'},
    [TI]@{Id='p2';Cat='priv';Badge='SAFE';BC='#22C55E';Name='BLOCK ADS';Desc='Stop Microsoft ad delivery';Cmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v ContentDeliveryAllowed /t REG_DWORD /d 0 /f'},
    [TI]@{Id='p3';Cat='priv';Badge='SAFE';BC='#22C55E';Name='DENY CAMERA';Desc='Block all apps from webcam';Cmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam" /v Value /t REG_SZ /d Deny /f'},
    [TI]@{Id='p4';Cat='priv';Badge='SAFE';BC='#22C55E';Name='DENY MICROPHONE';Desc='Block all apps from mic';Cmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone" /v Value /t REG_SZ /d Deny /f'},
    [TI]@{Id='p5';Cat='priv';Badge='SAFE';BC='#22C55E';Name='DISABLE LOCATION';Desc='Block location access';Cmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location" /v Value /t REG_SZ /d Deny /f'},
    [TI]@{Id='p6';Cat='priv';Badge='SAFE';BC='#22C55E';Name='DISABLE CORTANA';Desc='Remove Cortana completely';Cmd='reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search" /v AllowCortana /t REG_DWORD /d 0 /f'},
    [TI]@{Id='p7';Cat='priv';Badge='SAFE';BC='#22C55E';Name='DISABLE ACTIVITY';Desc='Stop timeline tracking';Cmd='reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v EnableActivityFeed /t REG_DWORD /d 0 /f'},
    [TI]@{Id='p8';Cat='priv';Badge='EXTREME';BC='#7c3aed';Name='DISABLE UPDATES';Desc='Stop Windows Update service';Cmd='sc config wuauserv start= disabled && sc stop wuauserv'}
)

# ── APPS ──────────────────────────────────────────────────────────────────────
$script:APPS_DATA = @(
    # Browsers
    [AI]@{Id='Google.Chrome';                  Name='Chrome';              Cat='Browser'},
    [AI]@{Id='Mozilla.Firefox';                Name='Firefox';             Cat='Browser'},
    [AI]@{Id='Brave.Brave';                    Name='Brave';               Cat='Browser'},
    [AI]@{Id='Opera.OperaGX';                  Name='Opera GX';            Cat='Browser'},
    # Communication
    [AI]@{Id='Discord.Discord';                Name='Discord';             Cat='Comm'},
    [AI]@{Id='Telegram.TelegramDesktop';       Name='Telegram';            Cat='Comm'},
    [AI]@{Id='WhatsApp.WhatsApp';              Name='WhatsApp';            Cat='Comm'},
    # Gaming
    [AI]@{Id='Valve.Steam';                    Name='Steam';               Cat='Gaming'},
    [AI]@{Id='EpicGames.EpicGamesLauncher';    Name='Epic Games';          Cat='Gaming'},
    [AI]@{Id='Ubisoft.Connect';                Name='Ubisoft Connect';     Cat='Gaming'},
    [AI]@{Id='ElectronicArts.EADesktop';       Name='EA Desktop';          Cat='Gaming'},
    # Hardware / Monitoring
    [AI]@{Id='TechPowerUp.GPU-Z';              Name='GPU-Z';               Cat='HW Monitor'},
    [AI]@{Id='CPUID.CPU-Z';                    Name='CPU-Z';               Cat='HW Monitor'},
    [AI]@{Id='REALiX.HWiNFO';                 Name='HWiNFO64';            Cat='HW Monitor'},
    [AI]@{Id='MSI.MSIAfterburner';             Name='MSI Afterburner';     Cat='HW Monitor'},
    [AI]@{Id='CrystalDewWorld.CrystalDiskInfo';Name='CrystalDiskInfo';     Cat='HW Monitor'},
    # Tools
    [AI]@{Id='7zip.7zip';                      Name='7-Zip';               Cat='Tools'},
    [AI]@{Id='RARLab.WinRAR';                  Name='WinRAR';              Cat='Tools'},
    [AI]@{Id='VideoLAN.VLC';                   Name='VLC';                 Cat='Tools'},
    [AI]@{Id='OBSProject.OBSStudio';           Name='OBS Studio';          Cat='Tools'},
    [AI]@{Id='Microsoft.PowerToys';            Name='PowerToys';           Cat='Tools'},
    [AI]@{Id='voidtools.Everything';           Name='Everything';          Cat='Tools'},
    [AI]@{Id='HandBrake.HandBrake';            Name='HandBrake';           Cat='Tools'},
    [AI]@{Id='Rufus.Rufus';                    Name='Rufus';               Cat='Tools'},
    [AI]@{Id='ShareX.ShareX';                  Name='ShareX';              Cat='Tools'},
    [AI]@{Id='AutoHotkey.AutoHotkey';          Name='AutoHotkey';          Cat='Tools'},
    [AI]@{Id='JAMSoftware.TreeSize.Free';      Name='TreeSize Free';       Cat='Tools'},
    [AI]@{Id='WinSCP.WinSCP';                  Name='WinSCP';              Cat='Tools'},
    # Media / Creative
    [AI]@{Id='GIMP.GIMP';                      Name='GIMP';                Cat='Media'},
    [AI]@{Id='Audacity.Audacity';              Name='Audacity';            Cat='Media'},
    [AI]@{Id='Spotify.Spotify';                Name='Spotify';             Cat='Media'},
    [AI]@{Id='Inkscape.Inkscape';              Name='Inkscape';            Cat='Media'},
    [AI]@{Id='BlenderFoundation.Blender';      Name='Blender';             Cat='Media'},
    [AI]@{Id='DaVinciResolve.DaVinciResolve';  Name='DaVinci Resolve';     Cat='Media'},
    [AI]@{Id='KDE.Kdenlive';                   Name='Kdenlive';            Cat='Media'},
    # Dev
    [AI]@{Id='SublimeHQ.SublimeText.4';        Name='Sublime Text';        Cat='Dev'},
    [AI]@{Id='Microsoft.VisualStudioCode';     Name='VS Code';             Cat='Dev'},
    [AI]@{Id='Notepad++.Notepad++';            Name='Notepad++';           Cat='Dev'},
    [AI]@{Id='Git.Git';                        Name='Git';                 Cat='Dev'},
    [AI]@{Id='GitHub.GitHubDesktop';           Name='GitHub Desktop';      Cat='Dev'},
    [AI]@{Id='OpenJS.NodeJS';                  Name='Node.js';             Cat='Dev'},
    [AI]@{Id='Python.Python.3';                Name='Python 3';            Cat='Dev'},
    [AI]@{Id='Postman.Postman';                Name='Postman';             Cat='Dev'},
    [AI]@{Id='Neovim.Neovim';                  Name='Neovim';              Cat='Dev'},
    [AI]@{Id='Microsoft.WindowsTerminal';      Name='Windows Terminal';    Cat='Dev'},
    [AI]@{Id='TimKosse.FileZilla.Client';      Name='FileZilla';           Cat='Dev'},
    # Security
    [AI]@{Id='Malwarebytes.Malwarebytes';      Name='Malwarebytes';        Cat='Security'},
    [AI]@{Id='Bitwarden.Bitwarden';            Name='Bitwarden';           Cat='Security'},
    [AI]@{Id='KeePass.KeePass';                Name='KeePass';             Cat='Security'},
    [AI]@{Id='Wireshark.Wireshark';            Name='Wireshark';           Cat='Security'},
    # Gaming extras
    [AI]@{Id='Parsec.Parsec';                  Name='Parsec';              Cat='Gaming'},
    # System / Utils
    [AI]@{Id='Microsoft.PowerShell';           Name='PowerShell 7';        Cat='System'},
    [AI]@{Id='Chocolatey.Chocolatey';          Name='Chocolatey';          Cat='System'},
    [AI]@{Id='CrystalDewWorld.CrystalDiskMark';Name='CrystalDiskMark';     Cat='System'},
    [AI]@{Id='Ventoy.Ventoy';                  Name='Ventoy';              Cat='System'},
    [AI]@{Id='WizTree.WizTree';                Name='WizTree';             Cat='System'},
    [AI]@{Id='Microsoft.Sysinternals.Suite';   Name='Sysinternals Suite';  Cat='System'},
    [AI]@{Id='ProcessHacker.ProcessHacker';    Name='Process Hacker';      Cat='System'},
    [AI]@{Id='Wagnardsoft.DDU';                Name='Display Driver Uninstaller'; Cat='System'},
    [AI]@{Id='lostindark.DriverStoreExplorer'; Name='DriverStoreExplorer'; Cat='System'},
    [AI]@{Id='AntibodySoftware.WizTree';       Name='BCUninstaller';       Cat='System'},
    [AI]@{Id='Geeks3D.FurMark';               Name='FurMark';             Cat='System'},
    [AI]@{Id='CPUID.HWMonitor';                Name='HWMonitor';           Cat='System'},
    [AI]@{Id='Maxon.CinebenchR23';             Name='Cinebench R23';       Cat='System'},
    [AI]@{Id='PuTTY.PuTTY';                    Name='PuTTY';               Cat='System'}
)

# ── SERVICES ──────────────────────────────────────────────────────────────────
# Dependencies map - used by dependency checker in services tab
$script:SVC_DEPS = @{
    'WSearch'          = @()                        # safe to disable
    'SysMain'          = @()                        # safe to disable
    'DiagTrack'        = @()                        # safe to disable
    'dmwappushservice' = @('DiagTrack')             # used by telemetry
    'wuauserv'         = @('BITS','CryptSvc')       # Windows Update depends on these
    'Spooler'          = @()                        # safe if no printer
    'bthserv'          = @()                        # safe if no Bluetooth
    'RemoteRegistry'   = @()                        # safe to disable
    'TabletInputService'=@()                        # safe on desktops
    'Fax'              = @()                        # safe to disable
    'MapsBroker'       = @()                        # safe to disable
    'RetailDemo'       = @()                        # safe to disable
    'WMPNetworkSvc'    = @()                        # safe to disable
    'XboxGip'          = @()                        # safe if no Xbox controller
    'XblAuthManager'   = @('XblGameSave')           # Xbox auth
    'XblGameSave'      = @()                        # safe to disable
    'XboxNetApiSvc'    = @()                        # safe to disable
    'W32Time'          = @()                        # safe to disable offline
    'DoSvc'            = @()                        # Delivery Optimization - safe to disable
    'BITS'             = @()                        # Background Intelligent Transfer - safe if not using Windows Update
    'WbioSrvc'         = @()                        # Windows Biometric - safe if no fingerprint reader
    'SensorService'    = @()                        # Sensor aggregator - safe on desktop
    'SensrSvc'         = @()                        # Sensor monitoring - safe on desktop
}

$script:SVCS_DATA = @(
    # SAFE to disable
    [SI]@{Name='DiagTrack';         Desc='Windows Telemetry — safe to disable'},
    [SI]@{Name='dmwappushservice';  Desc='WAP Push Routing — telemetry relay'},
    [SI]@{Name='SysMain';           Desc='Superfetch — useless on SSD'},
    [SI]@{Name='WSearch';           Desc='Windows Search indexing — kills HDD'},
    [SI]@{Name='Spooler';           Desc='Print Spooler — disable if no printer'},
    [SI]@{Name='RemoteRegistry';    Desc='Remote Registry — security risk'},
    [SI]@{Name='TabletInputService';Desc='Tablet PC Input — irrelevant on desktop'},
    [SI]@{Name='Fax';              Desc='Fax service — nobody uses this'},
    [SI]@{Name='MapsBroker';        Desc='Offline Maps — unused on most PCs'},
    [SI]@{Name='RetailDemo';        Desc='Store Display Mode — only for retail PCs'},
    [SI]@{Name='WMPNetworkSvc';     Desc='Windows Media Player sharing'},
    [SI]@{Name='W32Time';           Desc='Time sync — safe to disable offline'},
    [SI]@{Name='bthserv';           Desc='Bluetooth — disable if no BT devices'},
    [SI]@{Name='DoSvc';             Desc='Delivery Optimization — P2P Windows Update relay, safe to stop'},
    [SI]@{Name='BITS';              Desc='Background Intelligent Transfer — safe to disable if Windows Update is off'},
    [SI]@{Name='WbioSrvc';          Desc='Windows Biometric Service — disable if no fingerprint/face reader'},
    [SI]@{Name='SensorService';     Desc='Sensor Aggregator — disable on desktop, no sensors'},
    [SI]@{Name='SensrSvc';          Desc='Sensor Monitoring — disable on desktop, no sensors'},
    # Xbox - all safe to disable if no Xbox
    [SI]@{Name='XboxGip';           Desc='Xbox Game Input Protocol'},
    [SI]@{Name='XblAuthManager';    Desc='Xbox Live Authentication'},
    [SI]@{Name='XblGameSave';       Desc='Xbox Game Save sync'},
    [SI]@{Name='XboxNetApiSvc';     Desc='Xbox Network API'},
    # Careful
    [SI]@{Name='wuauserv';          Desc='Windows Update — disable with caution'}
)

# ── STATE ─────────────────────────────────────────────────────────────────────
$script:applied      = 0
$script:benchLog     = @()
$script:benchRunning = $false
$script:allPages     = @('PgDash','PgTweaks','PgApps','PgServices','PgStartup','PgPrivacy','PgDiag','PgBench','PgProc','PgSafety','PgScript','PgPersonalize','PgDrivers','PgGpuHealth','PgLatency')
$script:catMap       = @{0='cpu';1='gpu';2='ram';3='net';4='deb';5='pow';6='lat';7='game';8='bg';9='stor';10='audio';11='nuke';12='rst'}
