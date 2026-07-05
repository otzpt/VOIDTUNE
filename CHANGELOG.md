# Changelog

## [0.8.10] - 2026-07-04 (WinUI 3 edition)

### The quality-over-quantity pass
Every tweak now has to earn its place against one rule: **FPS, UX, fewer processes, less RAM — without touching stability.** Since most users just hit "apply everything," everything in that set has to be incapable of hurting them.

### Removed (harmful or placebo)
- **Harmful:** No Memory Compression, Large System Cache, Force All Cores / Disable Core Parking, No GPU Preemption, Max GPU Clocks, Intel/AMD C-state disables — all of which lower FPS/boost headroom or risk stability.
- **13 placebo tweaks**, mostly Network: Nagle off, TCP Fast Open, the "20% QoS reserve" myth, TIME_WAIT, max ports, ECN off, CTCP (dead on modern Windows), dubious VSync-latency keys, page combining. None of it affects a UDP game. Network tweaks: 15 → 5.
- **4 tweaks that increase RAM for negligible FPS**: Disable Paging Executive, NTFS RAM Boost, Large Paged Pool.

### Changed
- **Hardware GPU Scheduling (HAGS)** and **GPU MSI Mode** demoted from SAFE to EXTREME (opt-in) — both are coin-flips that help some systems and stutter others, so they no longer land in the set everyone applies blindly.
- **Auto Game Boost no longer restricts core affinity** — hard-pinning a game to a core subset could hurt 1% lows and even starved the game's own helper processes (browser/overlay) in edge cases. It's now priority + EcoQoS-off only, with all cores available to the game.

### Added
- **Real process-reducers:** Group svchost processes (merges ~50 split hosts into ~10 — the single biggest process-count cut in the catalog), Disable Telemetry Tasks, Disable Background Apps, Disable Consumer Features, Block Driver Updates in Windows Update (stops it silently overwriting your GPU driver), Faster Shutdown & App Kill, Disable Error Reporting, and a **"Remove Promoted Junk"** catch-all (Candy Crush and the rest of the King games, Disney+, TikTok, Spotify stub, Solitaire, and more) — future-proof against Windows re-installing them after updates.
- **"Full Reset to Windows Defaults"** (Restore tab) — resets power/timers/GPU-scheduling/memory/network to stock, including tweaks that were removed from the catalog and can no longer be toggled off individually.
- **Reboot prompt** after applying reboot-gated tweaks (GPU scheduling, MSI mode, timers) — running half-applied was causing transient stutter.
- **Blocks / Code mode toggle** in the Tweak Lab — switch between the visual block builder and raw apply/revert commands.

### Fixed
- **"VOIDTUNE opens a random folder at login"** — the disabled-startup stash lived *inside* the Startup folder, and Windows opens any folder placed there at login. Moved the stash to LocalAppData; the app now also auto-cleans legacy stash folders from both the per-user and all-users Startup on its own.

Net: ~170 → ~152 tweaks. Every survivor has a defensible reason to exist.

## [0.8.9] - 2026-07-03 (WinUI 3 edition)

### Added
- **Auto Game Boost** (Tweaks → Gaming) — toggle once and a lightweight watcher auto-detects any fullscreen game, pins it to your fastest cores (Intel P-cores / AMD CCD-0), pushes background apps onto the rest, raises its priority and turns off EcoQoS throttling — then **restores everything the instant the game closes**. Adapts to your CPU (topology-aware) and uses only documented Windows APIs.
- **DevTools built out** (Developer mode): a live **Process Monitor** (impact bars, CPU/RAM, End Task), a real **Registry Diff** (snapshot → change → diff, export as reg commands), a **Network** toolkit (adapters, latency test, live TCP connections, one-click Cloudflare/Google/DHCP DNS), **Console** presets, a **Core Affinity** pinner, and a **Tweak Lab** that now **persists** your creations and can export/import them as share codes.
- **Startup loading screen** — shows "Looking for already-activated tweaks…" while it reconciles state against the live system, then "Found N active tweaks."
- **New tweaks:** Cloudflare DNS, Disable Search Highlights, No Startup App Delay, Disable Core Parking, No Edge Preload, **Disable Windows Recall**, plus RAM-gated (NTFS RAM Boost 16 GB+, Large Paged Pool 32 GB+) and hybrid-CPU P-core bias.
- **Restore is its own tab** with a one-click **"Fix Stutter — Restore Memory Defaults."**

### Fixed
- **Startup enable/disable toggle** no longer inverts or hits the wrong entry — switched from TwoWay to the OneWay + echo-guard pattern (TwoWay was writing back during list virtualization).
- **Services page** now shows the **start type** (DISABLED / DISABLED · RUNNING / RUNNING / STOPPED) instead of just the run state, and **waits for the service to actually stop** before refreshing, so "Disable" reflects reality.
- **Tweaks that reported "failed" now apply cleanly** — an outcome-based fallback verifies the real system state when a command's exit code lies, plus an optional second-method fallback per tweak.
- **Removed base64-encoded PowerShell** (a strong antivirus/heuristic trigger) in favor of readable temp scripts — quote-proof and far less likely to be flagged.

