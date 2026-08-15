# Changelog

## 0.8.20 - 2026-08-08

### Added
- Standalone signed `.exe` release artifact, alongside the portable ZIP and MSI
- Release workflow: pushing a `v*` tag builds and publishes all three artifacts to a GitHub release

### Fixed
- `GameWatcherService`: reset `_powerGamePid` on `Stop()`, avoiding a stale/reused-PID race on the next `Start()`
- `CommandRunner`: bound the post-exit stdout/stderr read to the same timeout as the process wait, so a grandchild process holding the pipes open can't hang a tweak
- `UpdateService`: capped the self-wait loop in the MSI/portable update swap at 30s instead of waiting indefinitely
- `build.ps1`: checks `$LASTEXITCODE` after `dotnet publish` (`ErrorActionPreference` alone doesn't catch external process failures)
- LICENSE file was truncated to the preamble plus a link; replaced with the full GPL-3.0 text (GitHub's license detector was showing NOASSERTION as a result)

### Removed
- The original PowerShell + WPF edition (`core/`, `modules/`, `ui/`, `VOIDTUNE.ps1`, root `installer/`) — superseded by the WinUI 3 rewrite six weeks earlier, unreferenced by CI, and already out of sync with the shipping app's version number
- Duplicate `docs/CHANGELOG.md`, which was missing the two most recent releases

## 0.8.18 - 2026-07-23

### Fixed
- PS: tweaks could fail with "the term '...ps1' is not recognized" — `CommandRunner` invoked `powershell.exe` by bare name, which on some systems resolves to something other than real PowerShell (App Execution Alias shadowing, PATH ordering). Now invokes the fully-qualified System32 path.
- Reboot-gated tweaks could show as flipped off while scrolling the Tweaks/Startup/Customization lists — a remaining gap in 0.8.15's container-recycling fix. Added a short re-validation delay so a transient recycling artifact isn't mistaken for a real toggle.

## 0.8.17 - 2026-07-10

### Added
- Installer wizard (Welcome -> License -> Progress -> Exit) via WixUI_Minimal; the MSI previously had no authored UI at all
- "Launch VOIDTUNE" checkbox on the installer's finish page, checked by default
- Installer now closes a running VOIDTUNE before installing instead of failing on a locked exe

### Fixed
- MSI-based in-app updates now auto-launch VOIDTUNE when they finish, matching the portable-ZIP update path (previously required clicking through the wizard again and never reopened the app)

## 0.8.16 - 2026-07-08

### Fixed
- Extreme lag applying tweaks — 0.8.14's self-healing fallback retried failed tweaks sequentially in a blocking loop with no progress feedback. Fallback retries now run through the same bounded-parallelism batching as the main pass.

## 0.8.15 - 2026-07-08

### Fixed
- Toggling one tweak could silently apply/revert a different tweak while scrolling the Tweaks/Startup/Customization lists — a WinUI container-recycling race where `Toggled` could fire before a separately-bound `Tag` had caught up. Fixed by reading `DataContext` instead of `Tag`.

## 0.8.14 - 2026-07-07

### Added
- Automatic startup repair: on every launch, checks for known damage signatures from recalled tweaks and removes them without user action
- VOIDTUNE Power Plan — hardware-aware custom power plan (Ultimate-based on desktops, Balanced-based on laptops)
- Game-Time Power Plan — switches to the VOIDTUNE plan only while a game is running
- Tweak Validator (Benchmarks page) — 5x45s stress runs with a statistical verdict on whether tweaks helped, hurt, or made no measurable difference, including a thermal-throttle detector
- Failure logging with one-click GitHub issue report; full log written to `%LocalAppData%\VOIDTUNE\logs\`
- Self-healing fallbacks: when a service tweak is blocked, retries via the equivalent registry write

### Fixed
- "Intel Speed Shift EPP" tweak was disabling turbo boost machine-wide across every power plan, not just the one it targeted (confirmed on real hardware: 350->140 FPS regression). Removed; a "Restore CPU Turbo Boost" tool cleans up the stale override for anyone who applied it.
- "Faster Shutdown & App Kill" (`HungAppTimeout=2000` + `AutoEndTasks=1`) could cause Windows to kill and restart Explorer during routine stalls. Now scoped to shutdown timeouts only.
- "Max performance" power tweaks (CPU min 100%, aggressive boost, GPU max power state) made thermally-limited laptops throttle harder, not run faster. Now desktop-only.

## 0.8.13 - 2026-07-06

### Added
- Automated test suite: catalog-integrity checks, a dangerous-command blocklist, Apply/Revert round-trip tests running in CI
- GitHub Actions CI: every push/PR builds, tests, and validates release packaging
- 4 new tweaks (background process de-prioritization via IFEO, disable Automatic Maintenance wake, disable Fault Tolerant Heap, disable Explorer folder-type auto-discovery)
- 4 new NUCLEAR-tier tweaks (Bluetooth stack, Print Spooler, sensor/biometric services, Remote Desktop)

### Fixed
- 4 duplicate tweaks in the catalog, caught by the new integrity tests

## 0.8.12 - 2026-07-06

### Added
- Authenticode code signing for `VOIDTUNE.exe` and the MSI as part of the release build
- Custom app icon for the WinUI app, MSI, and Add/Remove Programs entry

## 0.8.11 - 2026-07-05

### Fixed
- "Full Reset to Windows Defaults" was deleting the Ultimate Performance power plan (`powercfg -restoredefaultschemes` wipes unlocked custom schemes) and could crash processes holding live sockets (`netsh int tcp/ip reset` reinitializes the Winsock LSP catalog). Replaced with targeted, settings-only resets.
- "Ultimate Performance" tweak detected an already-unlocked plan by matching the literal English string in `powercfg -list` output, which never matched on non-English Windows. Now detects by GUID.
- Removed "Disable Touch Keyboard Svc" — no real benefit (already Manual on most desktops), broke the Win+. emoji picker
- A stale process-affinity mask could inherit down into every child process, including VOIDTUNE itself

### Added
- Apply preview dialog for Apply SAFE / Apply EXTREME, listing every tweak about to run with individual checkboxes
- 7 more process-reducers mirrored from the Services page into the one-click flow

## 0.8.10 - 2026-07-04

### Changed
- Quality-over-quantity pass: every tweak now has to justify FPS/UX/process-count/RAM benefit without a stability tradeoff
- Hardware GPU Scheduling and GPU MSI Mode demoted from SAFE to EXTREME — both help some systems and hurt others
- Auto Game Boost no longer hard-pins core affinity (could hurt 1% lows); priority + EcoQoS-off only

### Removed
- ~27 tweaks that either hurt performance/stability (Force All Cores, GPU Preemption off, C-state disables) or had no measurable effect (most Network tweaks are TCP-only and don't affect UDP game traffic)

### Added
- Group svchost processes (largest single process-count reduction in the catalog), Disable Telemetry Tasks, Disable Background Apps, "Remove Promoted Junk," Block Driver Updates in Windows Update, Faster Shutdown
- "Full Reset to Windows Defaults" (Restore page)

### Fixed
- VOIDTUNE no longer opens a random folder at Windows login (disabled-startup stash was placed inside the real Startup folder)

## 0.8.9 - 2026-07-03

### Added
- Auto Game Boost — detects a running fullscreen game, pins it to the fastest cores, deprioritizes background apps, disables EcoQoS throttling, and restores everything when the game closes
- DevTools: Process Monitor, Registry Diff, Network toolkit, Console presets, Affinity pinner, persistent Tweak Lab with share codes
- Cloudflare DNS, Recall off, Core Parking off tweaks; RAM-gated and hybrid-CPU-gated tweaks

### Fixed
- Startup toggle no longer inverts
- Services page shows the real start type and waits for the stop to complete
- Tweaks that falsely reported "failed" now apply cleanly
- Removed base64-encoded PowerShell (antivirus heuristic trigger)

## 0.8.8 - 2026-07-03

### Added
- Dashboard redesign: health ring, CPU/RAM sparklines, per-drive storage meters, hardware/uptime chips, quick actions including restore-point creation
- Tweaks tab reorganized into 14 color-coded sections
- Search Highlights off, No Startup App Delay, Cloudflare DNS, Disable Core Parking, No Edge Preload, NTFS RAM Boost tweaks

## 0.8.7 - 2026-07-02

### Added
- Customization tab rebuilt as a searchable mod grid with instant toggles
- Tweaks now save and apply live, persisted to `settings.json`
- Developer mode / DevTools (WIP, gated behind a toggle)

### Removed
- Harmful/aggressive tweaks: DPC Watchdog off, HPET disable, Spectre mitigation off
- The entire Nuclear tier from this version (reintroduced later under stricter gating); Privacy tab folded into normal tweaks; camera/mic blocks removed

## 0.8.6 - 2026-07-01

### Fixed
- App-removal, security-tray, NTFS-journal, and CTCP tweaks reported failure for benign reasons (target already in desired state); now best-effort
- PowerShell tweaks sent via base64 `-EncodedCommand`, fixing scripts containing their own quotes

### Added
- Self-elevation fallback if the app is ever started unelevated

## 0.8.5 - 2026-07-01

### Fixed
- Apply could hang indefinitely near the end of a batch — stdout/stderr are now read concurrently with a timeout instead of sequentially with none

### Changed
- Power plans trimmed to one: Ultimate Performance (now activates the plan, not just unlocks it)

## 0.8.4 - 2026-07-01

### Fixed
- Startup toggle could spin into an infinite loop; now updates the entry in place

### Added
- Apply progress dialog with a live progress bar

## 0.8.3 - 2026-06-30

### Fixed
- Startup page crash when toggling an entry (list rebuild deferred until after the event unwinds)
- Registry backup and apply/revert commands now run concurrently (bounded) instead of one at a time

### Changed
- Gaming/Normal service profiles apply in parallel

## 0.8.2 - 2026-06-27

### Added
- Processes tweak category: ~50 reversible tweaks (services, scheduled tasks, per-user service templates, bloatware removal, idle updater disables)
- "Hide Security Tray Icon" tweak (Defender protection stays on)
- Services manager expanded to 37 services

### Removed
- Standalone Processes viewer page — process reduction now lives entirely in the Tweaks tab

## 0.8.1 - 2026-06-27

### Added
- Native C# / WinUI 3 rewrite (Windows App SDK): Mica backdrop, Fluent design, custom title bar
- In-app auto-update (MSI flow for installed users, self-extracting ZIP swap for portable)
- ~150 reversible tweaks including architecture-specific (Intel/AMD/NVIDIA/laptop) entries
- Registry backup before every apply; state persisted to `%LOCALAPPDATA%\VOIDTUNE`
- Portable ZIP and MSI installer builds (WiX v5)

## 0.8 - 2026-06-27

### Added
- One-click tiered apply: SAFE / EXTREME / NUCLEAR / REVERT ALL
- NUCLEAR tab (security-disabling tweaks, all revertible, gated behind confirmation)
- Process grouping by category
- Optimal Page File tweak
- MSI installer: per-machine install, Start Menu/Desktop shortcuts, Add/Remove Programs entry, clean upgrades and uninstall

### Fixed
- XAML failing to load (missing `xmlns:sys` namespace)
- App not starting (source files now saved UTF-8 with BOM)
- Splash screen crash under the compiled `-noConsole` build
- Executable path resolution inside the ps2exe build

## 0.7 - 2026-03-28

### Added
- Driver Info page (version, date, manufacturer, category, CSV export)
- GPU Health page via nvidia-smi
- Full System Latency Checker (6-section report, scored out of 100)
- Architecture-specific tweaks: Intel/AMD CPU, NVIDIA/AMD GPU
- Laptop detection with AC-only performance tweaks

### Fixed
- Chained commands (`&&`) now run correctly
- GUID corruption in registry paths
- GPU Health VRAM total now reads from registry before falling back to WMI (fixed an incorrect 4GB cap on modern GPUs)
- `nvidia-smi` path detection now checks multiple candidate paths instead of one

## 0.6 - 2026-03

### Added
- Initial public release
- Dashboard with health score and bottleneck detection
- Tweaks: CPU, GPU, RAM, Network, Debloat, Power, Latency, Game, Restore
- Privacy tab
- App Installer via winget / Chocolatey
- Services manager with Gaming and Normal presets
- Process Monitor with bloat detection
- Hardware Diagnostics
- Benchmarks: disk, RAM, CPU, network, DNS
- Personalize: dark mode, transparency, accent color, wallpaper, taskbar, animations
- Startup Manager
- Backup & Restore with automatic registry backup
- Script Runner
