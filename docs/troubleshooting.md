# Troubleshooting

## Tweak apply/revert failures

VOIDTUNE shows a popup naming exactly which tweak failed and why, and writes a full log to `%LocalAppData%\VOIDTUNE\logs\`. The popup can open a pre-filled GitHub issue directly.

## Reverting everything

Restore page -> "Full Reset to Windows Defaults" resets power, timers, GPU scheduling, memory, and network settings to stock, including tweaks no longer in the current catalog. Registry backups taken before each apply can also be restored individually from the Backup & Restore page.

## Unstable after applying tweaks

Create a Windows System Restore point before applying tweaks (Dashboard quick action, or the Backup & Restore page). If something goes wrong, restore through Windows' own System Restore.

## App won't start

Confirm Windows 10 build 19041+ or Windows 11, x64. The portable ZIP must be extracted with all files kept together — don't move `VOIDTUNE.exe` out on its own.
