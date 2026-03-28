@echo off
REM â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
REM  MaterialManager 1.0.x - Complete Presentation Package Creator
REM  Erstellt eine fertige ZIP-Datei fÃ¼r PrÃ¤sentation
REM â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

setlocal enabledelayedexpansion

cls
echo.
echo â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
echo â•‘                                                               â•‘
echo â•‘     MaterialManager 1.0.x - Presentation Package Creator       â•‘
echo â•‘                   v1.0.0                                     â•‘
echo â•‘                                                               â•‘
echo â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

REM TemporÃ¤rer Ordner
set "TEMP_DIR=%CD%\MaterialManager_1.0.x_RELEASE"
set "ZIP_NAME=MaterialManager_1.0.x_Presentation_%date:~-4%%date:~-10,2%%date:~-7,2%.zip"

echo [1/6] RÃ¤ume alte Build-Dateien auf...
if exist "%TEMP_DIR%" rmdir /s /q "%TEMP_DIR%" 2>nul
mkdir "%TEMP_DIR%"
echo     âœ“ Ordner erstellt

echo.
echo [2/6] Kompiliere Release-Version...
dotnet build --configuration Release --no-incremental >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo     âŒ Build fehlgeschlagen!
    pause
    exit /b 1
)
echo     âœ“ Release-Build erfolgreich

echo.
echo [3/6] Kopiere Dateien zusammen...

REM Kopiere Release-Binaries
xcopy /Y /I "bin\Release\net8.0-windows\win-x64\*.exe" "%TEMP_DIR%\" >nul 2>&1
xcopy /Y /I "bin\Release\net8.0-windows\win-x64\*.dll" "%TEMP_DIR%\" >nul 2>&1
xcopy /Y /I "bin\Release\net8.0-windows\win-x64\*.json" "%TEMP_DIR%\" >nul 2>&1

REM Kopiere Presentations-Dateien
copy /Y "PRESENTATION_EMAIL.md" "%TEMP_DIR%\" >nul 2>&1
copy /Y "PRESENTATION_CHECKLIST.md" "%TEMP_DIR%\" >nul 2>&1
copy /Y "Install_Release.bat" "%TEMP_DIR%\Install.bat" >nul 2>&1

REM Kopiere Dokumentation
copy /Y "README_DEMO.md" "%TEMP_DIR%\README_DE.md" >nul 2>&1
copy /Y "NETZWERK_SYNC_ANLEITUNG.md" "%TEMP_DIR%\NETZWERK_ANLEITUNG.md" >nul 2>&1

REM Erstelle Quick-Start
(
    echo @echo off
    echo title MaterialManager 1.0.x
    echo start "" MaterialManager_1.0.x.exe
) > "%TEMP_DIR%\START.bat"

echo     âœ“ Dateien kopiert

echo.
echo [4/6] Erstelle Willkommens-Info...

(
    echo â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
    echo â•‘                                                               â•‘
    echo â•‘     MaterialManager 1.0.x - Presentation Version v1.0.0        â•‘
    echo â•‘                    Dezember 2024                             â•‘
    echo â•‘                                                               â•‘
    echo â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    echo.
    echo ðŸ“¦ INHALT DIESES PACKAGES:
    echo.
    echo  âœ“ MaterialManager_1.0.x.exe           (Hauptprogramm^)
    echo  âœ“ Alle notwendigen Libraries        (DLLs^)
    echo  âœ“ Install.bat                       (Automatischer Installer^)
    echo  âœ“ START.bat                         (Schnellstart^)
    echo  âœ“ PRESENTATION_EMAIL.md             (Email-Text fÃ¼r Einladung^)
    echo  âœ“ PRESENTATION_CHECKLIST.md         (Demo-Vorbereitung^)
    echo  âœ“ README_DE.md                      (Deutsche Anleitung^)
    echo  âœ“ NETZWERK_ANLEITUNG.md             (Multi-PC Setup^)
    echo.
    echo ðŸš€ INSTALLATION ^(3 Schritte^):
    echo.
    echo  1. Doppelklick: Install.bat
    echo  2. Warten auf Meldung "Installation erfolgreich"
    echo  3. Starten: START.bat
    echo.
    echo ðŸ“Š SYSTEMANFORDERUNGEN:
    echo.
    echo  â€¢ Windows 10 / 11 / Server
    echo  â€¢ .NET 8.0 Runtime ^(wird automatisch geprÃ¼ft^)
    echo  â€¢ RAM: 512 MB
    echo  â€¢ Festplatte: 50 MB
    echo.
    echo ðŸ’¡ TIPPS FÃœR PRÃ„SENTATION:
    echo.
    echo  â€¢ Testen Sie Install.bat VORHER!
    echo  â€¢ Demo-Materialien hinzufÃ¼gen
    echo  â€¢ BildschirmgrÃ¶ÃŸe 1600x900
    echo  â€¢ Netzwerk-Sync Dialog zeigen
    echo.
    echo ðŸ“§ VERSAND:
    echo.
    echo  1. Sende diese ZIP-Datei
    echo  2. Schreibe Email nach: PRESENTATION_EMAIL.md
    echo  3. Anhang: Install.bat, README_DE.md
    echo.
    echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    echo Viel Erfolg bei der PrÃ¤sentation! ðŸŽ‰
    echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
) > "%TEMP_DIR%\README_FIRST.txt"

echo     âœ“ Info-Datei erstellt

echo.
echo [5/6] Erstelle ZIP-Datei...

REM ZIP erstellen mit PowerShell
powershell -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; [System.IO.Compression.ZipFile]::CreateFromDirectory('%TEMP_DIR%', '%ZIP_NAME%')" >nul 2>&1

if exist "%ZIP_NAME%" (
    echo     âœ“ ZIP erstellt: %ZIP_NAME%
) else (
    echo     âŒ ZIP-Erstellung fehlgeschlagen!
    pause
    exit /b 1
)

echo.
echo [6/6] RÃ¤ume auf...
rmdir /s /q "%TEMP_DIR%" >nul 2>&1
echo     âœ“ Fertig

echo.
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.
echo âœ… PACKAGE FERTIG!
echo.
echo ðŸ“¦ Dateiname:
echo    %ZIP_NAME%
echo.
echo ðŸ“ Speicherort:
echo    %CD%
echo.
echo ðŸ“Š GrÃ¶ÃŸe: Siehe Datei-Eigenschaften
echo.
echo ðŸš€ NÃ¤chste Schritte:
echo    1. ZIP-Datei herunterladen
echo    2. An Audience versenden
echo    3. PRESENTATION_EMAIL.md als Email-Text verwenden
echo.
echo ðŸ“‹ Checkliste vor PrÃ¤sentation:
echo    â˜ Install.bat testen
echo    â˜ Demo-Daten vorbereiten
echo    â˜ Bildschirm-GrÃ¶ÃŸe prÃ¼fen
echo    â˜ Beamer/Streaming testen
echo.
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.
pause
explorer "%CD%"

