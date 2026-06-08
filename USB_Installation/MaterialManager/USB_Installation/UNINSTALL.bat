@echo off
setlocal
set "SCRIPT_DIR=%~dp0"

echo ============================================
echo MaterialManager V01 - Deinstallation
echo ============================================
echo.
echo Diese Deinstallation startet die grafische UI-Deinstallation.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -Command "Start-Process powershell.exe -Verb RunAs -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File ''%SCRIPT_DIR%UNINSTALL_GUI.ps1'''"
exit /b %errorlevel%
