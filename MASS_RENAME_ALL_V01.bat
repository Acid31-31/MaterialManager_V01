@echo off
REM ═══════════════════════════════════════════════════════════════════════════════
REM MASS-RENAME: Ändert ALLE MaterialManager_R03 → MaterialManager_V01
REM © 2025 Alexander Hölzer
REM ═══════════════════════════════════════════════════════════════════════════════

setlocal enabledelayedexpansion

cls
echo.
echo ╔═══════════════════════════════════════════════════════════════════════════════╗
echo ║                       MASS-RENAME: R03 ^→ V01                              ║
echo ║                                                                               ║
echo ║                    ALLE Dateien werden geändert!                             ║
echo ╚═══════════════════════════════════════════════════════════════════════════════╝
echo.

set "count=0"

echo 📁 Durchsuche alle *.cs Dateien...
echo.

for /r %%F in (*.cs) do (
    setlocal enabledelayedexpansion
    
    set "file=%%F"
    set "changed=0"
    
    REM Lese Datei
    for /f "delims=" %%A in ('type "!file!"') do (
        set "line=%%A"
        
        REM Ersetze MaterialManager_R03
        if "!line:MaterialManager_R03=!" neq "!line!" (
            set "line=!line:MaterialManager_R03=MaterialManager_V01!"
            set "changed=1"
        )
        
        echo !line!
    ) > temp_file.txt
    
    if !changed! equ 1 (
        move /y temp_file.txt "!file!" >nul
        set /a count+=1
        echo ✓ !file!
    ) else (
        del temp_file.txt >nul 2>&1
    )
    
    endlocal
)

echo.
echo ✓ !count! Dateien aktualisiert!
echo.
echo ═══════════════════════════════════════════════════════════════════════════════
echo ✅ MASS-RENAME ABGESCHLOSSEN!
echo ═══════════════════════════════════════════════════════════════════════════════
echo.

pause
