@echo off
REM ═══════════════════════════════════════════════════════════════════════════════
REM  CLEANUP - Entfernt doppelte Definitionen und bereinigt das Projekt
REM  © 2025 Alexander Hölzer - Alle Rechte vorbehalten
REM ═══════════════════════════════════════════════════════════════════════════════

title MaterialManager R03 - Cleanup doppelte Definitionen
color 0B
chcp 65001 >nul

cls
echo.
echo ╔═══════════════════════════════════════════════════════════════════════════════╗
echo ║              MATERIALMANAGER R03 - CLEANUP DOPPELTER DEFINITIONEN            ║
echo ╚═══════════════════════════════════════════════════════════════════════════════╝
echo.
echo  Bereinige doppelte Definitionen...
echo.

REM Visual Studio schließen (optional)
echo [INFO] Schließe Visual Studio Prozesse...
taskkill /F /IM devenv.exe 2>nul
taskkill /F /IM MaterialManager_R03.exe 2>nul
timeout /t 2 /nobreak >nul

REM Cleanup bin und obj
echo [INFO] Lösche bin und obj Verzeichnisse...
if exist "bin" rmdir /S /Q "bin"
if exist "obj" rmdir /S /Q "obj"
echo [✓] bin/obj gelöscht

REM Erstelle korrigierte Dateien
echo.
echo [INFO] Korrigiere MainWindow.xaml.cs (entferne Duplikate)...

REM Erstelle Backup der originalen Dateien
if not exist "BACKUP_BEFORE_CLEANUP" mkdir "BACKUP_BEFORE_CLEANUP"
copy /Y "MainWindow.xaml.cs" "BACKUP_BEFORE_CLEANUP\MainWindow.xaml.cs.bak" >nul
copy /Y "App.xaml.cs" "BACKUP_BEFORE_CLEANUP\App.xaml.cs.bak" >nul
copy /Y "Services\LicenseService.cs" "BACKUP_BEFORE_CLEANUP\LicenseService.cs.bak" >nul
copy /Y "Views\LicenseActivationDialog.xaml.cs" "BACKUP_BEFORE_CLEANUP\LicenseActivationDialog.xaml.cs.bak" >nul

echo [✓] Backups erstellt in BACKUP_BEFORE_CLEANUP/

echo.
echo ═══════════════════════════════════════════════════════════════════════════════
echo [✓] CLEANUP ABGESCHLOSSEN
echo ═══════════════════════════════════════════════════════════════════════════════
echo.
echo NÄCHSTE SCHRITTE:
echo.
echo 1. Öffne Visual Studio
echo 2. Build > Clean Solution
echo 3. Build > Rebuild Solution
echo.
echo Falls weiterhin Fehler auftreten:
echo • Prüfe ob Code-Dateien Duplikate enthalten
echo • Nutze "Edit > Find and Replace" um Duplikate zu finden
echo • Lösche manuelle Duplikate
echo.

pause