### Changed / Removed
- Removed **No Memory Compression** (crashed a DRAM-less SSD under disk load with second-long freezes) and made hardware/OS gating comprehensive: Windows 11-only tweaks (Copilot, Widgets, Search Highlights) are hidden on Windows 10.

## [0.8.8] - 2026-07-03 (WinUI 3 edition)

### The Void redesign
- **New Dashboard** — a living hero panel: animated **health ring** with a smooth score sweep, an ambient **starfield**, live **CPU / RAM sparklines**, per-drive storage meters, uptime + hardware chips, and four one-click quick actions (Apply SAFE · Revert all · **Create restore point** · Clean temp + DNS).
- **Tweaks tab reorganized** — tweaks are now grouped into **14 color-coded sections** (Gaming, CPU, GPU, Memory, Latency, Network, Power, Storage, Processes, Debloat, Background, Privacy, Audio, Restore) with icons and per-section counts, in a curated order. Search and tier filters work across groups; leftover Nuclear UI removed.
- **Void design system** — signature violet→pink gradients, glow cards, an ambient nebula glow across the whole window, staggered entrance animations, and a **Discord support link** in the navigation pane.

### Added
- New quality tweaks: **Disable Search Highlights**, **No Startup App Delay**, **Cloudflare DNS** (1.1.1.1, revert restores DHCP), **Disable Core Parking**, **No Edge Preload**, **NTFS RAM Boost**.
- Dashboard **Create restore point** quick action — a safety net before heavy tweaking.

### Changed
- Health score is now honest — it reflects actual CPU/RAM pressure and process count instead of inflating with tweak count.

## [0.8.7] - 2026-07-02 (WinUI 3 edition)

### Added
- **Customization tab rebuilt** — a Windhawk-style **mod grid**: searchable cards, one-tap toggles that apply instantly, plus new mods: left-align taskbar, seconds in clock, hide Copilot / Chat, End Task on right-click, show file extensions, show hidden files, open Explorer to This PC, verbose logon.
- **Tweaks now save & toggle live** — toggling a tweak applies (or reverts) it immediately and persists to `settings.json`. Startup **verifies actual system state**, so tweaks already applied by another optimizer or an older version show correctly.
- **Developer mode** (Settings) — unlocks a new **DevTools** category (right after Tweaks): live system **Probe**, an elevated **Console**, and a **block-based Tweak Builder** that can submit new tweaks to GitHub for approval. Gated behind a WIP confirmation.

### Changed / Removed
- **Curated for quality over quantity.** Removed the aggressive tweaks that could cause harm: **DPC Watchdog Tune** (disabled the watchdog → system freezes), **Disable HPET** (timer instability), and **No Spectre Mitigation** (security risk). Removed the whole **Nuclear** tier (Defender/SmartScreen/UAC/Core Isolation/Firewall) — too aggressive.
- **Privacy tab removed** — telemetry, ad-blocking, location, Cortana, activity feed and Windows-Update toggles are now normal tweaks. **Camera and microphone blocks removed** (they break Discord etc.).
- Removed the mouse-settings tweaks; `GPU MSI Mode` is GPU-only (never touches input/USB controllers).

## [0.8.6] - 2026-07-01 (WinUI 3 edition)

### Fixed
- **Tweaks that reported "failed" now apply cleanly.** Several tweaks returned a non-zero exit code for benign reasons and were counted as failures:
  - The app-removal tweaks (Bing News/Weather, Zune, Teams, Get Help, People/Feedback, Maps/To Do, Cortana, etc.) pass multiple package name patterns; when any pattern matched nothing, `Get-AppxPackage` raised an error that made PowerShell exit non-zero even though it removed what it could. They're now best-effort (`try/catch; exit 0`).
  - "Hide Security Tray Icon" (value already absent), "Disable NTFS Journal" (journal already inactive), and "CTCP" (deprecated on modern Windows) no longer report failure when the target state is already met.
  - "No Memory Compression" hardened against transient errors.
- **PowerShell tweaks are now sent as `-EncodedCommand`** (base64) instead of inline `-Command "…"`, which corrupted any script containing its own double quotes (the MSI-mode and write-cache tweaks). Quote-proof.

### Added
- **Self-elevation fallback.** The manifest already requests admin (auto UAC prompt); as a safety net the app now also relaunches itself elevated if it ever starts unelevated (e.g. launched via a host that ignores the manifest).

## [0.8.5] - 2026-07-01 (WinUI 3 edition)

### Fixed
- **Apply hang near the end** — applying could freeze partway (e.g. "161 / 163") forever. `CommandRunner` read a process's stdout and stderr sequentially with no timeout, which deadlocks when a command fills the other pipe's buffer. It now reads both pipes concurrently, closes stdin (so nothing can wait for input), and kills any command that exceeds a 90s ceiling so the batch always completes

### Changed
- **Power plans trimmed to one** — removed the Balanced and High Performance plan tweaks (and their duplicates in the CPU category); the only power-plan tweak left is **Ultimate Performance**, which now also *activates* the plan (not just unlocks it) for maximum performance. Power settings (Disable Sleep/Hibernate/Fast Startup, CPU 100% Min/Max) are unchanged

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
