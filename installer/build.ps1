# VOIDTUNE MSI build script (WiX v3)
# Self-contained: downloads WiX if missing, stages payload, harvests, builds the MSI.
# Usage:  powershell -ExecutionPolicy Bypass -File installer\build.ps1
[CmdletBinding()]
param(
    [string]$Version = "0.8.0.0"
)
$ErrorActionPreference = 'Stop'

$proj  = Split-Path -Parent $MyInvocation.MyCommand.Path   # ...\installer
$root  = Split-Path -Parent $proj                          # project root
$build = Join-Path $proj '.build'
$stage = Join-Path $build 'stage'
$wix   = Join-Path $proj '.wix'

Write-Host "VOIDTUNE MSI builder" -ForegroundColor Magenta
Write-Host "  root  : $root"

# ── 1. Ensure WiX v3 binaries ─────────────────────────────────────────────────
if (-not (Test-Path (Join-Path $wix 'candle.exe'))) {
    Write-Host "Downloading WiX v3.14 binaries..." -ForegroundColor Cyan
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $zip = Join-Path $env:TEMP 'wix314-binaries.zip'
    Invoke-WebRequest -Uri 'https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314-binaries.zip' -OutFile $zip -UseBasicParsing
    if (Test-Path $wix) { Remove-Item $wix -Recurse -Force }
    Expand-Archive -Path $zip -DestinationPath $wix -Force
}
$heat   = Join-Path $wix 'heat.exe'
$candle = Join-Path $wix 'candle.exe'
$light  = Join-Path $wix 'light.exe'

# ── 2. Stage payload (exclude logs/backups/installer build artefacts) ─────────
if (Test-Path $build) { Remove-Item $build -Recurse -Force }
New-Item -ItemType Directory $stage | Out-Null
robocopy $root $stage /E /XD logs backups installer .git /XF *.log *.bak *.msi *.wixobj *.wixpdb | Out-Null
if (-not (Test-Path (Join-Path $stage 'VOIDTUNE.exe'))) {
    throw "VOIDTUNE.exe not found in staging - compile the exe first."
}
Write-Host ("  staged: " + (Get-ChildItem $stage -Recurse -File).Count + " files") -ForegroundColor Green

# ── 3. Harvest payload into Files.wxs ─────────────────────────────────────────
$filesWxs = Join-Path $build 'Files.wxs'
& $heat dir $stage -nologo -gg -sfrag -srd -sreg -scom -ke `
    -dr INSTALLFOLDER -cg VtFiles -var var.StageDir -out $filesWxs
if ($LASTEXITCODE -ne 0) { throw "heat failed ($LASTEXITCODE)" }

# ── 4. Compile + link ─────────────────────────────────────────────────────────
$mainWxs = Join-Path $proj 'VOIDTUNE.wxs'
& $candle -nologo -arch x64 -dStageDir="$stage" -dProjDir="$proj" `
    -ext WixUIExtension -ext WixUtilExtension -out "$build\" $mainWxs $filesWxs
if ($LASTEXITCODE -ne 0) { throw "candle failed ($LASTEXITCODE)" }

$msi = Join-Path $proj ("VOIDTUNE-" + ($Version -replace '\.\d+$','') + "-Setup.msi")
& $light -nologo -ext WixUIExtension -ext WixUtilExtension -cultures:en-us -sice:ICE61 `
    -out $msi "$build\VOIDTUNE.wixobj" "$build\Files.wixobj"
if ($LASTEXITCODE -ne 0) { throw "light failed ($LASTEXITCODE)" }

# ── 5. Cleanup intermediate ───────────────────────────────────────────────────
Remove-Item (Join-Path $proj 'VOIDTUNE-*-Setup.wixpdb') -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "MSI built: $msi" -ForegroundColor Green
Write-Host ("Size: {0:N0} KB" -f ((Get-Item $msi).Length / 1KB))
