# Tweaks

~175 tweaks. Every entry has been individually verified rather than added wholesale — see [../CHANGELOG.md](../CHANGELOG.md) for tweaks that were removed after turning out to be harmful or placebo.

## Tiers

- **SAFE** — default tier. No meaningful downside on any system.
- **EXTREME** — opt-in. Real tradeoffs (e.g. Hardware GPU Scheduling, GPU MSI Mode) that help some systems and hurt others.
- **NUCLEAR** — opt-in, hidden behind an explicit confirmation. Disables security features: Defender real-time protection, SmartScreen, UAC, Core Isolation/HVCI, Firewall. Fully revertible.

Apply SAFE / Apply EXTREME preview every tweak about to run before executing; individual tweaks can be deselected. Toggling a single EXTREME tweak directly still asks for confirmation.

## Categories

CPU, GPU, RAM, Network, Debloat, Power, Latency, Game, Background, Storage, Audio, Processes, Privacy, Restore, Nuclear.

**Processes** is the background-process-reduction set: services, scheduled tasks, per-user service templates, and bloatware removal. Applying it is the main lever for cutting the running process count.

## Hardware gating

Architecture-specific tweaks show only when the relevant hardware is detected: Intel/AMD CPU tweaks, NVIDIA/AMD GPU tweaks, laptop-specific behavior. Example: "max performance" power tweaks are desktop-only, since they make thermally-limited laptops throttle harder rather than run faster. RAM-gated and Windows-version-gated tweaks are hidden when they don't apply.

## Reverting

Every tweak records a revert path. A full registry backup is taken before each apply. "Full Reset to Windows Defaults" (Restore page) resets power, timers, GPU scheduling, memory, and network settings to stock, including tweaks no longer in the current catalog.
