@echo off
REM ╔════════════════════════════════════════════════════════════════════════╗
REM ║  🎯 MATERIALMANAGER R03 - USB START MENU                             ║
REM ║  Professionelle Präsentation & Installation                          ║
REM ╚════════════════════════════════════════════════════════════════════════╝

setlocal enabledelayedexpansion

:MENU
cls
color 0F
title MaterialManager R03 - Start Menu

echo.
echo ╔════════════════════════════════════════════════════════════════════════╗
echo ║                                                                        ║
echo ║            📦 MATERIALMANAGER R03 - START MENU                        ║
echo ║            Professional Material Management Software                  ║
echo ║                                                                        ║
echo ║                      Version 1.0.0                                    ║
echo ║                                                                        ║
echo ╚════════════════════════════════════════════════════════════════════════╝
echo.
echo.
echo   Willkommen zu MaterialManager R03!
echo.
echo   Bitte wähle eine Option:
echo.
echo   ┌─────────────────────────────────────────────────────────┐
echo   │  1) 📺 Präsentation anzeigen                           │
echo   │  2) 📖 Benutzerhandbuch lesen                          │
echo   │  3) 💾 Installieren                                    │
echo   │  4) ℹ️  Informationen & Features                       │
echo   │  5) 🔧 Systemcheck durchführen                         │
echo   │  6) ❌ Beenden                                         │
echo   └─────────────────────────────────────────────────────────┘
echo.
set /p choice="Gib eine Nummer ein (1-6): "

if "%choice%"=="1" goto PRESENTATION
if "%choice%"=="2" goto USER_GUIDE
if "%choice%"=="3" goto INSTALL
if "%choice%"=="4" goto INFO
if "%choice%"=="5" goto SYSTEMCHECK
if "%choice%"=="6" goto END

echo.
echo ❌ Ungültige Eingabe! Bitte versuche es erneut.
echo.
timeout /t 2
goto MENU

:PRESENTATION
cls
color 0F
echo.
echo ╔════════════════════════════════════════════════════════════════════════╗
echo ║  📺 PRÄSENTATION WIRD GEÖFFNET...                                     ║
echo ╚════════════════════════════════════════════════════════════════════════╝
echo.

REM Versuche HTML zu öffnen
if exist "%~dp0PRESENTATION.html" (
    echo Öffne PRESENTATION.html...
    start "" "%~dp0PRESENTATION.html"
    echo.
    echo ✅ Präsentation wurde im Browser geöffnet.
    echo.
) else (
    echo ❌ PRESENTATION.html nicht gefunden!
    echo.
)

echo Drücke eine Taste zum Fortfahren...
pause >nul
goto MENU

:USER_GUIDE
cls
color 0F
echo.
echo ╔════════════════════════════════════════════════════════════════════════╗
echo ║  📖 BENUTZERHANDBUCH WIRD GEÖFFNET...                                 ║
echo ╚════════════════════════════════════════════════════════════════════════╝
echo.

if exist "%~dp0USER_GUIDE.txt" (
    echo Öffne USER_GUIDE.txt...
    start notepad "%~dp0USER_GUIDE.txt"
    echo.
    echo ✅ Benutzerhandbuch wurde geöffnet.
) else (
    echo ❌ USER_GUIDE.txt nicht gefunden!
)

echo.
echo Drücke eine Taste zum Fortfahren...
pause >nul
goto MENU

:INSTALL
cls
color 0A
echo.
echo ╔════════════════════════════════════════════════════════════════════════╗
echo ║  💾 INSTALLATION                                                      ║
echo ╚════════════════════════════════════════════════════════════════════════╝
echo.

echo Suche Installer...
echo.

REM Suche Installer auf verschiedenen Pfaden
set INSTALLER_PATH=
if exist "%~dp0..\MaterialManager_R03_Installer.exe" (
    set INSTALLER_PATH=%~dp0..\MaterialManager_R03_Installer.exe
)
if exist "%~dp0Programm\MaterialManager_R03.exe" (
    echo ℹ️  Direktes Programm gefunden (kein Installer)
    echo Starte MaterialManager_R03.exe...
    start "" "%~dp0Programm\MaterialManager_R03.exe"
    echo.
    echo ✅ Programm wird gestartet...
    echo.
    echo Drücke eine Taste zum Fortfahren...
    pause >nul
    goto MENU
)

