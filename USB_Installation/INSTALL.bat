@echo off
REM MaterialManager V01 - GUI Installer
REM Fuehrt INSTALL_GUI.ps1 DIREKT aus (OHNE Temp-Copy!)

REM Cleanup: Alte Temp-Dateien loeschen
del "%TEMP%\*.ps1" /F /Q >nul 2>&1

REM Admin-Check
net session >nul 2>&1
if %errorLevel% == 0 (
    REM Als Admin - Starte PS1 DIREKT aus USB_Installation
    powershell -WindowStyle Hidden -ExecutionPolicy Bypass -File "%~dp0INSTALL_GUI.ps1"
    exit
) else (
    REM Keine Admin-Rechte - fordere sie an
    echo Administrator-Rechte werden angefordert...
    powershell -Command "Start-Process powershell -ArgumentList '-WindowStyle Hidden -ExecutionPolicy Bypass -File \"%~dp0INSTALL_GUI.ps1\"' -Verb RunAs"
    exit
)
