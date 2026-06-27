# VOIDTUNE - core/helpers.ps1
# Copyright (C) 2026 @OTZPT - GPL v3

# G - find named element, never throws
function G($n) { try { $script:Win.FindName($n) } catch { $null } }

# UI - run action on UI dispatcher
function UI($a) { try { $script:Win.Dispatcher.Invoke($a) } catch {} }

# AddLog - append to log panel and write to file
function AddLog($msg, $col='#AAAAAA') {
    try {
        UI {
            $lo = G 'LogOut'; $ls = G 'LogScroll'
            if ($lo) { $lo.Text += "$msg`n" }
            if ($ls) { $ls.ScrollToBottom() }
        }
    } catch {}
    WL $msg
}

function SetProg($p)   { try { UI { $b = G 'LogProg';   if ($b) { $b.Value = $p } } } catch {} }
function SetStatus($s) { try { UI { $b = G 'LogStatus'; if ($b) { $b.Text  = $s } } } catch {} }

# ── Apply/Revert progress overlay ─────────────────────────────────────────────
function Show-ApplyOverlay($title) {
    try {
        UI {
            $o = G 'ApplyOverlay'; if (-not $o) { return }
            $t = G 'ApplyPopupTitle'; if ($t) { $t.Text = if ($title) { $title } else { 'WORKING...' } }
            $b = G 'ApplyPopupBar';   if ($b) { $b.Value = 0 }
            $s = G 'ApplyPopupOp';    if ($s) { $s.Text = '...' }
            $c = G 'ApplyPopupCount'; if ($c) { $c.Text = '' }
            $p = G 'ApplyPopupPct';   if ($p) { $p.Text = '0%' }
            $o.Opacity = 0
            $o.Visibility = 'Visible'
            $anim          = New-Object System.Windows.Media.Animation.DoubleAnimation
            $anim.From     = 0.0
            $anim.To       = 1.0
            $anim.Duration = [System.Windows.Duration]([TimeSpan]::FromMilliseconds(180))
            $ease          = New-Object System.Windows.Media.Animation.CubicEase
            $ease.EasingMode = [System.Windows.Media.Animation.EasingMode]::EaseOut
            $anim.EasingFunction = $ease
            $o.BeginAnimation([System.Windows.UIElement]::OpacityProperty, $anim)
        }
    } catch {}
}

function Update-ApplyOverlay($op, $pct, $countText) {
    try {
        UI {
            $b = G 'ApplyPopupBar';   if ($b) { $b.Value = $pct }
            $s = G 'ApplyPopupOp';    if ($s) { $s.Text = $op }
            $c = G 'ApplyPopupCount'; if ($c) { $c.Text = $countText }
            $p = G 'ApplyPopupPct';   if ($p) { $p.Text = "$([int]$pct)%" }
        }
    } catch {}
}

function Hide-ApplyOverlay {
    try {
        UI {
            $o = G 'ApplyOverlay'
            if (-not $o) { return }
            $anim          = New-Object System.Windows.Media.Animation.DoubleAnimation
            $anim.From     = 1.0
            $anim.To       = 0.0
            $anim.Duration = [System.Windows.Duration]([TimeSpan]::FromMilliseconds(200))
            $anim.add_Completed([EventHandler]{
                $o2 = $script:Win.FindName('ApplyOverlay')
                if ($o2) { $o2.Visibility = 'Collapsed' }
            })
            $o.BeginAnimation([System.Windows.UIElement]::OpacityProperty, $anim)
        }
    } catch {}
}

# Page fade-in — call after making a page Visible
function Animate-PageIn($el) {
    try {
        if (-not $el) { return }
        $anim          = New-Object System.Windows.Media.Animation.DoubleAnimation
        $anim.From     = 0.0
        $anim.To       = 1.0
        $anim.Duration = [System.Windows.Duration]::new([TimeSpan]::FromMilliseconds(160))
        $ease          = New-Object System.Windows.Media.Animation.CubicEase
        $ease.EasingMode = [System.Windows.Media.Animation.EasingMode]::EaseOut
        $anim.EasingFunction = $ease
        $el.Opacity = 0
        $el.BeginAnimation([System.Windows.UIElement]::OpacityProperty, $anim)
    } catch {}
}

