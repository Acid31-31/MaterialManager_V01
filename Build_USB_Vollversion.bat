@echo off
REM ═══════════════════════════════════════════════════════════════════════════════
REM  BUILD USB-VOLLVERSION PAKET
REM  © 2025 Alexander HÃ¶lzer - Alle Rechte vorbehalten
REM ═══════════════════════════════════════════════════════════════════════════════

title MaterialManager R03 - USB-Vollversion erstellen
color 0B
chcp 65001 >nul

cls
echo.
echo ╔═══════════════════════════════════════════════════════════════════════════════╗
echo ║          MATERIALMANAGER R03 - USB-VOLLVERSION PAKET ERSTELLEN                ║
echo ╚═══════════════════════════════════════════════════════════════════════════════╝
echo.
echo  © 2025 Alexander HÃ¶lzer - Alle Rechte vorbehalten
echo.
echo ───────────────────────────────────────────────────────────────────────────────
echo.

REM Prüfe ob dotnet vorhanden
where dotnet >nul 2>&1
if %errorLevel% neq 0 (
    echo [FEHLER] .NET SDK nicht gefunden!
    echo Bitte installieren Sie .NET 8 SDK.
    pause
    exit /b 1
)

echo [✓] .NET SDK gefunden
echo.

REM Aufräumen alte Builds
echo [INFO] Räume alte Builds auf...
if exist "USB_Package" rmdir /S /Q "USB_Package"
if exist "bin\Release" rmdir /S /Q "bin\Release"
echo [✓] Alte Builds entfernt
echo.

REM Build Self-Contained Vollversion
echo ═══════════════════════════════════════════════════════════════════════════════
echo  SCHRITT 1: Kompiliere Vollversion (Self-Contained)
echo ═══════════════════════════════════════════════════════════════════════════════
echo.

dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=false /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishReadyToRun=true

if %errorLevel% neq 0 (
    echo.
    echo [FEHLER] Build fehlgeschlagen!
    pause
    exit /b 1
)

echo.
echo [✓] Build erfolgreich
echo.

REM USB-Paket-Struktur erstellen
echo ═══════════════════════════════════════════════════════════════════════════════
echo  SCHRITT 2: Erstelle USB-Paket-Struktur
echo ═══════════════════════════════════════════════════════════════════════════════
echo.

mkdir "USB_Package"
mkdir "USB_Package\App"

echo [INFO] Kopiere Programmdateien...
xcopy /E /I /Y /Q "bin\Release\net8.0-windows\win-x64\publish\*" "USB_Package\App\"

echo [✓] Programmdateien kopiert
echo.

REM Installer und Dokumentation kopieren
echo [INFO] Kopiere Installer und Dokumentation...

copy /Y "USB_Installer\INSTALL_VOLLVERSION.bat" "USB_Package\"
copy /Y "USB_Installer\README_VOLLVERSION.txt" "USB_Package\"
copy /Y "LICENSE.txt" "USB_Package\"
copy /Y "COPYRIGHT.txt" "USB_Package\"

echo [✓] Installer und Dokumentation kopiert
echo.

REM Lizenz-Generator erstellen (NUR FÜR DICH!)
echo [INFO] Erstelle Lizenz-Generator...

(
echo @echo off
echo title MaterialManager R03 - Lizenzschlüssel Generator
echo color 0E
echo cls
echo.
echo ══════════════════════════════════════════════════════════════════════════════
echo  LIZENZSCHLÜSSEL GENERATOR - NUR FÜR INTERNEN GEBRAUCH!
echo ══════════════════════════════════════════════════════════════════════════════
echo.
echo  WICHTIG: Dieser Generator ist vertraulich und darf NICHT weitergegeben werden!
echo.
echo ──────────────────────────────────────────────────────────────────────────────
echo.
echo.
echo Hardware-ID des Kunden eingeben:
echo ^(Kunde erhält diese über Hilfe ^> Lizenz aktivieren^)
echo.
set /p HWID=Hardware-ID: 
echo.
echo.
echo Lizenztyp wählen:
echo.
echo  1 - Einzelplatz     ^(499 EUR^)
echo  2 - Mehrplatz 3     ^(1.199 EUR^)
echo  3 - Firmenlizenz 10 ^(2.990 EUR^)
echo  4 - Enterprise      ^(Auf Anfrage^)
echo.
set /p TYPE=Auswahl [1-4]: 
echo.
echo.
echo [INFO] Generiere Lizenzschlüssel...
echo.
echo Starte MaterialManager_R03.exe mit Parameter: --generate-license "%%HWID%%" %%TYPE%%
echo.
start /WAIT "" "App\MaterialManager_R03.exe" --generate-license "%%HWID%%" %%TYPE%%
echo.
echo ──────────────────────────────────────────────────────────────────────────────
echo.
echo WICHTIG: Lizenzschlüssel per E-Mail an Kunden senden!
echo.
pause
) > "USB_Package\LIZENZ_GENERATOR_INTERN.bat"

