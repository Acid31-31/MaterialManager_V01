@echo off
REM â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
REM  MaterialManager 1.0.x - Release Installer
REM  FÃ¼r PrÃ¤sentation vorbereitet
REM â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

setlocal enabledelayedexpansion

cls
echo.
echo â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
echo â•‘                                                               â•‘
echo â•‘         MaterialManager 1.0.x - PrÃ¤sentations-Installer        â•‘
echo â•‘                        v1.0.0 Release                        â•‘
echo â•‘                                                               â•‘
echo â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

REM Alte Instanz beenden
taskkill /F /IM MaterialManager_1.0.x.exe 2>nul

echo [1/5] ÃœberprÃ¼fe Voraussetzungen...
timeout /t 1 /nobreak >nul

REM .NET 8 prÃ¼fen
dotnet --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo âš ï¸  .NET 8.0 SDK nicht gefunden!
    echo    Bitte installiere: https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)

echo     âœ“ .NET 8.0 gefunden
timeout /t 1 /nobreak >nul

echo.
echo [2/5] RÃ¤ume alte Versionen auf...
if exist "bin\Release" rmdir /s /q "bin\Release" 2>nul
timeout /t 1 /nobreak >nul
echo     âœ“ Fertig

echo.
echo [3/5] Kompiliere Releases-Version...
dotnet build --configuration Release --no-incremental 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo âŒ Build fehlgeschlagen!
    pause
    exit /b 1
)
echo     âœ“ Build erfolgreich

echo.
echo [4/5] Erstelle Installationspaket...

REM Installationsordner vorbereiten
if not exist "%USERPROFILE%\AppData\Local\MaterialManager_1.0.x" (
    mkdir "%USERPROFILE%\AppData\Local\MaterialManager_1.0.x"
)

REM Executables kopieren
xcopy /Y /I "bin\Release\net8.0-windows\win-x64\*.exe" "%USERPROFILE%\AppData\Local\MaterialManager_1.0.x\" >nul 2>&1
xcopy /Y /I "bin\Release\net8.0-windows\win-x64\*.dll" "%USERPROFILE%\AppData\Local\MaterialManager_1.0.x\" >nul 2>&1

echo     âœ“ Installiert zu: %USERPROFILE%\AppData\Local\MaterialManager_1.0.x

echo.
echo [5/5] Erstelle Start-Shortcuts...

REM Desktop Shortcut (Optional)
if not exist "%USERPROFILE%\Desktop\MaterialManager_1.0.x.lnk" (
    powershell -Command "^
    $WshShell = New-Object -ComObject WScript.Shell; ^
    $Shortcut = $WshShell.CreateShortcut('%USERPROFILE%\Desktop\MaterialManager_1.0.x.lnk'); ^
    $Shortcut.TargetPath = '%USERPROFILE%\AppData\Local\MaterialManager_1.0.x\MaterialManager_1.0.x.exe'; ^
    $Shortcut.WorkingDirectory = '%USERPROFILE%\AppData\Local\MaterialManager_1.0.x'; ^
    $Shortcut.Save();"
)

echo     âœ“ Shortcuts erstellt

echo.
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.
echo âœ… Installation ERFOLGREICH!
echo.
echo ðŸ“ Installationsort:
echo    %USERPROFILE%\AppData\Local\MaterialManager_1.0.x
echo.
echo ðŸš€ App starten:
echo    [1] Desktop-Shortcut doppelklicken
echo    [2] Oder MenÃ¼ â†’ MaterialManager 1.0.x
echo.
echo ðŸ“– Dokumentation:
echo    Siehe: README.txt und NETZWERK_ANLEITUNG.txt
echo.
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

REM App sofort starten
echo Starte MaterialManager 1.0.x in 3 Sekunden...
timeout /t 3 /nobreak
start "" "%USERPROFILE%\AppData\Local\MaterialManager_1.0.x\MaterialManager_1.0.x.exe"

echo.
pause

