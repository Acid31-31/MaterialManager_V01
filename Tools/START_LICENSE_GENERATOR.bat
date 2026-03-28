@echo off
REM MaterialManager V01 - Lizenzgenerator Starter
REM ===============================================

chcp 65001 >nul
cls

:menu
echo.
echo ╔════════════════════════════════════════════════════════════════╗
echo ║  MaterialManager V01 - Lizenzgenerator                         ║
echo ║  Lizenzschlüssel ausstellen leicht gemacht                     ║
echo ╚════════════════════════════════════════════════════════════════╝
echo.
echo Wählen Sie eine Option:
echo.
echo   1) Kommandozeilen-Generator starten (empfohlen)
echo   2) Lizenz-Verwaltung öffnen (Python)
echo   3) Anleitung öffnen
echo   4) Beenden
echo.

set /p choice="Ihre Wahl (1-4): "

if "%choice%"=="1" (
    cls
    echo.
    echo ╔════════════════════════════════════════════════════════════════╗
    echo ║         MaterialManager V01 - Kommandozeilen-Generator         ║
    echo ╚════════════════════════════════════════════════════════════════╝
    echo.
    echo VERWENDUNG:
    echo   dotnet run "HardwareID" "Firmenname" [Jahre]
    echo.
    echo BEISPIEL:
    echo   dotnet run "ABC123DEF456GHI789JKL012" "Musterfirma GmbH" 1
    echo.
    echo ────────────────────────────────────────────────────────────────
    echo.
    cd /d "%~dp0"
    dotnet run
    pause
    cls
    goto menu
) else if "%choice%"=="2" (
    if exist "license_manager.py" (
        python license_manager.py
    ) else (
        echo.
        echo ✗ Python-Skript nicht gefunden!
        pause
    )
    cls
    goto menu
) else if "%choice%"=="3" (
    if exist "LIZENZGENERATOR_ANLEITUNG.txt" (
        start notepad LIZENZGENERATOR_ANLEITUNG.txt
    ) else (
        echo.
        echo ✗ Anleitung nicht gefunden!
        pause
    )
    cls
    goto menu
) else if "%choice%"=="4" (
    exit /b 0
) else (
    echo.
    echo ✗ Ungültige Wahl!
    timeout /t 2 /nobreak
    cls
    goto menu
)
