@echo off
REM MaterialManager 1.0.x - Start nach Build Script
REM Beendet alte Instanz und startet die neue

echo.
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo   MaterialManager 1.0.x - Build & Run
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

REM Alte Instanz beenden
echo [1/3] Beende alte Instanz...
taskkill /F /IM MaterialManager_1.0.x.exe 2>nul
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
echo [3/3] Starte MaterialManager 1.0.x...
timeout /t 1 /nobreak >nul
start "" "bin\Debug\net8.0-windows\win-x64\MaterialManager_1.0.x.exe"

echo.
echo âœ“ MaterialManager 1.0.x gestartet!
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.
pause

