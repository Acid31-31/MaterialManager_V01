@echo off
REM Test-Skript für die korrigierte Lizenzierungs-Bugfix
REM Mit Debug-Ausgabe

setlocal enabledelayedexpansion

echo.
echo ╔════════════════════════════════════════════════════════════════╗
echo ║     Lizenzierungs-Bugfix TEST - KORRIGIERT                    ║
echo ║                                                                ║
echo ║  Mit Toleranz für Tag-Wechsel                                 ║
echo ║  (Generator und Validator können an verschiedenen Tagen       ║
echo ║   laufen, aber die Lizenz sollte trotzdem funktionieren)     ║
echo ╚════════════════════════════════════════════════════════════════╝
echo.

REM Test-Daten
set HARDWARE_ID=wz8g9cF1d1FYzdjC2Gb8bN
set COMPANY_NAME=TestFirma
set YEARS=1

echo SCHRITT 1: Aktuelle Daten
echo ════════════════════════════════════════════════════════════════
echo Hardware-ID:     %HARDWARE_ID%
echo Firma:           %COMPANY_NAME%
echo Laufzeit:        %YEARS% Jahr(e)
echo Heutiges Datum:  %date%
echo.

echo SCHRITT 2: Generiere Lizenzschlüssel...
echo ════════════════════════════════════════════════════════════════
cd /d "%~dp0Tools"
dotnet run "%HARDWARE_ID%" "%COMPANY_NAME%" %YEARS%

echo.
echo.
echo SCHRITT 3: AKTIVIERUNG TESTEN
echo ════════════════════════════════════════════════════════════════
echo.
echo WICHTIG:
echo  1. Notieren Sie den oben angezeigten Lizenzschlüssel (MM-XXXX-...)
echo  2. MaterialManager V01 starten
echo  3. Menü → Hilfe → Lizenzinformationen
echo     → Hardware-ID prüfen
echo     → Sollte EXAKT sein: %HARDWARE_ID%
echo.
echo  4. Menü → Hilfe → Lizenz aktivieren
echo     → Lizenzschlüssel eingeben (MM-XXXX-...)
echo     → Firma eingeben: %COMPANY_NAME%
echo     → Klick "Aktivieren"
echo.
echo  5. WICHTIG FÜR DEBUG:
echo     → Visual Studio Debug Console öffnen (Debug → Windows → Output)
echo     → Nach [License] Meldungen suchen
echo     → Zeigt exakte Fehlermeldungen
echo.
echo MÖGLICHE FEHLER:
echo  ❌ "[License] FEHLER: Lizenzschlüssel ungültig!"
echo     → Hardware-ID stimmt nicht überein
echo     → Firmenname hat Typos
echo     → Lizenzschlüssel falsch eingegeben
echo.
echo  ✓ "[License] ✓ Lizenz erfolgreich aktiviert!"
echo     → Lizenzierung funktioniert!
echo.
pause

echo.
echo ╔════════════════════════════════════════════════════════════════╗
echo ║  Test durchführen!                                             ║
echo ║                                                                ║
echo ║  Bei Problemen:                                                ║
echo ║  1. Visual Studio Debug Output anschauen                       ║
echo ║  2. BUGFIX_LIZENZIERUNG.txt lesen                              ║
echo ║  3. Alte Lizenz löschen: %%APPDATA%%\...\.license             ║
echo ╚════════════════════════════════════════════════════════════════╝
echo.
