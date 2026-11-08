# Roadmap

Planned direction for VOIDTUNE. Items here are committed to in intent, not in
schedule — each one lists what still blocks it, so a reader can tell the
difference between "next" and "wanted, but gated on something unsolved."

Shipped work lives in [CHANGELOG.md](CHANGELOG.md).


## VOIDTUNE for Linux

A Linux counterpart. Not a port — the tweak catalog is Windows-specific and does
not translate — but the same premise applied to a different platform: reversible
changes, a backup before every apply, and diagnostics that explain what the system
is actually doing.

What the Victus investigation showed is that the useful surface on Linux is
different from Windows. The fan and power problem above was diagnosed entirely
through `sysfs` — `hwmon` for fans and temperatures, `powercap`/RAPL for package
energy, `amd-pstate` for frequency policy, `hp-wmi` for vendor platform controls —
and corrected with a single write to `pwm1_enable`. No registry, no services, no
driver. The equivalent product is closer to a diagnostic and hardware-control tool
than to a debloater.

**Open questions:**

- **Distribution.** Void uses runit and xbps; most users are on systemd and
  apt/dnf/pacman. Anything touching services or boot-time state is per-distro work.
  A tool restricted to `sysfs` and `hwmon` avoids nearly all of it.
- **Language.** VOIDTUNE is C#/.NET and .NET runs on Linux, so sharing code is
  possible. Whether that is better than a native tool is undecided, and the WinUI 3
  layer does not carry over regardless.
- **Prior art.** [ZenTune](https://github.com/HorizonUnix/UXTU4Linux) (formerly
  UXTU4Linux, GPL-3.0, Python over RyzenAdj) already covers AMD power tuning on
  Linux and macOS and is actively maintained. If the goal is only power limits,
  that ground is taken. The gap it does not cover is the rest of what VOIDTUNE
  does: vendor platform controls, fan policy, diagnostics, and reversibility.

Scoping this one is the next step, not writing it.
