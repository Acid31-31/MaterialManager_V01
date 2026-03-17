@echo off
REM =====================================================================
REM MaterialManager 1.0.x - Direkter Launcher
REM Einfach nur: MaterialManager_V01.exe starten
REM =====================================================================

color 0A
cls

echo.
echo =====================================================================
echo   MaterialManager 1.0.x - Starter
echo =====================================================================
echo.

REM Versuche MaterialManager zu finden
if exist "USB_Installation\MaterialManager\MaterialManager_V01.exe" (
    echo   Starte MaterialManager (USB-Version)...
    echo.
    cd /d "USB_Installation\MaterialManager"
    start "" MaterialManager_V01.exe
    exit /b 0
)

if exist "MaterialManager\MaterialManager_V01.exe" (
    echo   Starte MaterialManager (Lokale Version)...
    echo.
    cd /d "MaterialManager"
    start "" MaterialManager_V01.exe
    exit /b 0
)

if exist "MaterialManager_V01.exe" (
    echo   Starte MaterialManager...
    echo.
    start "" MaterialManager_V01.exe
    exit /b 0
)

REM Wenn nicht gefunden - Fehler
echo   FEHLER: MaterialManager_V01.exe nicht gefunden!
echo.
echo   Bitte prüfe:
echo   - USB_Installation\MaterialManager\MaterialManager_V01.exe
echo   - Oder: MaterialManager\MaterialManager_V01.exe
echo.
pause
exit /b 1
