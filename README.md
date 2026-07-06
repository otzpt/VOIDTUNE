# VOIDTUNE v0.8.12

**Windows optimization suite for gamers and power users.**

| Version | License | Platform | Current edition |
|---------|---------|----------|-----------------|
| 0.8.12 | GPL v3 | Windows 10/11 (x64) | C# / WinUI 3 (Windows App SDK) |

🌐 **[Website](https://voidtune-optimizer.netlify.app)** • 📦 **[Releases](https://github.com/otzpt/VOIDTUNE/releases)** • 🐛 **[Issues](https://github.com/otzpt/VOIDTUNE/issues)**

---

## Editions

- **WinUI 3 (current)** — the native C# rewrite lives in [`VOIDTUNE.WinUI/`](VOIDTUNE.WinUI/). Fluent / Mica UI, ~165 reversible tweaks, in-app auto-update, MSI + portable builds. Build/run notes in [VOIDTUNE.WinUI/README.md](VOIDTUNE.WinUI/README.md).
- **VOIDTUNE One-Click (native C)** — a ~170 KB, zero-dependency automatic optimizer: [otzpt/Voidtune-one-click](https://github.com/otzpt/Voidtune-one-click). Hit "Optimize Now," done. No install, runs anywhere Windows runs.
- **PowerShell + WPF (original)** — the original ps2exe edition at the repo root (`VOIDTUNE.ps1`, `core/`, `modules/`, `ui/`). Still functional; superseded by the WinUI app.

## 🚀 What's New in v0.8.12 (WinUI 3 edition)

- **Signed builds** — VOIDTUNE.exe, the MSI, and the portable ZIP are now Authenticode-signed as part of the release build
- **Custom app icon** — the WinUI app, MSI, and Add/Remove Programs entry now use the real VOIDTUNE brand icon instead of the default

> Full history in [CHANGELOG.md](CHANGELOG.md).

## 🚀 What's New in v0.8.11 (WinUI 3 edition)

- **Fixed a real regression from 0.8.10** — the "Full Reset to Windows Defaults" tool was silently deleting the Ultimate Performance power plan and could crash processes holding live network sockets. Both replaced with safe, targeted resets
- **Apply preview dialog** — Apply SAFE / the new **Apply EXTREME** button now show exactly what's about to run, with checkboxes to deselect anything, before touching your system
- **7 more real process-reducers** mirrored from the Services page into the one-click flow (Xbox background stack, Fax, Offline Maps, Media Player Sharing, Remote Registry, Internet Connection Sharing, Retail Demo)
- Fixed a locale bug in the Ultimate Performance tweak (was English-only) and removed a tweak that broke the Win+. emoji picker for no real benefit

> Full history in [CHANGELOG.md](CHANGELOG.md).

## 🚀 What's New in v0.8.10 (WinUI 3 edition)

- **Quality over quantity pass** — every tweak now has to earn its place against one rule: FPS, UX, fewer processes, less RAM, without touching stability
- **Removed harmful & placebo tweaks** — cut ~27 tweaks that either hurt performance/stability (Force All Cores, GPU Preemption off, C-state disables) or did nothing measurable (most Network tweaks are TCP-only and don't touch UDP game traffic)
- **HAGS + GPU MSI Mode demoted to opt-in** — both are coin-flips that help some systems and stutter others, so they're no longer in the set everyone applies blindly
- **New real process-reducers** — Group svchost processes (the single biggest process-count cut in the catalog), Disable Telemetry Tasks, Disable Background Apps, **"Remove Promoted Junk"** (Candy Crush and friends), Block Driver Updates in Windows Update, Faster Shutdown
- **"Full Reset to Windows Defaults"** in the Restore tab, plus a reboot prompt after reboot-gated tweaks
- **Fixed** — VOIDTUNE no longer opens a random folder at Windows login

> Full history in [CHANGELOG.md](CHANGELOG.md).

## 🚀 What's New in v0.8.9 (WinUI 3 edition)

- **Auto Game Boost** — one toggle (Tweaks → Gaming) and games are auto-detected and pinned to your fastest cores (Intel P-cores / AMD CCD-0), background apps pushed aside, EcoQoS throttling off — then **fully restored when the game closes**. Topology-aware, documented APIs only
- **DevTools built out** — live **Process Monitor** (impact bars + End Task), real **Registry Diff**, **Network** toolkit (adapters, latency, live connections, DNS switch), **Console** presets, **Affinity** pinner, persistent **Tweak Lab** with share codes
- **Fixes** — Startup toggle no longer inverts; Services page shows the real start type and waits for the stop; tweaks that falsely reported "failed" now apply cleanly; removed base64 PowerShell (antivirus-flag trigger)
- **New tweaks + gating** — Cloudflare DNS, Recall off, Core Parking off, and comprehensive hardware/OS gating (RAM-gated, hybrid-CPU, Windows 11-only)
- **Startup loading screen** — "Looking for already-activated tweaks…"

> Full history in [CHANGELOG.md](CHANGELOG.md).

## 🚀 What's New in v0.8.8 (WinUI 3 edition)

- **The Void redesign** — new living Dashboard: animated **health ring**, ambient **starfield**, live **CPU/RAM sparklines**, per-drive storage meters, hardware + uptime chips, and one-click quick actions incl. **Create restore point**
- **Tweaks tab reorganized** — 14 **color-coded sections** with icons and counts (Gaming, CPU, GPU, Memory, Latency, Network, Power, Storage, …) in a curated order
- **New quality tweaks** — Search Highlights off, No Startup App Delay, **Cloudflare DNS**, Disable Core Parking, No Edge Preload, NTFS RAM Boost
- **Void design system** — violet→pink gradients, glow cards, nebula ambience across the app, entrance animations, Discord link in the nav

> Full history in [CHANGELOG.md](CHANGELOG.md).

## 🚀 What's New in v0.8.7 (WinUI 3 edition)

- **Customization tab rebuilt** — a Windhawk-style searchable **mod grid** with instant toggles + new mods (left taskbar, clock seconds, hide Copilot/Chat, show file extensions/hidden files, open to This PC, verbose logon…)
- **Tweaks save & apply live** — toggle to apply/revert instantly (persisted to `settings.json`); startup **verifies real system state**
- **Developer mode → DevTools** — live Probe, elevated Console, and a **block-based Tweak Builder** (submit to GitHub for approval), gated behind a WIP toggle
- **Curated for quality:** removed the harmful/aggressive tweaks (DPC-watchdog off, HPET, Spectre-off) and the entire **Nuclear** tier; removed the Privacy tab (its items are normal tweaks now) and the camera/mic blocks

> Full history in [CHANGELOG.md](CHANGELOG.md).

## 🚀 What's New in v0.8.6 (WinUI 3 edition)

- **No more false "failed" tweaks** — the app-removal, security-tray, NTFS-journal and CTCP tweaks reported failure for benign reasons (target already in desired state, or an empty package match). All are now best-effort and apply cleanly
- **Quote-proof PowerShell** — PS tweaks run via base64 `-EncodedCommand`, fixing scripts that contain their own quotes (MSI-mode, write-cache)
- **Self-elevation fallback** — the app already auto-prompts UAC via its manifest; it now also relaunches elevated if ever started unelevated

## 🚀 What's New in v0.8.5 (WinUI 3 edition)

- **Fixed: apply hang near the end** — applying could freeze forever at e.g. "161 / 163". Command output is now read without deadlocking, and any command that runs too long is killed so the apply always finishes
- **Power plans trimmed to one** — only **Ultimate Performance** remains (and it now activates the plan, not just unlocks it); the Balanced/High Performance choices and their duplicates were removed. Power settings (Disable Sleep/Hibernate/Fast Startup, CPU 100%) are unchanged

## 🚀 What's New in v0.8.4 (WinUI 3 edition)

- **Fixed: startup toggle loop** — toggling a startup entry could spin into an infinite loop; it now updates the entry in place and does exactly one change per toggle
- **Apply progress dialog** — applying/reverting tweaks shows a live progress bar ("Applying 12 / 40") instead of a small spinner
- Updater now compares all four version fields for accurate update detection

> Full history in [CHANGELOG.md](CHANGELOG.md).

## 🚀 What's New in v0.8.3 (WinUI 3 edition)

- **Fixed: Startup page crash** — toggling a startup entry on/off no longer crashes the app (the bound list was being rebuilt from inside the toggle's own event; the rebuild is now deferred)
- **Faster tweak apply** — the registry backup and the apply/revert commands now run concurrently (bounded) instead of one process at a time, cutting a full apply from tens of seconds to a few
- **Faster service profiles** — the Gaming and Normal presets apply their service changes in parallel
- Safer threading — UI state is updated only after the parallel work completes, avoiding cross-thread UI exceptions

> Full history in [CHANGELOG.md](CHANGELOG.md).

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

1. Download **`VOIDTUNE-0.8.12-Setup.msi`** from the [Releases](https://github.com/otzpt/VOIDTUNE/releases) page.
2. Run it and follow the wizard.

Installs to `Program Files`, adds Start Menu + Desktop shortcuts, and registers in Add/Remove Programs for a clean uninstall.

### Option B — Portable ZIP

1. Download **`VOIDTUNE-0.8.12-portable-win-x64.zip`** from [Releases](https://github.com/otzpt/VOIDTUNE/releases).
2. Extract it — **keep all files and folders together**.
3. Run `VOIDTUNE.exe` (it auto-requests Administrator).

> The portable build is self-contained — no .NET install required. Keep the extracted folder intact; don't move files around.

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
