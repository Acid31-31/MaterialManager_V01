@echo off
REM FINAL TEST - MaterialManager V01 - Lizenzierungs-Bugfix
REM Testet die komplette Lösung

setlocal enabledelayedexpansion

cls
echo.
echo ╔════════════════════════════════════════════════════════════════════════════════╗
echo ║                   ✅ MATERIALMANAGER V01 - LIZENZIERUNGS-TEST                 ║
echo ║                                                                                 ║
echo ║              Alle Probleme gelöst: Tag-Wechsel, Case-Sensibilität              ║
echo ╚════════════════════════════════════════════════════════════════════════════════╝
echo.

REM Test-Daten
set HARDWARE_ID=wz8g9cF1d1FYzdjC2Gb8bN
set COMPANY_NAME=TestFirma
set YEARS=1

echo SCHRITT 1: Test-Daten
echo ════════════════════════════════════════════════════════════════════════════════
echo Hardware-ID:       %HARDWARE_ID%
echo Firma:             %COMPANY_NAME%
echo Laufzeit:          %YEARS% Jahr(e)
echo Datum:             %date%
echo.

echo SCHRITT 2: Lizenzschlüssel generieren
echo ════════════════════════════════════════════════════════════════════════════════
cd /d "%~dp0Tools"

echo.
echo Starte LicenseGenerator...
echo.
dotnet run "%HARDWARE_ID%" "%COMPANY_NAME%" %YEARS%

if not !errorlevel! == 0 (
    echo.
    echo ✗ FEHLER beim Generieren des Lizenzschlüssels!
    echo.
    echo Mögliche Ursachen:
    echo   - Hardware-ID Format ungültig
    echo   - Firma-Name hat ungültige Zeichen
    echo   - .NET 8 SDK nicht installiert
    echo.
    pause
    exit /b 1
)

echo.
echo ✓ Lizenzschlüssel erfolgreich generiert!
echo.

echo SCHRITT 3: Aktivierungsanleitung
echo ════════════════════════════════════════════════════════════════════════════════
echo.
echo Der Lizenzschlüssel wurde in die Zwischenablage kopiert!
echo.
echo ✓ Nächste Schritte:
echo.
echo  1. MaterialManager V01 starten
echo     cd ..
echo     start bin\Debug\net8.0-windows\win-x64\MaterialManager_V01.exe
echo.
echo  2. Menü → Hilfe → Lizenzinformationen
echo     WICHTIG: Hardware-ID prüfen!
echo     Sollte EXAKT sein: %HARDWARE_ID%
echo.
echo  3. Menü → Hilfe → Lizenz aktivieren
echo     • Lizenzschlüssel: MM-XXXX-... (in Zwischenablage)
echo     • Firma: %COMPANY_NAME% (exakt!)
echo     • Aktivieren klicken
echo.
echo  4. Bei Erfolg: MessageBox "✓ Lizenz erfolgreich aktiviert!"
echo.
echo.

echo SCHRITT 4: DEBUG-TIPPS (falls Fehler)
echo ════════════════════════════════════════════════════════════════════════════════
echo.
echo Falls Aktivierung fehlschlägt:
echo.
echo  A) Visual Studio öffnen
echo     Debug → Windows → Output (Ctrl+Alt+O)
echo     Nach [License] suchen → zeigt genaue Fehlerursache
echo.
echo  B) Häufigste Fehler:
echo     ❌ Hardware-ID stimmt nicht
echo        → Im Dialog mit Generator-Output vergleichen
echo     ❌ Firma-Name Typo
echo        → %COMPANY_NAME% EXAKT eingeben (Groß-/Kleinschreibung!)
echo     ❌ Alte Lizenz blockiert
echo        → %%APPDATA%%\MaterialManager_V01\.license löschen
echo     ❌ Lizenzschlüssel falsch
echo        → Zwischenablage nutzen (automatisch kopiert!)
echo.
echo.

echo SCHRITT 5: Starte MaterialManager jetzt
echo ════════════════════════════════════════════════════════════════════════════════
echo.

cd /d "%~dp0.."

if not exist "bin\Debug\net8.0-windows\win-x64\MaterialManager_V01.exe" (
    echo ✗ MaterialManager.exe nicht gefunden!
    echo.
    echo Build-Verzeichnis prüfen:
    echo bin\Debug\net8.0-windows\win-x64\MaterialManager_V01.exe
    echo.
    echo Bitte zuerst Build durchführen!
    pause
    exit /b 1
)

echo ✓ MaterialManager.exe gefunden!
echo.
echo Starte MaterialManager V01...
echo.

start "" "bin\Debug\net8.0-windows\win-x64\MaterialManager_V01.exe"

echo.
echo ✓ MaterialManager V01 gestartet!
echo.
echo Führen Sie jetzt die Lizenzaktivierung durch (Schritte oben).
echo.

pause

echo.
echo ╔════════════════════════════════════════════════════════════════════════════════╗
echo ║  Test abgeschlossen!                                                           ║
echo ║                                                                                 ║
echo ║  Falls alles funktioniert:  ✓ Der Bugfix ist erfolgreich!                     ║
echo ║  Falls Fehler auftritt:     → Debug-Output prüfen (siehe Schritt 4)            ║
echo ║                                                                                 ║
echo ║  Weitere Hilfe: FINAL_BUGFIX_COMPLETE.txt                                      ║
echo ╚════════════════════════════════════════════════════════════════════════════════╝
echo.
