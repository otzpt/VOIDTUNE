# Building

## Requirements

- .NET 8 SDK
- Windows 10 (19041+) or Windows 11
- WiX v5 (`dotnet tool install --global wix --version 5.0.2`), for installer builds only

## Build and run

```powershell
dotnet build VOIDTUNE.WinUI/VOIDTUNE.WinUI.csproj -c Debug -r win-x64
```

Output: `VOIDTUNE.WinUI/bin/Debug/net8.0-windows10.0.19041.0/win-x64/VOIDTUNE.exe` (requires admin to run).

Built unpackaged (`WindowsPackageType=None`), so it runs without MSIX. `EnableMsixTooling=true` lets it build from the CLI alone — no Visual Studio required.

## Tests

```powershell
dotnet test VOIDTUNE.WinUI.Tests/VOIDTUNE.WinUI.Tests.csproj -c Release
```

Covers catalog-integrity checks, a dangerous-command blocklist, and real Apply/Revert round-trip tests against the live system. The round-trip tests are destructive by design (`VOIDTUNE_DESTRUCTIVE_TESTS=1`) — only run them on a disposable machine or VM, never a dev box. CI runs them on a throwaway GitHub-hosted runner.

## Release artifacts

```powershell
./VOIDTUNE.WinUI/installer/build.ps1 -Version <version>
```

Produces three artifacts in `VOIDTUNE.WinUI/installer/out/`:

- `VOIDTUNE-portable-win-x64.zip`
- `VOIDTUNE-standalone-win-x64.exe`
- `VOIDTUNE-Setup.msi` — WiX v5, per-machine install

All three are self-contained .NET publishes. Code signing runs automatically if `codesign.pfx` / `codesign.pwd.txt` are present locally; both are gitignored, so CI produces unsigned artifacts.

## CI

`.github/workflows/ci.yml` builds, runs the full test suite, and validates release packaging on every push and pull request. `release.yml` builds and publishes all three artifacts to a GitHub release on any `v*` tag push.