echo [✓] Lizenz-Generator erstellt
echo.

REM Autorun.inf erstellen
echo [INFO] Erstelle Autorun...

(
echo [AutoRun]
echo open=INSTALL_VOLLVERSION.bat
echo icon=App\MaterialManager_R03.exe
echo label=MaterialManager R03 - Vollversion Installer
echo action=MaterialManager R03 installieren
) > "USB_Package\autorun.inf"

echo [✓] Autorun erstellt
echo.

REM Versions-Info erstellen
echo [INFO] Erstelle Versions-Info...

(
echo ═══════════════════════════════════════════════════════════════════════════════
echo  MATERIALMANAGER R03 - VOLLVERSION USB-PAKET
echo ═══════════════════════════════════════════════════════════════════════════════
echo.
echo  Version:        3.0.0 ^(Vollversion^)
echo  Build-Datum:    %date% %time%
echo  Build-Type:     Self-Contained ^(kein .NET erforderlich^)
echo  Plattform:      Windows 10/11 ^(64-bit^)
echo  
echo  Enthaltene Dateien:
echo  • INSTALL_VOLLVERSION.bat    - Installations-Script
echo  • README_VOLLVERSION.txt     - Installations-Anleitung
echo  • LICENSE.txt                - Lizenzvereinbarung
echo  • COPYRIGHT.txt              - Urheberrechtshinweis
echo  • App\                       - Programmdateien
echo  • LIZENZ_GENERATOR_INTERN.bat - Für interne Nutzung!
echo.
echo  © 2025 Alexander HÃ¶lzer - Alle Rechte vorbehalten
echo  Urheberrechtlich geschützt gemäß §§ 69a ff. UrhG
echo.
echo ═══════════════════════════════════════════════════════════════════════════════
) > "USB_Package\VERSION_INFO.txt"

echo [✓] Versions-Info erstellt
echo.

REM Größe berechnen
echo [INFO] Berechne Paket-Größe...

for /f "tokens=3" %%a in ('dir /s "USB_Package" ^| find "Bytes"') do set SIZE=%%a
set /a SIZE_MB=%SIZE:~0,-7%

echo [✓] Paket-Größe: ca. %SIZE_MB% MB
echo.

REM Zusammenfassung
echo ═══════════════════════════════════════════════════════════════════════════════
echo  PAKET ERFOLGREICH ERSTELLT!
echo ═══════════════════════════════════════════════════════════════════════════════
echo.
echo  Speicherort:    %CD%\USB_Package
echo  Paket-Größe:    ca. %SIZE_MB% MB
echo.
echo ───────────────────────────────────────────────────────────────────────────────
echo  NÄCHSTE SCHRITTE:
echo ───────────────────────────────────────────────────────────────────────────────
echo.
echo  1. Kopiere den Ordner "USB_Package" auf einen USB-Stick
echo.
echo  2. Übergebe den USB-Stick an den Kunden
echo.
echo  3. Kunde führt "INSTALL_VOLLVERSION.bat" als Admin aus
echo.
echo  4. Kunde sendet dir seine Hardware-ID
echo.
echo  5. Generiere Lizenzschlüssel mit LIZENZ_GENERATOR_INTERN.bat
echo.
echo  6. Sende Lizenzschlüssel an Kunden per E-Mail
echo.
echo ═══════════════════════════════════════════════════════════════════════════════
echo.
echo  WICHTIG: LIZENZ_GENERATOR_INTERN.bat ist NUR FÜR DICH!
echo           Niemals an Kunden weitergeben!
echo.
echo ═══════════════════════════════════════════════════════════════════════════════
echo.

REM USB-Package öffnen?
choice /C JN /M "Möchten Sie den USB-Package-Ordner öffnen"
if errorlevel 1 (
    explorer "USB_Package"
)

pause
