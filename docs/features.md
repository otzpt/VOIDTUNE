# Features

Reference for every page in VOIDTUNE. For the tweak catalog specifically, see [tweaks.md](tweaks.md).

## Dashboard

Live CPU/RAM usage, a health score based on actual system pressure (not tweak count), hardware summary, uptime. Quick actions: Apply SAFE, revert all, create a restore point, clean temp files and flush DNS.

## Tweaks

The full catalog, filterable by category and tier. Apply SAFE / Apply EXTREME preview every tweak about to run, each individually deselectable, before anything executes. See [tweaks.md](tweaks.md).

## Services

Enable, disable, start, and stop Windows services. Gaming and Normal profiles apply curated sets in one action. Shows the actual service start type, not just current run state.

## Startup

Enable or disable startup entries from the HKCU/HKLM Run keys and the Startup folders. Disabled entries are stashed so they can be restored later.

## Personalize

Registry-backed toggles for theme, transparency, rounded corners, taskbar layout, accent color, and animations, read live from the registry. Changes apply immediately, with an Explorer restart where needed.

## Latency

Latency report covering timer resolution, uncached disk reads, memory access and network ping. The score out of 100 takes off points for a coarse timer, slow disk reads, slow memory and poor network results, and lists what it found.

## Benchmarks

In-app CPU (single/multi-thread), RAM bandwidth, and disk sequential read/write benchmarks, plus a five-run sustained stress validator that compares tweaks against a saved baseline.

## Backup & Restore

Registry snapshot of the main tuning keys taken before bulk applies (single toggles skip it). Manual backup, Windows restore point creation, and restore from any saved snapshot.

## DevTools

Work in progress, hidden behind a Developer Mode toggle in Settings. Process monitor, registry diff, network toolkit, apply/revert log, core-affinity pinner, and a Tweak Lab (visual builder plus share codes).

## Settings

Developer Mode toggle, update checks, app preferences.
