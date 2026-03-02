@echo off
REM ╔════════════════════════════════════════════════════════════════╗
REM ║  AUTOMATISCHES BACKUP SYSTEM                                  ║
REM ║  Erstellt nach jedem Change ein Backup                        ║
REM ╚════════════════════════════════════════════════════════════════╝

setlocal enabledelayedexpansion

REM Backup-Ordner
set BACKUP_ROOT=C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03\Backups
set TIMESTAMP=%date:~10,4%%date:~4,2%%date:~7,2%_%time:~0,2%%time:~3,2%%time:~6,2%
set BACKUP_DIR=%BACKUP_ROOT%\Backup_%TIMESTAMP%

REM Erstelle Backup-Ordner
if not exist "%BACKUP_ROOT%" mkdir "%BACKUP_ROOT%"
if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"

echo.
echo ╔════════════════════════════════════════════════════════════════╗
echo ║  📦 BACKUP WIRD ERSTELLT...                                   ║
echo ╚════════════════════════════════════════════════════════════════╝
echo.

REM Kopiere wichtige Dateien
echo [1/6] Kopiere Source-Code...
xcopy "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03\*.cs" "%BACKUP_DIR%\Source\" /S /Y >nul 2>&1

echo [2/6] Kopiere XAML-Dateien...
xcopy "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03\Views\*.xaml" "%BACKUP_DIR%\Views\" /S /Y >nul 2>&1

echo [3/6] Kopiere Services...
xcopy "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03\Services\*" "%BACKUP_DIR%\Services\" /S /Y >nul 2>&1

echo [4/6] Kopiere Project-Files...
copy "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03\*.csproj" "%BACKUP_DIR%\" >nul 2>&1
copy "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03\*.sln" "%BACKUP_DIR%\" >nul 2>&1

echo [5/6] Kopiere Build-Dateien...
xcopy "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03\*.ps1" "%BACKUP_DIR%\" /Y >nul 2>&1
xcopy "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03\*.bat" "%BACKUP_DIR%\" /Y >nul 2>&1

echo [6/6] Kopiere Dokumentation...
xcopy "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03\*.txt" "%BACKUP_DIR%\" /Y >nul 2>&1
xcopy "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03\*.md" "%BACKUP_DIR%\" /Y >nul 2>&1

echo.
echo ✅ BACKUP ERSTELLT!
echo.
echo 📁 Backup-Pfad:
echo    %BACKUP_DIR%
echo.
echo 📊 Backup-Info:
for /f "tokens=*" %%A in ('dir "%BACKUP_DIR%" /s /b ^| find /c /v ""') do (
    echo    Dateien: %%A
)
echo.
echo ✅ Automatisches Backup ist AKTIVIERT!
echo.

REM Behalte nur die letzten 10 Backups
echo Bereinige alte Backups (behalte nur die 10 neuesten)...
cd /d "%BACKUP_ROOT%"
for /f "skip=10 tokens=*" %%A in ('dir /b /ad /o-d') do (
    rmdir "%%A" /s /q >nul 2>&1
)

echo ✅ Alte Backups bereinigt!
echo.
echo ════════════════════════════════════════════════════════════════
pause
