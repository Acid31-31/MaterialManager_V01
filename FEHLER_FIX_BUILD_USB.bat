@echo off
REM ============================================
REM FEHLER-LÃ–SUNG: USB_Distribution erstellen
REM ============================================

color 0B
cls

echo.
echo â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
echo â•‘  BUILD-USBVersion.ps1 - USB_Distribution erstellen            â•‘
echo â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

REM Pfad
set "PROJECT_PATH=C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x"

if not exist "%PROJECT_PATH%" (
    echo âŒ Projekt-Pfad nicht gefunden:
    echo    %PROJECT_PATH%
    pause
    exit /b 1
)

echo âœ“ Wechsel zu: %PROJECT_PATH%
cd /d "%PROJECT_PATH%"

echo.
echo [1/2] PrÃ¼fe PowerShell-Script...
if not exist "Build-USBVersion.ps1" (
    echo âŒ FEHLER: Build-USBVersion.ps1 nicht gefunden!
    echo    Pfad: %PROJECT_PATH%\Build-USBVersion.ps1
    pause
    exit /b 1
)
echo âœ“ Build-USBVersion.ps1 gefunden

echo.
echo [2/2] Starte Build-USBVersion.ps1 -Action Package...
echo.
echo â³ Dies kann 2-5 Minuten dauern...
echo.

REM PowerShell ausfÃ¼hren
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
"cd '%PROJECT_PATH%'; .\Build-USBVersion.ps1 -Action Package"

if %ERRORLEVEL% equ 0 (
    echo.
    echo â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
    echo â•‘  âœ… ERFOLGREICH!                                              â•‘
    echo â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    echo.
    echo USB_Distribution wurde erstellt!
    echo.
    echo ðŸŽ¯ NÃ„CHSTER SCHRITT:
    echo    cd USB_Installation
    echo    SETUP_PROGRAMM.bat
    echo.
) else (
    echo.
    echo âŒ FEHLER beim Build!
    echo Bitte prÃ¼fe die Meldungen oben.
    echo.
)

pause

