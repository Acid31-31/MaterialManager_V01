@echo off
REM ============================================
REM AUTOMATISCHES FEHLERBEHEBUNGSTOOL
REM Findet und behebt alle Probleme automatisch!
REM ============================================

color 0B
cls

echo.
echo ╔════════════════════════════════════════════════════════════════╗
echo ║  🔧 AUTOMATISCHES FEHLERBEHEBUNGSTOOL                         ║
echo ║  Ich finde und behebe alle Probleme!                          ║
echo ╚════════════════════════════════════════════════════════════════╝
echo.

REM ==== SCHRITT 1: Admin-Check ====
echo [SCHRITT 1/5] Prüfe Admin-Rechte...

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Fehler: Admin-Rechte erforderlich!
    echo.
    echo LÖSUNG:
    echo Rechtsklick auf diese Datei
    echo "Als Administrator ausführen" wählen
    echo.
    pause
    exit /b 1
)

echo ✅ Admin-Rechte OK
echo.

REM ==== SCHRITT 2: .NET Check ====
echo [SCHRITT 2/5] Prüfe .NET 8 Installation...

dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Fehler: .NET 8 ist nicht installiert!
    echo.
    echo LÖSUNG:
    echo Gehe zu: https://dotnet.microsoft.com/download/dotnet/8.0
    echo Lade ".NET 8 Desktop Runtime" herunter
    echo Installiere und starten Sie neu
    echo.
    pause
    exit /b 1
)

echo ✅ .NET 8 installiert
echo.

REM ==== SCHRITT 3: Alte Build-Dateien löschen ====
echo [SCHRITT 3/5] Räume alte Dateien auf...

cd "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03"

if exist "Installer_Source\bin" (
    echo Lösche Installer_Source\bin...
    rmdir /s /q "Installer_Source\bin" 2>nul
)

if exist "Installer_Source\obj" (
    echo Lösche Installer_Source\obj...
    rmdir /s /q "Installer_Source\obj" 2>nul
)

if exist "USB_InstallationHelper\bin" (
    echo Lösche USB_InstallationHelper\bin...
    rmdir /s /q "USB_InstallationHelper\bin" 2>nul
)

if exist "USB_InstallationHelper\obj" (
    echo Lösche USB_InstallationHelper\obj...
    rmdir /s /q "USB_InstallationHelper\obj" 2>nul
)

if exist "USB_Distribution" (
    echo Lösche USB_Distribution\...
    rmdir /s /q "USB_Distribution" 2>nul
)

echo ✅ Cleanup abgeschlossen
echo.

REM ==== SCHRITT 4: Build durchführen ====
echo [SCHRITT 4/5] Starte Build (das dauert ein paar Minuten)...
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "COMPLETE_AUTOMATIC_BUILD.ps1" 2>&1

if %errorlevel% neq 0 (
    echo.
    echo ❌ Build fehlgeschlagen!
    echo.
    echo Mögliche Lösungen:
    echo 1. Schließe alle Explorer-Fenster
    echo 2. Öffne Task Manager (Shift+Ctrl+Esc)
    echo 3. Suche nach "dotnet" oder "msbuild"
    echo 4. Beende diese Prozesse
    echo 5. Nochmal dieses Script starten
    echo.
    pause
    exit /b 1
)

echo ✅ Build erfolgreich
echo.

REM ==== SCHRITT 5: USB_InstallationHelper starten ====
echo [SCHRITT 5/5] Starte USB_InstallationHelper GUI...
echo.

set EXEPATH=C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03\USB_InstallationHelper\bin\Release\net8.0-windows\USB_InstallationHelper.exe

if not exist "%EXEPATH%" (
    echo ❌ Fehler: USB_InstallationHelper.exe nicht gefunden!
    echo.
    pause
    exit /b 1
)

start "" "%EXEPATH%"

echo ✅ GUI sollte sich jetzt öffnen!
echo.
echo ════════════════════════════════════════════════════════════════
echo.
echo IM FENSTER DANN:
echo 1. USB-Stick einstecken
echo 2. Klick auf "Aktualisieren"
echo 3. USB auswählen
echo 4. Klick auf "Auf USB kopieren"
echo 5. Fertig!
echo.
echo ════════════════════════════════════════════════════════════════
echo.

pause
