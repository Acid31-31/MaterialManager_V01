@echo off
REM â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
REM â•‘  KOMPLETTE USB-INSTALLATION - AUTOMATISCH!                           â•‘
REM â•‘  Erstellt das komplette MaterialManager_1.0.x Programm fÃ¼r USB         â•‘
REM â•‘  KEINE Shell-Befehle - KEINE alleinstehenden Dateien                 â•‘
REM â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

setlocal enabledelayedexpansion

cls
color 0A

echo.
echo â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
echo â•‘                                                                        â•‘
echo â•‘     ðŸš€ MATERIALMANAGER 1.0.x - KOMPLETTE USB INSTALLATION              â•‘
echo â•‘                                                                        â•‘
echo â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

REM Pfade
set PROJECT_ROOT=C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x
set USB_INSTALLATION=%PROJECT_ROOT%\USB_Installation
set BUILD_OUTPUT=%PROJECT_ROOT%\bin\Release\net8.0-windows\win-x64
set BACKUP_ROOT=%PROJECT_ROOT%\Backups
set TIMESTAMP=%date:~10,4%%date:~4,2%%date:~7,2%_%time:~0,2%%time:~3,2%%time:~6,2%
set BACKUP_DIR=%BACKUP_ROOT%\USB_Build_%TIMESTAMP%

REM â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo [SCHRITT 1/5] Erstelle Backup...
echo â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

if not exist "%BACKUP_ROOT%" mkdir "%BACKUP_ROOT%"
if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"

echo Kopiere Source-Code...
xcopy "%PROJECT_ROOT%\Services\*" "%BACKUP_DIR%\Services\" /S /Y /Q >nul 2>&1
xcopy "%PROJECT_ROOT%\Views\*" "%BACKUP_DIR%\Views\" /S /Y /Q >nul 2>&1
copy "%PROJECT_ROOT%\*.csproj" "%BACKUP_DIR%\" /Y >nul 2>&1
copy "%PROJECT_ROOT%\*.sln" "%BACKUP_DIR%\" /Y >nul 2>&1

echo âœ… Backup erstellt: %BACKUP_DIR%
echo.

REM â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo [SCHRITT 2/5] Cleanup alte Build-Dateien...
echo â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

if exist "%PROJECT_ROOT%\bin" (
    echo LÃ¶sche alte bin-Dateien...
    rmdir /s /q "%PROJECT_ROOT%\bin" >nul 2>&1
)

if exist "%PROJECT_ROOT%\obj" (
    echo LÃ¶sche alte obj-Dateien...
    rmdir /s /q "%PROJECT_ROOT%\obj" >nul 2>&1
)

echo âœ… Cleanup abgeschlossen
echo.

REM â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo [SCHRITT 3/5] Baue Programm (Release-Modus)...
echo â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
echo.
echo Bitte warten... Dies kann 2-5 Minuten dauern!
echo.

cd /d "%PROJECT_ROOT%"
dotnet build -c Release -p:Platform=x64 -p:SelfContained=true -p:RuntimeIdentifier=win-x64 --no-restore --verbosity minimal

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo âŒ BUILD FEHLER!
    echo Der Build ist fehlgeschlagen. PrÃ¼fe die Ausgabe oben.
    pause
    exit /b 1
)

echo âœ… Build erfolgreich abgeschlossen
echo.

REM â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo [SCHRITT 4/5] Kopiere Dateien zu USB_Installation...
echo â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

REM Erstelle Programm-Ordner wenn nicht vorhanden
if not exist "%USB_INSTALLATION%\Programm" mkdir "%USB_INSTALLATION%\Programm"

echo Kopiere Hauptprogramm (MaterialManager_1.0.x.exe)...
if exist "%BUILD_OUTPUT%\MaterialManager_1.0.x.exe" (
    copy "%BUILD_OUTPUT%\MaterialManager_1.0.x.exe" "%USB_INSTALLATION%\Programm\" /Y >nul 2>&1
    echo   âœ… MaterialManager_1.0.x.exe
) else (
    echo   âŒ MaterialManager_1.0.x.exe nicht gefunden!
)

