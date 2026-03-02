@echo off
REM ============================================
REM MaterialManager R03 - MASTER SETUP (Admin)
REM ============================================

color 0A
cls

echo.
echo ╔════════════════════════════════════════════════════════════════╗
echo ║  MATERIALMANAGER R03 - COMPLETE SETUP (Admin Required)        ║
echo ║  Erstellt: Installer.exe + USB-Paket + Backup                 ║
echo ╚════════════════════════════════════════════════════════════════╝
echo.

REM Admin-Prüfung
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo ❌ FEHLER: Admin-Rechte erforderlich!
    echo.
    echo Bitte:
    echo 1. Windows PowerShell öffnen
    echo 2. Rechtsklick → "Als Administrator ausführen"
    echo 3. Diesen Befehl ausführen:
    echo.
    echo    cd C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03
    echo    .\MASTER_SETUP.ps1 -Action Full
    echo.
    pause
    exit /b 1
)

echo ✓ Admin-Rechte bestätigt
echo.

REM PowerShell ausführen
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
"cd 'C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03'; .\MASTER_SETUP.ps1 -Action Full"

echo.
pause
