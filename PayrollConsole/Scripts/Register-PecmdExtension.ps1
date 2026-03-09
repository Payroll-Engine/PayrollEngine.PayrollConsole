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
    Write-Host "Done."
    exit 0
}

# --- Resolve PayrollConsole path ---
if (-not $ConsolePath) {
    # Try to find PayrollConsole.exe next to this script
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $candidate = Join-Path $scriptDir "PayrollEngine.PayrollConsole.exe"
    if (Test-Path $candidate) {
        $ConsolePath = $candidate
    } else {
        # Try PATH
        $found = Get-Command "PayrollEngine.PayrollConsole.exe" -ErrorAction SilentlyContinue
        if ($found) {
            $ConsolePath = $found.Source
        } else {
            Write-Error "PayrollConsole.exe not found. Use -ConsolePath to specify the location."
        }
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

Write-Host "Registration complete. .pecmd files now open with PayrollConsole."
Write-Host "To undo: .\Register-PecmdExtension.ps1 -Unregister"
