# Builds the VOIDTUNE WinUI release artifacts: a portable ZIP and an MSI installer.
#
# Prereqs:
#   - .NET 8 SDK
#   - WiX v5:  dotnet tool install --global wix --version 5.0.2
#
# Usage:  pwsh installer/build.ps1 -Version 0.8.1
#
# Keep -Version in sync with:
#   - VOIDTUNE.WinUI.csproj  <Version>
#   - Services/UpdateService.cs  CurrentVersion
#   - installer/Package.wxs  Package Version + registry Version
#   - the GitHub release tag (vX.Y.Z)

param([string]$Version = "0.8.1")
$ErrorActionPreference = "Stop"

$projDir = Split-Path $PSScriptRoot -Parent
$proj    = Join-Path $projDir "VOIDTUNE.WinUI.csproj"
$pub     = Join-Path $projDir "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"
$out     = Join-Path $PSScriptRoot "out"
$wix     = Join-Path $env:USERPROFILE ".dotnet\tools\wix.exe"

# Code signing (optional — no-ops if the local cert/signtool aren't present).
# codesign.pfx / codesign.pwd.txt are machine-local and gitignored; see the
# "sign VOIDTUNE" setup that generated the self-signed cert for CN=otzpt.
$pfx    = Join-Path $PSScriptRoot "codesign.pfx"
$pwdTxt = Join-Path $PSScriptRoot "codesign.pwd.txt"
$signtool = Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
$canSign = (Test-Path $pfx) -and (Test-Path $pwdTxt) -and $signtool

function Sign-File($path) {
    if (-not $canSign) { return }
    $pwd = Get-Content $pwdTxt -Raw
    & $signtool sign /f $pfx /p $pwd /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 $path
    if ($LASTEXITCODE -ne 0) { throw "signtool failed on $path" }
}

New-Item -ItemType Directory -Force $out | Out-Null

Write-Host "==> Publishing self-contained win-x64..."
dotnet publish $proj -c Release -r win-x64 --self-contained

if ($canSign) {
    Write-Host "==> Signing VOIDTUNE.exe..."
    Sign-File (Join-Path $pub "VOIDTUNE.exe")
} else {
    Write-Host "==> Skipping signing (no local cert/signtool found)."
}

Write-Host "==> Creating portable ZIP..."
$zip = Join-Path $out "VOIDTUNE-$Version-portable-win-x64.zip"
Remove-Item $zip -ErrorAction SilentlyContinue
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($pub, $zip, 'Optimal', $false)

Write-Host "==> Ensuring WiX UI/Util extensions are installed..."
# -g (global/per-user cache): without it, extensions are cached per-directory and invisible to
# `wix build` when run from a different CWD than `wix extension add` was.
$installedExts = & $wix extension list -g 2>&1 | Out-String
foreach ($ext in "WixToolset.UI.wixext", "WixToolset.Util.wixext") {
    if ($installedExts -notmatch [regex]::Escape($ext)) {
        & $wix extension add "$ext/5.0.2" -g | Out-Null
    }
}

Write-Host "==> Building MSI..."
$msi = Join-Path $out "VOIDTUNE-$Version-Setup.msi"
Remove-Item $msi -ErrorAction SilentlyContinue
Push-Location $PSScriptRoot
try {
    & $wix build "Package.wxs" -arch x64 -d "PublishDir=$pub" -o $msi `
        -ext WixToolset.UI.wixext -ext WixToolset.Util.wixext
    if ($LASTEXITCODE -ne 0) { throw "wix build failed" }
} finally {
    Pop-Location
}

if ($canSign) {
    Write-Host "==> Signing MSI..."
    Sign-File $msi
}

Write-Host ""
Write-Host "Done:"
Write-Host "  $zip"
Write-Host "  $msi"
