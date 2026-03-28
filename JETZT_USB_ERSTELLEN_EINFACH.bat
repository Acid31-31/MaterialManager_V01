@echo off
REM Einfachstes mÃ¶gliches Script zum USB-Installation vorbereiten
setlocal enabledelayedexpansion

echo.
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo ðŸš€ EINFACHE USB-INSTALLATION ERSTELLER
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

set PROJ=C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x

cd /d "%PROJ%"

echo [1] LÃ¶sche alte Ordner...
if exist bin\Debug rmdir /s /q bin\Debug >nul 2>&1
if exist bin\Release rmdir /s /q bin\Release >nul 2>&1
if exist obj rmdir /s /q obj >nul 2>&1

echo [2] Baue Projekt...
call dotnet build -c Debug -f net8.0-windows

echo.
echo [3] Kopiere Dateien...
if exist "bin\Debug\net8.0-windows" (
    if not exist "USB_Installation\Programm" mkdir "USB_Installation\Programm"
    xcopy "bin\Debug\net8.0-windows\*" "USB_Installation\Programm\" /S /Y /Q
    echo âœ… FERTIG!
    echo.
    echo ðŸ“ Deine Dateien sind jetzt in:
    echo    USB_Installation\Programm\
    echo.
    echo ðŸŽ¯ JETZT: Diese auf USB kopieren!
) else (
    echo âŒ Build fehlgeschlagen!
)

echo.
pause

