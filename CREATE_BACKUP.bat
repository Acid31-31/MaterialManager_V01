@echo off
REM â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
REM â•‘  AUTOMATISCHES BACKUP SYSTEM                                  â•‘
REM â•‘  Erstellt nach jedem Change ein Backup                        â•‘
REM â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

setlocal enabledelayedexpansion

REM Backup-Ordner
set BACKUP_ROOT=C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x\Backups
set TIMESTAMP=%date:~10,4%%date:~4,2%%date:~7,2%_%time:~0,2%%time:~3,2%%time:~6,2%
set BACKUP_DIR=%BACKUP_ROOT%\Backup_%TIMESTAMP%

REM Erstelle Backup-Ordner
if not exist "%BACKUP_ROOT%" mkdir "%BACKUP_ROOT%"
if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"

echo.
echo â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
echo â•‘  ðŸ“¦ BACKUP WIRD ERSTELLT...                                   â•‘
echo â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

REM Kopiere wichtige Dateien
echo [1/6] Kopiere Source-Code...
xcopy "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x\*.cs" "%BACKUP_DIR%\Source\" /S /Y >nul 2>&1

echo [2/6] Kopiere XAML-Dateien...
xcopy "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x\Views\*.xaml" "%BACKUP_DIR%\Views\" /S /Y >nul 2>&1

echo [3/6] Kopiere Services...
xcopy "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x\Services\*" "%BACKUP_DIR%\Services\" /S /Y >nul 2>&1

echo [4/6] Kopiere Project-Files...
copy "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x\*.csproj" "%BACKUP_DIR%\" >nul 2>&1
copy "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x\*.sln" "%BACKUP_DIR%\" >nul 2>&1

echo [5/6] Kopiere Build-Dateien...
xcopy "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x\*.ps1" "%BACKUP_DIR%\" /Y >nul 2>&1
xcopy "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x\*.bat" "%BACKUP_DIR%\" /Y >nul 2>&1

echo [6/6] Kopiere Dokumentation...
xcopy "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x\*.txt" "%BACKUP_DIR%\" /Y >nul 2>&1
xcopy "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x\*.md" "%BACKUP_DIR%\" /Y >nul 2>&1

echo.
echo âœ… BACKUP ERSTELLT!
echo.
echo ðŸ“ Backup-Pfad:
echo    %BACKUP_DIR%
echo.
echo ðŸ“Š Backup-Info:
for /f "tokens=*" %%A in ('dir "%BACKUP_DIR%" /s /b ^| find /c /v ""') do (
    echo    Dateien: %%A
)
echo.
echo âœ… Automatisches Backup ist AKTIVIERT!
echo.

REM Behalte nur die letzten 10 Backups
echo Bereinige alte Backups (behalte nur die 10 neuesten)...
cd /d "%BACKUP_ROOT%"
for /f "skip=10 tokens=*" %%A in ('dir /b /ad /o-d') do (
    rmdir "%%A" /s /q >nul 2>&1
)

echo âœ… Alte Backups bereinigt!
echo.
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
pause

