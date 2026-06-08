@echo off
REM MaterialManager V01 - Aktueller GUI Installer
REM Verwendet immer INSTALL_GUI.ps1 (maßgeblicher Installationsstand)

setlocal
set "SCRIPT_DIR=%~dp0"
set "GUI_SCRIPT=%SCRIPT_DIR%INSTALL_GUI.ps1"
set "INSTALLER_EXE=%SCRIPT_DIR%Installer.exe"
set "UNINSTALL_SCRIPT=%SCRIPT_DIR%UNINSTALL.bat"

if not exist "%GUI_SCRIPT%" (
    echo FEHLER: INSTALL_GUI.ps1 wurde nicht gefunden.
    echo Erwartet: %GUI_SCRIPT%
    pause
    exit /b 1
)

for /f "usebackq delims=" %%i in (`powershell -NoProfile -Command "(Get-Item '%GUI_SCRIPT%').LastWriteTime.ToString('dd.MM.yyyy HH:mm:ss')"`) do set "GUI_DATE=%%i"

echo.
echo ================================================================
echo   MaterialManager V01 - Installation
echo ================================================================
echo   Aktueller Installer-Stand (INSTALL_GUI.ps1): %GUI_DATE%

if exist "%INSTALLER_EXE%" (
    for /f "usebackq delims=" %%i in (`powershell -NoProfile -Command "(Get-Item '%INSTALLER_EXE%').LastWriteTime.ToString('dd.MM.yyyy HH:mm:ss')"`) do set "EXE_DATE=%%i"
    echo   Installer.exe Datum: %EXE_DATE%
    powershell -NoProfile -Command "$a=(Get-Item '%INSTALLER_EXE%').LastWriteTime; $b=(Get-Item '%GUI_SCRIPT%').LastWriteTime; if($a -lt $b){ exit 2 } else { exit 0 }"
    if errorlevel 2 (
        echo.
        echo HINWEIS: Installer.exe ist aelter als INSTALL_GUI.ps1.
        echo          Es wird trotzdem der AKTUELLE GUI-Installer gestartet.
    )
)

echo.

REM Cleanup: Alte Temp-Dateien loeschen
del "%TEMP%\*.ps1" /F /Q >nul 2>&1

REM Sicherstellen, dass die Deinstallation mit ausgeliefert wird
if exist "%UNINSTALL_SCRIPT%" (
    copy /Y "%UNINSTALL_SCRIPT%" "%SCRIPT_DIR%MaterialManager\UNINSTALL_GUI.ps1" >nul 2>&1
)

REM Admin-Check
net session >nul 2>&1
if %errorlevel%==0 (
    powershell -ExecutionPolicy Bypass -File "%GUI_SCRIPT%"
    exit /b %errorlevel%
) else (
    echo Administrator-Rechte werden angefordert...
    powershell -Command "Start-Process powershell -ArgumentList '-ExecutionPolicy Bypass -File \"%GUI_SCRIPT%\"' -Verb RunAs"
    exit /b 0
)
