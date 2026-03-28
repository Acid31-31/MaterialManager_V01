@echo off
REM Aktualisierte Version - nutzt den neuesten Testablauf
setlocal

chcp 65001 >nul
cls
echo.
echo ╔════════════════════════════════════════════════════════════════╗
echo ║   TEST_LICENSE_BUGFIX.bat (AKTUALISIERT)                     ║
echo ║   Diese Datei startet jetzt immer den neuesten Testablauf     ║
echo ╚════════════════════════════════════════════════════════════════╝
echo.

if exist "%~dp0FINAL_TEST_COMPLETE.bat" (
    echo Starte: FINAL_TEST_COMPLETE.bat
    call "%~dp0FINAL_TEST_COMPLETE.bat"
    exit /b %errorlevel%
)

if exist "%~dp0TEST_LICENSE_BUGFIX_V2.bat" (
    echo FINAL_TEST_COMPLETE.bat nicht gefunden.
    echo Fallback: TEST_LICENSE_BUGFIX_V2.bat
    call "%~dp0TEST_LICENSE_BUGFIX_V2.bat"
    exit /b %errorlevel%
)

echo FEHLER: Keine aktuelle Testdatei gefunden.
echo Erwartet wurde eine der folgenden Dateien:
echo   - FINAL_TEST_COMPLETE.bat
echo   - TEST_LICENSE_BUGFIX_V2.bat
echo.
pause
exit /b 1
