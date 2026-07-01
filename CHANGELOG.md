# Changelog

## [0.8.4] - 2026-07-01 (WinUI 3 edition)

### Fixed
- **Startup toggle loop** — regression from 0.8.3: toggling a startup entry could spin into an infinite loop (the list was rebuilt on every toggle, and the TwoWay `IsOn` binding re-fired the event as the containers re-realized). Toggling now updates the entry in place and ignores the binding's own echo, so one toggle does exactly one change

### Added
- **Apply progress dialog** — applying or reverting tweaks now shows a modal dialog with a live progress bar ("Creating registry backup…", then "Applying 12 / 40") instead of just a small spinner

### Changed
- Updater now compares all four version fields for accurate update detection

## [0.8.3] - 2026-06-30 (WinUI 3 edition)

### Fixed
- **Startup page crash** — toggling a startup entry on/off no longer crashes the app. The `Toggled` handler was rebuilding the bound `ListView` collection from inside the ToggleSwitch's own event, tearing out the live container; the rebuild is now deferred until after the event unwinds
- **Slow tweak apply** — applying tweaks no longer blocks for tens of seconds. The registry backup exports and the apply/revert commands now run concurrently (bounded) instead of strictly one process at a time

### Optimized
- **Service profiles** — the Gaming and Normal service profiles apply their `sc` changes in parallel instead of sequentially, finishing in a fraction of the time
- UI-bound state (`Applied` / `Enabled`) is now mutated only on the caller's thread after the parallel work completes, avoiding cross-thread UI exceptions

## [0.8.2] - 2026-06-27 (WinUI 3 edition)

### Added
- **Process-reduction overhaul** — the **Processes** tweak category (in the Tweaks tab) now bundles ~50 reversible tweaks: services, scheduled tasks, per-user service templates (`Start=4`), bloatware Appx removal, and idle third-party updater disables — built to cut the background process count **without disabling Windows Security**
- **Bloatware removal** (reversible): Bing News/Weather, Phone Link, Groove & Movies, Teams Chat, Clipchamp, Help/Get Started, People/Feedback, Maps/To Do/Office Hub, Cortana/Mixed Reality. Xbox / Game Bar left intact for gamers
- **Idle updater disables**: Google Update, Edge Update + their scheduled tasks
- **Hide Security Tray Icon** — removes `SecurityHealthSystray.exe` with Defender protection fully ON
- Services manager expanded to 37 services; Gaming profile now disables 31

### Changed
- Removed the standalone Processes viewer page — process reduction now lives entirely in the Tweaks tab

## [0.8.1] - 2026-06-27 (WinUI 3 edition)

### Added
- **Native C# / WinUI 3 rewrite** (Windows App SDK) — Mica backdrop, Fluent design, custom title bar, runs elevated. Pages: Dashboard, Tweaks, Services, Startup, Privacy, Personalize, Drivers, GPU Health, Latency, Benchmarks, Apps (winget), Backup & Restore, Script Runner, Settings
- **In-app auto-update** — checks GitHub releases on launch; MSI flow for installed users, self-extracting ZIP swap for portable users
- ~150 reversible tweaks incl. architecture-specific (Intel/AMD/NVIDIA/laptop) gated on detected hardware
- Registry backup before every apply; tweak state persisted to `%LOCALAPPDATA%\VOIDTUNE`
- Self-contained portable ZIP + MSI installer builds (`installer/build.ps1`, WiX v5)

### Changed
- Smarter hardware detection: discrete GPU preferred over integrated (registry VRAM), real battery-based laptop detection
- NUCLEAR tweaks hidden behind an explicit "Show NUCLEAR" confirmation

## [0.8] - 2026-06-27

