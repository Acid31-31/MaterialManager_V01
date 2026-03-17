@echo off
REM ============================================
REM FORCE-CLEANUP - Beendet ALLE PowerShell Prozesse
REM ============================================

echo.
echo ========================================
echo NOTFALL-CLEANUP - MaterialManager V01
echo ========================================
echo.
echo Alle PowerShell-Prozesse werden beendet...
echo.

REM Beende ALLE PowerShell-Prozesse (inkl. GUI)
taskkill /F /IM powershell.exe >nul 2>&1
taskkill /F /IM pwsh.exe >nul 2>&1

echo PowerShell-Prozesse beendet!
echo.
echo Loesche alte Temp-Dateien...

REM Loesche alle PS1 Temp-Dateien
del "%TEMP%\*.ps1" /F /Q >nul 2>&1
del "%TEMP%\*.tmp" /F /Q >nul 2>&1

echo Temp-Dateien geloescht!
echo.
echo Warte 2 Sekunden...
timeout /t 2 >nul

echo.
echo Starte Installer NEU...
echo.

REM Starte Installer mit Administrator-Rechten
cd /d "%~dp0"
powershell -Command "Start-Process -FilePath 'INSTALL.bat' -Verb RunAs"

echo.
echo FERTIG! Der Installer sollte sich jetzt oeffnen.
timeout /t 3 >nul
exit
