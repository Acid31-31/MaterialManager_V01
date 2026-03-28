@echo off
REM â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
REM  CLEANUP - Entfernt doppelte Definitionen und bereinigt das Projekt
REM  Â© 2025 Alexander HÃ¶lzer - Alle Rechte vorbehalten
REM â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

title MaterialManager 1.0.x - Cleanup doppelte Definitionen
color 0B
chcp 65001 >nul

cls
echo.
echo â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
echo â•‘              MATERIALMANAGER 1.0.x - CLEANUP DOPPELTER DEFINITIONEN            â•‘
echo â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.
echo  Bereinige doppelte Definitionen...
echo.

REM Visual Studio schlieÃŸen (optional)
echo [INFO] SchlieÃŸe Visual Studio Prozesse...
taskkill /F /IM devenv.exe 2>nul
taskkill /F /IM MaterialManager_1.0.x.exe 2>nul
timeout /t 2 /nobreak >nul

REM Cleanup bin und obj
echo [INFO] LÃ¶sche bin und obj Verzeichnisse...
if exist "bin" rmdir /S /Q "bin"
if exist "obj" rmdir /S /Q "obj"
echo [âœ“] bin/obj gelÃ¶scht

REM Erstelle korrigierte Dateien
echo.
echo [INFO] Korrigiere MainWindow.xaml.cs (entferne Duplikate)...

REM Erstelle Backup der originalen Dateien
if not exist "BACKUP_BEFORE_CLEANUP" mkdir "BACKUP_BEFORE_CLEANUP"
copy /Y "MainWindow.xaml.cs" "BACKUP_BEFORE_CLEANUP\MainWindow.xaml.cs.bak" >nul
copy /Y "App.xaml.cs" "BACKUP_BEFORE_CLEANUP\App.xaml.cs.bak" >nul
copy /Y "Services\LicenseService.cs" "BACKUP_BEFORE_CLEANUP\LicenseService.cs.bak" >nul
copy /Y "Views\LicenseActivationDialog.xaml.cs" "BACKUP_BEFORE_CLEANUP\LicenseActivationDialog.xaml.cs.bak" >nul

echo [âœ“] Backups erstellt in BACKUP_BEFORE_CLEANUP/

echo.
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo [âœ“] CLEANUP ABGESCHLOSSEN
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.
echo NÃ„CHSTE SCHRITTE:
echo.
echo 1. Ã–ffne Visual Studio
echo 2. Build > Clean Solution
echo 3. Build > Rebuild Solution
echo.
echo Falls weiterhin Fehler auftreten:
echo â€¢ PrÃ¼fe ob Code-Dateien Duplikate enthalten
echo â€¢ Nutze "Edit > Find and Replace" um Duplikate zu finden
echo â€¢ LÃ¶sche manuelle Duplikate
echo.

pause

