@echo off
REM Einfachstes mögliches Script zum USB-Installation vorbereiten
setlocal enabledelayedexpansion

echo.
echo ════════════════════════════════════════════════════════════════
echo 🚀 EINFACHE USB-INSTALLATION ERSTELLER
echo ════════════════════════════════════════════════════════════════
echo.

set PROJ=C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03

cd /d "%PROJ%"

echo [1] Lösche alte Ordner...
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
    echo ✅ FERTIG!
    echo.
    echo 📁 Deine Dateien sind jetzt in:
    echo    USB_Installation\Programm\
    echo.
    echo 🎯 JETZT: Diese auf USB kopieren!
) else (
    echo ❌ Build fehlgeschlagen!
)

echo.
pause
