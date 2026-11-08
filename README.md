# VOIDTUNE

Windows optimization and debloating tool for gamers and power users.

VOIDTUNE is a native WinUI 3 application built in C#/.NET 8. It provides reversible system tweaks, diagnostics, service management, and debloating for Windows 10/11.

## Status

0.8.21, actively maintained. Windows 10 (build 19041+) and Windows 11, x64.

## Why VOIDTUNE

VOIDTUNE began as a PowerShell + WPF tool and was rewritten from scratch as a native WinUI 3 application; the original edition has been removed from this repository. Bulk applies take a registry backup of the main tuning keys first, and tweaks are built to revert — the project would rather cut a tweak than ship one nobody's checked. The 0.8.14 turbo-boost regression is the kind of mistake that bar exists to catch: a mislabeled tweak was silently capping CPU boost machine-wide, confirmed on real hardware by a 350→140 FPS drop, and removed the same release.

## Features

- About 150 reversible tweaks across CPU, GPU, RAM, network, power, latency, and background processes, tiered SAFE / EXTREME
- Architecture-specific tweaks gated on detected hardware (Intel/AMD CPU, CPU generation, NVIDIA/AMD GPU, laptop vs desktop)
- Registry backup before bulk applies, individual revert, and one-click reset to Windows defaults
- Service manager with Gaming/Normal profiles
- Process monitor ranked by CPU and memory impact (Developer Mode)
- Hardware diagnostics, latency checker, benchmarks
- Personalization toggles, startup manager
- In-app auto-update

Full list: [docs/features.md](docs/features.md). Tweak catalog: [docs/tweaks.md](docs/tweaks.md).

## Requirements

- Windows 10 (build 19041+) or Windows 11, x64
- Administrator privileges (requested automatically on launch)

Release builds are self-contained — no separate .NET or Windows App SDK install needed.

## Installation

Download from [Releases](https://github.com/otzpt/VOIDTUNE/releases):

- `VOIDTUNE-Setup.msi` — installer, Start Menu/Desktop shortcuts, clean uninstall
- `VOIDTUNE-standalone-win-x64.exe`: single executable, no install
- `VOIDTUNE-portable-win-x64.zip` — extract and run, keep the folder together

Release builds are currently unsigned, so expect a SmartScreen "unknown publisher" warning. The in-app updater checks each download against the SHA-256 digest GitHub publishes for the release. VOIDTUNE has applied to the [SignPath Foundation](https://signpath.io/solutions/open-source-community)'s free code-signing program for open source projects to replace this with a real trusted certificate.

## Usage

Launch VOIDTUNE; it elevates automatically. On the Tweaks page, browse by category or tier and apply individually, or use Apply SAFE / Apply EXTREME to review and confirm a batch. Every applied tweak can be reverted from the same page or from Restore.

## Documentation

- [docs/features.md](docs/features.md) — every page and what it does
- [docs/tweaks.md](docs/tweaks.md) — tiers, categories, hardware gating
- [docs/building.md](docs/building.md) — build from source, run tests, produce release artifacts
- [docs/contributing.md](docs/contributing.md)
- [docs/troubleshooting.md](docs/troubleshooting.md)
- [CHANGELOG.md](CHANGELOG.md)

## Related

[Voidtune-one-click](https://github.com/otzpt/Voidtune-one-click) — a ~170 KB dependency-free C binary that applies a fixed optimization set with no UI. Separate project, no shared code.

## License

GPL-3.0. See [LICENSE](LICENSE).

VOIDTUNE modifies registry entries, services, and system settings. Create a restore point before applying tweaks — see [docs/troubleshooting.md](docs/troubleshooting.md) if something goes wrong.