# ── RunC ──────────────────────────────────────────────────────────────────────
# Uses ProcessStartInfo so the full string (including &&) is passed to cmd.exe
# intact. PowerShell's & operator tokenises && and breaks chained commands.
function RunC($cmd) {
    try {
        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName               = 'cmd.exe'
        $psi.Arguments              = "/c $cmd"
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError  = $true
        $psi.UseShellExecute        = $false
        $psi.CreateNoWindow         = $true
        $p   = [System.Diagnostics.Process]::Start($psi)
        $out = $p.StandardOutput.ReadToEnd() + $p.StandardError.ReadToEnd()
        $p.WaitForExit()
        return @{ OK = ($p.ExitCode -eq 0); Out = $out.Trim() }
    } catch { return @{ OK = $false; Out = $_.Exception.Message } }
}

# ── RunPS ─────────────────────────────────────────────────────────────────────
# Run a PowerShell scriptblock natively in this session.
# Use for cmdlets that don't exist in cmd (Disable-MMAgent, etc.)
function RunPS([scriptblock]$block) {
    try {
        $out = & $block 2>&1
        return @{ OK = $true; Out = ($out -join "`n") }
    } catch { return @{ OK = $false; Out = $_.Exception.Message } }
}

# ── Exec-Cmd ──────────────────────────────────────────────────────────────────
# Dispatcher: commands prefixed with "PS:" go to RunPS, everything else to RunC
function Exec-Cmd($cmd) {
    if ([string]::IsNullOrEmpty($cmd)) { return @{ OK = $true; Out = 'no-op' } }
    if ($cmd.StartsWith('PS:')) { return RunPS ([scriptblock]::Create($cmd.Substring(3).Trim())) }
    return RunC $cmd
}

