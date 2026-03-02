@echo off
REM ============================================
REM FEHLER-LÖSUNG: USB_Distribution erstellen
REM ============================================

color 0B
cls

echo.
echo ╔════════════════════════════════════════════════════════════════╗
echo ║  BUILD-USBVersion.ps1 - USB_Distribution erstellen            ║
echo ╚════════════════════════════════════════════════════════════════╝
echo.

REM Pfad
set "PROJECT_PATH=C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03"

if not exist "%PROJECT_PATH%" (
    echo ❌ Projekt-Pfad nicht gefunden:
    echo    %PROJECT_PATH%
    pause
    exit /b 1
)

echo ✓ Wechsel zu: %PROJECT_PATH%
cd /d "%PROJECT_PATH%"

echo.
echo [1/2] Prüfe PowerShell-Script...
if not exist "Build-USBVersion.ps1" (
    echo ❌ FEHLER: Build-USBVersion.ps1 nicht gefunden!
    echo    Pfad: %PROJECT_PATH%\Build-USBVersion.ps1
    pause
    exit /b 1
)
echo ✓ Build-USBVersion.ps1 gefunden

echo.
echo [2/2] Starte Build-USBVersion.ps1 -Action Package...
echo.
echo ⏳ Dies kann 2-5 Minuten dauern...
echo.

REM PowerShell ausführen
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
"cd '%PROJECT_PATH%'; .\Build-USBVersion.ps1 -Action Package"

if %ERRORLEVEL% equ 0 (
    echo.
    echo ╔════════════════════════════════════════════════════════════════╗
    echo ║  ✅ ERFOLGREICH!                                              ║
    echo ╚════════════════════════════════════════════════════════════════╝
    echo.
    echo USB_Distribution wurde erstellt!
    echo.
    echo 🎯 NÄCHSTER SCHRITT:
    echo    cd USB_Installation
    echo    SETUP_PROGRAMM.bat
    echo.
) else (
    echo.
    echo ❌ FEHLER beim Build!
    echo Bitte prüfe die Meldungen oben.
    echo.
)

pause
