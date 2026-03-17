@echo off
REM =====================================================================
REM MaterialManager 1.0.x - Direkter Launcher
REM Einfach nur: MaterialManager_V01.exe starten
REM =====================================================================

setlocal
set "SCRIPT_DIR=%~dp0"
set "USB_APP_DIR=%SCRIPT_DIR%MaterialManager"

color 0A
cls

echo.
echo =====================================================================
echo   MaterialManager 1.0.x - Starter
echo =====================================================================
echo.

if exist "%USB_APP_DIR%\MaterialManager_V01.exe" (
    echo   Starte MaterialManager (USB-Version)...
    echo.
    cd /d "%USB_APP_DIR%"
    start "" MaterialManager_V01.exe
    exit /b 0
)

if exist "%SCRIPT_DIR%MaterialManager_V01.exe" (
    echo   Starte MaterialManager...
    echo.
    cd /d "%SCRIPT_DIR%"
    start "" MaterialManager_V01.exe
    exit /b 0
)

REM Wenn nicht gefunden - Fehler
echo   FEHLER: MaterialManager_V01.exe nicht gefunden!
echo.
echo   Bitte prüfe:
echo   - %USB_APP_DIR%\MaterialManager_V01.exe
echo.
pause
exit /b 1
