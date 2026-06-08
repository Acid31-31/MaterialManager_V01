@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
set "GUI_UNINSTALL="

if exist "%SCRIPT_DIR%UNINSTALL_GUI.ps1" set "GUI_UNINSTALL=%SCRIPT_DIR%UNINSTALL_GUI.ps1"
if not defined GUI_UNINSTALL if exist "%SCRIPT_DIR%USB_Installation\UNINSTALL_GUI.ps1" set "GUI_UNINSTALL=%SCRIPT_DIR%USB_Installation\UNINSTALL_GUI.ps1"
if not defined GUI_UNINSTALL if exist "%SCRIPT_DIR%..\UNINSTALL_GUI.ps1" set "GUI_UNINSTALL=%SCRIPT_DIR%..\UNINSTALL_GUI.ps1"

if not defined GUI_UNINSTALL (
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Add-Type -AssemblyName System.Windows.Forms; [System.Windows.Forms.MessageBox]::Show('UNINSTALL_GUI.ps1 wurde nicht gefunden.','Deinstallation','OK','Error')" >nul 2>&1
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -WindowStyle Normal -File "%GUI_UNINSTALL%"
exit /b %errorlevel%
