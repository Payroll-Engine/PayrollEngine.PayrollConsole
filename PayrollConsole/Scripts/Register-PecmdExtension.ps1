# Register-PecmdExtension.ps1
# Registers the .pecmd file extension for the current user (no admin required).
# Usage: .\Register-PecmdExtension.ps1 [-ConsolePath <path>] [-Unregister]

param(
    [string] $ConsolePath = "",
    [switch] $Unregister
)

$ErrorActionPreference = "Stop"
$regRoot  = "HKCU:\Software\Classes"
$extKey   = "$regRoot\.pecmd"
$typeKey  = "$regRoot\pecmdfile"
$openKey  = "$typeKey\shell\open\command"

if ($Unregister) {
    Write-Host "Removing .pecmd registration..."
    Remove-Item -Path $typeKey -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $extKey  -Recurse -Force -ErrorAction SilentlyContinue
    $fileExtsBase = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.pecmd"
    Remove-Item -Path "$fileExtsBase\UserChoice"     -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "$fileExtsBase\OpenWithProgids" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "$fileExtsBase\OpenWithList"    -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "Done."
    exit 0
}

# --- Resolve PayrollConsole path ---
if (-not $ConsolePath) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $exeName   = "PayrollEngine.PayrollConsole.exe"

    # 1. Next to this script (correct when run from the published Bin\Console\ folder)
    $candidates = @(
        (Join-Path $scriptDir $exeName),
        # 2. One level up (when run from Scripts\ subfolder in the source repo)
        (Join-Path (Split-Path -Parent $scriptDir) $exeName)
    )

    $ConsolePath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1

    if (-not $ConsolePath) {
        # 3. Try PATH
        $found = Get-Command $exeName -ErrorAction SilentlyContinue
        if ($found) { $ConsolePath = $found.Source }
    }

    if (-not $ConsolePath) {
        Write-Error (
            "PayrollEngine.PayrollConsole.exe not found.`n" +
            "Run this script from the published Bin\Console\ folder, " +
            "or pass -ConsolePath to specify the location."
        )
    }
}

$ConsolePath = (Resolve-Path $ConsolePath).Path
Write-Host "Using PayrollConsole: $ConsolePath"

# --- Register in HKCU (no admin needed) ---
New-Item -Path $extKey  -Force | Out-Null
Set-ItemProperty -Path $extKey  -Name "(Default)" -Value "pecmdfile"
Set-ItemProperty -Path $extKey  -Name "Content Type" -Value "application/x-pecmd"

New-Item -Path $typeKey -Force | Out-Null
Set-ItemProperty -Path $typeKey -Name "(Default)" -Value "Payroll Engine Command File"

New-Item -Path "$typeKey\DefaultIcon" -Force | Out-Null
Set-ItemProperty -Path "$typeKey\DefaultIcon" -Name "(Default)" -Value "`"$ConsolePath`",0"

New-Item -Path $openKey -Force | Out-Null
Set-ItemProperty -Path $openKey -Name "(Default)" -Value "`"$ConsolePath`" `"%1`""

# --- Clean up OpenWithProgids: remove all stale ProgID entries except pecmdfile ---
$openWithProgidsKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.pecmd\OpenWithProgids"
if (Test-Path $openWithProgidsKey) {
    Write-Host "Cleaning up OpenWithProgids..."
    $stale = (Get-Item $openWithProgidsKey).GetValueNames() | Where-Object { $_ -ne 'pecmdfile' }
    foreach ($name in $stale) {
        Remove-ItemProperty -Path $openWithProgidsKey -Name $name -Force -ErrorAction SilentlyContinue
        Write-Host "  Removed stale entry: '$name'"
    }
} else {
    New-Item -Path $openWithProgidsKey -Force | Out-Null
}
Set-ItemProperty -Path $openWithProgidsKey -Name 'pecmdfile' -Value ([byte[]]@()) -Type Binary

# --- Set UserChoice to pecmdfile (highest priority, prevents Open With dialog) ---
$userChoiceKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.pecmd\UserChoice"
New-Item -Path $userChoiceKey -Force | Out-Null
Set-ItemProperty -Path $userChoiceKey -Name 'ProgId' -Value 'pecmdfile'
Write-Host "UserChoice set to 'pecmdfile'."

Write-Host "Registration complete. .pecmd files now open with PayrollConsole."
Write-Host "To undo: .\Register-PecmdExtension.ps1 -Unregister"
