@echo off
setlocal

if "%~1"=="" (
  echo Verwendung:
  echo   SIGN_RELEASE.bat "Pfad-zur-pfx" "Passwort"
  echo.
  echo Beispiel:
  echo   SIGN_RELEASE.bat "C:\Certs\MaterialManager.pfx" "MeinPasswort"
  exit /b 1
)

if "%~2"=="" (
  echo Fehler: Passwort fehlt.
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0SIGN_RELEASE.ps1" -CertificatePath "%~1" -CertificatePassword "%~2" -RemoveExistingSignature
