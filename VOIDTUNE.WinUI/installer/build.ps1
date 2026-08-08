# Builds the VOIDTUNE WinUI release artifacts: a portable ZIP and an MSI installer.
#
# Output filenames are DELIBERATELY version-free (VOIDTUNE-Setup.msi /
# VOIDTUNE-portable-win-x64.zip, not VOIDTUNE-0.8.x-...) — the website links directly to
# https://github.com/otzpt/VOIDTUNE/releases/latest/download/<name>, which GitHub always
# redirects to whatever the latest release's matching-named asset is. A stable filename
# means that link never needs updating; -Version only flows into the package metadata
# (Package.wxs Version attribute, HKLM registry Version value) where it's actually needed.
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

param([string]$Version = "0.8.20")
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
# $ErrorActionPreference = "Stop" only catches PowerShell-native errors -- an
# external exe's non-zero exit code is not one of them, so without this check
# a failed publish would fall straight through, signing/zipping/MSI-packaging
# whatever stale build already happened to be sitting in $pub, then printing
# "Done:" as if it had worked. The `wix build` call below already gets this
# right; this is the same guard for the step just as capable of failing.
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)" }

if ($canSign) {
    Write-Host "==> Signing VOIDTUNE.exe..."
    Sign-File (Join-Path $pub "VOIDTUNE.exe")
} else {
    Write-Host "==> Skipping signing (no local cert/signtool found)."
}

Write-Host "==> Creating portable ZIP..."
$zip = Join-Path $out "VOIDTUNE-portable-win-x64.zip"
Remove-Item $zip -ErrorAction SilentlyContinue
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($pub, $zip, 'Optimal', $false)

# Single-file EXE: a separate publish, not a repackaging of $pub above -- Microsoft's
# own docs (learn.microsoft.com/.../unpackage-winui-app#single-file-exe) require
# SelfContained/PublishSingleFile/IncludeAllContentForSelfExtract together, and
# baking those into the .csproj's main PropertyGroup would make every publish
# single-file, breaking the portable ZIP above (which wants the plain multi-file
# output). WindowsPackageType=None and EnableMsixTooling=true are already set
# project-wide, which the build-time validation target requires or else errors.
#
# This is NOT a zero-extraction binary -- Windows App SDK dependencies extract to
# a temp directory on first launch, same as .NET's own single-file apps generally.
# One file to distribute, not one file with nothing else ever touching disk.
Write-Host "==> Publishing single-file win-x64 exe..."
$singleFilePub = Join-Path $projDir "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish-singlefile"
dotnet publish $proj -c Release -r win-x64 --self-contained `
    -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true `
    -o $singleFilePub
if ($LASTEXITCODE -ne 0) { throw "single-file publish failed ($LASTEXITCODE)" }

$exe = Join-Path $out "VOIDTUNE-standalone-win-x64.exe"
Remove-Item $exe -ErrorAction SilentlyContinue
Copy-Item (Join-Path $singleFilePub "VOIDTUNE.exe") $exe

if ($canSign) {
    Write-Host "==> Signing standalone exe..."
    Sign-File $exe
}

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
$msi = Join-Path $out "VOIDTUNE-Setup.msi"
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
Write-Host "  $exe"
Write-Host "  $msi"
