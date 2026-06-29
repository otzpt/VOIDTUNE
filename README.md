# VOIDTUNE v0.8.2

**Windows optimization suite for gamers and power users.**

| Version | License | Platform | Current edition |
|---------|---------|----------|-----------------|
| 0.8.2 | GPL v3 | Windows 10/11 (x64) | C# / WinUI 3 (Windows App SDK) |

🌐 **[Website](https://voidtune-optimizer.netlify.app)** • 📦 **[Releases](https://github.com/otzpt/VOIDTUNE/releases)** • 🐛 **[Issues](https://github.com/otzpt/VOIDTUNE/issues)**

---

## Editions

- **WinUI 3 (current)** — the native C# rewrite lives in [`VOIDTUNE.WinUI/`](VOIDTUNE.WinUI/). Fluent / Mica UI, ~150 reversible tweaks, in-app auto-update, MSI + portable builds. Build/run notes in [VOIDTUNE.WinUI/README.md](VOIDTUNE.WinUI/README.md).
- **PowerShell + WPF (original)** — the original ps2exe edition at the repo root (`VOIDTUNE.ps1`, `core/`, `modules/`, `ui/`). Still functional; superseded by the WinUI app.

## 🚀 What's New in v0.8.2 (WinUI 3 edition)

- **Process-reduction overhaul** — the **Processes** tweak category bundles ~50 reversible tweaks (services, scheduled tasks, per-user service templates, bloatware removal, idle third-party updaters) to cut the background process count **without disabling Windows Security**
- **In-app auto-update** for both the portable and installer builds (checks GitHub on launch)
- **Bloatware removal** (reversible), idle updater disables (Google / Edge), and a "Hide Security Tray Icon" tweak that keeps Defender protection fully on
- Smarter hardware detection (discrete GPU preferred, accurate VRAM, real battery-based laptop detection)
- NUCLEAR tweaks hidden behind an explicit "Show NUCLEAR" confirmation

> Full history in [CHANGELOG.md](CHANGELOG.md).

---

## 🎨 What's New in v0.8 (PowerShell edition)

### One-click tiered apply
- **SAFE / EXTREME / NUCLEAR / REVERT ALL** buttons in the topbar — apply everything up to a tier in one click, or roll every applied tweak back to Windows defaults
- **NUCLEAR tab** — a new tier of security-disabling tweaks (Defender real-time, SmartScreen, UAC, Core Isolation/HVCI, Firewall). All revertible, gated behind a severe confirmation

### Quality-of-life
- **Process grouping** — the Processes page groups by category (SYSTEM / BROWSER / GAMING / MEDIA / COMM / SECURITY / BLOAT / USER) with per-group headers and counts
- **Optimal Page File tweak** — auto-sizes the pagefile to 1.5× RAM initial / 3× RAM max from your installed memory
- **Smart SELECT ALL** — selects all tweaks + privacy tweaks (skips the Restore tab) and applies the gaming services profile in one click

### MSI installer
- Proper Windows Installer (`VOIDTUNE-0.8-Setup.msi`): per-machine install, Start Menu + Desktop shortcuts, Add/Remove Programs entry, wizard UI, clean upgrades and a fully clean uninstall

### Stability fixes
- Fixed XAML failing to load (missing `xmlns:sys` namespace)
- Fixed the app not starting — source files are now UTF-8 **with BOM** so the runtime decodes correctly
- Fixed the splash screen tearing the process down under the compiled `-noConsole` build
- Robust executable path resolution inside the ps2exe build

> Full history in [CHANGELOG.md](CHANGELOG.md).

---

## Overview

VOIDTUNE is a free, open-source Windows optimizer and debloater built with PowerShell 5.1+ and WPF/XAML. It gives gamers and power users a clean, dark UI to apply system tweaks, manage services, monitor hardware, and install apps — all in one place, without touching the command line.

**Visit the official site at [voidtune-optimizer.netlify.app](https://voidtune-optimizer.netlify.app) for more info and screenshots.**

---

## Installation

### Option A — MSI installer (recommended)

1. Download **`VOIDTUNE-0.8-Setup.msi`** from the [Releases](https://github.com/otzpt/VOIDTUNE/releases) page.
2. Run it and follow the wizard.

Installs to `Program Files`, adds Start Menu + Desktop shortcuts, and registers in Add/Remove Programs for a clean uninstall.

### Option B — Portable ZIP

1. Download **`VOIDTUNE_0.8V.zip`** from [Releases](https://github.com/otzpt/VOIDTUNE/releases).
2. Extract it — **keep all files and folders together**.
3. Right-click `LAUNCH_VOIDTUNE.bat` → **Run as Administrator** (or run `VOIDTUNE.exe`).

> All files (`core\`, `modules\`, `ui\`, `*.xaml`) must stay in the same folder. Do not move files around.

### Building the EXE

Requires [ps2exe](https://github.com/MScholtes/PS2EXE):

```powershell
Install-Module ps2exe -Scope CurrentUser
Invoke-PS2EXE .\VOIDTUNE.ps1 .\VOIDTUNE.exe -noConsole -requireAdmin -title "VOIDTUNE" -version "0.8.0.0"
```

> Source files must be saved **UTF-8 with BOM** so PowerShell decodes them correctly when run from the compiled exe.

### Building the MSI

The `installer\` folder contains the WiX authoring and a self-contained build script:

```powershell
powershell -ExecutionPolicy Bypass -File installer\build.ps1
```

It downloads WiX v3 on first run, harvests the payload and produces `VOIDTUNE-0.8-Setup.msi`.

---

## Features

### Tweaks
Apply and revert registry tweaks, power plan changes, and system settings across multiple categories:

- **CPU** — High performance plan, Win32 priority, timer resolution, core parking, boost modes
- **GPU** — Hardware GPU scheduling, TDR delay, Direct Flip, MPO disable, GPU priority
- **RAM** — Disable paging executive, large system cache, memory compression, optimal pagefile
- **Network** — TCP no delay, Fast Open, RSS, DNS flush, QoS throttle removal
- **Debloat** — Telemetry, Cortana, Game Bar, animations, mouse acceleration, Superfetch
- **Power** — Balanced, High Performance, Ultimate Performance (hidden plan)
- **Latency** — App kill timeout, NTFS optimization, IRQ priority, HPET disable
- **Game** — Game Mode, MMCSS scheduling, fullscreen optimizations, DWM flush rate
- **NUCLEAR** — Security-disabling tweaks (Defender, SmartScreen, UAC, Core Isolation, Firewall) — all revertible
- **Restore** — One-click revert to Windows defaults
- **Architecture tweaks** — Intel/AMD CPU and NVIDIA/AMD GPU specific tweaks unlocked at runtime based on detected hardware

All tweaks show their current applied state and can be individually reverted.

### Dashboard
Real-time CPU and RAM usage, health score, bottleneck detection, quick stats on applied tweaks, backups, and running processes.

### App Installer
Install common apps via winget (Chocolatey fallback) — browsers, dev tools, gaming launchers, hardware monitors, media, security tools — all in one click.

### Services
Toggle, stop, start and profile Windows services. Includes Gaming and Normal presets, dependency warnings, and a disable-all option.

### Process Monitor
View top processes grouped by category and RAM usage, tag known bloat automatically, kill individual processes or sweep all bloat in one click.

### Hardware Diagnostics
Full hardware info cards: CPU, GPU, RAM, Disk, OS, Motherboard, Uptime. Driver version and build info included.

### Driver Info
Full list of installed drivers with version, date, manufacturer and category. Filter by name, category or manufacturer. Export to CSV.

### GPU Health
GPU name, vendor, driver version and date, VRAM total and free, GPU usage, temperature, core clock, memory clock, fan speed, power draw. Powered by nvidia-smi on NVIDIA systems.

### Full System Latency Checker
Six-section latency report: timer resolution, DPC heuristic, disk I/O, memory, network ping, DNS resolution. Scored out of 100 with recommendations. Results can be copied or saved to file.

### Benchmarks
Quick in-app benchmarks: disk write, disk read, RAM throughput, CPU prime sieve, network ping, DNS resolution. History log included.

### Privacy
Block telemetry, ads, camera, microphone, location, Cortana, activity feed and Windows Update.

### Personalize
Toggle dark mode, transparency, rounded corners, taskbar items, accent colors, wallpaper, ClearType, animations, AeroPeek, and more — with live preview and Explorer restart.

### Startup Manager
View and disable startup entries from HKCU and HKLM.

### Backup & Restore
Auto registry backup before every apply. Manual backup and Windows restore point creation. Restore from any saved backup.

### Script Runner
Run CMD or PowerShell commands directly with full admin rights from inside VOIDTUNE.

---

## Requirements

- Windows 10 (Build 19041+) or Windows 11
- PowerShell 5.1 or newer
- Administrator privileges (auto-requested on launch)
- winget (recommended) or Chocolatey for App Installer

---

## Contributing

Contributions are welcome. If you want to add tweaks, fix bugs, or improve the UI:

1. Fork the repository
2. Create a branch: `git checkout -b feature/your-feature`
3. Commit your changes: `git commit -m "add: your feature"`
4. Push and open a Pull Request

For new tweaks, add them to `modules/data.ps1` following the existing `[TI]` model. Include a `RevertCmd` wherever possible.

For bug reports, open an issue with your Windows build, hardware info, and the relevant lines from `logs/voidtune_*.log`.

---

## Disclaimer

VOIDTUNE modifies Windows registry entries, services, and system settings. **Use at your own risk.** Always create a backup or restore point before applying tweaks. The author accepts no responsibility for data loss, system instability, or any other issues arising from use of this software.

---

## License

VOIDTUNE is licensed under the **GNU General Public License v3.0**.

You are free to use, modify, and distribute this software under the terms of the GPL v3. Any derivative work must also be distributed under the same license.

Copyright (C) 2026 @otzpt • [voidtune-optimizer.netlify.app](https://voidtune-optimizer.netlify.app)
