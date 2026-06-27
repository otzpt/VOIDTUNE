# VOIDTUNE MSI Installer

Builds a proper Windows Installer (`.msi`) for VOIDTUNE Performance Suite.

## Output

`VOIDTUNE-0.8-Setup.msi` — a single self-contained installer (embedded CAB) that:

- Installs to `C:\Program Files\VOIDTUNE` (per-machine, 64-bit)
- Creates **Start Menu** and **Desktop** shortcuts
- Registers in **Add/Remove Programs** (publisher, version, icon, uninstall)
- Shows a wizard UI (welcome → license → choose folder → progress)
- Handles **clean upgrades** (old version auto-removed via `UpgradeCode`)

## Build

```powershell
powershell -ExecutionPolicy Bypass -File installer\build.ps1
```

The script is self-contained: it downloads WiX v3.14 binaries to `installer\.wix`
on first run, stages the payload (excluding `logs\`, `backups\`), harvests the
files with `heat`, then compiles + links with `candle` + `light`.

**Rebuild the `.exe` first** (`LAUNCH_VOIDTUNE.bat`-compatible ps2exe build) so the
MSI packages the latest `VOIDTUNE.exe`.

## Files

| File | Purpose |
|------|---------|
| `VOIDTUNE.wxs` | WiX authoring (product, directories, shortcuts, ARP) |
| `License.rtf`  | License/disclaimer shown in the wizard |
| `build.ps1`    | Self-contained build script |

## Notes

- `UpgradeCode` (`35F9C58F-...`) must stay constant across versions for upgrades to work.
  Bump the `Version` in `VOIDTUNE.wxs` for each release.
- Uninstall is **fully clean**: a `util:RemoveFolderEx` action recursively deletes the
  entire install folder — including runtime-generated `logs\` and `backups\` — plus the
  shortcuts, the `HKLM\Software\OTZPT\VOIDTUNE` key and the ARP entry. Nothing is left behind.
- `.wix\` and `.build\` are regenerable and can be deleted at any time.
