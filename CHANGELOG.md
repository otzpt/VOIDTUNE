# Changelog

## [0.8.16] - 2026-07-08 (WinUI 3 edition)

### Fixed — extreme lag applying tweaks (self-inflicted by 0.8.14's own fallback feature)
- The self-healing fallback retries added in 0.8.14 ran **sequentially** — each failed tweak's fallback was awaited one at a time in a blocking loop, right after the fast parallel main batch had already finished, with no progress-bar feedback during the wait. Each retry spawns a process (PowerShell alone costs 300ms-1s+ to start), so any run with a handful of legitimate failures — more likely now with 175+ tweaks — added many seconds of silent hang at the tail of every Apply/Revert. Both loops now run all fallback retries concurrently through the same bounded-parallelism batching as the main pass, matching the speed of every other bulk operation in the app.

## [0.8.15] - 2026-07-08 (WinUI 3 edition)

### Fixed — toggling one tweak could silently apply/revert a DIFFERENT one while scrolling
- Tweaks, Startup, and Customization pages all identified "which item is this switch for" via `ToggleSwitch.Tag` (bound with `{x:Bind}`). During virtualized-list container recycling, WinUI can re-fire `Toggled` (from the `IsOn` binding refreshing for the recycled row) *before* that row's separate `Tag` binding has caught up — so the handler read the **previous** row's item from `Tag` while `IsOn` already reflected the **new** row, defeated the echo-guard (comparing two different tweaks' states), and applied/reverted the wrong entry. Field-reported as "toggles change tweaks that already had a state" while scrolling with tweaks already active. Fixed by reading `DataContext` instead of `Tag` — WinUI updates a container's `DataContext` atomically before any child bindings re-evaluate, so it can't go stale the way a sibling property binding can. Affects real system state, not just display, so this ships as an immediate follow-up to 0.8.14.

## [0.8.14] - 2026-07-07 (WinUI 3 edition)

### Added — automatic startup repair (recall system)
- The app now checks for the exact damage signatures of recalled tweaks on **every launch** and removes them automatically — no user action, no knowing what "turbo boost" means, no Restore-tab archaeology. Currently repairs: the turbo-boost range clamp (old ix1/ix3) and the hung-app auto-kill pair (old ux1). Repairs are reported on the Dashboard when they happen. Signature-based (reads the actual registry damage), because the applied-tweaks state can't be trusted to remember removed tweaks.

### Fixed — "Intel Speed Shift EPP" was disabling turbo boost machine-wide
- The ix1 tweak (SAFE tier, auto-applied on every Intel machine since the early catalog) was mislabeled and wrong: it wrote `ValueMax=0` onto the machine-wide *definition* of the PERFBOOSTMODE power setting — clamping CPU turbo boost off **across every power plan**, which is why switching plans never fully recovered performance. Field-confirmed on an i7-8750H that stayed ~100 FPS below its known-good baseline until the override was deleted. ix1 and ix3 are removed; the new **"Restore CPU Turbo Boost"** tool (Restore tab) and Full Reset clean up the stale overrides for anyone who ever applied them; a new blocklist test bans this class of write permanently.

### Fixed — "Faster Shutdown & App Kill" made Windows restart Explorer mid-session
- `HungAppTimeout=2000` + `AutoEndTasks=1` (SAFE tier) told Windows to auto-kill any app unresponsive for 2 seconds — including Explorer during routine stalls (thumbnails, network folders, slow disks), which showed up in the field as "Explorer randomly restarts itself." The tweak (now "Faster Shutdown") keeps only the shutdown-scoped timeouts it always advertised, re-applying it or Full Reset removes the old values, and AutoEndTasks writes are now blocklist-banned.

### Fixed — a real, field-confirmed laptop regression
- **"Max performance" power tweaks were hurting laptops.** `CPU 100% Min/Max` (PROCTHROTTLEMIN=100), `Perf Boost Mode` (Aggressive), `NVIDIA Max Performance`, and `AMD GPU Deep Sleep Off` all remove the hardware's thermal recovery window. Fine with desktop cooling; on a thermally-limited laptop the chip heat-soaks and throttles *harder* — confirmed on an i7-8750H Lenovo Legion at 97°C sustained: 350→140 FPS in Minecraft, with ~100 FPS recovered just by returning to the Balanced plan. All four are now desktop-only; the Ultimate Performance tweak's description now tells laptop users to test instead of assume.

### Added — measure, don't guess
- **Tweak Validator** (Benchmarks page): 5 × 45-second sustained all-core stress runs (long enough to heat-soak — short burst benchmarks hide exactly this failure mode), median + noise-band statistics across the runs, and an A/B verdict against a saved baseline: IMPROVED / REGRESSED / WITHIN NOISE. Includes an independent **thermal-throttle detector** (last-10s vs first-10s throughput per run) that flags tweaks causing sag even when the median looks better. Baseline survives reboots.
- **Apply-failure logging**: any failed tweak now writes a full log (system info + per-tweak result + error output) to `%LocalAppData%\VOIDTUNE\logs\`, and a popup lists exactly what failed with two actions — open a pre-filled GitHub issue (in your own browser, nothing auto-uploaded) or open the log folder.

### Added — power, done right this time
- **VOIDTUNE Power Plan** (new tweak, hardware-aware): desktop profile = Ultimate Performance base + Aggressive boost + EPP 0 + parking off + USB selective suspend off + PCIe ASPM off; laptop profile = **Balanced base** (encoding the field result above) + only the latency settings that don't generate heat, AC-only so battery behavior is untouched. Deliberately excludes C-state/idle disable (IDLEDISABLE) — community-measured net-negative (~30 W idle, worse frame pacing). Locale-safe plan detection; revert deletes the plan and returns to Balanced. Apply/re-apply(dedupe)/revert cycle live-tested.
- **Game-Time Power Plan** (new tweak): activates the VOIDTUNE plan only while a fullscreen game is running and restores your previous plan when it closes — the Bitsum-recommended pattern instead of aggressive-24/7.

### Added — self-healing fallbacks
- When a service tweak's `sc config` is blocked (SCM quirks, policy, permission edge cases — the source of real "N tweaks failed" reports from other machines), the engine now automatically retries with the equivalent registry `Start` value write, which converges to the same state at reboot. Works on **both apply and revert** — a silently-failed revert is worse than a failed apply. Fully unit-tested against every service command in the shipped catalog.

### Added — guardrails and advice
- **Conflict detection test**: the suite now parses every registry write in the catalog and fails the build if two tweaks write different data to the same value (last-applied silently wins and one toggle lies).
- **Hardware advisor** (Dashboard): detects RAM running below its rated speed — XMP/EXPO off in the BIOS is worth more FPS than any registry tweak, and nothing on the software side can fix it silently, so VOIDTUNE now tells you.
- **Game-Time Power Plan** and self-healing fallbacks round out the "measure, don't guess" direction started with the Tweak Validator.

## [0.8.13] - 2026-07-06 (WinUI 3 edition)

### Added — automated test suite (`VOIDTUNE.WinUI.Tests`)
- **Catalog integrity tests** — every tweak must have a unique ID, all required fields populated, and a real RevertCmd unless it's an explicitly allow-listed one-shot/restore action. Directly caught 4 tweaks that were byte-identical duplicates under different IDs (`p6`==`deb3`, `p2`==`deb2`, `stor30`==`pow5`, `lat4`==`stor5`) — all four removed.
- **Dangerous-command blocklist** — a static check against every tweak's Apply/Revert/Fallback command for the exact class of mistake that caused the 0.8.10 regression (`powercfg -restoredefaultschemes`, bare `netsh int tcp/ip reset`) plus structurally similar footguns (`reg delete` without `/v`, `diskpart`, `vssadmin delete shadows`, `bcdedit`, bare `format`, embedded `shutdown /r`). A new tweak matching any of these fails the build instead of shipping.
- **Apply/Revert round-trip tests** — actually run every toggleable tweak's Apply then Revert command against a real Windows machine. Gated behind `VOIDTUNE_DESTRUCTIVE_TESTS=1` so it only executes in a disposable environment (this repo's CI); a no-op everywhere else.
- **GitHub Actions CI** (`.github/workflows/ci.yml`) — every push/PR now builds the app, runs the full test suite (including the real Apply/Revert pass, safe on a throwaway CI VM), and validates that the release packaging pipeline (publish → portable ZIP → MSI) still builds end-to-end.

### Added — 4 new tweaks, researched against AtlasOS/ReviOS/Microsoft docs and cross-checked for real mechanism + no known regressions before inclusion
- **De-prioritize Background Processes (IFEO)** — lowers CPU/I-O scheduling priority for SearchIndexer, ctfmon, fontdrvhost and sihost via Image File Execution Options (the same documented mechanism Windows itself uses for foreground-app boosting).
- **Disable Automatic Maintenance Wake** — stops nightly Automatic Maintenance from waking the PC from sleep; maintenance still runs when already awake.
- **Disable Fault Tolerant Heap** — turns off FTH's per-app memory-allocation shims, a documented (Microsoft's own docs) measurable slowdown for apps with many small allocations; several game/render-software vendors independently recommend disabling it.
- **Disable Explorer Auto Folder-Type Discovery** — stops Explorer from probing every folder's contents to guess its view template, removing disk churn and open-lag on large/networked folders.

### Added — 4 new EXTREME tweaks (opt-in; each disables a real, sometimes-needed feature)
- Disable Bluetooth Stack, Disable Print Spooler, Disable Sensor & Biometric Services, Disable Remote Desktop Services.

### Rejected during research (documented for posterity — not added, and why)
- **LowLevelHooksTimeout reduction** — looked promising secondhand, but verification showed the "optimized" value (1000ms) has been the Windows default since 10 version 1709 (placebo on any supported system), and it's specifically implicated in documented full-screen-game crash/flicker reports from another optimizer's users.
- **DisablePagingExecutive / DisablePageCombining, Memory Compression disable** — all three trade *more RAM* for lower latency, directly opposite our low-RAM priority; same reasoning as the C-state/Force-All-Cores purge in 0.8.10.

## [0.8.12] - 2026-07-06 (WinUI 3 edition)

### Added
- **Code signing** — release builds (`installer/build.ps1`) now Authenticode-sign `VOIDTUNE.exe` and the MSI as part of the build, so Explorer's Digital Signatures tab shows a real publisher instead of nothing.
- **Custom app icon** — the WinUI app, MSI, and Add/Remove Programs entry now embed the real VOIDTUNE brand icon (`Assets/voidtune.ico`, matching the website favicon) instead of the default WinUI icon.

## [0.8.11] - 2026-07-05 (WinUI 3 edition)

### Fixed — a real regression from 0.8.10's own Restore tools
- **"Full Reset to Windows Defaults" was deleting the Ultimate Performance power plan.** It called `powercfg -restoredefaultschemes`, which doesn't just reset values — it wipes any unlocked custom scheme and force-switches to Balanced. This silently downgraded real systems to Balanced power (worse *and* less consistent CPU benchmark results) and, separately, its `netsh int tcp reset` / `netsh int ip reset` fully reinitialized the network stack including the Winsock LSP catalog — capable of crashing any process holding a live socket or custom network filter (VPNs, anti-cheat, cloud sync, voice chat, launchers), which is why some systems lost several running processes after using it. Both replaced with safe, targeted settings-only resets.
- **"Ultimate Performance" tweak had a locale bug** — it detected an already-unlocked plan by matching the literal English string `"Ultimate Performance"` in `powercfg -list` output, which never matches on non-English Windows. Every apply on a non-English system silently created a brand-new duplicate power scheme instead of reusing the existing one. Now detects it by GUID, independent of display language.
- Removed **"Disable Touch Keyboard Svc"** — the underlying service is Manual/on-demand by default on virtually every desktop install, so disabling it gave no real benefit while fully breaking the Win+. emoji/symbol picker on some systems.
- Fixed a process-affinity inheritance chain where a stale restricted core mask (a leftover artifact from an earlier build) could propagate from a parent process down into every child process launched from it, including VOIDTUNE itself, silently limiting it to a subset of CPU cores.

### Added
- **Apply preview dialog** — clicking Apply SAFE or the new **Apply EXTREME** button now shows exactly which tweaks are about to run, each with its own checkbox (all checked by default) and description. Nothing runs until you review and confirm; uncheck anything you don't want.
- **Apply EXTREME button** (Tweaks page) — same one-click flow as SAFE, for the opt-in tier, with an extra warning in the preview.
- **Single EXTREME-tweak confirmation** — toggling on an individual EXTREME tweak now asks for confirmation first.
- **7 more real process-reducers**, mirroring services that were only manually togglable on the Services page into the one-click SAFE flow: Fax, Offline Maps, Media Player Sharing, Remote Registry, Internet Connection Sharing, Retail Demo Mode, and the Xbox background services stack (Auth Manager, Game Save, Networking, Accessory) — none of which affect Steam/Epic/Riot/FiveM gaming.

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
