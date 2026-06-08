@echo off
REM ============================================
REM MaterialManager 1.0.x - USB Installation
REM ============================================
setlocal enabledelayedexpansion

color 0A
cls

echo.
echo ============================================
echo MaterialManager 1.0.x - USB Installation
echo ============================================
echo.

REM Detektiere USB-Pfad
set USB_SOURCE=%~dp0
set INSTALL_PATH=%PROGRAMFILES%\MaterialManager_1.0.x

REM PrÃ¼fe Admin-Rechte
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo FEHLER: Dieses Skript benÃ¶tigt Administrator-Rechte!
    echo.
    echo Bitte klicken Sie mit Rechtsklick auf "INSTALL.bat" und wÃ¤hlen
    echo Sie "Als Administrator ausfÃ¼hren"
    echo.
    pause
    exit /b 1
)

echo [1/5] PrÃ¼fe Systemanforderungen...
REM PrÃ¼fe Windows-Version
ver | find "10.0" >nul
if %errorlevel% neq 0 (
    echo FEHLER: Windows 10 oder hÃ¶her erforderlich!
    pause
    exit /b 1
)

echo [2/5] Erstelle Installationsverzeichnis...
if not exist "%INSTALL_PATH%" (
    mkdir "%INSTALL_PATH%"
    echo âœ“ Verzeichnis erstellt: %INSTALL_PATH%
) else (
    echo âœ“ Verzeichnis existiert bereits
)

echo [3/5] Kopiere Dateien...
xcopy "%USB_SOURCE%MaterialManager_1.0.x.exe" "%INSTALL_PATH%\" /Y /Q
xcopy "%USB_SOURCE%*.dll" "%INSTALL_PATH%\" /Y /Q /S
if exist "%USB_SOURCE%license.dat" copy /Y "%USB_SOURCE%license.dat" "%INSTALL_PATH%\license.dat" >nul

if %errorlevel% neq 0 (
    echo FEHLER beim Kopieren der Dateien!
    pause
    exit /b 1
)
echo âœ“ Dateien kopiert

echo [4/5] Erstelle Windows-Shortcuts...
powershell -Command ^
"$WshShell = New-Object -ComObject WScript.Shell; ^
$Shortcut = $WshShell.CreateShortcut('%USERPROFILE%\Desktop\MaterialManager 1.0.x.lnk'); ^
$Shortcut.TargetPath = '%INSTALL_PATH%\MaterialManager_1.0.x.exe'; ^
$Shortcut.WorkingDirectory = '%INSTALL_PATH%'; ^
$Shortcut.Save()"
echo âœ“ Desktop-VerknÃ¼pfung erstellt

echo [5/5] Registriere im Windows StartMenu...
powershell -Command ^
"$AppPath = '%INSTALL_PATH%'; ^
$StartMenu = [System.IO.Path]::Combine([System.Environment]::GetFolderPath('StartMenu'), 'Programs'); ^
New-Item -ItemType Directory -Path $StartMenu -Force -ErrorAction SilentlyContinue; ^
$Shortcut = (New-Object -ComObject WScript.Shell).CreateShortcut((Join-Path $StartMenu 'MaterialManager 1.0.x.lnk')); ^
$Shortcut.TargetPath = (Join-Path $AppPath 'MaterialManager_1.0.x.exe'); ^
$Shortcut.Save()"
echo âœ“ StartmenÃ¼-VerknÃ¼pfung erstellt

echo.
echo ============================================
echo Installation abgeschlossen!
echo ============================================
echo.
echo MaterialManager 1.0.x wurde installiert in:
echo %INSTALL_PATH%
echo.
echo Sie kÃ¶nnen die Anwendung starten von:
echo - Desktop-VerknÃ¼pfung "MaterialManager 1.0.x"
echo - Windows StartMenu
echo.
echo.
pause

