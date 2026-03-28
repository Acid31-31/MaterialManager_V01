@echo off
setlocal DisableDelayedExpansion
chcp 65001 >nul
title Lizenzgenerator - MaterialManager V01
color 0B

cls
echo.
echo ======================================================================
echo                LIZENZGENERATOR - MaterialManager V01
echo ======================================================================
echo.
echo VERFUEGBARE LIZENZTYPEN UND PREISE:
echo.
echo   1^) Single ^(S1^)        - 1 Arbeitsplatz                - 99,00 EUR/Jahr
echo   2^) Mehrplatz ^(M3^)     - 3 Arbeitsplaetze             - 249,00 EUR/Jahr
echo   3^) Firmenlizenz ^(C10^) - 10 Arbeitsplaetze + Support  - 699,00 EUR/Jahr
echo   4^) Enterprise ^(ENT^)   - Unbegrenzt + Premium Support - Auf Anfrage
echo.
echo ======================================================================
echo.

set /p CHOICE=Lizenztyp waehlen (1-4): 
if "%CHOICE%"=="1" (
  set "TYPE=Single (S1)"
  set "PRICE=99,00 EUR/Jahr"
) else if "%CHOICE%"=="2" (
  set "TYPE=Mehrplatz (M3)"
  set "PRICE=249,00 EUR/Jahr"
) else if "%CHOICE%"=="3" (
  set "TYPE=Firmenlizenz (C10)"
  set "PRICE=699,00 EUR/Jahr"
) else if "%CHOICE%"=="4" (
  set "TYPE=Enterprise (ENT)"
  set "PRICE=Auf Anfrage"
) else (
  echo FEHLER: Ungueltige Auswahl.
  pause
  exit /b 1
)

echo.
set /p HWID=Hardware-ID des Kunden: 
if "%HWID%"=="" (
  echo FEHLER: Hardware-ID erforderlich.
  pause
  exit /b 1
)

echo.
set /p CUSTOMER=Kunde/Firma: 
if "%CUSTOMER%"=="" (
  echo FEHLER: Kunde/Firma erforderlich.
  pause
  exit /b 1
)

echo.
set /p YEARS=Lizenzlaufzeit in Jahren [Standard 1]: 
if "%YEARS%"=="" set "YEARS=1"

echo %YEARS%| findstr /r "^[1-9][0-9]*$" >nul
if errorlevel 1 (
  echo FEHLER: Laufzeit muss eine ganze Zahl groesser 0 sein.
  pause
  exit /b 1
)

echo.
echo Generiere Lizenzschluessel...
echo.

if not exist "%~dp0Tools\LicenseGenerator.csproj" (
  echo FEHLER: %~dp0Tools\LicenseGenerator.csproj nicht gefunden.
  pause
  exit /b 1
)

dotnet run --project "%~dp0Tools\LicenseGenerator.csproj" "%HWID%" "%CUSTOMER%" %YEARS%
if errorlevel 1 (
  echo.
  echo FEHLER: Lizenzschluessel konnte nicht erstellt werden.
  pause
  exit /b 1
)

cls
echo.
echo ======================================================================
echo                      KUNDEN-INFORMATION (V01)
echo ======================================================================
echo.
echo Produkt:            MaterialManager V01
echo Lizenztyp:          %TYPE%
echo Preis:              %PRICE%
echo Laufzeit:           %YEARS% Jahr^(e^)
echo Kunde/Firma:        %CUSTOMER%
echo Hardware-ID:        %HWID%
echo.
echo HINWEIS: Der Lizenzschluessel wurde oben angezeigt und in die
echo Zwischenablage kopiert.
echo.
echo ----------------------------------------------------------------------
echo AKTIVIERUNG FUER DEN KUNDEN:
echo 1. MaterialManager V01 starten
echo 2. Hilfe ^> Lizenz aktivieren
echo 3. Lizenzschluessel einfuegen
echo 4. Exakt denselben Namen/Firma eingeben: %CUSTOMER%
echo 5. Auf "Lizenz aktivieren" klicken
echo ----------------------------------------------------------------------
echo.
echo DOKU-ZEILE:
echo %date% ^| %time% ^| %CUSTOMER% ^| %HWID% ^| %TYPE% ^| %PRICE% ^| %YEARS% Jahr^(e^)
echo.
pause
exit /b 0
