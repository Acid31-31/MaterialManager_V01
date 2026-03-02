@echo off
REM ═══════════════════════════════════════════════════════════════
REM  MaterialManager R03 - Schnell-Backup (Batch)
REM ═══════════════════════════════════════════════════════════════

cls
echo.
echo ╔═══════════════════════════════════════════════════════════════╗
echo ║                                                               ║
echo ║   MaterialManager R03 - Automatisches Backup                ║
echo ║                                                               ║
echo ╚═══════════════════════════════════════════════════════════════╝
echo.

REM Starte PowerShell-Script
powershell -ExecutionPolicy Bypass -File "%~dp0CreateAutoBackup.ps1"

echo.
pause
