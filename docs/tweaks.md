# Tweaks

About 150 tweaks, each kept because it does something specific. Registry values are checked against the Windows binaries of a current build, and 0.8.21 removed the ones Windows no longer reads. ~27 were cut in a single pass (0.8.10) after being traced against real FPS numbers and found to do nothing or hurt — see [../CHANGELOG.md](../CHANGELOG.md) for the rest of what got removed and why.

## Tiers

- **SAFE** — default tier. No meaningful downside on any system.
- **EXTREME** — opt-in. Real tradeoffs (e.g. Hardware GPU Scheduling, GPU MSI Mode) that help some systems and hurt others.

Apply SAFE / Apply EXTREME preview every tweak about to run before executing; individual tweaks can be deselected. Toggling a single EXTREME tweak directly still asks for confirmation.

## Categories

CPU, GPU, RAM, Network, Debloat, Power, Latency, Game, Background, Storage, Audio, Processes, Privacy, Restore.

**Processes** is the background-process-reduction set: services, scheduled tasks, per-user service templates, and bloatware removal. Applying it is the main lever for cutting the running process count.

## Hardware gating

Architecture-specific tweaks show only when the relevant hardware is detected: Intel/AMD CPU tweaks, NVIDIA/AMD GPU tweaks, laptop-specific behavior. Example: "max performance" power tweaks are desktop-only, since they make thermally-limited laptops throttle harder rather than run faster. Windows-version-gated tweaks are hidden when they don't apply, and so are tweaks whose services and scheduled tasks don't exist on this machine. Dual-CCD X3D Ryzen and 13th/14th gen Intel desktop CPUs get the Balanced-based power plan instead of pinned clocks, because of core parking and Vmin shift respectively.

## How success is decided

After a tweak runs, VOIDTUNE reads the live registry values, service start types and scheduled task states it should have changed. That result beats the command's exit code. PowerShell, `powercfg`, `netsh` and `fsutil` steps can't be checked this way and rely on the exit code.

## Reverting

Tweaks define a revert command, except the one-shot actions ram3 (temp cleanup) and stor3 (TRIM), which have nothing to undo. MSI mode and write cache tweaks record the previous value when applied and restore it on revert; removed apps can only return if Windows still has the package. A registry snapshot of the main tuning keys is taken before bulk applies. "Full Reset to Windows Defaults" (Restore page) resets power, timers, GPU scheduling, memory, and network settings to stock, including tweaks no longer in the current catalog.
