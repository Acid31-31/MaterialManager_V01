@echo off
chcp 65001 > nul
echo ╔════════════════════════════════════════════════════════════════╗
echo ║        MaterialManager V01 - USB VERSION ERSTELLEN             ║
echo ║                 © 2025 Alexander Hölzer                        ║
echo ╚════════════════════════════════════════════════════════════════╝
echo.

REM 📦 ZIELVERZEICHNIS
set TARGET=D:\MaterialManager_V01_komplett\USB_Installation
set OUTPUT=%TARGET%\MaterialManager

echo 🔧 Erstelle Zielordner...
if not exist "%TARGET%" mkdir "%TARGET%"
if not exist "%OUTPUT%" mkdir "%OUTPUT%"

echo.
echo 🏗️  Kompiliere MaterialManager als EXE (Single File)...
echo ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
dotnet publish MaterialManager_V01.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:PublishReadyToRun=true ^
    -p:PublishTrimmed=false ^
    -p:DebugType=none ^
    -p:DebugSymbols=false ^
    -o "%OUTPUT%"

if %errorlevel% neq 0 (
    echo ❌ Fehler beim Kompilieren!
    pause
    exit /b 1
)

echo.
echo ✅ Kompilierung erfolgreich!
echo.
echo 📄 Kopiere Anleitungen und Dokumentation...
echo ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

REM Kopiere Anleitungen
copy /Y "USB_Installation\INSTALLATIONSANLEITUNG.txt" "%TARGET%\" > nul
copy /Y "USB_Installation\PREISBERECHNUNG.txt" "%TARGET%\" > nul

REM Erstelle START.bat im Zielordner
echo @echo off > "%TARGET%\START_MaterialManager.bat"
echo cd /d "%%~dp0MaterialManager" >> "%TARGET%\START_MaterialManager.bat"
echo start MaterialManager_V01.exe >> "%TARGET%\START_MaterialManager.bat"

REM Erstelle README im Hauptordner
echo ╔════════════════════════════════════════════════════════════════╗ > "%TARGET%\README.txt"
echo ║          MaterialManager V01 - USB INSTALLATION                ║ >> "%TARGET%\README.txt"
echo ║                 © 2025 Alexander Hölzer                        ║ >> "%TARGET%\README.txt"
echo ╚════════════════════════════════════════════════════════════════╝ >> "%TARGET%\README.txt"
echo. >> "%TARGET%\README.txt"
echo 🚀 SCHNELLSTART: >> "%TARGET%\README.txt"
echo    1. Doppelklick auf: START_MaterialManager.bat >> "%TARGET%\README.txt"
echo    2. Oder: MaterialManager\MaterialManager_V01.exe direkt starten >> "%TARGET%\README.txt"
echo. >> "%TARGET%\README.txt"
echo 📖 DOKUMENTATION: >> "%TARGET%\README.txt"
echo    - INSTALLATIONSANLEITUNG.txt >> "%TARGET%\README.txt"
echo    - PREISBERECHNUNG.txt >> "%TARGET%\README.txt"
echo. >> "%TARGET%\README.txt"
echo 📞 SUPPORT: >> "%TARGET%\README.txt"
echo    E-Mail: hoelzer_alex@yahoo.de >> "%TARGET%\README.txt"
echo    Telefon: +49 170 8339993 >> "%TARGET%\README.txt"

echo.
echo ✅ FERTIG!
echo.
echo 📁 Alles wurde erstellt in:
echo    %TARGET%
echo.
echo 📦 INHALT:
echo    ├─ MaterialManager\MaterialManager_V01.exe (Hauptprogramm)
echo    ├─ START_MaterialManager.bat (Schnellstart)
echo    ├─ INSTALLATIONSANLEITUNG.txt
echo    ├─ PREISBERECHNUNG.txt
echo    └─ README.txt
echo.
echo 💾 Größe: ca. 60-80 MB (inkl. .NET Runtime)
echo.
pause
