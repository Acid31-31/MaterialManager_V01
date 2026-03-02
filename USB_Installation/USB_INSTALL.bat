@echo off
REM ============================================
REM MaterialManager R03 - USB Installation
REM ============================================
setlocal enabledelayedexpansion

color 0A
cls

echo.
echo ============================================
echo MaterialManager R03 - USB Installation
echo ============================================
echo.

REM Detektiere USB-Pfad
set USB_SOURCE=%~dp0
set INSTALL_PATH=%PROGRAMFILES%\MaterialManager_R03

REM Prüfe Admin-Rechte
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo FEHLER: Dieses Skript benötigt Administrator-Rechte!
    echo.
    echo Bitte klicken Sie mit Rechtsklick auf "INSTALL.bat" und wählen
    echo Sie "Als Administrator ausführen"
    echo.
    pause
    exit /b 1
)

echo [1/5] Prüfe Systemanforderungen...
REM Prüfe Windows-Version
ver | find "10.0" >nul
if %errorlevel% neq 0 (
    echo FEHLER: Windows 10 oder höher erforderlich!
    pause
    exit /b 1
)

echo [2/5] Erstelle Installationsverzeichnis...
if not exist "%INSTALL_PATH%" (
    mkdir "%INSTALL_PATH%"
    echo ✓ Verzeichnis erstellt: %INSTALL_PATH%
) else (
    echo ✓ Verzeichnis existiert bereits
)

echo [3/5] Kopiere Dateien...
xcopy "%USB_SOURCE%MaterialManager_R03.exe" "%INSTALL_PATH%\" /Y /Q
xcopy "%USB_SOURCE%*.dll" "%INSTALL_PATH%\" /Y /Q /S

if %errorlevel% neq 0 (
    echo FEHLER beim Kopieren der Dateien!
    pause
    exit /b 1
)
echo ✓ Dateien kopiert

echo [4/5] Erstelle Windows-Shortcuts...
powershell -Command ^
"$WshShell = New-Object -ComObject WScript.Shell; ^
$Shortcut = $WshShell.CreateShortcut('%USERPROFILE%\Desktop\MaterialManager R03.lnk'); ^
$Shortcut.TargetPath = '%INSTALL_PATH%\MaterialManager_R03.exe'; ^
$Shortcut.WorkingDirectory = '%INSTALL_PATH%'; ^
$Shortcut.Save()"
echo ✓ Desktop-Verknüpfung erstellt

echo [5/5] Registriere im Windows StartMenu...
powershell -Command ^
"$AppPath = '%INSTALL_PATH%'; ^
$StartMenu = [System.IO.Path]::Combine([System.Environment]::GetFolderPath('StartMenu'), 'Programs'); ^
New-Item -ItemType Directory -Path $StartMenu -Force -ErrorAction SilentlyContinue; ^
$Shortcut = (New-Object -ComObject WScript.Shell).CreateShortcut((Join-Path $StartMenu 'MaterialManager R03.lnk')); ^
$Shortcut.TargetPath = (Join-Path $AppPath 'MaterialManager_R03.exe'); ^
$Shortcut.Save()"
echo ✓ Startmenü-Verknüpfung erstellt

echo.
echo ============================================
echo Installation abgeschlossen!
echo ============================================
echo.
echo MaterialManager R03 wurde installiert in:
echo %INSTALL_PATH%
echo.
echo Sie können die Anwendung starten von:
echo - Desktop-Verknüpfung "MaterialManager R03"
echo - Windows StartMenu
echo.
echo.
pause
