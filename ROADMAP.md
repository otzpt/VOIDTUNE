# Roadmap

Planned direction for VOIDTUNE. Items here are committed to in intent, not in
schedule — each one lists what still blocks it, so a reader can tell the
difference between "next" and "wanted, but gated on something unsolved."

Shipped work lives in [CHANGELOG.md](CHANGELOG.md).

## CPU/SoC power tuning

Direct control of processor power limits — PPT, STAPM, TDC, EDC on AMD; the
equivalent MSR-backed limits on Intel — instead of only the registry and power-plan
tweaks VOIDTUNE ships today.

The motivating case: on an HP Victus 16 (Ryzen 5 5600H), a failed lid Hall sensor
left the EC holding both fans at ~2200 rpm regardless of temperature. Measured
under a 12-thread load, that cost 8.4 W of sustained package power and 222 MHz of
all-core clock, because the SMU kept trading power away to hold 100 °C. None of
VOIDTUNE's current tweaks can see that, let alone correct it. A platform whose
firmware is quietly wrong is exactly the case a tuning tool should catch.

**Reference implementation:** [Universal x86 Tuning Utility](https://github.com/JamesCJ60/Universal-x86-Tuning-Utility)
(UXTU) does this on Windows, and its AMD path is a frontend over
[RyzenAdj](https://github.com/FlyGoat/RyzenAdj).

**Licensing — resolved.** UXTU is GPL-3.0 and VOIDTUNE is GPL-3.0, so its code can
be incorporated directly: keep the license, preserve copyright headers, note
modifications. RyzenAdj is LGPL-3.0, which flows into a GPL-3.0 project without
friction. Both are C#/.NET and C respectively; UXTU targets .NET 10 against
VOIDTUNE's .NET 8, which is a retarget rather than a port.

**Blocker — kernel access.** The SMU mailbox sits behind PCI configuration space,
unreachable from usermode Windows. UXTU reaches it through WinRing0, which is on
Microsoft's vulnerable driver blocklist (CVE-2020-14979 — unrestricted physical
memory read/write with no ACL), blocked outright under HVCI, and flagged by
anti-cheat and some AV. Inheriting UXTU's code means inheriting that, which is a
poor trade for a tool whose entire premise is being safe to run.

Options, none yet chosen:

- Bundle WinRing0 and accept the blocklist, anti-cheat, and AV consequences.
- Ship a purpose-built signed driver. Needs an EV certificate and Microsoft
  attestation signing, plus the ongoing liability of maintaining kernel code.
- Read-only first: surface current limits, power draw, and thermals through
  existing signed interfaces, show what is wrong, and leave changing it to the
  user. No driver, no risk, and it covers diagnosis — which is most of the value
  when the fault is a firmware misconfiguration rather than a user preference.

The read-only path is the likely first step. It is shippable without resolving
the driver question at all.

**Safety.** Power limits are not a reversible registry key. Values that pass a
stress test can still be unstable days later, and the SMU will accept settings the
board cannot sustain. Whatever ships here needs harder guardrails than the
SAFE/EXTREME tiering — at minimum per-model validated ranges rather than free
numeric entry, and no "apply all" equivalent.

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
