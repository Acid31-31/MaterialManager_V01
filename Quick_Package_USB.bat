@echo off
REM ═══════════════════════════════════════════════════════════════
REM  MaterialManager R03 - USB-Ready Package Creator
REM  SCHNELLVERSION - Direkt ZIP erstellen
REM ═══════════════════════════════════════════════════════════════

setlocal enabledelayedexpansion

cd /d "%~dp0"

echo.
echo [1/4] Kompiliere Release-Version...
dotnet build --configuration Release --no-incremental >nul 2>&1

if %ERRORLEVEL% NEQ 0 (
    echo FEHLER beim Build!
    pause
    exit /b 1
)

echo [2/4] Erstelle Arbeitsordner...
set "TEMP_DIR=%TEMP%\MaterialManager_R03_USB"
if exist "%TEMP_DIR%" rmdir /s /q "%TEMP_DIR%" >nul 2>&1
mkdir "%TEMP_DIR%"

echo [3/4] Kopiere Dateien...
xcopy /Y /I "bin\Release\net8.0-windows\win-x64\*.exe" "%TEMP_DIR%\" >nul 2>&1
xcopy /Y /I "bin\Release\net8.0-windows\win-x64\*.dll" "%TEMP_DIR%\" >nul 2>&1
xcopy /Y /I "bin\Release\net8.0-windows\win-x64\*.json" "%TEMP_DIR%\" >nul 2>&1

copy /Y "Install_Release.bat" "%TEMP_DIR%\Install.bat" >nul 2>&1
copy /Y "PRESENTATION_EMAIL.md" "%TEMP_DIR%\" >nul 2>&1
copy /Y "PRESENTATION_CHECKLIST.md" "%TEMP_DIR%\" >nul 2>&1

REM Willkommens-Datei erstellen
(
    echo MaterialManager R03 - Presentation Edition v1.0.0
    echo.
    echo Inhalt:
    echo - MaterialManager_R03.exe ^(Hauptprogramm^)
    echo - Install.bat ^(Installer^)
    echo - PRESENTATION_EMAIL.md ^(Email-Text^)
    echo - PRESENTATION_CHECKLIST.md ^(Demo-Vorbereitung^)
    echo.
    echo Installation:
    echo 1. Doppelklick: Install.bat
    echo 2. Warten auf Meldung
    echo 3. Fertig!
) > "%TEMP_DIR%\README_FIRST.txt"

echo [4/4] Erstelle ZIP...
set "ZIP_FILE=%CD%\MaterialManager_R03_USB_Presentation.zip"

powershell -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; [System.IO.Compression.ZipFile]::CreateFromDirectory('%TEMP_DIR%', '%ZIP_FILE%')" >nul 2>&1

rmdir /s /q "%TEMP_DIR%" >nul 2>&1

echo.
echo ═══════════════════════════════════════════════════════════════
echo.
if exist "%ZIP_FILE%" (
    echo FERTIG! ZIP-DATEI ERSTELLT
    echo.
    echo Dateiname: MaterialManager_R03_USB_Presentation.zip
    echo Speicherort: %ZIP_FILE%
    echo.
    echo Ordner öffnet sich jetzt...
    echo.
) else (
    echo FEHLER beim ZIP-Erstellen!
)
echo ═══════════════════════════════════════════════════════════════

REM Ordner öffnen
start "" "%CD%"

pause
