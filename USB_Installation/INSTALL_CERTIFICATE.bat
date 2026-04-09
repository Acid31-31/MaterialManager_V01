@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "CERT_FILE=%SCRIPT_DIR%MaterialManager_CodeSigning_PUBLIC.cer"

echo.
echo =====================================================================
echo   MaterialManager - Code-Signing Zertifikat installieren
echo =====================================================================
echo.

if not exist "%CERT_FILE%" (
    echo FEHLER: Zertifikatsdatei nicht gefunden:
    echo %CERT_FILE%
    echo.
    echo Bitte zuerst auf dem Build-PC SIGN_RELEASE.bat ausfuehren.
    pause
    exit /b 1
)

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo FEHLER: Bitte als Administrator ausfuehren.
    echo Rechtsklick auf INSTALL_CERTIFICATE.bat -> Als Administrator ausfuehren
    pause
    exit /b 1
)

echo [1/2] Import in TrustedPublisher ...
certutil -f -addstore "TrustedPublisher" "%CERT_FILE%" >nul
if %errorlevel% neq 0 (
    echo FEHLER beim Import in TrustedPublisher.
    pause
    exit /b 1
)

echo [2/2] Import in Root ...
certutil -f -addstore "Root" "%CERT_FILE%" >nul
if %errorlevel% neq 0 (
    echo FEHLER beim Import in Root.
    pause
    exit /b 1
)

echo.
echo OK: Zertifikat erfolgreich installiert.
echo Danach Installation/Update normal starten.
echo.
pause
exit /b 0
