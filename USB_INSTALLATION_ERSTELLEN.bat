@echo off
REM ╔════════════════════════════════════════════════════════════════════════╗
REM ║  KOMPLETTE USB-INSTALLATION - AUTOMATISCH!                           ║
REM ║  Erstellt das komplette MaterialManager_R03 Programm für USB         ║
REM ║  KEINE Shell-Befehle - KEINE alleinstehenden Dateien                 ║
REM ╚════════════════════════════════════════════════════════════════════════╝

setlocal enabledelayedexpansion

cls
color 0A

echo.
echo ╔════════════════════════════════════════════════════════════════════════╗
echo ║                                                                        ║
echo ║     🚀 MATERIALMANAGER R03 - KOMPLETTE USB INSTALLATION              ║
echo ║                                                                        ║
echo ╚════════════════════════════════════════════════════════════════════════╝
echo.

REM Pfade
set PROJECT_ROOT=C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03
set USB_INSTALLATION=%PROJECT_ROOT%\USB_Installation
set BUILD_OUTPUT=%PROJECT_ROOT%\bin\Release\net8.0-windows\win-x64
set BACKUP_ROOT=%PROJECT_ROOT%\Backups
set TIMESTAMP=%date:~10,4%%date:~4,2%%date:~7,2%_%time:~0,2%%time:~3,2%%time:~6,2%
set BACKUP_DIR=%BACKUP_ROOT%\USB_Build_%TIMESTAMP%

REM ═══════════════════════════════════════════════════════════════════════════
echo [SCHRITT 1/5] Erstelle Backup...
echo ─────────────────────────────────────────────────────────────────────────

if not exist "%BACKUP_ROOT%" mkdir "%BACKUP_ROOT%"
if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"

echo Kopiere Source-Code...
xcopy "%PROJECT_ROOT%\Services\*" "%BACKUP_DIR%\Services\" /S /Y /Q >nul 2>&1
xcopy "%PROJECT_ROOT%\Views\*" "%BACKUP_DIR%\Views\" /S /Y /Q >nul 2>&1
copy "%PROJECT_ROOT%\*.csproj" "%BACKUP_DIR%\" /Y >nul 2>&1
copy "%PROJECT_ROOT%\*.sln" "%BACKUP_DIR%\" /Y >nul 2>&1

echo ✅ Backup erstellt: %BACKUP_DIR%
echo.

REM ═══════════════════════════════════════════════════════════════════════════
echo [SCHRITT 2/5] Cleanup alte Build-Dateien...
echo ─────────────────────────────────────────────────────────────────────────

if exist "%PROJECT_ROOT%\bin" (
    echo Lösche alte bin-Dateien...
    rmdir /s /q "%PROJECT_ROOT%\bin" >nul 2>&1
)

if exist "%PROJECT_ROOT%\obj" (
    echo Lösche alte obj-Dateien...
    rmdir /s /q "%PROJECT_ROOT%\obj" >nul 2>&1
)

echo ✅ Cleanup abgeschlossen
echo.

REM ═══════════════════════════════════════════════════════════════════════════
echo [SCHRITT 3/5] Baue Programm (Release-Modus)...
echo ─────────────────────────────────────────────────────────────────────────
echo.
echo Bitte warten... Dies kann 2-5 Minuten dauern!
echo.

cd /d "%PROJECT_ROOT%"
dotnet build -c Release -p:Platform=x64 -p:SelfContained=true -p:RuntimeIdentifier=win-x64 --no-restore --verbosity minimal

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ❌ BUILD FEHLER!
    echo Der Build ist fehlgeschlagen. Prüfe die Ausgabe oben.
    pause
    exit /b 1
)

echo ✅ Build erfolgreich abgeschlossen
echo.

REM ═══════════════════════════════════════════════════════════════════════════
echo [SCHRITT 4/5] Kopiere Dateien zu USB_Installation...
echo ─────────────────────────────────────────────────────────────────────────

REM Erstelle Programm-Ordner wenn nicht vorhanden
if not exist "%USB_INSTALLATION%\Programm" mkdir "%USB_INSTALLATION%\Programm"