### Added
- **One-click tiered apply** — `SAFE` / `EXTREME` / `NUCLEAR` / `REVERT ALL` buttons in the topbar. Each tier applies everything up to its level; REVERT ALL rolls every applied tweak back to Windows defaults
- **NUCLEAR tab** — a new tier of security-disabling tweaks (Defender real-time, SmartScreen, UAC, Core Isolation/HVCI, Firewall). All revertible, gated behind a severe confirmation dialog
- **Process grouping** — the Processes page now groups by category (SYSTEM / BROWSER / GAMING / MEDIA / COMM / SECURITY / BLOAT / USER) with per-group headers and counts
- **Optimal Page File tweak** — auto-sizes the pagefile to 1.5x RAM initial / 3x RAM max from your installed memory
- **Smart SELECT ALL** — selects all tweaks + privacy tweaks (skips the Restore tab) and applies the gaming services profile in one click
- **MSI installer** — proper Windows Installer (`VOIDTUNE-0.8-Setup.msi`): per-machine install, Start Menu + Desktop shortcuts, Add/Remove Programs entry, wizard UI, clean upgrades and a fully clean uninstall (removes runtime logs/backups too)

### Fixed
- **XAML failed to load** — added the missing `xmlns:sys` namespace used by the font-size constants
- **App would not start** — `.ps1` files are now saved UTF-8 **with BOM** so the runtime decodes the special characters correctly instead of as ANSI (which broke the parser mid-file)
- **Splash screen crash** — the splash no longer runs on a separate `Dispatcher.Run()` STA thread, which silently tore the process down under the `-noConsole` compiled build. It now renders inline on the main thread
- **Compiled `.exe` path resolution** — robust 5-stage fallback so `ROOT` is never null inside the ps2exe build (fixes "cannot bind argument to parameter 'Path'")

## [0.7] - 2026-03-28

### Added
- **Driver Info page** — full list of installed drivers with version, date, manufacturer and category. Filter and export to CSV
- **GPU Health page** — GPU stats powered by nvidia-smi: temperature, core/mem clocks, fan speed, power draw, VRAM free/total
- **Full System Latency Checker** — 6-section latency report scored out of 100: timer resolution, DPC heuristic, disk I/O, memory, network ping, DNS. Save or copy results
- Architecture tweaks: Intel/AMD CPU and NVIDIA/AMD GPU specific tweaks unlocked at runtime based on detected hardware
- AMD GPU tweaks: deep sleep disable, Radeon Chill off, ULPS disable
- NVIDIA GPU tweaks: max perf mode, telemetry disable, shader cache, HDCP off
- Game tweaks: DWM flush rate, CPU priority via MMCSS, fullscreen optimizations, HPET disable
- Debloat tweaks: Aero Peek, Aero Shake, Snap Assist, menu delay, thumbnail cache, sticky keys
- Laptop detection with AC-only performance tweak

### Fixed
- `RunC` rewritten with `ProcessStartInfo` — `&&` chained commands now work correctly
- `Exec-Cmd` dispatcher added — PS: prefix routes to PowerShell, everything else to cmd
- GUID corruption in registry paths resolved
- Em dash encoding issue fixed
- Tweak state persistence via `voidtune_state.txt`
- FORCE ALL CORES now uses explicit power plan GUIDs to avoid failing after Ultimate Perf activation
- DISABLE TELEMETRY / DISABLE SUPERFETCH no longer report FAILED when service is already stopped
- DISABLE HPET no longer fails when `useplatformclock` was never set
- GPU Health: `\u{00B0}` replaced with `[char]176` for PowerShell 5.1 compatibility
- GPU Health: VRAM total now reads from registry (`HardwareInformation.qwMemorySize`) before falling back to WMI, fixing incorrect 4GB cap on modern GPUs
- nvidia-smi path detection made more robust with multiple candidate paths

## [0.6] - 2026-03

### Added
- Initial public release
- Dashboard with health score and bottleneck detection
- Tweaks: CPU, GPU, RAM, Network, Debloat, Power, Latency, Game, Restore
- Privacy tab
- App Installer via winget / Chocolatey
- Services manager with Gaming and Normal presets
- Process Monitor with bloat detection and kill sweep
- Hardware Diagnostics
- Benchmarks: disk, RAM, CPU, network, DNS
- Personalize: dark mode, transparency, accent color, wallpaper, taskbar, animations
- Startup Manager
- Backup & Restore with auto registry backup
- Script Runner
