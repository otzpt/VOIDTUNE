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

New-Item -ItemType Directory -Force $out | Out-Null

Write-Host "==> Publishing self-contained win-x64..."
dotnet publish $proj -c Release -r win-x64 --self-contained

Write-Host "==> Creating portable ZIP..."
$zip = Join-Path $out "VOIDTUNE-$Version-portable-win-x64.zip"
Remove-Item $zip -ErrorAction SilentlyContinue
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($pub, $zip, 'Optimal', $false)

Write-Host "==> Building MSI..."
$msi = Join-Path $out "VOIDTUNE-$Version-Setup.msi"
Remove-Item $msi -ErrorAction SilentlyContinue
& $wix build (Join-Path $PSScriptRoot "Package.wxs") -arch x64 -d "PublishDir=$pub" -o $msi

Write-Host ""
Write-Host "Done:"
Write-Host "  $zip"
Write-Host "  $msi"
