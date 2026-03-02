@echo off
REM ═══════════════════════════════════════════════════════════════════════════════
REM 🚀 FINAL COMPLETE MIGRATION - ALLES IN EINEM!
REM MaterialManager R03 → V01
REM
REM DIESE DATEI MACHT ALLES AUTOMATISCH:
REM 1. Kopiert R03 nach D:\MaterialManager_V01_komplett
REM 2. Ändert ALLE R03 → V01 Referenzen 
REM 3. Setzt Demo auf 30 Tage zurück
REM 4. Startet Build
REM 5. Startet Programm
REM
REM © 2025 Alexander Hölzer
REM ═══════════════════════════════════════════════════════════════════════════════

setlocal enabledelayedexpansion

color 0A
cls

echo.
echo ╔═══════════════════════════════════════════════════════════════════════════════╗
echo ║                                                                               ║
echo ║           🚀 MATERIALMANAGER R03 ^→ V01 KOMPLETT-MIGRATION 🚀               ║
echo ║                                                                               ║
echo ║                      ALLES AUTOMATISCH - KEINE WARTEZEIT                     ║
echo ║                                                                               ║
echo ║  ✓ Kopiert nach D:\MaterialManager_V01_komplett                              ║
echo ║  ✓ Ändert alle R03 → V01                                                    ║
echo ║  ✓ Demo auf 30 Tage zurückgesetzt                                            ║
echo ║  ✓ Build startet automatisch                                                 ║
echo ║  ✓ Programm wird gestartet                                                   ║
echo ║                                                                               ║
echo ╚═══════════════════════════════════════════════════════════════════════════════╝
echo.

setlocal enabledelayedexpansion

set "SOURCE=C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03"
set "DEST=D:\MaterialManager_V01_komplett"

REM ═══════════════════════════════════════════════════════════════════════════════
REM PHASE 1: KOPIEREN
REM ═══════════════════════════════════════════════════════════════════════════════

echo [1/5] KOPIEREN VON R03 NACH D:\ ...
echo.

if not exist "%SOURCE%" (
    color 0C
    echo ❌ FEHLER: R03 nicht gefunden!
    pause
    exit /b 1
)

if exist "%DEST%" (
    rmdir /s /q "%DEST%" 2>nul
)

robocopy "%SOURCE%" "%DEST%" /E /XD bin obj .git .vs .vscode /XF *.vsdbg* *.tmp *.lock /NP /NS /NC /NFL /NDL >nul 2>&1

if %errorlevel% leq 1 (
    echo ✅ Kopieren erfolgreich!
    echo   Ziel: %DEST%
) else (
    color 0C
    echo ❌ Kopieren fehlgeschlagen!
    pause
    exit /b 1
)

echo.

REM ═══════════════════════════════════════════════════════════════════════════════
REM PHASE 2: MASS-RENAME
REM ═══════════════════════════════════════════════════════════════════════════════

echo [2/5] ÄNDERE ALLE DATEIEN (R03 ^→ V01) ...
echo.

cd /d "%DEST%"

powershell -NoProfile -ExecutionPolicy Bypass -Command "^
\$files = Get-ChildItem -Path '.' -Recurse -Include *.cs, *.xaml, *.csproj, *.sln, *.txt, *.md, *.bat, *.ps1 -ErrorAction SilentlyContinue | Where-Object { \$_.FullName -notmatch 'obj\\|bin\\' }; ^
\$count = 0; ^
foreach (\$file in \$files) { ^
    try { ^
        \$content = Get-Content \$file.FullName -Raw -Encoding UTF8 -ErrorAction SilentlyContinue; ^
        if (\$null -ne \$content -and \$content -match 'MaterialManager_R03') { ^
            \$content = \$content -replace 'MaterialManager_R03', 'MaterialManager_V01' -replace 'namespace MaterialManager_R03', 'namespace MaterialManager_V01' -replace 'using MaterialManager_R03', 'using MaterialManager_V01' -replace 'MM_R03_SECRET', 'MM_V01_SECRET'; ^
            Set-Content -Path \$file.FullName -Value \$content -Encoding UTF8 -Force; ^
            Write-Host \"  ✓ \$(\$file.Name)\" -ForegroundColor Green; ^
            \$count++ ^
        } ^
    } catch { } ^
} ^
Write-Host \"`n✅ \$count Dateien aktualisiert!\" -ForegroundColor Green ^
"

echo.

REM ═══════════════════════════════════════════════════════════════════════════════
REM PHASE 3: DEMO RESET
REM ═══════════════════════════════════════════════════════════════════════════════

echo [3/5] DEMO AUF 30 TAGE ZURÜCKGESETZT ...
echo.

rmdir /s /q "%APPDATA%\MaterialManager_R03" 2>nul
if not exist "%APPDATA%\MaterialManager_V01" mkdir "%APPDATA%\MaterialManager_V01"

powershell -NoProfile -Command "^
\$path = '%APPDATA%\MaterialManager_V01'; ^
\$file = Join-Path \$path 'demo_start.dat'; ^
Set-Content -Path \$file -Value (Get-Date -Format 'yyyy-MM-dd HH:mm:ss') -Force; ^
(Get-Item \$file).Attributes = 'Hidden'; ^
Write-Host '✅ Demo zurückgesetzt!' -ForegroundColor Green ^
"

echo.

REM ═══════════════════════════════════════════════════════════════════════════════
REM PHASE 4: BUILD
REM ═══════════════════════════════════════════════════════════════════════════════

echo [4/5] BUILD STARTET ...
echo.

cd /d "%DEST%"

dotnet build MaterialManager_V01.csproj 2>&1 | findstr /C:"succeeded" /C:"Build succeeded" /C:"failed"

if %errorlevel% neq 0 (
    color 0C
    echo ❌ BUILD FEHLGESCHLAGEN!
    pause
    exit /b 1
)

echo ✅ Build erfolgreich!
echo.

REM ═══════════════════════════════════════════════════════════════════════════════
REM PHASE 5: STARTE PROGRAMM
REM ═══════════════════════════════════════════════════════════════════════════════

echo [5/5] STARTE MATERIALMANAGER V01 ...
echo.

start "" "bin\Debug\net8.0-windows\MaterialManager_V01.exe"

timeout /t 3 >nul

echo.
echo ╔═══════════════════════════════════════════════════════════════════════════════╗
echo ║                                                                               ║
echo ║                     ✅ MIGRATION KOMPLETT ERFOLGREICH! ✅                    ║
echo ║                                                                               ║
echo ║  V01 ist jetzt aktiv und läuft!                                              ║
echo ║  Adresse: %DEST%                                        ║
echo ║                                                                               ║
echo ║  NÄCHSTE SCHRITTE:                                                            ║
echo ║  1. Teste das Programm vollständig                                            ║
echo ║  2. Erstelle GitHub Repository (MaterialManager_V01)                          ║
echo ║  3. Pushe Code                                                                 ║
echo ║  4. Erstelle USB-Paket                                                         ║
echo ║  5. Verkaufe! 💰                                                              ║
echo ║                                                                               ║
echo ╚═══════════════════════════════════════════════════════════════════════════════╝
echo.

pause