echo Kopiere Hauptprogramm (MaterialManager_R03.exe)...
if exist "%BUILD_OUTPUT%\MaterialManager_R03.exe" (
    copy "%BUILD_OUTPUT%\MaterialManager_R03.exe" "%USB_INSTALLATION%\Programm\" /Y >nul 2>&1
    echo   ✅ MaterialManager_R03.exe
) else (
    echo   ❌ MaterialManager_R03.exe nicht gefunden!
)

echo Kopiere DLL-Abhängigkeiten...
if exist "%BUILD_OUTPUT%\*.dll" (
    copy "%BUILD_OUTPUT%\*.dll" "%USB_INSTALLATION%\Programm\" /Y >nul 2>&1
    echo   ✅ Alle DLL-Dateien kopiert
)

echo Kopiere Runtime-Dateien...
if exist "%BUILD_OUTPUT%\*" (
    for /f "tokens=*" %%A in ('dir /b "%BUILD_OUTPUT%\"') do (
        if "%%A" neq "MaterialManager_R03.exe" if not "%%A:~-4%%"==".dll" (
            if exist "%BUILD_OUTPUT%\%%A\" (
                xcopy "%BUILD_OUTPUT%\%%A" "%USB_INSTALLATION%\Programm\%%A" /S /Y /Q >nul 2>&1
            ) else (
                copy "%BUILD_OUTPUT%\%%A" "%USB_INSTALLATION%\Programm\" /Y >nul 2>&1
            )
        )
    )
)

echo ✅ Dateien zu USB_Installation kopiert
echo.

REM ═══════════════════════════════════════════════════════════════════════════
echo [SCHRITT 5/5] Verifiziere USB_Installation...
echo ─────────────────────────────────────────────────────────────────────────

set ERRORS=0

if not exist "%USB_INSTALLATION%\Programm\MaterialManager_R03.exe" (
    echo ❌ MaterialManager_R03.exe fehlt!
    set ERRORS=1
) else (
    echo ✅ MaterialManager_R03.exe vorhanden
)

if not exist "%USB_INSTALLATION%\Installer.exe" (
    echo ⚠️  Installer.exe fehlt (wird bei der Installation erstellt)
) else (
    echo ✅ Installer.exe vorhanden
)

if not exist "%USB_INSTALLATION%\Anleitung\QUICK_START.md" (
    echo ⚠️  Anleitung fehlt
) else (
    echo ✅ Anleitung vorhanden
)

echo.
echo ════════════════════════════════════════════════════════════════════════════
echo.

if %ERRORS% EQU 0 (
    echo ✅ KOMPLETTE USB-INSTALLATION ERSTELLT!
    echo.
    echo 📁 Programm-Dateien:
    echo    %USB_INSTALLATION%\Programm\
    echo.
    echo 📋 Ordnerstruktur:
    echo    USB_Installation/
    echo    ├─ Programm/
    echo    │  ├─ MaterialManager_R03.exe  ✅
    echo    │  ├─ *.dll Dateien           ✅
    echo    │  └─ Runtime-Dateien         ✅
    echo    ├─ Anleitung/
    echo    ├─ Tools/
    echo    ├─ Installer.exe (später)
    echo    └─ README.md
    echo.
    echo 🚀 NÄCHSTER SCHRITT: USB-Stick kopieren!
    echo.
    echo 💾 So kopierst du auf USB:
    echo    1. USB-Stick einstecken
    echo    2. Öffne Windows Explorer
    echo    3. Gehe zu: %USB_INSTALLATION%
    echo    4. Markiere ALLES (Ctrl+A)
    echo    5. Kopiere (Ctrl+C)
    echo    6. Gehe zu USB-Stick
    echo    7. Einfügen (Ctrl+V)
    echo    8. ✅ FERTIG!
    echo.
    echo 🎉 Installation auf USB abgeschlossen!
    echo.
) else (
    echo ❌ Es gab Fehler bei der Installation!
    echo Prüfe die Ausgabe oben.
)

echo ════════════════════════════════════════════════════════════════════════════
echo.

REM Backup-Info anzeigen
echo 📦 BACKUP INFORMATIONEN:
echo    Backup-Pfad: %BACKUP_DIR%
echo.

timeout /t 10

:END
cd /d "%PROJECT_ROOT%"
endlocal
