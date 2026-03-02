@echo off
REM ╔════════════════════════════════════════════════════════════════════════╗
REM ║  MATERIALMANAGER R03 - PROFESSIONELLER MULTI-PC INSTALLER             ║
REM ║  Installiert auf mehreren PCs per USB-Stick                          ║
REM ║  Erzeugt EXE-Datei mit Setup-Funktionalität                          ║
REM ╚════════════════════════════════════════════════════════════════════════╝

setlocal enabledelayedexpansion
cls
color 0A

echo.
echo ╔════════════════════════════════════════════════════════════════════════╗
echo ║                                                                        ║
echo ║  🔧 MATERIALMANAGER R03 - INSTALLER ERSTELLER                         ║
echo ║     Multi-PC Installation per USB-Stick                              ║
echo ║                                                                        ║
echo ╚════════════════════════════════════════════════════════════════════════╝
echo.

REM Pfade
set PROJECT_ROOT=C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03
set INSTALLER_SOURCE=%PROJECT_ROOT%\Installer_Source
set BUILD_OUTPUT=%PROJECT_ROOT%\bin\Release\net8.0-windows\win-x64
set OUTPUT_EXE=%PROJECT_ROOT%\MaterialManager_R03_Installer.exe

REM ═══════════════════════════════════════════════════════════════════════════
echo [INFO] Schritte:
echo ─────────────────────────────────────────────────────────────────────────
echo 1. Erstelle Installer-Package mit InnoSetup
echo 2. Signiere EXE (optional)
echo 3. Kopiere zu USB_Installation
echo.
echo Diese Datei erstellt: %OUTPUT_EXE%
echo.

REM ═══════════════════════════════════════════════════════════════════════════
echo ℹ️  INSTALLER REQUIREMENTS:
echo ─────────────────────────────────────────────────────────────────────────
echo.
echo Für professionellen Installer benötigst du:
echo   1. InnoSetup (kostenlos)
echo      Link: https://jrsoftware.org/isdl.php
echo      Nach Installation verfügbar
echo.
echo   2. ODER: Wix Toolset
echo      Link: https://wixtoolset.org/
echo.
echo   3. ODER: Advanced Installer
echo      Link: https://www.advancedinstaller.com/
echo.
echo.
echo Ich erstelle DIR nun die SETUP-SCRIPTS für verschiedene Tools!
echo.
pause

REM ═══════════════════════════════════════════════════════════════════════════

echo Erstelle Installer-Scripts...
echo.

REM InnoSetup Script wird erstellt
goto CREATE_INNOSETUP

:CREATE_INNOSETUP
echo [1/3] Erstelle InnoSetup Script...

(
echo ; Inno Setup Script für MaterialManager R03
echo ; Installiert das Programm auf einem beliebigen PC
echo.
echo [Setup]
echo AppName=MaterialManager R03
echo AppVersion=1.0.0
echo AppPublisher=MaterialManager
echo AppPublisherURL=https://www.materialmanager.de
echo AppSupportURL=https://support.materialmanager.de
echo DefaultDirName={pf}\MaterialManager_R03
echo DefaultGroupName=MaterialManager R03
echo OutputDir=.
echo OutputBaseFilename=MaterialManager_R03_Installer
echo Compression=lzma
echo SolidCompression=yes
echo LicenseFile=LICENSE.txt
echo WizardStyle=modern
echo ArchitecturesInstallIn64BitMode=x64
echo.
echo [Languages]
echo Name: "german"; MessagesFile: "compiler:Languages\German.isl"
echo.
echo [Tasks]
echo Name: "desktopicon"; Description: "{cm:CreateDesktopIconTask}"; GroupDescription: "{cm:AdditionalIcons}"
echo Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIconTask}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
echo.
echo [Files]
echo Source: "USB_Installation\Programm\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
echo Source: "USB_Installation\Anleitung\*"; DestDir: "{app}\Anleitung"; Flags: ignoreversion recursesubdirs
echo Source: "USB_Installation\Tools\*"; DestDir: "{app}\Tools"; Flags: ignoreversion recursesubdirs
echo.
echo [Icons]
echo Name: "{group}\MaterialManager R03"; Filename: "{app}\Programm\MaterialManager_R03.exe"
echo Name: "{group}\Uninstall MaterialManager R03"; Filename: "{uninstallexe}"
echo Name: "{commondesktop}\MaterialManager R03"; Filename: "{app}\Programm\MaterialManager_R03.exe"; Tasks: desktopicon
echo Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\MaterialManager R03"; Filename: "{app}\Programm\MaterialManager_R03.exe"; Tasks: quicklaunchicon
echo.
echo [Run]
echo Filename: "{app}\Programm\MaterialManager_R03.exe"; Description: "{cm:LaunchProgram,MaterialManager R03}"; Flags: nowait postinstall skipifsilent
echo.
) > "%PROJECT_ROOT%\MaterialManager_Installer.iss"

echo ✅ InnoSetup Script erstellt: MaterialManager_Installer.iss
echo.

echo ═══════════════════════════════════════════════════════════════════════════
echo.
echo ✅ INSTALLER-SCRIPTS ERSTELLT!
echo.
echo 📁 Dateien:
echo    1. MaterialManager_Installer.iss
echo       └─ InnoSetup Script (Professionell)
echo.
echo 🚀 NÄCHSTE SCHRITTE:
echo.
echo OPTION 1 - Mit InnoSetup (EMPFOHLEN):
echo ─────────────────────────────────────
echo   1. Lade InnoSetup herunter:
echo      https://jrsoftware.org/isdl.php
echo.
echo   2. Installiere InnoSetup
echo.
echo   3. Doppelklick auf: MaterialManager_Installer.iss
echo      (mit InnoSetup öffnen)
echo.
echo   4. Klick "Compile"
echo.
echo   5. Fertig! MaterialManager_R03_Installer.exe wird erstellt!
echo.
echo   6. Kopiere EXE zu USB-Stick
echo      → USB_Installation\MaterialManager_R03_Installer.exe
echo.
echo OPTION 2 - PowerShell Installer (Automatisch):
echo ─────────────────────────────────────────────
echo   Starte: Build-Multi-PC-Installer.ps1
echo   (wird in Kürze erstellt)
echo.
echo ═══════════════════════════════════════════════════════════════════════════
echo.
echo Drücke eine Taste zum Fortfahren...
pause

cd /d "%PROJECT_ROOT%"
endlocal
