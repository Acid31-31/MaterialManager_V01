@echo off
REM Diagnose-Skript für Lizenzierungs-Probleme
REM Zeigt exakt wo der Fehler ist

setlocal enabledelayedexpansion

echo.
echo ╔════════════════════════════════════════════════════════════════╗
echo ║      LIZENZIERUNGS-DIAGNOSE - FEHLERSUCHE                     ║
echo ║                                                                ║
echo ║  Dieses Skript prüft jeden Schritt einzeln                   ║
echo ╚════════════════════════════════════════════════════════════════╝
echo.

REM Test-Daten
set HARDWARE_ID=wz8g9cF1d1FYzdjC2Gb8bN
set COMPANY_NAME=TestFirma
set YEARS=1

echo DIAGNOSE SCHRITT 1: Tools-Verzeichnis
echo ════════════════════════════════════════════════════════════════
if exist "Tools" (
    echo ✓ Tools Verzeichnis existiert
) else (
    echo ✗ FEHLER: Tools Verzeichnis NICHT GEFUNDEN!
    echo Bitte im Hauptverzeichnis des Projekts ausführen!
    pause
    exit /b 1
)
echo.

echo DIAGNOSE SCHRITT 2: LicenseGenerator.cs
echo ════════════════════════════════════════════════════════════════
if exist "Tools\LicenseGenerator.cs" (
    echo ✓ LicenseGenerator.cs existiert
) else (
    echo ✗ FEHLER: Tools\LicenseGenerator.cs NICHT GEFUNDEN!
    pause
    exit /b 1
)
echo.

echo DIAGNOSE SCHRITT 3: LicenseGenerator.csproj
echo ════════════════════════════════════════════════════════════════
if exist "Tools\LicenseGenerator.csproj" (
    echo ✓ LicenseGenerator.csproj existiert
) else (
    echo ✗ FEHLER: Tools\LicenseGenerator.csproj NICHT GEFUNDEN!
    pause
    exit /b 1
)
echo.

echo DIAGNOSE SCHRITT 4: Lizenzschlüssel generieren
echo ════════════════════════════════════════════════════════════════
cd /d "%~dp0Tools"

if not exist "bin\Debug\net8.0\LicenseGenerator.exe" (
    echo Building LicenseGenerator...
    dotnet build LicenseGenerator.csproj -c Debug
    if not !errorlevel! == 0 (
        echo ✗ FEHLER beim Build von LicenseGenerator!
        pause
        exit /b 1
    )
    echo ✓ Build erfolgreich
) else (
    echo ✓ LicenseGenerator bereits gebaut
)
echo.

echo Generiere jetzt Lizenzschlüssel...
echo Hardware-ID: %HARDWARE_ID%
echo Firma: %COMPANY_NAME%
echo.

dotnet run "%HARDWARE_ID%" "%COMPANY_NAME%" %YEARS%

if not !errorlevel! == 0 (
    echo ✗ FEHLER beim Generieren des Lizenzschlüssels!
    echo.
    echo Mögliche Ursachen:
    echo  - Hardware-ID ist ungültig
    echo  - Firma-Name hat ungültige Zeichen
    echo  - .NET SDK nicht installiert
    pause
    exit /b 1
)

echo.
echo ✓ Lizenzschlüssel erfolgreich generiert!
echo.

echo DIAGNOSE SCHRITT 5: MaterialManager prüfen
echo ════════════════════════════════════════════════════════════════
cd /d "%~dp0"

if exist "bin\Debug\net8.0-windows\win-x64\MaterialManager_V01.exe" (
    echo ✓ MaterialManager.exe existiert
) else (
    echo ✗ FEHLER: MaterialManager.exe NICHT GEFUNDEN!
    echo MaterialManager muss zuerst gebaut werden
    echo Führen Sie einen Build in Visual Studio durch!
    pause
    exit /b 1
)
echo.

echo ════════════════════════════════════════════════════════════════
echo NÄCHSTE SCHRITTE
echo ════════════════════════════════════════════════════════════════
echo.
echo 1. MaterialManager starten:
echo    bin\Debug\net8.0-windows\win-x64\MaterialManager_V01.exe
echo.
echo 2. Menü → Hilfe → Lizenzinformationen
echo    Hardware-ID: %HARDWARE_ID%
echo    (sollte EXAKT übereinstimmen)
echo.
echo 3. Menü → Hilfe → Lizenz aktivieren
echo    Lizenzschlüssel: MM-XXXX-... (siehe oben)
echo    Firma: %COMPANY_NAME%
echo    → Aktivieren
echo.
echo 4. BEI FEHLER: Visual Studio öffnen
echo    Debug → Windows → Output
echo    Nach [License] Meldungen suchen
echo.
echo ════════════════════════════════════════════════════════════════
echo.
pause

echo.
echo Starte MaterialManager...
start "" "bin\Debug\net8.0-windows\win-x64\MaterialManager_V01.exe"
