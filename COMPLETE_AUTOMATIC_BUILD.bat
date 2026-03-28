@echo off
REM ============================================
REM MaterialManager 1.0.x - COMPLETE AUTOMATIC BUILD
REM Mit Admin-Rechten - Baut ALLES in EINEM Durchgang!
REM ============================================

color 0A
cls

echo.
echo â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
echo â•‘  ðŸš€ MATERIALMANAGER 1.0.x - COMPLETE AUTOMATIC BUILD            â•‘
echo â•‘  Alles wird automatisch gebaut (mit Admin-Rechten)            â•‘
echo â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

REM Admin-PrÃ¼fung
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo âŒ FEHLER: Admin-Rechte erforderlich!
    echo.
    echo Bitte:
    echo 1. Dieses Skript Rechtsklick
    echo 2. "Als Administrator ausfÃ¼hren" wÃ¤hlen
    echo.
    pause
    exit /b 1
)

echo âœ“ Admin-Rechte bestÃ¤tigt
echo.
echo â³ BUILD STARTET JETZT...
echo.
echo Dieser Prozess dauert ca. 5-10 Minuten
echo Bitte NICHT UNTERBRECHEN!
echo.

REM PowerShell ausfÃ¼hren
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
"cd 'C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x'; .\COMPLETE_AUTOMATIC_BUILD.ps1"

if %ERRORLEVEL% equ 0 (
    echo.
    echo â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
    echo â•‘  âœ… BUILD ERFOLGREICH ABGESCHLOSSEN!                          â•‘
    echo â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    echo.
    echo ðŸŽ¯ NÃ„CHSTE SCHRITTE:
    echo    1. USB-Stick einstecken
    echo    2. Daten auf USB kopieren:
    echo       copy-item -Path "C:\...\USB_Installation\*" -Destination "D:\" -Recurse
    echo    3. USB zu Kunde senden
    echo.
) else (
    echo.
    echo âŒ BUILD FEHLGESCHLAGEN!
    echo Bitte prÃ¼fe die Meldungen oben.
    echo.
)

pause

