@echo off
REM ============================================
REM EXE-FEHLER DIAGNOSE & FEHLERBEHEBUNG
REM ============================================

color 0C
cls

echo.
echo â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
echo â•‘  ðŸ”´ EXE-FEHLER DIAGNOSE                                       â•‘
echo â•‘  Was ist das Problem? Ich fixe es!                            â•‘
echo â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

REM Admin-Check
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo âŒ FEHLER: Admin-Rechte erforderlich!
    echo.
    echo Bitte:
    echo 1. Rechtsklick auf diese Datei
    echo 2. "Als Administrator ausfÃ¼hren"
    echo.
    pause
    exit /b 1
)

echo âœ“ Admin-Rechte bestÃ¤tigt
echo.

REM PROBLEM 1: .NET 8 Runtime nicht installiert
echo ðŸ” PRÃœFE .NET 8 INSTALLATION...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo âŒ .NET 8 NICHT INSTALLIERT!
    echo.
    echo LÃ–SUNG:
    echo 1. Gehe zu: https://dotnet.microsoft.com/download/dotnet/8.0
    echo 2. Lade ".NET 8 Desktop Runtime" herunter
    echo 3. Starten und installieren
    echo 4. Neustart!
    echo.
    pause
    exit /b 1
)

echo âœ“ .NET 8 ist installiert
echo.

REM PROBLEM 2: EXE nicht gebaut
echo ðŸ” PRÃœFE OB EXE EXISTIERT...

if not exist "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x\USB_InstallationHelper\bin\Release\net8.0-windows\USB_InstallationHelper.exe" (
    echo âŒ USB_InstallationHelper.exe NICHT GEFUNDEN!
    echo.
    echo LÃ–SUNG:
    echo 1. Ã–ffne: Build-USBHelper.ps1
    echo 2. Oder: FÃ¼hre aus: .\Build-USBHelper.ps1
    echo 3. Warten bis âœ… BUILD ERFOLGREICH
    echo.
    pause
    exit /b 1
)

echo âœ“ USB_InstallationHelper.exe existiert
echo.

REM PROBLEM 3: Dateien-Berechtigungen
echo ðŸ” PRÃœFE BERECHTIGUNGEN...

set EXEPATH=C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x\USB_InstallationHelper\bin\Release\net8.0-windows\USB_InstallationHelper.exe

icacls "%EXEPATH%" >nul 2>&1
if %errorlevel% neq 0 (
    echo âŒ BERECHTIGUNGSPROBLEM!
    echo.
    echo LÃ–SUNG:
    echo 1. Rechtsklick auf USB_InstallationHelper.exe
    echo 2. Eigenschaften
    echo 3. Reiter "Sicherheit"
    echo 4. Button "Bearbeiten"
    echo 5. Alle HÃ¤kchen setzen
    echo 6. OK
    echo.
    pause
    exit /b 1
)

echo âœ“ Berechtigungen OK
echo.

REM PROBLEM 4: Dependencies fehlen
echo ðŸ” PRÃœFE ABHÃ„NGIGKEITEN...

if not exist "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x\USB_InstallationHelper\bin\Release\net8.0-windows\System.Windows.Forms.dll" (
    echo âŒ ABHÃ„NGIGKEITEN FEHLEN!
    echo.
    echo LÃ–SUNG:
    echo Baue die EXE nochmal:
    echo.
    echo cd C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x
    echo .\Build-USBHelper.ps1 -Configuration Release
    echo.
    pause
    exit /b 1
)

echo âœ“ AbhÃ¤ngigkeiten OK
echo.

REM Versuche EXE zu starten
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo ðŸš€ STARTEN DER EXE JETZT...
echo.

start "" "%EXEPATH%"

if %errorlevel% equ 0 (
    echo âœ… EXE GESTARTET!
    echo.
) else (
    echo âš ï¸  EXE konnte nicht gestartet werden
    echo Fehlercode: %errorlevel%
    echo.
)

echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.
pause

