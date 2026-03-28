@echo off
REM ============================================
REM MaterialManager 1.0.x - MASTER SETUP (Admin)
REM ============================================

color 0A
cls

echo.
echo â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
echo â•‘  MATERIALMANAGER 1.0.x - COMPLETE SETUP (Admin Required)        â•‘
echo â•‘  Erstellt: Installer.exe + USB-Paket + Backup                 â•‘
echo â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

REM Admin-PrÃ¼fung
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo âŒ FEHLER: Admin-Rechte erforderlich!
    echo.
    echo Bitte:
    echo 1. Windows PowerShell Ã¶ffnen
    echo 2. Rechtsklick â†’ "Als Administrator ausfÃ¼hren"
    echo 3. Diesen Befehl ausfÃ¼hren:
    echo.
    echo    cd C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x
    echo    .\MASTER_SETUP.ps1 -Action Full
    echo.
    pause
    exit /b 1
)

echo âœ“ Admin-Rechte bestÃ¤tigt
echo.

REM PowerShell ausfÃ¼hren
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
"cd 'C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x'; .\MASTER_SETUP.ps1 -Action Full"

echo.
pause