if not "%INSTALLER_PATH%"=="" (
    echo ✅ Installer gefunden!
    echo Starte Installation...
    echo.
    call "%INSTALLER_PATH%"
) else (
    echo ⚠️  Installer nicht gefunden!
    echo.
    echo Bitte stelle sicher, dass sich der Installer auf dem USB-Stick befindet.
    echo.
    echo Suchte an:
    echo   - %~dp0MaterialManager_R03_Installer.exe
    echo   - %~dp0..\MaterialManager_R03_Installer.exe
    echo.
    echo Du kannst das Programm auch direkt starten:
    echo   - Programm\MaterialManager_R03.exe doppelklicken
    echo.
)

echo.
echo Drücke eine Taste zum Fortfahren...
pause >nul
goto MENU

:INFO
cls
color 0E
echo.
echo ╔════════════════════════════════════════════════════════════════════════╗
echo ║  ℹ️  INFORMATIONEN & FEATURES                                         ║
echo ╚════════════════════════════════════════════════════════════════════════╝
echo.

echo 📦 Was ist MaterialManager R03?
echo ──────────────────────────────
echo MaterialManager R03 ist eine professionelle Software zur Verwaltung
echo von Materialbeständen in Blechlägern mit Netzwerk-Unterstützung.
echo.

echo ✨ Hauptmerkmale:
echo ─────────────────
echo   ✅ Bestandsverwaltung in Echtzeit
echo   ✅ Netzwerk-fähig für mehrere Benutzer
echo   ✅ Automatische Datensynchronisierung
echo   ✅ Benutzerlizenzen und Zugriffskontrolle
echo   ✅ Excel-Export und Berichtsfunktion
echo   ✅ Komplettes Audit-Logging
echo   ✅ Automatische Datensicherung
echo.

echo 🖥️  Systemvoraussetzungen:
echo ──────────────────────────
echo   • Windows 10 oder Windows 11 (64-bit)
echo   • 2 GB RAM (4 GB empfohlen)
echo   • 500 MB Festplattenspeicher
echo.

echo 🚀 Installation:
echo ────────────────
echo   • Dauer: ca. 2-3 Minuten
echo   • Admin-Rechte erforderlich
echo   • Automatische Lizenzaktivierung
echo.

echo 🔒 Lizenzierung:
echo ────────────────
echo   • Verschiedene Pakete für unterschiedliche Teamgrößen
echo   • Kostenlose Updates für 1 Jahr
echo   • Professioneller Support
echo.

echo 💬 Support:
echo ───────────
echo   Email: support@materialmanager.de
echo   Website: www.materialmanager.de
echo.

echo Drücke eine Taste zum Fortfahren...
pause >nul
goto MENU

:SYSTEMCHECK
cls
color 0B
echo.
echo ╔════════════════════════════════════════════════════════════════════════╗
echo ║  🔧 SYSTEMCHECK                                                       ║
echo ╚════════════════════════════════════════════════════════════════════════╝
echo.

echo Prüfe Systemanforderungen...
echo.

REM Windows Version
echo [1] Betriebssystem...
for /f "tokens=*" %%A in ('systeminfo ^| find "OS Name"') do (
    echo     %%A
)
echo.

REM RAM
echo [2] Arbeitsspeicher...
for /f "tokens=*" %%A in ('systeminfo ^| find "Total Physical Memory"') do (
    echo     %%A
)
echo.

REM Festplatte
echo [3] Festplattenspeicher...
for /f "tokens=*" %%A in ('wmic LogicalDisk get Name,Size ^| find "C:"') do (
    echo     %%A
)
echo.

REM .NET Check
echo [4] .NET Framework...
if exist "C:\Program Files\dotnet" (
    echo     ✅ .NET Runtime ist installiert
) else (
    echo     ⚠️  .NET Runtime nicht gefunden (wird mit Installer installiert)
)
echo.

REM Admin Rights
echo [5] Admin-Rechte...
net session >nul 2>&1
if %errorlevel% equ 0 (
    echo     ✅ Administrator-Rechte verfügbar
) else (
    echo     ⚠️  Keine Admin-Rechte - werden für Installation benötigt
)
echo.

echo ════════════════════════════════════════════════════════════════════════
echo ✅ Systemcheck abgeschlossen!
echo.

echo Drücke eine Taste zum Fortfahren...
pause >nul
goto MENU

:END
cls
echo.
echo ╔════════════════════════════════════════════════════════════════════════╗
echo ║                                                                        ║
echo ║  Vielen Dank für die Nutzung von MaterialManager R03!                ║
echo ║                                                                        ║
echo ║  Weitere Informationen: www.materialmanager.de                       ║
echo ║  Support: support@materialmanager.de                                 ║
echo ║                                                                        ║
echo ║  Version 1.0.0 - © 2025 MaterialManager                             ║
echo ║                                                                        ║
echo ╚════════════════════════════════════════════════════════════════════════╝
echo.
endlocal
exit /b 0