# ── MakeBackup ────────────────────────────────────────────────────────────────
# Exports registry keys to a .reg file (one header, no duplicates).
# Also writes a _svc.txt with service startup states and a _meta.txt with
# applied tweak count + active power plan — used by Restore-BackupFull.
function MakeBackup($note) {
    try {
        $ts       = Get-Date -Format 'yyyyMMdd_HHmmss'
        $file     = Join-Path $script:BACKUPS "backup_${ts}_${note}.reg"
        $svcFile  = Join-Path $script:BACKUPS "backup_${ts}_${note}_svc.txt"
        $metaFile = Join-Path $script:BACKUPS "backup_${ts}_${note}_meta.txt"

        $keys = @(
            # CPU / Kernel / Memory
            'HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl',
            'HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel',
            'HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management',
            'HKLM\SYSTEM\CurrentControlSet\Control\Processor',
            # GPU / Graphics / DWM
            'HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers',
            'HKLM\SOFTWARE\Microsoft\Windows\Dwm',
            'HKCU\Software\Microsoft\Windows\DWM',
            # Network
            'HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters',
            'HKLM\SYSTEM\CurrentControlSet\Services\AFD\Parameters',
            'HKLM\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters',
            'HKLM\SOFTWARE\Policies\Microsoft\Windows\Psched',
            'HKLM\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization',
            # MMCSS / Multimedia (audio + games — no duplicate)
            'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile',
            'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games',
            'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio',
            # Power
            'HKLM\SOFTWARE\Policies\Microsoft\Power\PowerThrottling',
            'HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power',
            # Privacy / Telemetry
            'HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search',
            'HKLM\SOFTWARE\Policies\Microsoft\Dsh',
            # User prefs / Explorer
            'HKCU\System\GameConfigStore',
            'HKCU\Software\Microsoft\GameBar',
            'HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager',
            'HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects',
            'HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced',
            'HKCU\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications',
            'HKCU\Software\Microsoft\Windows\CurrentVersion\Audio',
            'HKCU\Control Panel\Desktop',
            'HKCU\Control Panel\Mouse',
            'HKCU\Control Panel\Accessibility\StickyKeys',
            'HKCU\Control Panel\Accessibility\ToggleKeys',
            # Audio
            'HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Audio',
            'HKLM\SYSTEM\CurrentControlSet\Services\Audiosrv'
        )

        # .reg file — single header, then each key's data
        'Windows Registry Editor Version 5.00' | Set-Content $file -Encoding Unicode
        '' | Add-Content $file -Encoding Unicode

        $exported = 0
        $i = 0
        foreach ($k in $keys) {
            $i++
            $tmp = "$file.$i.tmp"
            $null = reg export $k $tmp /y 2>$null
            if (Test-Path $tmp) {
                $lines = Get-Content $tmp -Encoding Unicode -EA SilentlyContinue
                if ($lines -and $lines.Count -gt 2) {
                    $lines | Select-Object -Skip 2 | Add-Content $file -Encoding Unicode
                    '' | Add-Content $file -Encoding Unicode
                    $exported++
                }
                Remove-Item $tmp -Force -EA SilentlyContinue
            }
        }

        # Service startup states — lets Restore-BackupFull reset them precisely
        $svcNames = @(
            'DiagTrack','dmwappushservice','SysMain','WSearch','Spooler',
            'XboxGip','XblAuthManager','XblGameSave','XboxNetApiSvc',
            'MapsBroker','RetailDemo','WMPNetworkSvc','TabletInputService',
            'Fax','DoSvc','BITS','WbioSrvc','SensorService','SensrSvc',
            'RemoteRegistry','bthserv','W32Time','wuauserv','Audiosrv',
            'NvTelemetryContainer'
        )
        $svcLines = foreach ($n in $svcNames) {
            $s = Get-Service $n -EA SilentlyContinue
            if ($s) { "$($s.Name)=$($s.StartType)" }
        }
        $svcLines | Set-Content $svcFile -Encoding UTF8

        # Active power plan GUID
        $planOut  = powercfg -getactivescheme 2>$null
        $planGuid = if ($planOut -match 'GUID:\s+([0-9a-f-]{36})') { $Matches[1] } else { '' }

        # Metadata — applied tweaks list + power plan
        $appliedIds = @(
            $script:TWEAKS + $script:PRIVTWEAKS + $script:ARCH_TWEAKS |
            Where-Object { $_.Applied } |
            ForEach-Object { "$($_.Id):$($_.Name)" }
        )
        @(
            "Timestamp=$ts"
            "Note=$note"
            "PowerPlan=$planGuid"
            "RegKeysExported=$exported"
            "AppliedTweaks=$($appliedIds.Count)"
            '---'
        ) + $appliedIds | Set-Content $metaFile -Encoding UTF8

        return (Split-Path $file -Leaf)
    } catch { return 'backup-failed' }
}

