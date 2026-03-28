@echo off
REM =====================================================================
REM MaterialManager - One-Click Multi-PC Installation
REM Installiert lokal + schreibt Netzwerk-Konfiguration für diesen PC
REM =====================================================================

setlocal
set "SCRIPT_DIR=%~dp0"

echo.
echo =====================================================================
echo   MaterialManager - Multi-PC Installation (One-Click)
echo =====================================================================
echo.

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Administrator-Rechte werden angefordert...
    powershell -Command "Start-Process cmd -ArgumentList '/c ""%~f0""' -Verb RunAs"
    exit /b 0
)

call "%SCRIPT_DIR%Install_LocalPC.bat"
if %errorlevel% neq 0 (
    echo.
    echo FEHLER: Lokale Installation fehlgeschlagen.
    pause
    exit /b 1
)

echo.
set /p NETZPFAD=Netzwerkpfad für gemeinsame Daten eingeben (z.B. \\SERVER\MaterialManager\Daten): 
if "%NETZPFAD%"=="" (
    echo Kein Netzwerkpfad angegeben. Installation bleibt lokal.
    pause
    exit /b 0
)

set /p ARCHIVPFAD=Archivpfad eingeben [ENTER = %NETZPFAD%\Auftragsarchiv]: 
if "%ARCHIVPFAD%"=="" set "ARCHIVPFAD=%NETZPFAD%\Auftragsarchiv"

powershell -NoProfile -ExecutionPolicy Bypass -Command "$cfgDir = Join-Path $env:LOCALAPPDATA 'MaterialManager_V01'; if (!(Test-Path $cfgDir)) { New-Item -ItemType Directory -Path $cfgDir | Out-Null }; $cfg = [ordered]@{ Aktiviert = $true; NetzwerkPfad = '%NETZPFAD%'; BenutzerName = $env:USERNAME; AuftragsArchivPfad = '%ARCHIVPFAD%' }; $json = $cfg | ConvertTo-Json -Depth 4; Set-Content -Path (Join-Path $cfgDir 'netzwerk_config.json') -Value $json -Encoding UTF8; if (!(Test-Path '%NETZPFAD%')) { New-Item -ItemType Directory -Path '%NETZPFAD%' -Force | Out-Null }; if (!(Test-Path '%ARCHIVPFAD%')) { New-Item -ItemType Directory -Path '%ARCHIVPFAD%' -Force | Out-Null }"

echo.
echo Netzwerk-Konfiguration geschrieben:
echo   Daten:  %NETZPFAD%
echo   Archiv: %ARCHIVPFAD%
echo.
echo Fertig. Bitte dieses Skript auf jedem PC ausführen.
pause
exit /b 0
