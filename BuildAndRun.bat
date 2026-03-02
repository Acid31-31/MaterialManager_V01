@echo off
REM MaterialManager R03 - Start nach Build Script
REM Beendet alte Instanz und startet die neue

echo.
echo ════════════════════════════════════════════════════
echo   MaterialManager R03 - Build & Run
echo ════════════════════════════════════════════════════
echo.

REM Alte Instanz beenden
echo [1/3] Beende alte Instanz...
taskkill /F /IM MaterialManager_R03.exe 2>nul
timeout /t 1 /nobreak >nul

REM Build
echo [2/3] Build-Vorgang startet...
dotnet build --configuration Debug

REM Check ob Build erfolgreich
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Build fehlgeschlagen!
    pause
    exit /b 1
)

REM App starten
echo [3/3] Starte MaterialManager R03...
timeout /t 1 /nobreak >nul
start "" "bin\Debug\net8.0-windows\win-x64\MaterialManager_R03.exe"

echo.
echo ✓ MaterialManager R03 gestartet!
echo ════════════════════════════════════════════════════
echo.
pause
