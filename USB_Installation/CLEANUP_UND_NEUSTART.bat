@echo off
echo.
echo ========================================
echo CLEANUP - Alte Installer-Dateien
echo ========================================
echo.

REM Loesche alle Temp PowerShell Dateien
del "%TEMP%\*.ps1" /F /Q >nul 2>&1

echo Alte Dateien geloescht!
echo.
echo Starte Installer neu...
timeout /t 2 >nul

REM Starte Installer neu
cd /d "%~dp0"
start "" "INSTALL.bat"

exit
