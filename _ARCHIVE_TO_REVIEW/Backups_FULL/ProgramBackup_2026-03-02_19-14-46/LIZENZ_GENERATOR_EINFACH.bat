@echo off
setlocal enabledelayedexpansion
title LIZENZGENERATOR - MaterialManager R03
color 0B
chcp 65001 >nul 2>&1

cls
echo.
echo ======================================================================
echo         LIZENZGENERATOR - MaterialManager R03 Vollversion
echo ======================================================================
echo.

set /p HWID="Hardware-ID eingeben: "
if "!HWID!"=="" (
    echo FEHLER: Hardware-ID erforderlich!
    pause
    exit /b 1
)

echo.
echo Lizenztyp:
echo  1 = Einzelplatz (499 EUR)
echo  2 = Mehrplatz (1199 EUR)
echo  3 = Firmenlizenz (2990 EUR)
echo  4 = Enterprise (Auf Anfrage)
echo.

set /p CHOICE="Auswahl (1-4): "

if "!CHOICE!"=="1" set TYPE=Einzelplatz
if "!CHOICE!"=="2" set TYPE=Mehrplatz
if "!CHOICE!"=="3" set TYPE=Firmenlizenz
if "!CHOICE!"=="4" set TYPE=Enterprise

if not defined TYPE (
    echo FEHLER: Ungueltige Auswahl!
    pause
    exit /b 1
)

set /p CUSTOMER="Kundenname: "
if "!CUSTOMER!"=="" (
    echo FEHLER: Kundenname erforderlich!
    pause
    exit /b 1
)

echo.
echo Generiere Schluessel...
echo.

REM Erstelle temporaere Datei mit den Daten
echo !HWID!-!CHOICE!-!CUSTOMER! > TEMP_LICENSE_DATA.txt

REM Generiere SHA256-Hash
for /f "delims=" %%A in ('certutil -hashfile TEMP_LICENSE_DATA.txt SHA256 ^| find /i /v "certutil" ^| find /i /v "SHA256"') do set FULLHASH=%%A

REM Entferne Leerzeichen aus dem Hash
set HASH=!FULLHASH: =!

REM Formatiere als Lizenzschluessel
set KEY=!HASH:~0,4!-!HASH:~4,4!-!HASH:~8,4!-!HASH:~12,4!

REM Loesche temporaere Datei
del TEMP_LICENSE_DATA.txt

if "!KEY!"=="" (
    echo FEHLER: Konnte Lizenzschluessel nicht erstellen!
    pause
    exit /b 1
)

cls
echo.
echo ======================================================================
echo                OKAY, LIZENZSCHLUESSEL ERSTELLT
echo ======================================================================
echo.
echo Typ:       !TYPE!
echo Kunde:     !CUSTOMER!
echo Hardware:  !HWID!
echo.
echo LIZENZSCHLUESSEL:
echo.
echo                     !KEY!
echo.
echo ======================================================================
echo                   KOPIERE DIESEN SCHLUESSEL!
echo ======================================================================
echo.

echo EMAIL AN KUNDEN:
echo.
echo Betreff: Ihr Lizenzschluessel - MaterialManager R03
echo.
echo Lieber Kunde,
echo.
echo Vielen Dank fuer Ihren Kauf!
echo.
echo Ihr Lizenzschluessel: !KEY!
echo Typ: !TYPE!
echo Registriert auf: !CUSTOMER!
echo.
echo Aktivierung:
echo 1. MaterialManager R03 starten
echo 2. Hilfe ^> Lizenz aktivieren
echo 3. Schluessel: !KEY!
echo 4. Name: !CUSTOMER!
echo 5. Klick auf "Aktivieren"
echo 6. FERTIG!
echo.
echo Support: hoelzer_alex@yahoo.de
echo.

echo.
echo EXCEL-DOKUMENTATION:
echo %date% ^| !CUSTOMER! ^| !HWID! ^| !KEY! ^| !TYPE!
echo.

REM Kopiere in Clipboard
echo !KEY! | clip
echo [OK] Schluessel in Zwischenablage kopiert!
echo.
pause
