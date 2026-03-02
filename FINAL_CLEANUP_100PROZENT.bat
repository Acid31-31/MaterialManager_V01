@echo off
REM ═══════════════════════════════════════════════════════════════════════════════
REM  FINAL CLEANUP - Löscht alle Backup-Ordner und Compile-Cache
REM  © 2025 Alexander Hölzer - Alle Rechte vorbehalten
REM ═══════════════════════════════════════════════════════════════════════════════

title MaterialManager R03 - Finale Bereinigung
color 0B
chcp 65001 >nul

cls
echo.
echo ╔═══════════════════════════════════════════════════════════════════════════════╗
echo ║                      FINALE BEREINIGUNG - CLEANUP 100%% ║
echo ╚═══════════════════════════════════════════════════════════════════════════════╝
echo.

echo [1] Beende Visual Studio und MaterialManager-Prozesse...
taskkill /F /IM devenv.exe 2>nul
taskkill /F /IM MaterialManager_R03.exe 2>nul
timeout /t 2 /nobreak >nul

echo [✓] Prozesse beendet
echo.

echo [2] Lösche Compile-Output und Cache...
if exist "bin" (
    echo [DELETING] bin/
    rmdir /S /Q "bin"
)
if exist "obj" (
    echo [DELETING] obj/
    rmdir /S /Q "obj"
)

echo [✓] Compile-Output gelöscht
echo.

echo [3] Lösche Backup-Ordner...
if exist "Backup_Vor_Personalisierung_20260225_195211" (
    echo [DELETING] Backup_Vor_Personalisierung_20260225_195211/
    rmdir /S /Q "Backup_Vor_Personalisierung_20260225_195211"
)

echo [✓] Backup-Ordner gelöscht
echo.

echo [4] Öffne Visual Studio neu...
echo.
echo Bitte WARTE - Visual Studio wird geöffnet...
timeout /t 2 /nobreak >nul

REM Öffne Solution
start "" "MaterialManager_R03.sln"

echo.
echo ═══════════════════════════════════════════════════════════════════════════════
echo [✓] CLEANUP 100%% ABGESCHLOSSEN!
echo ═══════════════════════════════════════════════════════════════════════════════
echo.
echo IN VISUAL STUDIO:
echo.
echo  1. Warte bis Projekt geladen ist (30 Sekunden)
echo  2. Build > Clean Solution
echo  3. Build > Rebuild Solution
echo.
echo  ✅ Build sollte jetzt erfolgreich sein!
echo.

pause
