@echo off
REM â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
REM  SCHNELLE LÃ–SUNG: Bereinige das Projekt
REM â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

title MaterialManager 1.0.x - Projekt bereinigen
color 0C
chcp 65001 >nul

cls
echo.
echo â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
echo â•‘          PROJEKT WIRD BEREINIGT - Fehler werden automatisch behoben          â•‘
echo â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

echo [1] Beende Visual Studio und MaterialManager Prozesse...
taskkill /F /IM devenv.exe 2>nul
taskkill /F /IM MaterialManager_1.0.x.exe 2>nul
timeout /t 2 /nobreak >nul

echo [âœ“] Prozesse beendet
echo.

echo [2] LÃ¶sche Compile-Output (bin/obj)...
if exist "bin" (
    rmdir /S /Q "bin"
    echo [âœ“] bin gelÃ¶scht
)
if exist "obj" (
    rmdir /S /Q "obj"
    echo [âœ“] obj gelÃ¶scht
)
echo.

echo [3] Ã–ffne Visual Studio neu...
echo.
echo Bitte WARTE - Visual Studio wird geÃ¶ffnet...
timeout /t 2 /nobreak >nul

REM Ã–ffne Solution
start "" "MaterialManager_1.0.x.sln"

echo.
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.
echo IN VISUAL STUDIO:
echo.
echo 1. Warte bis Projekt geladen ist (30 Sekunden)
echo.
echo 2. Gehe zu: Tools > NuGet Package Manager > Package Manager Console
echo.
echo 3. FÃ¼hre aus: Update-Package -Reinstall
echo.
echo 4. Gehe zu: Build > Clean Solution
echo.
echo 5. Gehe zu: Build > Rebuild Solution
echo.
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

pause

