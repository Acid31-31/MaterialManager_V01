@echo off
REM â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
REM  FINAL CLEANUP - LÃ¶scht alle Backup-Ordner und Compile-Cache
REM  Â© 2025 Alexander HÃ¶lzer - Alle Rechte vorbehalten
REM â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

title MaterialManager 1.0.x - Finale Bereinigung
color 0B
chcp 65001 >nul

cls
echo.
echo â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
echo â•‘                      FINALE BEREINIGUNG - CLEANUP 100%% â•‘
echo â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

echo [1] Beende Visual Studio und MaterialManager-Prozesse...
taskkill /F /IM devenv.exe 2>nul
taskkill /F /IM MaterialManager_1.0.x.exe 2>nul
timeout /t 2 /nobreak >nul

echo [âœ“] Prozesse beendet
echo.

echo [2] LÃ¶sche Compile-Output und Cache...
if exist "bin" (
    echo [DELETING] bin/
    rmdir /S /Q "bin"
)
if exist "obj" (
    echo [DELETING] obj/
    rmdir /S /Q "obj"
)

echo [âœ“] Compile-Output gelÃ¶scht
echo.

echo [3] LÃ¶sche Backup-Ordner...
if exist "Backup_Vor_Personalisierung_20260225_195211" (
    echo [DELETING] Backup_Vor_Personalisierung_20260225_195211/
    rmdir /S /Q "Backup_Vor_Personalisierung_20260225_195211"
)

echo [âœ“] Backup-Ordner gelÃ¶scht
echo.

echo [4] Ã–ffne Visual Studio neu...
echo.
echo Bitte WARTE - Visual Studio wird geÃ¶ffnet...
timeout /t 2 /nobreak >nul

REM Ã–ffne Solution
start "" "MaterialManager_1.0.x.sln"

echo.
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo [âœ“] CLEANUP 100%% ABGESCHLOSSEN!
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.
echo IN VISUAL STUDIO:
echo.
echo  1. Warte bis Projekt geladen ist (30 Sekunden)
echo  2. Build > Clean Solution
echo  3. Build > Rebuild Solution
echo.
echo  âœ… Build sollte jetzt erfolgreich sein!
echo.

pause

