@echo off
REM ============================================
REM MaterialManager R03 - COMPLETE AUTOMATIC BUILD
REM Mit Admin-Rechten - Baut ALLES in EINEM Durchgang!
REM ============================================

color 0A
cls

echo.
echo ╔════════════════════════════════════════════════════════════════╗
echo ║  🚀 MATERIALMANAGER R03 - COMPLETE AUTOMATIC BUILD            ║
echo ║  Alles wird automatisch gebaut (mit Admin-Rechten)            ║
echo ╚════════════════════════════════════════════════════════════════╝
echo.

REM Admin-Prüfung
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo ❌ FEHLER: Admin-Rechte erforderlich!
    echo.
    echo Bitte:
    echo 1. Dieses Skript Rechtsklick
    echo 2. "Als Administrator ausführen" wählen
    echo.
    pause
    exit /b 1
)

echo ✓ Admin-Rechte bestätigt
echo.
echo ⏳ BUILD STARTET JETZT...
echo.
echo Dieser Prozess dauert ca. 5-10 Minuten
echo Bitte NICHT UNTERBRECHEN!
echo.

REM PowerShell ausführen
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
"cd 'C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03'; .\COMPLETE_AUTOMATIC_BUILD.ps1"

if %ERRORLEVEL% equ 0 (
    echo.
    echo ╔════════════════════════════════════════════════════════════════╗
    echo ║  ✅ BUILD ERFOLGREICH ABGESCHLOSSEN!                          ║
    echo ╚════════════════════════════════════════════════════════════════╝
    echo.
    echo 🎯 NÄCHSTE SCHRITTE:
    echo    1. USB-Stick einstecken
    echo    2. Daten auf USB kopieren:
    echo       copy-item -Path "C:\...\USB_Installation\*" -Destination "D:\" -Recurse
    echo    3. USB zu Kunde senden
    echo.
) else (
    echo.
    echo ❌ BUILD FEHLGESCHLAGEN!
    echo Bitte prüfe die Meldungen oben.
    echo.
)

pause
