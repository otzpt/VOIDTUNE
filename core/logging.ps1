# VOIDTUNE - core/logging.ps1
# Copyright (C) 2026 @OTZPT - GPL v3

$script:LOGFILE = Join-Path $script:LOGS "voidtune_$(Get-Date -f yyyyMMdd).log"

function WL($m) {
    try { "[$((Get-Date).ToString('HH:mm:ss'))] $m" | Add-Content $script:LOGFILE -EA SilentlyContinue } catch {}
}
