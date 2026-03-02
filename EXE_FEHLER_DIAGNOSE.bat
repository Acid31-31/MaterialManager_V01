@echo off
REM ============================================
REM EXE-FEHLER DIAGNOSE & FEHLERBEHEBUNG
REM ============================================

color 0C
cls

echo.
echo ╔════════════════════════════════════════════════════════════════╗
echo ║  🔴 EXE-FEHLER DIAGNOSE                                       ║
echo ║  Was ist das Problem? Ich fixe es!                            ║
echo ╚════════════════════════════════════════════════════════════════╝
echo.

REM Admin-Check
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ FEHLER: Admin-Rechte erforderlich!
    echo.
    echo Bitte:
    echo 1. Rechtsklick auf diese Datei
    echo 2. "Als Administrator ausführen"
    echo.
    pause
    exit /b 1
)

echo ✓ Admin-Rechte bestätigt
echo.

REM PROBLEM 1: .NET 8 Runtime nicht installiert
echo 🔍 PRÜFE .NET 8 INSTALLATION...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ .NET 8 NICHT INSTALLIERT!
    echo.
    echo LÖSUNG:
    echo 1. Gehe zu: https://dotnet.microsoft.com/download/dotnet/8.0
    echo 2. Lade ".NET 8 Desktop Runtime" herunter
    echo 3. Starten und installieren
    echo 4. Neustart!
    echo.
    pause
    exit /b 1
)

echo ✓ .NET 8 ist installiert
echo.

REM PROBLEM 2: EXE nicht gebaut
echo 🔍 PRÜFE OB EXE EXISTIERT...

if not exist "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03\USB_InstallationHelper\bin\Release\net8.0-windows\USB_InstallationHelper.exe" (
    echo ❌ USB_InstallationHelper.exe NICHT GEFUNDEN!
    echo.
    echo LÖSUNG:
    echo 1. Öffne: Build-USBHelper.ps1
    echo 2. Oder: Führe aus: .\Build-USBHelper.ps1
    echo 3. Warten bis ✅ BUILD ERFOLGREICH
    echo.
    pause
    exit /b 1
)

echo ✓ USB_InstallationHelper.exe existiert
echo.

REM PROBLEM 3: Dateien-Berechtigungen
echo 🔍 PRÜFE BERECHTIGUNGEN...

set EXEPATH=C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03\USB_InstallationHelper\bin\Release\net8.0-windows\USB_InstallationHelper.exe

icacls "%EXEPATH%" >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ BERECHTIGUNGSPROBLEM!
    echo.
    echo LÖSUNG:
    echo 1. Rechtsklick auf USB_InstallationHelper.exe
    echo 2. Eigenschaften
    echo 3. Reiter "Sicherheit"
    echo 4. Button "Bearbeiten"
    echo 5. Alle Häkchen setzen
    echo 6. OK
    echo.
    pause
    exit /b 1
)

echo ✓ Berechtigungen OK
echo.

REM PROBLEM 4: Dependencies fehlen
echo 🔍 PRÜFE ABHÄNGIGKEITEN...

if not exist "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03\USB_InstallationHelper\bin\Release\net8.0-windows\System.Windows.Forms.dll" (
    echo ❌ ABHÄNGIGKEITEN FEHLEN!
    echo.
    echo LÖSUNG:
    echo Baue die EXE nochmal:
    echo.
    echo cd C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03
    echo .\Build-USBHelper.ps1 -Configuration Release
    echo.
    pause
    exit /b 1
)

echo ✓ Abhängigkeiten OK
echo.

REM Versuche EXE zu starten
echo ════════════════════════════════════════════════════════════════
echo 🚀 STARTEN DER EXE JETZT...
echo.

start "" "%EXEPATH%"

if %errorlevel% equ 0 (
    echo ✅ EXE GESTARTET!
    echo.
) else (
    echo ⚠️  EXE konnte nicht gestartet werden
    echo Fehlercode: %errorlevel%
    echo.
)

echo ════════════════════════════════════════════════════════════════
echo.
pause
