@echo off
REM Einfacher Netzwerk-Server Setup
REM Windows Command Prompt Version

cls
color 0A

echo.
echo ════════════════════════════════════════════════════════════════
echo 🖥️  NETZWERK-SERVER ERSTELLEN (Einfach)
echo ════════════════════════════════════════════════════════════════
echo.

REM Prüfe Admin-Rechte
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Bitte als Administrator ausführen!
    echo Rechtsklick auf CMD → "Als Administrator ausführen"
    pause
    exit /b 1
)

echo ✅ Admin-Rechte bestätigt
echo.

set SHARE_PATH=C:\MaterialManager_TestShare
set SHARE_NAME=MaterialManager_TestShare

echo [1] Lösche alte Share (falls vorhanden)...
net share %SHARE_NAME% /delete /yes >nul 2>&1

echo [2] Erstelle Ordner...
if not exist "%SHARE_PATH%" (
    mkdir "%SHARE_PATH%"
    echo ✅ Ordner erstellt: %SHARE_PATH%
) else (
    echo ℹ️  Ordner existiert bereits
)

echo.
echo [3] Erstelle Netzwerk-Share...
net share %SHARE_NAME%="%SHARE_PATH%" /grant:Everyone,FULL /remark:"MaterialManager Test-Share"

if %errorlevel% equ 0 (
    echo ✅ Share erstellt!
) else (
    echo ❌ Fehler beim Erstellen!
    pause
    exit /b 1
)

echo.
echo [4] Prüfe Share...
net share %SHARE_NAME%

echo.
echo ════════════════════════════════════════════════════════════════
echo ✅ NETZWERK-SERVER IST AKTIV!
echo ════════════════════════════════════════════════════════════════
echo.
echo 📍 Share-Informationen:
echo    Name: %SHARE_NAME%
echo    Pfad: %SHARE_PATH%
echo    Adresse: \\localhost\%SHARE_NAME%
echo            ODER
echo            \\%COMPUTERNAME%\%SHARE_NAME%
echo.
echo 🚀 Im Programm eingeben:
echo    Einstellungen → Netzwerk → Pfad:
echo    \\localhost\%SHARE_NAME%
echo.
echo 🧪 Multi-User Test:
echo    Terminal 1: dotnet run
echo    Terminal 2: dotnet run
echo    Beide im Programm Netzwerk aktivieren
echo.
echo ════════════════════════════════════════════════════════════════
echo.

pause
