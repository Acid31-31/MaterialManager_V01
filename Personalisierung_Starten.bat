@echo off
REM ═══════════════════════════════════════════════════════════════════════════════
REM  PERSONALISIERUNG STARTEN
REM  MaterialManager R03 - Alexander Hölzer
REM ═══════════════════════════════════════════════════════════════════════════════

title MaterialManager R03 - Personalisierung
color 0A

cls
echo.
echo ═══════════════════════════════════════════════════════════════════════════════
echo               MATERIALMANAGER R03 - AUTOMATISCHE PERSONALISIERUNG
echo ═══════════════════════════════════════════════════════════════════════════════
echo.
echo  Dieses Script ersetzt automatisch alle Platzhalter in deinen Dateien:
echo.
echo  [DEIN NAME]         -^> Alexander Hölzer
echo  [DEINE EMAIL]       -^> hoelzer_alex@yahoo.de
echo  [DEINE ADRESSE]     -^> Pfarrer-Rosenkranz-Strasse 9, 56642 Kruft
echo  [DEINE TELEFON]     -^> +49 170 8339993
echo  [DEINE STADT]       -^> Kruft
echo.
echo ───────────────────────────────────────────────────────────────────────────────
echo.

REM Prüfe ob PowerShell verfügbar
where powershell >nul 2>&1
if %errorLevel% neq 0 (
    echo [FEHLER] PowerShell nicht gefunden!
    echo Bitte installieren Sie PowerShell.
    pause
    exit /b 1
)

echo [INFO] Starte Personalisierung...
echo.

REM Führe PowerShell-Script aus
powershell -ExecutionPolicy Bypass -File "Personalisierung_Automatisch.ps1"

if %errorLevel% neq 0 (
    echo.
    echo [FEHLER] Personalisierung fehlgeschlagen!
    pause
    exit /b 1
)

echo.
echo ═══════════════════════════════════════════════════════════════════════════════
echo  PERSONALISIERUNG ABGESCHLOSSEN
echo ═══════════════════════════════════════════════════════════════════════════════
echo.

pause
