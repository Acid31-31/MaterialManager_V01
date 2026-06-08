@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
set "GUI_UNINSTALL=%SCRIPT_DIR%UNINSTALL_GUI.ps1"
if not exist "%GUI_UNINSTALL%" set "GUI_UNINSTALL=%SCRIPT_DIR%MaterialManager\USB_Installation\UNINSTALL_GUI.ps1"

echo ============================================
echo MaterialManager V01 - Deinstallation
echo ============================================
echo.
echo Diese Deinstallation startet die grafische UI-Deinstallation.
echo.

if not exist "%GUI_UNINSTALL%" (
    echo FEHLER: UNINSTALL_GUI.ps1 wurde nicht gefunden.
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -Command "Start-Process powershell.exe -Verb RunAs -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File ''%GUI_UNINSTALL%'''"
exit /b %errorlevel%