echo Kopiere DLL-AbhÃ¤ngigkeiten...
if exist "%BUILD_OUTPUT%\*.dll" (
    copy "%BUILD_OUTPUT%\*.dll" "%USB_INSTALLATION%\Programm\" /Y >nul 2>&1
    echo   âœ… Alle DLL-Dateien kopiert
)

echo Kopiere Runtime-Dateien...
if exist "%BUILD_OUTPUT%\*" (
    for /f "tokens=*" %%A in ('dir /b "%BUILD_OUTPUT%\"') do (
        if "%%A" neq "MaterialManager_1.0.x.exe" if not "%%A:~-4%%"==".dll" (
            if exist "%BUILD_OUTPUT%\%%A\" (
                xcopy "%BUILD_OUTPUT%\%%A" "%USB_INSTALLATION%\Programm\%%A" /S /Y /Q >nul 2>&1
            ) else (
                copy "%BUILD_OUTPUT%\%%A" "%USB_INSTALLATION%\Programm\" /Y >nul 2>&1
            )
        )
    )
)

echo âœ… Dateien zu USB_Installation kopiert
echo.

REM â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo [SCHRITT 5/5] Verifiziere USB_Installation...
echo â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

set ERRORS=0

if not exist "%USB_INSTALLATION%\Programm\MaterialManager_1.0.x.exe" (
    echo âŒ MaterialManager_1.0.x.exe fehlt!
    set ERRORS=1
) else (
    echo âœ… MaterialManager_1.0.x.exe vorhanden
)

if not exist "%USB_INSTALLATION%\Installer.exe" (
    echo âš ï¸  Installer.exe fehlt (wird bei der Installation erstellt)
) else (
    echo âœ… Installer.exe vorhanden
)

if not exist "%USB_INSTALLATION%\Anleitung\QUICK_START.md" (
    echo âš ï¸  Anleitung fehlt
) else (
    echo âœ… Anleitung vorhanden
)

echo.
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

if %ERRORS% EQU 0 (
    echo âœ… KOMPLETTE USB-INSTALLATION ERSTELLT!
    echo.
    echo ðŸ“ Programm-Dateien:
    echo    %USB_INSTALLATION%\Programm\
    echo.
    echo ðŸ“‹ Ordnerstruktur:
    echo    USB_Installation/
    echo    â”œâ”€ Programm/
    echo    â”‚  â”œâ”€ MaterialManager_1.0.x.exe  âœ…
    echo    â”‚  â”œâ”€ *.dll Dateien           âœ…
    echo    â”‚  â””â”€ Runtime-Dateien         âœ…
    echo    â”œâ”€ Anleitung/
    echo    â”œâ”€ Tools/
    echo    â”œâ”€ Installer.exe (spÃ¤ter)
    echo    â””â”€ README.md
    echo.
    echo ðŸš€ NÃ„CHSTER SCHRITT: USB-Stick kopieren!
    echo.
    echo ðŸ’¾ So kopierst du auf USB:
    echo    1. USB-Stick einstecken
    echo    2. Ã–ffne Windows Explorer
    echo    3. Gehe zu: %USB_INSTALLATION%
    echo    4. Markiere ALLES (Ctrl+A)
    echo    5. Kopiere (Ctrl+C)
    echo    6. Gehe zu USB-Stick
    echo    7. EinfÃ¼gen (Ctrl+V)
    echo    8. âœ… FERTIG!
    echo.
    echo ðŸŽ‰ Installation auf USB abgeschlossen!
    echo.
) else (
    echo âŒ Es gab Fehler bei der Installation!
    echo PrÃ¼fe die Ausgabe oben.
)

echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

REM Backup-Info anzeigen
echo ðŸ“¦ BACKUP INFORMATIONEN:
echo    Backup-Pfad: %BACKUP_DIR%
echo.

timeout /t 10

:END
cd /d "%PROJECT_ROOT%"
endlocal

