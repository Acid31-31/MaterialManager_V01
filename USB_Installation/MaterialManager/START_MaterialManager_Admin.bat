@echo off
REM MaterialManager V01 - Starter mit Admin-Rechten und Fehleranzeige
REM Automatisch erstellt vom Installer

cd /d "%~dp0"

REM Pruefen ob Administrator-Rechte vorhanden
net session >nul 2>&1
if %errorLevel% == 0 (
    REM Bereits Administrator - starte Anwendung
    echo Starte MaterialManager V01...
    start "" "MaterialManager_V01.exe"
    if %errorLevel% NEQ 0 (
        echo.
        echo FEHLER: Anwendung konnte nicht gestartet werden!
        echo.
        echo Moegliche Ursachen:
        echo - .NET 8 Runtime fehlt
        echo - Datenbank-Datei fehlt
        echo - EXE-Datei ist beschaedigt
        echo.
        pause
    )
) else (
    REM KEINE Admin-Rechte - fordere sie an!
    powershell -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
)

exit
