@echo off
REM =====================================================================
REM MaterialManager 1.0.x - INSTALLATION AUF DIESEM PC
REM Mit Desktop-Verknüpfung
REM =====================================================================

setlocal enabledelayedexpansion
set "SCRIPT_DIR=%~dp0"
set "SOURCE_DIR=%SCRIPT_DIR%MaterialManager"
set "TARGET_DIR=C:\Program Files\MaterialManager_V01"
set "LEGACY_DIR=C:\Program Files\MaterialManager"

color 0A
cls

echo.
echo =====================================================================
echo   MaterialManager 1.0.x - INSTALLATION
echo =====================================================================
echo.

REM Prüfe Admin-Rechte
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo   FEHLER: Admin-Rechte erforderlich!
    echo.
    echo   Bitte: Rechtsklick auf diese Datei
    echo           "Als Administrator ausführen"
    echo.
    pause
    exit /b 1
)

echo   [1/4] Prüfe Programm-Dateien...
if not exist "%SOURCE_DIR%\MaterialManager_V01.exe" (
    echo   FEHLER: MaterialManager_V01.exe nicht gefunden!
    echo   Erwartet unter: %SOURCE_DIR%
    pause
    exit /b 1
)
echo   OK: Dateien gefunden

echo.
echo   [2/4] Kopiere nach: %TARGET_DIR%
if exist "%LEGACY_DIR%" (
    echo   Entferne alten Legacy-Ordner: %LEGACY_DIR%
    rmdir /s /q "%LEGACY_DIR%" 2>nul
)
if exist "%TARGET_DIR%" (
    echo   Lösche alte Installation...
    rmdir /s /q "%TARGET_DIR%" 2>nul
)
mkdir "%TARGET_DIR%"
xcopy "%SOURCE_DIR%\*" "%TARGET_DIR%" /E /I /Y >nul
if %errorlevel% neq 0 (
    echo   FEHLER beim Kopieren!
    pause
    exit /b 1
)
echo   OK: Kopiert

echo.
echo   [3/4] Erstelle Desktop-Verknüpfung...

REM PowerShell Shortcut erstellen
powershell -Command "^
[string]$SourceFilePath = '%TARGET_DIR%\MaterialManager_V01.exe'; ^
[string]$ShortcutPath = [Environment]::GetFolderPath('Desktop') + '\MaterialManager.lnk'; ^
$WshShell = New-Object -ComObject WScript.Shell; ^
$Shortcut = $WshShell.CreateShortcut($ShortcutPath); ^
$Shortcut.TargetPath = $SourceFilePath; ^
$Shortcut.WorkingDirectory = '%TARGET_DIR%'; ^
$Shortcut.Description = 'MaterialManager 1.0.x'; ^
$Shortcut.Save()"

if exist "%USERPROFILE%\Desktop\MaterialManager.lnk" (
    echo   OK: Verknüpfung auf Desktop erstellt
) else (
    echo   WARNUNG: Desktop-Verknüpfung konnte nicht erstellt werden
)

echo.
echo   [4/4] Starte Programm...
start "" "%TARGET_DIR%\MaterialManager_V01.exe"

echo.
echo =====================================================================
echo   Installation FERTIG!
echo =====================================================================
echo.
echo   MaterialManager ist jetzt installiert:
echo   %TARGET_DIR%\
echo.
echo   Verknüpfung auf Desktop vorhanden!
echo.
echo   Programm wird jetzt gestartet...
echo.
timeout /t 3 /nobreak
exit /b 0
