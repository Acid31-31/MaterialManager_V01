@echo off
REM ============================================
REM AUTOMATISCHES FEHLERBEHEBUNGSTOOL
REM Findet und behebt alle Probleme automatisch!
REM ============================================

color 0B
cls

echo.
echo â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
echo â•‘  ðŸ”§ AUTOMATISCHES FEHLERBEHEBUNGSTOOL                         â•‘
echo â•‘  Ich finde und behebe alle Probleme!                          â•‘
echo â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

REM ==== SCHRITT 1: Admin-Check ====
echo [SCHRITT 1/5] PrÃ¼fe Admin-Rechte...

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo âŒ Fehler: Admin-Rechte erforderlich!
    echo.
    echo LÃ–SUNG:
    echo Rechtsklick auf diese Datei
    echo "Als Administrator ausfÃ¼hren" wÃ¤hlen
    echo.
    pause
    exit /b 1
)

echo âœ… Admin-Rechte OK
echo.

REM ==== SCHRITT 2: .NET Check ====
echo [SCHRITT 2/5] PrÃ¼fe .NET 8 Installation...

dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo âŒ Fehler: .NET 8 ist nicht installiert!
    echo.
    echo LÃ–SUNG:
    echo Gehe zu: https://dotnet.microsoft.com/download/dotnet/8.0
    echo Lade ".NET 8 Desktop Runtime" herunter
    echo Installiere und starten Sie neu
    echo.
    pause
    exit /b 1
)

echo âœ… .NET 8 installiert
echo.

REM ==== SCHRITT 3: Alte Build-Dateien lÃ¶schen ====
echo [SCHRITT 3/5] RÃ¤ume alte Dateien auf...

cd "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x"

if exist "Installer_Source\bin" (
    echo LÃ¶sche Installer_Source\bin...
    rmdir /s /q "Installer_Source\bin" 2>nul
)

if exist "Installer_Source\obj" (
    echo LÃ¶sche Installer_Source\obj...
    rmdir /s /q "Installer_Source\obj" 2>nul
)

if exist "USB_InstallationHelper\bin" (
    echo LÃ¶sche USB_InstallationHelper\bin...
    rmdir /s /q "USB_InstallationHelper\bin" 2>nul
)

if exist "USB_InstallationHelper\obj" (
    echo LÃ¶sche USB_InstallationHelper\obj...
    rmdir /s /q "USB_InstallationHelper\obj" 2>nul
)

if exist "USB_Distribution" (
    echo LÃ¶sche USB_Distribution\...
    rmdir /s /q "USB_Distribution" 2>nul
)

echo âœ… Cleanup abgeschlossen
echo.

REM ==== SCHRITT 4: Build durchfÃ¼hren ====
echo [SCHRITT 4/5] Starte Build (das dauert ein paar Minuten)...
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "COMPLETE_AUTOMATIC_BUILD.ps1" 2>&1

if %errorlevel% neq 0 (
    echo.
    echo âŒ Build fehlgeschlagen!
    echo.
    echo MÃ¶gliche LÃ¶sungen:
    echo 1. SchlieÃŸe alle Explorer-Fenster
    echo 2. Ã–ffne Task Manager (Shift+Ctrl+Esc)
    echo 3. Suche nach "dotnet" oder "msbuild"
    echo 4. Beende diese Prozesse
    echo 5. Nochmal dieses Script starten
    echo.
    pause
    exit /b 1
)

echo âœ… Build erfolgreich
echo.

REM ==== SCHRITT 5: USB_InstallationHelper starten ====
echo [SCHRITT 5/5] Starte USB_InstallationHelper GUI...
echo.

set EXEPATH=C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x\USB_InstallationHelper\bin\Release\net8.0-windows\USB_InstallationHelper.exe

if not exist "%EXEPATH%" (
    echo âŒ Fehler: USB_InstallationHelper.exe nicht gefunden!
    echo.
    pause
    exit /b 1
)

start "" "%EXEPATH%"

echo âœ… GUI sollte sich jetzt Ã¶ffnen!
echo.
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.
echo IM FENSTER DANN:
echo 1. USB-Stick einstecken
echo 2. Klick auf "Aktualisieren"
echo 3. USB auswÃ¤hlen
echo 4. Klick auf "Auf USB kopieren"
echo 5. Fertig!
echo.
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

pause

