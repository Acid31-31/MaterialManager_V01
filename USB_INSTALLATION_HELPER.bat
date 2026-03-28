@echo off
REM ============================================
REM USB Installation Helper - Batch Version
REM Einfaches Tool zum Kopieren auf USB-Stick
REM ============================================

echo.
echo â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
echo â•‘  ðŸ“ MaterialManager 1.0.x - USB Installation Helper             â•‘
echo â•‘  Kopiere alle Dateien auf einen USB-Stick                    â•‘
echo â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

setlocal enabledelayedexpansion

REM Pfade
set USB_INSTALL=C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x\USB_Installation

REM Check ob Ordner existiert
if not exist "%USB_INSTALL%" (
    echo âŒ FEHLER: USB_Installation Ordner nicht gefunden!
    echo Erwartet: %USB_INSTALL%
    echo.
    pause
    exit /b 1
)

echo âœ“ USB_Installation Ordner gefunden
echo  Pfad: %USB_INSTALL%
echo.

REM Zeige verfÃ¼gbare Laufwerke
echo ðŸ“ VerfÃ¼gbare USB-Laufwerke:
echo.
for %%A in (D E F G H I J K L M N O P Q R S T U V W X Y Z) do (
    if exist %%A: (
        echo   %%A:\ (PrÃ¼fe...)
        REM PrÃ¼fe ob es ein USB-Stick ist (einfache PrÃ¼fung)
        dir %%A: >nul 2>&1
        if !errorlevel! equ 0 (
            for /f "tokens=3" %%S in ('dir %%A:\ ^| find "Bytes"') do set SIZE=%%S
            echo   âœ“ %%A:\ verfÃ¼gbar
        )
    )
)

echo.
echo WÃ¤hle ein USB-Laufwerk (z.B. D, E, F):
set /p DRIVE="USB-Laufwerk: "

REM Validiere Eingabe
if not exist %DRIVE%: (
    echo âŒ Laufwerk %DRIVE%: nicht gefunden!
    pause
    exit /b 1
)

echo.
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo ðŸš€ Kopiere USB_Installation auf %DRIVE%:\
echo.
echo Warte bitte... (kann mehrere Minuten dauern)
echo.

REM Kopiere Dateien
xcopy "%USB_INSTALL%\*" "%DRIVE%:\" /E /I /Y >nul 2>&1

if %errorlevel% equ 0 (
    echo âœ… Erfolgreich kopiert!
    echo.
    echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    echo.
    echo USB-Stick ist bereit! âœ…
    echo.
    echo âž¤ Gib den USB-Stick dem Kunden
    echo âž¤ Kunde steckt ihn ein
    echo âž¤ Kunde startet: Installer.exe
    echo âž¤ Installation lÃ¤uft automatisch!
    echo.
) else (
    echo âŒ Fehler beim Kopieren!
    echo.
    echo MÃ¶gliche GrÃ¼nde:
    echo â€¢ USB-Stick nicht genug Platz (mind. 500 MB nÃ¶tig)
    echo â€¢ USB-Stick schreibgeschÃ¼tzt
    echo â€¢ Dateien sind noch in Verwendung
    echo.
)

pause

