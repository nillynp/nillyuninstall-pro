# NillyUninstall Pro — Clean Uninstaller (Zero Admin · HKCU only)
param([switch]$Silent)
$ErrorActionPreference = 'SilentlyContinue'
Get-Process NillyUninstallPro -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
if (Test-Path 'HKCU:\Software\Classes\Applications\NillyUninstallPro.exe') { Remove-Item 'HKCU:\Software\Classes\Applications\NillyUninstallPro.exe' -Recurse -Force }
Remove-Item (Join-Path ([Environment]::GetFolderPath('Desktop')) 'NillyUninstall Pro.lnk') -Force
Remove-Item (Join-Path ([Environment]::GetFolderPath('Programs')) 'Nilly graphics\NillyUninstall Pro.lnk') -Force
if (-not $Silent) { Write-Host '  Το NillyUninstall Pro απεγκαταστάθηκε καθαρά.' -ForegroundColor Green }
