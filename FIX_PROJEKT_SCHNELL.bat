@echo off
REM ═══════════════════════════════════════════════════════════════════════════════
REM  SCHNELLE LÖSUNG: Bereinige das Projekt
REM ═══════════════════════════════════════════════════════════════════════════════

title MaterialManager R03 - Projekt bereinigen
color 0C
chcp 65001 >nul

cls
echo.
echo ╔═══════════════════════════════════════════════════════════════════════════════╗
echo ║          PROJEKT WIRD BEREINIGT - Fehler werden automatisch behoben          ║
echo ╚═══════════════════════════════════════════════════════════════════════════════╝
echo.

echo [1] Beende Visual Studio und MaterialManager Prozesse...
taskkill /F /IM devenv.exe 2>nul
taskkill /F /IM MaterialManager_R03.exe 2>nul
timeout /t 2 /nobreak >nul

echo [✓] Prozesse beendet
echo.

echo [2] Lösche Compile-Output (bin/obj)...
if exist "bin" (
    rmdir /S /Q "bin"
    echo [✓] bin gelöscht
)
if exist "obj" (
    rmdir /S /Q "obj"
    echo [✓] obj gelöscht
)
echo.

echo [3] Öffne Visual Studio neu...
echo.
echo Bitte WARTE - Visual Studio wird geöffnet...
timeout /t 2 /nobreak >nul

REM Öffne Solution
start "" "MaterialManager_R03.sln"

echo.
echo ═══════════════════════════════════════════════════════════════════════════════
echo.
echo IN VISUAL STUDIO:
echo.
echo 1. Warte bis Projekt geladen ist (30 Sekunden)
echo.
echo 2. Gehe zu: Tools > NuGet Package Manager > Package Manager Console
echo.
echo 3. Führe aus: Update-Package -Reinstall
echo.
echo 4. Gehe zu: Build > Clean Solution
echo.
echo 5. Gehe zu: Build > Rebuild Solution
echo.
echo ═══════════════════════════════════════════════════════════════════════════════
echo.

pause
