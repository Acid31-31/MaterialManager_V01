@echo off
REM ============================================
REM USB Installation - Programm-Dateien füllen
REM ============================================

color 0B
cls

echo.
echo ============================================
echo MaterialManager R03 - Programm-Dateien kopieren
echo ============================================
echo.

REM Pfade definieren
set SOURCE_DIR=%~dp0..\..\USB_Distribution
set TARGET_DIR=%~dp0Programm

echo [1/3] Prüfe Source-Verzeichnis...
if not exist "%SOURCE_DIR%" (
    echo.
    echo ❌ FEHLER: USB_Distribution nicht gefunden!
    echo.
    echo Bitte führe zuerst aus:
    echo   cd %~dp0..
    echo   .\Build-USBVersion.ps1 -Action Package
    echo.
    pause
    exit /b 1
)

echo [2/3] Leere Programm-Verzeichnis...
if exist "%TARGET_DIR%" (
    rmdir /s /q "%TARGET_DIR%"
)
mkdir "%TARGET_DIR%"

echo [3/3] Kopiere Dateien...
xcopy "%SOURCE_DIR%\*" "%TARGET_DIR%\" /E /Y /Q

if %errorlevel% neq 0 (
    echo.
    echo ❌ Fehler beim Kopieren!
    pause
    exit /b 1
)

cls
echo.
echo ✅ ERFOLGREICH!
echo.
echo Programm-Dateien in: %TARGET_DIR%
echo.
dir /b "%TARGET_DIR%"
echo.
echo ============================================
echo USB Installation ist jetzt bereit!
echo ============================================
echo.
echo Nächster Schritt:
echo 1. Gesamten USB_Installation Ordner auf USB-Stick kopieren
echo 2. Auf Zielrechner: USB_INSTALL.bat (als Admin) ausführen
echo.
pause
