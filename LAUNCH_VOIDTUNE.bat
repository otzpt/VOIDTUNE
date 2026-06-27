@echo off
title VOIDTUNE v0.9
cd /d "%~dp0"

:: ── VERSION CHECK ────────────────────────────────────────────────────────────
for /f %%v in ('powershell -NoProfile -Command "$PSVersionTable.PSVersion.Major"') do set PSVER=%%v
if "%PSVER%"=="" set PSVER=0
if %PSVER% LSS 5 (
    echo [ERROR] PowerShell 5+ required. Current: %PSVER%
    echo Get PowerShell 7: https://github.com/PowerShell/PowerShell/releases
    pause & exit /b 1
)

:: ── CHECK FILES ──────────────────────────────────────────────────────────────
if not exist "%~dp0VOIDTUNE.ps1"     ( echo [ERROR] VOIDTUNE.ps1 missing.  & pause & exit /b 1 )
if not exist "%~dp0VOIDTUNE.xaml"    ( echo [ERROR] VOIDTUNE.xaml missing.  & pause & exit /b 1 )
if not exist "%~dp0core\logging.ps1" ( echo [ERROR] core\ folder missing.   & pause & exit /b 1 )

:: ── UNBLOCK ──────────────────────────────────────────────────────────────────
powershell -NoProfile -Command "Get-ChildItem '%~dp0' -Recurse -Include '*.ps1','*.xaml' | ForEach-Object { Unblock-File $_.FullName -EA SilentlyContinue }"

:: ── LAUNCH (elevation handled by VOIDTUNE.ps1) ───────────────────────────────
echo Launching VOIDTUNE v0.9...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0VOIDTUNE.ps1"

:: ── ERROR HANDLER ────────────────────────────────────────────────────────────
if %errorlevel% neq 0 (
    echo.
    echo [ERROR] VOIDTUNE exited with code %errorlevel%
    echo Check logs\ folder for details.
    echo.
    pause
)
