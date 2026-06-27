# VOIDTUNE - core/hardware.ps1
# Copyright (C) 2026 @OTZPT - GPL v3

$script:HW = @{
    CpuName   = ''
    CpuVendor = 'Unknown'
    IsLaptop  = $false
    WinBuild  = [int][System.Environment]::OSVersion.Version.Build
    WingetOK  = $false
    ChocoOK   = $false
    GpuName   = ''
    GpuVendor = 'Unknown'
    AllGpus   = @()
}

# Pick best discrete GPU
# Scoring: NVIDIA/AMD dedicated=100, Intel Arc=60, Intel Iris=55,
# Intel UHD/HD iGPU=20, other Intel=30, other unknown=40, Microsoft Basic=0
function Get-DiscreteGpu {
    $all = Get-CimInstance Win32_VideoController -EA SilentlyContinue
    if (-not $all) { return $null }
    $scored = foreach ($g in $all) {
        $n = $g.Name
        $score = switch -Wildcard ($n) {
            '*NVIDIA*'                              { 100 }
            '*Radeon*'                              { 100 }
            '*RX *'                                 { 100 }
            '*Intel*Arc*'                           { 60  }
            '*Intel*Iris*'                          { 55  }
            { $_ -like '*Intel*UHD*' -or
              $_ -like '*Intel*HD*' }               { 20  }
            '*Intel*'                               { 30  }
            '*Microsoft*Basic*'                     { 0   }
            default                                 { 40  }
        }
        [PSCustomObject]@{ Gpu=$g; Score=$score }
    }
    ($scored | Sort-Object Score -Descending | Select-Object -First 1).Gpu
}

try {
    $cpuObj = Get-CimInstance Win32_Processor -EA SilentlyContinue | Select-Object -First 1
    if ($cpuObj) {
        $script:HW.CpuName = $cpuObj.Name.Trim()
        if     ($cpuObj.Manufacturer -like '*Intel*' -or $cpuObj.Name -like '*Intel*') { $script:HW.CpuVendor = 'Intel' }
        elseif ($cpuObj.Manufacturer -like '*AMD*'   -or $cpuObj.Name -like '*AMD*')   { $script:HW.CpuVendor = 'AMD'   }
    }
    $gpuObj = Get-DiscreteGpu
    if ($gpuObj) {
        $script:HW.GpuName = $gpuObj.Name.Trim()
        if     ($gpuObj.Name -like '*NVIDIA*')                         { $script:HW.GpuVendor = 'NVIDIA' }
        elseif ($gpuObj.Name -like '*Radeon*' -or
                $gpuObj.Name -like '*RX *')                            { $script:HW.GpuVendor = 'AMD'    }
        elseif ($gpuObj.Name -like '*Intel*')                          { $script:HW.GpuVendor = 'Intel'  }
    }
    $script:HW.AllGpus = @(
        Get-CimInstance Win32_VideoController -EA SilentlyContinue |
        Where-Object { $_.Name -notlike '*Microsoft Basic*' } |
        ForEach-Object { $_.Name.Trim() }
    )
    $script:HW.IsLaptop = ($null -ne (Get-CimInstance Win32_Battery -EA SilentlyContinue))
    $script:HW.WingetOK = ($null -ne (Get-Command winget -EA SilentlyContinue))
    $script:HW.ChocoOK  = ($null -ne (Get-Command choco  -EA SilentlyContinue))
} catch {}

WL "HW: CPU=$($script:HW.CpuVendor) GPU=$($script:HW.GpuVendor) [$($script:HW.GpuName)] Laptop=$($script:HW.IsLaptop) Build=$($script:HW.WinBuild) Winget=$($script:HW.WingetOK) Choco=$($script:HW.ChocoOK)"
