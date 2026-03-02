@echo off
REM ============================================
REM MaterialManager R03 - ONE-CLICK USB KOPIEREN
REM Alles automatisch: Build + USB Kopieren
REM ============================================

color 0A
cls

echo.
echo ╔════════════════════════════════════════════════════════════════╗
echo ║  🚀 MATERIALMANAGER R03 - ONE-CLICK USB KOPIEREN             ║
echo ║  Baut automatisch und kopiert auf USB!                       ║
echo ╚════════════════════════════════════════════════════════════════╝
echo.

REM Admin-Prüfung
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo ❌ FEHLER: Admin-Rechte erforderlich!
    echo.
    echo Bitte:
    echo 1. Rechtsklick auf diese Datei
    echo 2. "Als Administrator ausführen" wählen
    echo.
    pause
    exit /b 1
)

echo ✓ Admin-Rechte bestätigt
echo.

REM Starte COMPLETE_AUTOMATIC_BUILD
echo ⏳ STARTE COMPLETE_AUTOMATIC_BUILD...
echo.
echo Dieser Prozess dauert ca. 5-10 Minuten
echo Bitte NICHT unterbrechen!
echo.

cd "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03"

powershell -NoProfile -ExecutionPolicy Bypass -Command ".\COMPLETE_AUTOMATIC_BUILD.ps1"

if %ERRORLEVEL% neq 0 (
    echo.
    echo ❌ BUILD FEHLGESCHLAGEN!
    echo Bitte Fehler prüfen oben.
    echo.
    pause
    exit /b 1
)

echo.
echo ✅ BUILD ERFOLGREICH!
echo.
echo ════════════════════════════════════════════════════════════════
echo.

REM Starte USB_InstallationHelper
echo 🖥️  Starte USB_InstallationHelper GUI...
echo.
echo Im Fenster:
echo 1. USB-Stick auswählen
echo 2. Button "💾 Auf USB kopieren" drücken
echo 3. Warten
echo 4. ✅ Fertig!
echo.

timeout /t 3

start "" "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03\USB_InstallationHelper\bin\Release\net8.0-windows\USB_InstallationHelper.exe"

echo.
echo ════════════════════════════════════════════════════════════════
echo ✅ USB_InstallationHelper ist gestartet!
echo.
echo Nächste Schritte im GUI-Fenster...
echo.
pause
