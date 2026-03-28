@echo off
REM ============================================
REM MaterialManager 1.0.x - ONE-CLICK USB KOPIEREN
REM Alles automatisch: Build + USB Kopieren
REM ============================================

color 0A
cls

echo.
echo â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
echo â•‘  ðŸš€ MATERIALMANAGER 1.0.x - ONE-CLICK USB KOPIEREN             â•‘
echo â•‘  Baut automatisch und kopiert auf USB!                       â•‘
echo â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

REM Admin-PrÃ¼fung
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo âŒ FEHLER: Admin-Rechte erforderlich!
    echo.
    echo Bitte:
    echo 1. Rechtsklick auf diese Datei
    echo 2. "Als Administrator ausfÃ¼hren" wÃ¤hlen
    echo.
    pause
    exit /b 1
)

echo âœ“ Admin-Rechte bestÃ¤tigt
echo.

REM Starte COMPLETE_AUTOMATIC_BUILD
echo â³ STARTE COMPLETE_AUTOMATIC_BUILD...
echo.
echo Dieser Prozess dauert ca. 5-10 Minuten
echo Bitte NICHT unterbrechen!
echo.

cd "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x"

powershell -NoProfile -ExecutionPolicy Bypass -Command ".\COMPLETE_AUTOMATIC_BUILD.ps1"

if %ERRORLEVEL% neq 0 (
    echo.
    echo âŒ BUILD FEHLGESCHLAGEN!
    echo Bitte Fehler prÃ¼fen oben.
    echo.
    pause
    exit /b 1
)

echo.
echo âœ… BUILD ERFOLGREICH!
echo.
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

REM Starte USB_InstallationHelper
echo ðŸ–¥ï¸  Starte USB_InstallationHelper GUI...
echo.
echo Im Fenster:
echo 1. USB-Stick auswÃ¤hlen
echo 2. Button "ðŸ’¾ Auf USB kopieren" drÃ¼cken
echo 3. Warten
echo 4. âœ… Fertig!
echo.

timeout /t 3

start "" "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x\USB_InstallationHelper\bin\Release\net8.0-windows\USB_InstallationHelper.exe"

echo.
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo âœ… USB_InstallationHelper ist gestartet!
echo.
echo NÃ¤chste Schritte im GUI-Fenster...
echo.
pause

