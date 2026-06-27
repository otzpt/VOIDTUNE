# VOIDTUNE - modules/personalize.ps1
# Copyright (C) 2026 @OTZPT - GPL v3
# Personalization, Aero, Color & Display tweaks

# ── ACCENT COLOR PRESETS ──────────────────────────────────────────────────────
$script:ACCENT_COLORS = @(
    @{Hex='#0078D4';Name='Windows Blue'},
    @{Hex='#7c3aed';Name='VOIDTUNE Purple'},
    @{Hex='#06B6D4';Name='Cyan'},
    @{Hex='#16A34A';Name='Green'},
    @{Hex='#E01818';Name='Red'},
    @{Hex='#F97316';Name='Orange'},
    @{Hex='#EC4899';Name='Pink'},
    @{Hex='#EAB308';Name='Yellow'},
    @{Hex='#64748B';Name='Slate'},
    @{Hex='#0EA5E9';Name='Sky'},
    @{Hex='#8B5CF6';Name='Violet'},
    @{Hex='#10B981';Name='Emerald'}
)

# ── STATE READER ──────────────────────────────────────────────────────────────
function Get-PersonalizeState($id) {
    try {
        switch ($id) {
            'DarkMode'       { return (Get-ItemPropertyValue 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize' 'AppsUseLightTheme'  -EA SilentlyContinue) -eq 0 }
            'Transparency'   { return (Get-ItemPropertyValue 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize' 'EnableTransparency' -EA SilentlyContinue) -eq 1 }
            'ClassicAltTab'  { return (Get-ItemPropertyValue 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer' 'AltTabSettings' -EA SilentlyContinue) -eq 1 }
            'NoLoginBlur'    { return (Get-ItemPropertyValue 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\System' 'DisableAcrylicBackgroundOnLogon' -EA SilentlyContinue) -eq 1 }
            'OldContextMenu' { return (Test-Path 'HKCU:\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32') }
            'NoRoundCorners' { return (Get-ItemPropertyValue 'HKCU:\Software\Microsoft\Windows\DWM' 'UseWindowDarkMode' -EA SilentlyContinue) -eq 1 }
            'AeroPeek'       { return (Get-ItemPropertyValue 'HKCU:\Software\Microsoft\Windows\DWM' 'EnableAeroPeek'          -EA SilentlyContinue) -eq 1 }
            'AeroTitlebar'   { return (Get-ItemPropertyValue 'HKCU:\Software\Microsoft\Windows\DWM' 'ColorPrevalence'         -EA SilentlyContinue) -eq 1 }
            'AeroGlass'      { return (Get-ItemPropertyValue 'HKCU:\Software\Microsoft\Windows\DWM' 'ColorizationOpaqueBlend' -EA SilentlyContinue) -eq 0 }
            'AeroAnimations' { return (Get-ItemPropertyValue 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' 'TaskbarAnimations' -EA SilentlyContinue) -eq 1 }
            'AeroSnapAssist' { $v = Get-ItemPropertyValue 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' 'SnapAssist' -EA SilentlyContinue; return ($v -ne 0 -and $null -ne $v) }
            'AeroDragFull'   { return (Get-ItemPropertyValue 'HKCU:\Control Panel\Desktop' 'DragFullWindows' -EA SilentlyContinue) -eq '1' }
            'SmallIcons'     { return (Get-ItemPropertyValue 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' 'TaskbarSmallIcons'    -EA SilentlyContinue) -eq 1 }
            'HideSearch'     { return (Get-ItemPropertyValue 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Search'            'SearchboxTaskbarMode' -EA SilentlyContinue) -eq 0 }
            'HideWidgets'    { return (Get-ItemPropertyValue 'HKLM:\SOFTWARE\Policies\Microsoft\Dsh' 'AllowNewsAndInterests' -EA SilentlyContinue) -eq 0 }
            'HideTaskView'   { return (Get-ItemPropertyValue 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' 'ShowTaskViewButton'   -EA SilentlyContinue) -eq 0 }
            'CompactMode'    { return (Get-ItemPropertyValue 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' 'UseCompactMode'       -EA SilentlyContinue) -eq 1 }
            'ClearType'      { return (Get-ItemPropertyValue 'HKCU:\Control Panel\Desktop' 'FontSmoothingType' -EA SilentlyContinue) -eq 2 }
            default          { return $false }
        }
    } catch { return $false }
}

# ── APPLY TOGGLE ──────────────────────────────────────────────────────────────
function Set-PersonalizeTweak($id, $enable) {
    try {
        $on  = if ($enable) { 1 } else { 0 }
        $off = if ($enable) { 0 } else { 1 }
        switch ($id) {
            'DarkMode' {
                $v = if ($enable) { 0 } else { 1 }
                RunC "reg add `"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize`" /v AppsUseLightTheme    /t REG_DWORD /d $v /f" | Out-Null
                RunC "reg add `"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize`" /v SystemUsesLightTheme /t REG_DWORD /d $v /f" | Out-Null
            }
            'Transparency' {
                RunC "reg add `"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize`" /v EnableTransparency /t REG_DWORD /d $on /f" | Out-Null
            }
            'ClassicAltTab' {
                RunC "reg add `"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer`" /v AltTabSettings /t REG_DWORD /d $on /f" | Out-Null
            }
            'NoLoginBlur' {
                if ($enable) { RunC 'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v DisableAcrylicBackgroundOnLogon /t REG_DWORD /d 1 /f' | Out-Null }
                else         { RunC 'reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v DisableAcrylicBackgroundOnLogon /f' | Out-Null }
            }
            'OldContextMenu' {
                if ($enable) { RunC 'reg add "HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32" /f /ve' | Out-Null }
                else         { RunC 'reg delete "HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}" /f' | Out-Null }
            }
            'NoRoundCorners' {
                RunC "reg add `"HKCU\Software\Microsoft\Windows\DWM`" /v UseWindowDarkMode /t REG_DWORD /d $on /f" | Out-Null
            }
            'AeroPeek' {
                RunC "reg add `"HKCU\Software\Microsoft\Windows\DWM`" /v EnableAeroPeek /t REG_DWORD /d $on /f" | Out-Null
            }
            'AeroTitlebar' {
                RunC "reg add `"HKCU\Software\Microsoft\Windows\DWM`" /v ColorPrevalence /t REG_DWORD /d $on /f" | Out-Null
                RunC "reg add `"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize`" /v ColorPrevalence /t REG_DWORD /d $on /f" | Out-Null
            }
            'AeroGlass' {
                # 0 = glass/transparent ON, 1 = opaque
                RunC "reg add `"HKCU\Software\Microsoft\Windows\DWM`" /v ColorizationOpaqueBlend    /t REG_DWORD /d $off /f" | Out-Null
                RunC "reg add `"HKCU\Software\Microsoft\Windows\DWM`" /v ColorizationGlassAttribute /t REG_DWORD /d $on  /f" | Out-Null
            }
            'AeroAnimations' {
                RunC "reg add `"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced`" /v TaskbarAnimations /t REG_DWORD /d $on /f" | Out-Null
                RunC "reg add `"HKCU\Control Panel\Desktop\WindowMetrics`" /v MinAnimate /t REG_SZ /d $on /f" | Out-Null
                RunC "reg add `"HKCU\Control Panel\Desktop`" /v MenuShowDelay /t REG_SZ /d $(if($enable){'400'}else{'0'}) /f" | Out-Null
            }
            'AeroSnapAssist' {
                RunC "reg add `"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced`" /v SnapAssist /t REG_DWORD /d $on /f" | Out-Null
            }
            'AeroDragFull' {
                RunC "reg add `"HKCU\Control Panel\Desktop`" /v DragFullWindows /t REG_SZ /d $(if($enable){'1'}else{'0'}) /f" | Out-Null
            }
            'SmallIcons' {
                RunC "reg add `"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced`" /v TaskbarSmallIcons /t REG_DWORD /d $on /f" | Out-Null
            }
            'HideSearch' {
                $v = if ($enable) { 0 } else { 1 }
                RunC "reg add `"HKCU\Software\Microsoft\Windows\CurrentVersion\Search`" /v SearchboxTaskbarMode /t REG_DWORD /d $v /f" | Out-Null
            }
            'HideWidgets' {
                if ($enable) { RunC 'reg add "HKLM\SOFTWARE\Policies\Microsoft\Dsh" /v AllowNewsAndInterests /t REG_DWORD /d 0 /f' | Out-Null }
                else         { RunC 'reg delete "HKLM\SOFTWARE\Policies\Microsoft\Dsh" /v AllowNewsAndInterests /f' | Out-Null }
            }
            'HideTaskView' {
                $v = if ($enable) { 0 } else { 1 }
                RunC "reg add `"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced`" /v ShowTaskViewButton /t REG_DWORD /d $v /f" | Out-Null
            }
            'CompactMode' {
                RunC "reg add `"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced`" /v UseCompactMode /t REG_DWORD /d $on /f" | Out-Null
            }
            'ClearType' {
                $type   = if ($enable) { 2 } else { 1 }
                $smooth = if ($enable) { 2 } else { 0 }
                RunC "reg add `"HKCU\Control Panel\Desktop`" /v FontSmoothingType /t REG_DWORD /d $type   /f" | Out-Null
                RunC "reg add `"HKCU\Control Panel\Desktop`" /v FontSmoothing     /t REG_DWORD /d $smooth /f" | Out-Null
            }
        }
        return $true
    } catch { return $false }
}

# ── ACCENT COLOR ──────────────────────────────────────────────────────────────
function Get-CurrentAccentHex {
    try {
        $raw = Get-ItemPropertyValue 'HKCU:\Software\Microsoft\Windows\DWM' 'ColorizationColor' -EA SilentlyContinue
        if ($null -eq $raw) { return '#0078D4' }
        $r = ($raw -shr 16) -band 0xFF
        $g = ($raw -shr 8)  -band 0xFF
        $b =  $raw           -band 0xFF
        return "#{0:X2}{1:X2}{2:X2}" -f $r, $g, $b
    } catch { return '#0078D4' }
}

function Set-AccentColor($hexColor) {
    try {
        $hex = $hexColor.TrimStart('#')
        $r = [Convert]::ToInt32($hex.Substring(0,2), 16)
        $g = [Convert]::ToInt32($hex.Substring(2,2), 16)
        $b = [Convert]::ToInt32($hex.Substring(4,2), 16)
        # DWM ColorizationColor: ARGB 0xAARRGGBB, 0xC4 alpha = Windows default
        $argb = [long](0xC4000000) + ($r -shl 16) + ($g -shl 8) + $b
        RunC "reg add `"HKCU\Software\Microsoft\Windows\DWM`" /v ColorizationColor     /t REG_DWORD /d $argb /f" | Out-Null
        RunC "reg add `"HKCU\Software\Microsoft\Windows\DWM`" /v ColorizationAfterglow /t REG_DWORD /d $argb /f" | Out-Null
        # Explorer\Accent AccentColorMenu: ABGR format
        $abgr = [long](0xFF000000) + ($b -shl 16) + ($g -shl 8) + $r
        RunC "reg add `"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Accent`" /v AccentColorMenu /t REG_DWORD /d $abgr /f" | Out-Null
        UI {
            $ai = G 'PAccentCurrent'
            if ($ai) { $ai.Background = [System.Windows.Media.SolidColorBrush]([System.Windows.Media.ColorConverter]::ConvertFromString($hexColor)) }
            $al = G 'PAccentLabel'
            if ($al) {
                $match = $script:ACCENT_COLORS | Where-Object { $_.Hex -ieq $hexColor } | Select-Object -First 1
                $al.Text = if ($match) { $match.Name } else { $hexColor }
            }
        }
        AddLog "Accent: $hexColor -- restart Explorer to apply fully." '#38BDF8'
        return $true
    } catch { AddLog "Accent error: $($_.Exception.Message)" '#E01818'; return $false }
}

# ── REFRESH PAGE ──────────────────────────────────────────────────────────────
function RefreshPersonalize {
    try {
        $ids = @(
            'DarkMode','Transparency','ClassicAltTab','NoLoginBlur','OldContextMenu','NoRoundCorners',
            'AeroPeek','AeroTitlebar','AeroGlass','AeroAnimations','AeroSnapAssist','AeroDragFull',
            'SmallIcons','HideSearch','HideWidgets','HideTaskView','CompactMode','ClearType'
        )
        foreach ($id in $ids) {
            $state = Get-PersonalizeState $id
            $btn   = G "PBtn_$id"
            $dot   = G "PDot_$id"
            if ($btn) { $btn.Content = if ($state) { 'ON' } else { 'OFF' } }
            if ($dot) {
                $dot.Fill = if ($state) {
                    [System.Windows.Media.Brushes]::LimeGreen
                } else {
                    [System.Windows.Media.SolidColorBrush][System.Windows.Media.Color]::FromRgb(60,60,60)
                }
            }
        }
        try {
            $wp = Get-ItemPropertyValue 'HKCU:\Control Panel\Desktop' 'Wallpaper' -EA SilentlyContinue
            $wl = G 'PWallpaperPath'; if ($wl) { $wl.Text = if ($wp) { $wp } else { 'No wallpaper set' } }
        } catch {}
        try {
            $hex = Get-CurrentAccentHex
            $ai  = G 'PAccentCurrent'
            $al  = G 'PAccentLabel'
            if ($ai) { $ai.Background = [System.Windows.Media.SolidColorBrush]([System.Windows.Media.ColorConverter]::ConvertFromString($hex)) }
            if ($al) {
                $match = $script:ACCENT_COLORS | Where-Object { $_.Hex -ieq $hex } | Select-Object -First 1
                $al.Text = if ($match) { $match.Name } else { $hex }
            }
        } catch {}
    } catch { AddLog "Personalize refresh: $($_.Exception.Message)" '#E01818' }
}
