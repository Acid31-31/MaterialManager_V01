@echo off
REM ═══════════════════════════════════════════════════════════════
REM  MaterialManager R03 - Complete Presentation Package Creator
REM  Erstellt eine fertige ZIP-Datei für Präsentation
REM ═══════════════════════════════════════════════════════════════

setlocal enabledelayedexpansion

cls
echo.
echo ╔═══════════════════════════════════════════════════════════════╗
echo ║                                                               ║
echo ║     MaterialManager R03 - Presentation Package Creator       ║
echo ║                   v1.0.0                                     ║
echo ║                                                               ║
echo ╚═══════════════════════════════════════════════════════════════╝
echo.

REM Temporärer Ordner
set "TEMP_DIR=%CD%\MaterialManager_R03_RELEASE"
set "ZIP_NAME=MaterialManager_R03_Presentation_%date:~-4%%date:~-10,2%%date:~-7,2%.zip"

echo [1/6] Räume alte Build-Dateien auf...
if exist "%TEMP_DIR%" rmdir /s /q "%TEMP_DIR%" 2>nul
mkdir "%TEMP_DIR%"
echo     ✓ Ordner erstellt

echo.
echo [2/6] Kompiliere Release-Version...
dotnet build --configuration Release --no-incremental >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo     ❌ Build fehlgeschlagen!
    pause
    exit /b 1
)
echo     ✓ Release-Build erfolgreich

echo.
echo [3/6] Kopiere Dateien zusammen...

REM Kopiere Release-Binaries
xcopy /Y /I "bin\Release\net8.0-windows\win-x64\*.exe" "%TEMP_DIR%\" >nul 2>&1
xcopy /Y /I "bin\Release\net8.0-windows\win-x64\*.dll" "%TEMP_DIR%\" >nul 2>&1
xcopy /Y /I "bin\Release\net8.0-windows\win-x64\*.json" "%TEMP_DIR%\" >nul 2>&1

REM Kopiere Presentations-Dateien
copy /Y "PRESENTATION_EMAIL.md" "%TEMP_DIR%\" >nul 2>&1
copy /Y "PRESENTATION_CHECKLIST.md" "%TEMP_DIR%\" >nul 2>&1
copy /Y "Install_Release.bat" "%TEMP_DIR%\Install.bat" >nul 2>&1

REM Kopiere Dokumentation
copy /Y "README_DEMO.md" "%TEMP_DIR%\README_DE.md" >nul 2>&1
copy /Y "NETZWERK_SYNC_ANLEITUNG.md" "%TEMP_DIR%\NETZWERK_ANLEITUNG.md" >nul 2>&1

REM Erstelle Quick-Start
(
    echo @echo off
    echo title MaterialManager R03
    echo start "" MaterialManager_R03.exe
) > "%TEMP_DIR%\START.bat"

echo     ✓ Dateien kopiert

echo.
echo [4/6] Erstelle Willkommens-Info...

(
    echo ╔═══════════════════════════════════════════════════════════════╗
    echo ║                                                               ║
    echo ║     MaterialManager R03 - Presentation Version v1.0.0        ║
    echo ║                    Dezember 2024                             ║
    echo ║                                                               ║
    echo ╚═══════════════════════════════════════════════════════════════╝
    echo.
    echo 📦 INHALT DIESES PACKAGES:
    echo.
    echo  ✓ MaterialManager_R03.exe           (Hauptprogramm^)
    echo  ✓ Alle notwendigen Libraries        (DLLs^)
    echo  ✓ Install.bat                       (Automatischer Installer^)
    echo  ✓ START.bat                         (Schnellstart^)
    echo  ✓ PRESENTATION_EMAIL.md             (Email-Text für Einladung^)
    echo  ✓ PRESENTATION_CHECKLIST.md         (Demo-Vorbereitung^)
    echo  ✓ README_DE.md                      (Deutsche Anleitung^)
    echo  ✓ NETZWERK_ANLEITUNG.md             (Multi-PC Setup^)
    echo.
    echo 🚀 INSTALLATION ^(3 Schritte^):
    echo.
    echo  1. Doppelklick: Install.bat
    echo  2. Warten auf Meldung "Installation erfolgreich"
    echo  3. Starten: START.bat
    echo.
    echo 📊 SYSTEMANFORDERUNGEN:
    echo.
    echo  • Windows 10 / 11 / Server
    echo  • .NET 8.0 Runtime ^(wird automatisch geprüft^)
    echo  • RAM: 512 MB
    echo  • Festplatte: 50 MB
    echo.
    echo 💡 TIPPS FÜR PRÄSENTATION:
    echo.
    echo  • Testen Sie Install.bat VORHER!
    echo  • Demo-Materialien hinzufügen
    echo  • Bildschirmgröße 1600x900
    echo  • Netzwerk-Sync Dialog zeigen
    echo.
    echo 📧 VERSAND:
    echo.
    echo  1. Sende diese ZIP-Datei
    echo  2. Schreibe Email nach: PRESENTATION_EMAIL.md
    echo  3. Anhang: Install.bat, README_DE.md
    echo.
    echo ═══════════════════════════════════════════════════════════════
    echo Viel Erfolg bei der Präsentation! 🎉
    echo ═══════════════════════════════════════════════════════════════
) > "%TEMP_DIR%\README_FIRST.txt"

echo     ✓ Info-Datei erstellt

echo.
echo [5/6] Erstelle ZIP-Datei...

REM ZIP erstellen mit PowerShell
powershell -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; [System.IO.Compression.ZipFile]::CreateFromDirectory('%TEMP_DIR%', '%ZIP_NAME%')" >nul 2>&1

if exist "%ZIP_NAME%" (
    echo     ✓ ZIP erstellt: %ZIP_NAME%
) else (
    echo     ❌ ZIP-Erstellung fehlgeschlagen!
    pause
    exit /b 1
)

echo.
echo [6/6] Räume auf...
rmdir /s /q "%TEMP_DIR%" >nul 2>&1
echo     ✓ Fertig

echo.
echo ═══════════════════════════════════════════════════════════════
echo.
echo ✅ PACKAGE FERTIG!
echo.
echo 📦 Dateiname:
echo    %ZIP_NAME%
echo.
echo 📍 Speicherort:
echo    %CD%
echo.
echo 📊 Größe: Siehe Datei-Eigenschaften
echo.
echo 🚀 Nächste Schritte:
echo    1. ZIP-Datei herunterladen
echo    2. An Audience versenden
echo    3. PRESENTATION_EMAIL.md als Email-Text verwenden
echo.
echo 📋 Checkliste vor Präsentation:
echo    ☐ Install.bat testen
echo    ☐ Demo-Daten vorbereiten
echo    ☐ Bildschirm-Größe prüfen
echo    ☐ Beamer/Streaming testen
echo.
echo ═══════════════════════════════════════════════════════════════
echo.
pause
explorer "%CD%"
