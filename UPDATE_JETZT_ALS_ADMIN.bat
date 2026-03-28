@echo off
echo Starte Update als Administrator...
set "SRC=D:\MaterialManager_V01_komplett\USB_Installation\MaterialManager"
set "DST=C:\Program Files\MaterialManager"
taskkill /f /im MaterialManager_V01.exe >nul 2>&1
timeout /t 2 /nobreak >nul
xcopy /E /Y /I "%SRC%\*" "%DST%\" >nul
if %errorlevel%==0 (
    echo Update erfolgreich installiert!
) else (
    echo Fehler beim Kopieren - evtl. kein Admin-Recht?
)
pause
