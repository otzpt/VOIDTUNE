# VOIDTUNE — WinUI 3 edition

Native **C# / WinUI 3** (Windows App SDK) rewrite of VOIDTUNE, migrating off the
PowerShell + WPF (ps2exe) stack. Mica backdrop, Fluent design, custom title bar,
runs elevated. Aiming for a first-class Windows 11 / Xbox-app feel.

## Requirements

- .NET 8 SDK
- Windows App SDK 1.6 runtime (bundled self-contained in the build output)
- Windows 10 19041+ / Windows 11

## Build & run

```powershell
dotnet build VOIDTUNE.WinUI.csproj -c Debug -r win-x64
# output: bin\Debug\net8.0-windows10.0.19041.0\win-x64\VOIDTUNE.exe  (requires admin)
```

Publish a redistributable, self-contained build:

```powershell
dotnet publish VOIDTUNE.WinUI.csproj -c Release -r win-x64 --self-contained
```

> Built unpackaged (`WindowsPackageType=None`) and self-contained for the Windows
> App SDK (`WindowsAppSDKSelfContained=true`) so it runs without installing a
> matching framework runtime. `EnableMsixTooling=true` lets it build with the CLI
> (no Visual Studio required).

## Architecture

```
VOIDTUNE.WinUI/
├── App.xaml(.cs)              App + violet-accent Fluent dark theme + crash log
├── MainWindow.xaml(.cs)       Shell: NavigationView (Optimize/System/Tools) + Mica
├── app.manifest               requireAdministrator + PerMonitorV2 DPI
├── Models/                    Tweak, AppItem, PersonalizeToggle, DriverItem,
│                              StartupItem, BackupItem, ServiceItem, ProcessItem
├── Services/
│   ├── CommandRunner.cs       cmd / PowerShell exec + multi-line script runner
│   ├── TweakCatalog.cs        Full catalog + arch tweaks (ported from data.ps1)
│   ├── TweakEngine.cs         State, apply/revert, persistence, backup-on-apply
│   ├── HardwareInfo.cs        CPU/GPU vendor, RAM, build, laptop detection
│   ├── SystemMonitor.cs       Live CPU / RAM via Win32
│   ├── ServiceManager.cs      Curated services + gaming/normal profiles
│   ├── ProcessMonitor.cs      Grouped processes + kill
│   ├── PersonalizeService.cs  Registry toggles + accent colour
│   ├── AppCatalog/AppInstaller winget catalog + install/uninstall/detect
│   ├── DriverService.cs       Signed drivers via CIM → JSON
│   ├── GpuHealthService.cs    WMI + nvidia-smi telemetry
│   ├── LatencyService.cs      Timer/disk/memory/ping probes
│   ├── BenchmarkService.cs    CPU/RAM/disk benchmarks
│   ├── StartupManager.cs      Run keys + Startup folders enable/disable
│   └── BackupService.cs       Create/list/restore/delete registry snapshots
├── Converters/                Hex→Brush, Bool→Visibility, Bool-negation
└── Views/                     Dashboard, Tweaks, Services, Startup, Privacy,
                               Personalize, Drivers, GpuHealth, Latency,
                               Benchmarks, Apps, Backup, Script, Settings  ✅
```

## Status — full feature parity with the PowerShell edition

The WinUI 3 rewrite now ports **every** module of the PS edition. Navigation is
grouped into **Optimize / System / Tools** sections plus a Dashboard and Settings.
All pages build clean (0 errors) and were verified rendering at runtime.

**Optimize**
- **Dashboard** — live CPU/RAM (Win32), health score, hardware banner, quick actions
- **Tweaks** — the complete catalog (**~150 tweaks**: CPU/GPU/RAM/Network/Debloat/Power/
  Latency/Game/Background/Storage/Audio/**Processes**/Privacy/Restore/Nuclear) plus
  architecture tweaks (Intel/AMD/NVIDIA/laptop) gated on `HardwareInfo`. Category +
  tier + search filters. The **Processes** category is the background-process-reduction
  set (services, scheduled tasks, per-user service templates) — selecting it + Apply is
  how you cut the running process count. NUCLEAR is hidden behind a confirm checkbox.
- **Services** — ServiceController status (locale-independent), Enable/Disable,
  Gaming / Normal profiles
- **Startup** — registry Run keys (HKCU/HKLM) + Startup folders, enable/disable with
  a VOIDTUNE stash so entries can be restored
- **Privacy** — one-click privacy hardening / revert
- **Personalize** — 18 registry-backed toggles (Theme/Aero/Taskbar/Display) read live
  from the registry, + a 12-swatch accent-colour picker

**System**
- **Drivers** — installed signed drivers via CIM, category filter
- **GPU Health** — driver/VRAM via WMI, live temp/clocks/fan/power via nvidia-smi
- **Latency** — timer resolution (NtQueryTimerResolution), disk 4K, memory stride,
  ping/jitter, health score + remediation hints
- **Benchmarks** — CPU single/multi-thread, RAM bandwidth, disk sequential R/W

**Tools**
- **Apps** — 64-app winget catalog, install selected / detect installed
- **Backup & Restore** — list/create/restore/delete registry snapshots
- **Script Runner** — run PowerShell or cmd elevated, captured output

**Engine** — apply/revert, state persistence (`%LOCALAPPDATA%\VOIDTUNE`),
registry backup before every apply, hardware detection (CPU/GPU vendor, RAM, laptop).

**Next (optional polish):** MSIX packaging option; richer GPU telemetry for AMD/Intel;
per-tweak "current state" detection on the Tweaks page.
