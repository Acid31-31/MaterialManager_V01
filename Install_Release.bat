@echo off
REM ═══════════════════════════════════════════════════════════════
REM  MaterialManager R03 - Release Installer
REM  Für Präsentation vorbereitet
REM ═══════════════════════════════════════════════════════════════

setlocal enabledelayedexpansion

cls
echo.
echo ╔═══════════════════════════════════════════════════════════════╗
echo ║                                                               ║
echo ║         MaterialManager R03 - Präsentations-Installer        ║
echo ║                        v1.0.0 Release                        ║
echo ║                                                               ║
echo ╚═══════════════════════════════════════════════════════════════╝
echo.

REM Alte Instanz beenden
taskkill /F /IM MaterialManager_R03.exe 2>nul

echo [1/5] Überprüfe Voraussetzungen...
timeout /t 1 /nobreak >nul

REM .NET 8 prüfen
dotnet --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ⚠️  .NET 8.0 SDK nicht gefunden!
    echo    Bitte installiere: https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)

echo     ✓ .NET 8.0 gefunden
timeout /t 1 /nobreak >nul

echo.
echo [2/5] Räume alte Versionen auf...
if exist "bin\Release" rmdir /s /q "bin\Release" 2>nul
timeout /t 1 /nobreak >nul
echo     ✓ Fertig

echo.
echo [3/5] Kompiliere Releases-Version...
dotnet build --configuration Release --no-incremental 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ❌ Build fehlgeschlagen!
    pause
    exit /b 1
)
echo     ✓ Build erfolgreich

echo.
echo [4/5] Erstelle Installationspaket...

REM Installationsordner vorbereiten
if not exist "%USERPROFILE%\AppData\Local\MaterialManager_R03" (
    mkdir "%USERPROFILE%\AppData\Local\MaterialManager_R03"
)

REM Executables kopieren
xcopy /Y /I "bin\Release\net8.0-windows\win-x64\*.exe" "%USERPROFILE%\AppData\Local\MaterialManager_R03\" >nul 2>&1
xcopy /Y /I "bin\Release\net8.0-windows\win-x64\*.dll" "%USERPROFILE%\AppData\Local\MaterialManager_R03\" >nul 2>&1

echo     ✓ Installiert zu: %USERPROFILE%\AppData\Local\MaterialManager_R03

echo.
echo [5/5] Erstelle Start-Shortcuts...

REM Desktop Shortcut (Optional)
if not exist "%USERPROFILE%\Desktop\MaterialManager_R03.lnk" (
    powershell -Command "^
    $WshShell = New-Object -ComObject WScript.Shell; ^
    $Shortcut = $WshShell.CreateShortcut('%USERPROFILE%\Desktop\MaterialManager_R03.lnk'); ^
    $Shortcut.TargetPath = '%USERPROFILE%\AppData\Local\MaterialManager_R03\MaterialManager_R03.exe'; ^
    $Shortcut.WorkingDirectory = '%USERPROFILE%\AppData\Local\MaterialManager_R03'; ^
    $Shortcut.Save();"
)

echo     ✓ Shortcuts erstellt

echo.
echo ═══════════════════════════════════════════════════════════════
echo.
echo ✅ Installation ERFOLGREICH!
echo.
echo 📍 Installationsort:
echo    %USERPROFILE%\AppData\Local\MaterialManager_R03
echo.
echo 🚀 App starten:
echo    [1] Desktop-Shortcut doppelklicken
echo    [2] Oder Menü → MaterialManager R03
echo.
echo 📖 Dokumentation:
echo    Siehe: README.txt und NETZWERK_ANLEITUNG.txt
echo.
echo ═══════════════════════════════════════════════════════════════
echo.

REM App sofort starten
echo Starte MaterialManager R03 in 3 Sekunden...
timeout /t 3 /nobreak
start "" "%USERPROFILE%\AppData\Local\MaterialManager_R03\MaterialManager_R03.exe"

echo.
pause
