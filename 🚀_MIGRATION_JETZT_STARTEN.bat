@echo off
REM ===============================================================================
REM FINAL COMPLETE MIGRATION - ALLES IN EINEM!
REM MaterialManager 1.0.x â†’ V01
REM
REM DIESE DATEI MACHT ALLES AUTOMATISCH:
REM 1. Kopiert 1.0.x nach D:\MaterialManager_V01_komplett
REM 2. Ã„ndert ALLE 1.0.x â†’ V01 Referenzen 
REM 3. Setzt Demo auf 30 Tage zurÃ¼ck
REM 4. Startet Build
REM 5. Startet Programm
REM
REM Â© 2025 Alexander HÃ¶lzer
REM ===============================================================================

setlocal enabledelayedexpansion

color 0A
cls

echo.
echo ===============================================================================
echo â•‘                                                                               â•‘
echo â•‘           ðŸš€ MATERIALMANAGER 1.0.x ^â†’ V01 KOMPLETT-MIGRATION ðŸš€               â•‘
echo â•‘                                                                               â•‘
echo â•‘                      ALLES AUTOMATISCH - KEINE WARTEZEIT                     â•‘
echo â•‘                                                                               â•‘
echo â•‘  âœ“ Kopiert nach D:\MaterialManager_V01_komplett                              â•‘
echo â•‘  âœ“ Ã„ndert alle 1.0.x â†’ V01                                                    â•‘
echo â•‘  âœ“ Demo auf 30 Tage zurÃ¼ckgesetzt                                            â•‘
echo â•‘  âœ“ Build startet automatisch                                                 â•‘
echo â•‘  âœ“ Programm wird gestartet                                                   â•‘
echo â•‘                                                                               â•‘
echo â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

setlocal enabledelayedexpansion

set "SOURCE=C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x"
set "DEST=D:\MaterialManager_V01_komplett"

REM ===============================================================================
REM PHASE 1: KOPIEREN
REM ===============================================================================

echo [1/5] KOPIEREN VON 1.0.x NACH D:\ ...
echo.

if not exist "%SOURCE%" (
    color 0C
    echo âŒ FEHLER: 1.0.x nicht gefunden!
    pause
    exit /b 1
)

if exist "%DEST%" (
    rmdir /s /q "%DEST%" 2>nul
)

robocopy "%SOURCE%" "%DEST%" /E /XD bin obj .git .vs .vscode /XF *.vsdbg* *.tmp *.lock /NP /NS /NC /NFL /NDL >nul 2>&1

if %errorlevel% leq 1 (
    echo âœ… Kopieren erfolgreich!
    echo   Ziel: %DEST%
) else (
    color 0C
    echo âŒ Kopieren fehlgeschlagen!
    pause
    exit /b 1
)

echo.

REM ===============================================================================
REM PHASE 2: MASS-RENAME
REM ===============================================================================

echo [2/5] Ã„NDERE ALLE DATEIEN (1.0.x ^â†’ V01) ...
echo.

cd /d "%DEST%"

powershell -NoProfile -ExecutionPolicy Bypass -Command "^
\$files = Get-ChildItem -Path '.' -Recurse -Include *.cs, *.xaml, *.csproj, *.sln, *.txt, *.md, *.bat, *.ps1 -ErrorAction SilentlyContinue | Where-Object { \$_.FullName -notmatch 'obj\\|bin\\' }; ^
\$count = 0; ^
foreach (\$file in \$files) { ^
    try { ^
        \$content = Get-Content \$file.FullName -Raw -Encoding UTF8 -ErrorAction SilentlyContinue; ^
        if (\$null -ne \$content -and \$content -match 'MaterialManager_1.0.x') { ^
            \$content = \$content -replace 'MaterialManager_1.0.x', 'MaterialManager_V01' -replace 'namespace MaterialManager_1.0.x', 'namespace MaterialManager_V01' -replace 'using MaterialManager_1.0.x', 'using MaterialManager_V01' -replace 'MM_1.0.x_SECRET', 'MM_V01_SECRET'; ^
            Set-Content -Path \$file.FullName -Value \$content -Encoding UTF8 -Force; ^
            Write-Host \"  âœ“ \$(\$file.Name)\" -ForegroundColor Green; ^
            \$count++ ^
        } ^
    } catch { } ^
} ^
Write-Host \"`nâœ… \$count Dateien aktualisiert!\" -ForegroundColor Green ^
"

echo.

REM ===============================================================================
REM PHASE 3: DEMO RESET
REM ===============================================================================

echo [3/5] DEMO AUF 30 TAGE ZURÃœCKGESETZT ...
echo.

rmdir /s /q "%APPDATA%\MaterialManager_1.0.x" 2>nul
if not exist "%APPDATA%\MaterialManager_V01" mkdir "%APPDATA%\MaterialManager_V01"

powershell -NoProfile -Command "^
\$path = '%APPDATA%\MaterialManager_V01'; ^
\$file = Join-Path \$path 'demo_start.dat'; ^
Set-Content -Path \$file -Value (Get-Date -Format 'yyyy-MM-dd HH:mm:ss') -Force; ^
(Get-Item \$file).Attributes = 'Hidden'; ^
Write-Host 'âœ… Demo zurÃ¼ckgesetzt!' -ForegroundColor Green ^
"

echo.

REM ===============================================================================
REM PHASE 4: BUILD
REM ===============================================================================

echo [4/5] BUILD STARTET ...
echo.

cd /d "%DEST%"

dotnet build MaterialManager_V01.csproj 2>&1 | findstr /C:"succeeded" /C:"Build succeeded" /C:"failed"

if %errorlevel% neq 0 (
    color 0C
    echo [FEHLER] BUILD FEHLGESCHLAGEN!
    pause
    exit /b 1
)

echo [OK] Build erfolgreich!
echo.

REM ===============================================================================
REM PHASE 5: STARTE PROGRAMM
REM ===============================================================================

echo [5/5] STARTE MATERIALMANAGER V01 ...
echo.

start "" "bin\Debug\net8.0-windows\MaterialManager_V01.exe"

timeout /t 3 >nul

echo.
echo ===============================================================================
echo MIGRATION KOMPLETT ERFOLGREICH!
echo.
echo V01 ist jetzt aktiv und laeuft!
echo Adresse: %DEST%
echo.
echo NAECHSTE SCHRITTE:
echo 1. Teste das Programm vollstaendig
echo 2. Erstelle GitHub Repository (MaterialManager_V01)
echo 3. Pushe Code
echo 4. Erstelle USB-Paket
echo 5. Verkaufe!
echo ===============================================================================
echo.

pause

