@echo off
REM ============================================
REM USB Installation Helper - Batch Version
REM Einfaches Tool zum Kopieren auf USB-Stick
REM ============================================

echo.
echo ╔════════════════════════════════════════════════════════════════╗
echo ║  📁 MaterialManager R03 - USB Installation Helper             ║
echo ║  Kopiere alle Dateien auf einen USB-Stick                    ║
echo ╚════════════════════════════════════════════════════════════════╝
echo.

setlocal enabledelayedexpansion

REM Pfade
set USB_INSTALL=C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03\USB_Installation

REM Check ob Ordner existiert
if not exist "%USB_INSTALL%" (
    echo ❌ FEHLER: USB_Installation Ordner nicht gefunden!
    echo Erwartet: %USB_INSTALL%
    echo.
    pause
    exit /b 1
)

echo ✓ USB_Installation Ordner gefunden
echo  Pfad: %USB_INSTALL%
echo.

REM Zeige verfügbare Laufwerke
echo 📁 Verfügbare USB-Laufwerke:
echo.
for %%A in (D E F G H I J K L M N O P Q R S T U V W X Y Z) do (
    if exist %%A: (
        echo   %%A:\ (Prüfe...)
        REM Prüfe ob es ein USB-Stick ist (einfache Prüfung)
        dir %%A: >nul 2>&1
        if !errorlevel! equ 0 (
            for /f "tokens=3" %%S in ('dir %%A:\ ^| find "Bytes"') do set SIZE=%%S
            echo   ✓ %%A:\ verfügbar
        )
    )
)

echo.
echo Wähle ein USB-Laufwerk (z.B. D, E, F):
set /p DRIVE="USB-Laufwerk: "

REM Validiere Eingabe
if not exist %DRIVE%: (
    echo ❌ Laufwerk %DRIVE%: nicht gefunden!
    pause
    exit /b 1
)

echo.
echo ════════════════════════════════════════════════════════════════
echo 🚀 Kopiere USB_Installation auf %DRIVE%:\
echo.
echo Warte bitte... (kann mehrere Minuten dauern)
echo.

REM Kopiere Dateien
xcopy "%USB_INSTALL%\*" "%DRIVE%:\" /E /I /Y >nul 2>&1

if %errorlevel% equ 0 (
    echo ✅ Erfolgreich kopiert!
    echo.
    echo ════════════════════════════════════════════════════════════════
    echo.
    echo USB-Stick ist bereit! ✅
    echo.
    echo ➤ Gib den USB-Stick dem Kunden
    echo ➤ Kunde steckt ihn ein
    echo ➤ Kunde startet: Installer.exe
    echo ➤ Installation läuft automatisch!
    echo.
) else (
    echo ❌ Fehler beim Kopieren!
    echo.
    echo Mögliche Gründe:
    echo • USB-Stick nicht genug Platz (mind. 500 MB nötig)
    echo • USB-Stick schreibgeschützt
    echo • Dateien sind noch in Verwendung
    echo.
)

pause