# ── Restore-BackupFull ────────────────────────────────────────────────────────
# Full restore: validates the .reg file, imports registry, restores service
# startup states from companion _svc.txt, restores power plan from _meta.txt,
# and clears VOIDTUNE's applied tweak tracking.
function Restore-BackupFull($regFileName) {
    try {
        $regFile  = Join-Path $script:BACKUPS $regFileName
        $base     = [System.IO.Path]::GetFileNameWithoutExtension($regFileName)
        $svcFile  = Join-Path $script:BACKUPS "${base}_svc.txt"
        $metaFile = Join-Path $script:BACKUPS "${base}_meta.txt"

        # ── 1. Validate ────────────────────────────────────────────────────────
        if (-not (Test-Path $regFile)) {
            AddLog "Backup file not found: $regFileName" '#E01818'
            return $false
        }
        $header = Get-Content $regFile -TotalCount 1 -Encoding Unicode -EA SilentlyContinue
        if ($header -notlike '*Windows Registry Editor*') {
            AddLog "Backup appears corrupted (bad header): $regFileName" '#E01818'
            return $false
        }

        # ── 2. Import registry ─────────────────────────────────────────────────
        AddLog "Importing registry: $regFileName" '#F59E0B'
        $r = RunC "reg import `"$regFile`""
        if (-not $r.OK) {
            AddLog "  Registry import FAILED: $($r.Out.Split("`n")[0])" '#E01818'
            return $false
        }
        AddLog "  Registry OK." '#22C55E'

        # ── 3. Restore service startup states ──────────────────────────────────
        if (Test-Path $svcFile) {
            $lines = Get-Content $svcFile -Encoding UTF8 -EA SilentlyContinue
            $svcOk = 0; $svcFail = 0
            foreach ($line in $lines) {
                if ($line -notmatch '^(\w+)=(.+)$') { continue }
                $svcName   = $Matches[1]
                $startType = $Matches[2]
                $scArg = switch ($startType) {
                    'Automatic' { 'auto'     }
                    'Manual'    { 'demand'   }
                    'Disabled'  { 'disabled' }
                    default     { $null }
                }
                if (-not $scArg) { continue }
                $r2 = RunC "sc config $svcName start= $scArg"
                if ($r2.OK) {
                    if ($scArg -in 'auto','demand') { RunC "sc start $svcName" | Out-Null }
                    $svcOk++
                } else { $svcFail++ }
            }
            AddLog "  Services: $svcOk restored, $svcFail failed." '#22C55E'
        } else {
            AddLog "  No service state file found — services NOT restored." '#F59E0B'
        }

        # ── 4. Restore power plan ──────────────────────────────────────────────
        if (Test-Path $metaFile) {
            $meta     = Get-Content $metaFile -Encoding UTF8 -EA SilentlyContinue
            $planLine = $meta | Where-Object { $_ -like 'PowerPlan=*' }
            if ($planLine) {
                $guid = ($planLine -split '=',2)[1].Trim()
                if ($guid -and $guid -ne 'unknown' -and $guid -match '^[0-9a-f-]{36}$') {
                    $rp = RunC "powercfg -setactive $guid"
                    if ($rp.OK) { AddLog "  Power plan restored: $guid" '#22C55E' }
                }
            }
            $twLine = $meta | Where-Object { $_ -like 'AppliedTweaks=*' }
            if ($twLine) {
                $cnt = ($twLine -split '=',2)[1].Trim()
                AddLog "  This backup had $cnt tweaks applied at creation time." '#38BDF8'
            }
        }

        # ── 5. Clear VOIDTUNE applied state ────────────────────────────────────
        $all = $script:TWEAKS + $script:PRIVTWEAKS + $script:ARCH_TWEAKS
        foreach ($t in $all) { $t.Applied = $false; $t.Sel = $false }
        $script:applied = 0
        Save-TweakState
        UI {
            FilterTweaks
            $da = G 'DashApplied'; if ($da) { $da.Text = 0 }
        }

        AddLog "RESTORE COMPLETE — restart recommended for all changes to take effect." '#22C55E'
        return $true
    } catch {
        AddLog "Restore error: $($_.Exception.Message)" '#E01818'
        return $false
    }
}

# ── Tweak State Persistence ───────────────────────────────────────────────────
$script:STATE_FILE = Join-Path $script:ROOT 'voidtune_state.txt'

function Save-TweakState {
    try {
        $all     = $script:TWEAKS + $script:PRIVTWEAKS + $script:ARCH_TWEAKS
        $applied = @($all | Where-Object { $_.Applied } | ForEach-Object { $_.Id })
        Set-Content -Path $script:STATE_FILE -Value ($applied -join "`n") -Encoding UTF8
    } catch {}
}

function Load-TweakState {
    try {
        if (-not (Test-Path $script:STATE_FILE)) { return }
        $ids = @(Get-Content $script:STATE_FILE -Encoding UTF8 | Where-Object { $_ -ne '' })
        $all = $script:TWEAKS + $script:PRIVTWEAKS + $script:ARCH_TWEAKS
        foreach ($t in $all) {
            if ($ids -contains $t.Id) { $t.Applied = $true; $t.Sel = $true }
        }
        $script:applied = $ids.Count
        AddLog "Restored $($ids.Count) applied tweaks from last session." '#38BDF8'
    } catch {}
}
