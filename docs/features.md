# Features

Reference for every page in VOIDTUNE. For the tweak catalog specifically, see [tweaks.md](tweaks.md).

## Dashboard

Live CPU/RAM usage, a health score based on actual system pressure (not tweak count), hardware summary, uptime. Quick actions: Apply SAFE, revert all, create a restore point, clean temp files and flush DNS.

## Tweaks

The full catalog, filterable by category and tier. Apply SAFE / Apply EXTREME preview every tweak about to run, each individually deselectable, before anything executes. NUCLEAR tweaks require an explicit confirmation to reveal. See [tweaks.md](tweaks.md).

## Services

Enable, disable, start, and stop Windows services. Gaming and Normal profiles apply curated sets in one action. Shows the actual service start type, not just current run state.

## Startup

Enable or disable startup entries from the HKCU/HKLM Run keys and the Startup folders. Disabled entries are stashed so they can be restored later.

## Privacy

One-click privacy hardening (telemetry, ads, activity feed, Cortana, Windows Update behavior) and revert.

## Personalize

Registry-backed toggles for theme, transparency, rounded corners, taskbar layout, accent color, and animations, read live from the registry. Changes apply immediately, with an Explorer restart where needed.

## Drivers

Installed signed drivers via CIM: version, date, manufacturer, category. Filterable, exportable to CSV.

## GPU Health

GPU name, driver version, VRAM. Live temperature, clocks, fan speed, and power draw via `nvidia-smi` on NVIDIA systems. AMD/Intel live telemetry is not implemented yet.

## Latency

Six-part latency report — timer resolution, DPC heuristic, disk I/O, memory, network ping, DNS resolution — scored out of 100 with remediation hints. Results can be copied or saved.

## Benchmarks

In-app CPU (single/multi-thread), RAM bandwidth, and disk sequential read/write benchmarks, with history.

## Apps

Install from a 64-app winget catalog: browsers, dev tools, game launchers, hardware monitors, media, security tools. Detects already-installed apps.

## Backup & Restore

Registry snapshot taken before every tweak apply. Manual backup, Windows restore point creation, and restore from any saved snapshot.

## Script Runner

Run PowerShell or cmd commands directly with the app's elevated privileges.

## DevTools

Work in progress, hidden behind a Developer Mode toggle in Settings. Process monitor, registry diff, network toolkit, console presets, core-affinity pinner, and a Tweak Lab (visual builder plus share codes).

## Settings

Developer Mode toggle, update checks, app preferences.
