@echo off
setlocal
set "INSTALL_PATH=C:\Program Files\MaterialManager"
set "USERDATA_PATH=%LOCALAPPDATA%\MaterialManager_V01"
set "SCRIPT_DIR=%~dp0"

echo ============================================
echo MaterialManager V01 - Deinstallation
echo ============================================
echo.
echo Diese Deinstallation startet die grafische UI-Deinstallation.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%UNINSTALL_GUI.ps1"
exit /b %errorlevel%
