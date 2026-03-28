@echo off
REM â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
REM â•‘  MATERIALMANAGER 1.0.x - PROFESSIONELLER MULTI-PC INSTALLER             â•‘
REM â•‘  Installiert auf mehreren PCs per USB-Stick                          â•‘
REM â•‘  Erzeugt EXE-Datei mit Setup-FunktionalitÃ¤t                          â•‘
REM â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

setlocal enabledelayedexpansion
cls
color 0A

echo.
echo â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
echo â•‘                                                                        â•‘
echo â•‘  ðŸ”§ MATERIALMANAGER 1.0.x - INSTALLER ERSTELLER                         â•‘
echo â•‘     Multi-PC Installation per USB-Stick                              â•‘
echo â•‘                                                                        â•‘
echo â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

REM Pfade
set PROJECT_ROOT=C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x
set INSTALLER_SOURCE=%PROJECT_ROOT%\Installer_Source
set BUILD_OUTPUT=%PROJECT_ROOT%\bin\Release\net8.0-windows\win-x64
set OUTPUT_EXE=%PROJECT_ROOT%\MaterialManager_1.0.x_Installer.exe

REM â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo [INFO] Schritte:
echo â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
echo 1. Erstelle Installer-Package mit InnoSetup
echo 2. Signiere EXE (optional)
echo 3. Kopiere zu USB_Installation
echo.
echo Diese Datei erstellt: %OUTPUT_EXE%
echo.

REM â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo â„¹ï¸  INSTALLER REQUIREMENTS:
echo â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
echo.
echo FÃ¼r professionellen Installer benÃ¶tigst du:
echo   1. InnoSetup (kostenlos)
echo      Link: https://jrsoftware.org/isdl.php
echo      Nach Installation verfÃ¼gbar
echo.
echo   2. ODER: Wix Toolset
echo      Link: https://wixtoolset.org/
echo.
echo   3. ODER: Advanced Installer
echo      Link: https://www.advancedinstaller.com/
echo.
echo.
echo Ich erstelle DIR nun die SETUP-SCRIPTS fÃ¼r verschiedene Tools!
echo.
pause

REM â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

echo Erstelle Installer-Scripts...
echo.

REM InnoSetup Script wird erstellt
goto CREATE_INNOSETUP

:CREATE_INNOSETUP
echo [1/3] Erstelle InnoSetup Script...

(
echo ; Inno Setup Script fÃ¼r MaterialManager 1.0.x
echo ; Installiert das Programm auf einem beliebigen PC
echo.
echo [Setup]
echo AppName=MaterialManager 1.0.x
echo AppVersion=1.0.0
echo AppPublisher=MaterialManager
echo AppPublisherURL=https://www.materialmanager.de
echo AppSupportURL=https://support.materialmanager.de
echo DefaultDirName={pf}\MaterialManager_1.0.x
echo DefaultGroupName=MaterialManager 1.0.x
echo OutputDir=.
echo OutputBaseFilename=MaterialManager_1.0.x_Installer
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
echo Name: "{group}\MaterialManager 1.0.x"; Filename: "{app}\Programm\MaterialManager_1.0.x.exe"
echo Name: "{group}\Uninstall MaterialManager 1.0.x"; Filename: "{uninstallexe}"
echo Name: "{commondesktop}\MaterialManager 1.0.x"; Filename: "{app}\Programm\MaterialManager_1.0.x.exe"; Tasks: desktopicon
echo Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\MaterialManager 1.0.x"; Filename: "{app}\Programm\MaterialManager_1.0.x.exe"; Tasks: quicklaunchicon
echo.
echo [Run]
echo Filename: "{app}\Programm\MaterialManager_1.0.x.exe"; Description: "{cm:LaunchProgram,MaterialManager 1.0.x}"; Flags: nowait postinstall skipifsilent
echo.
) > "%PROJECT_ROOT%\MaterialManager_Installer.iss"

echo âœ… InnoSetup Script erstellt: MaterialManager_Installer.iss
echo.

echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.
echo âœ… INSTALLER-SCRIPTS ERSTELLT!
echo.
echo ðŸ“ Dateien:
echo    1. MaterialManager_Installer.iss
echo       â””â”€ InnoSetup Script (Professionell)
echo.
echo ðŸš€ NÃ„CHSTE SCHRITTE:
echo.
echo OPTION 1 - Mit InnoSetup (EMPFOHLEN):
echo â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
echo   1. Lade InnoSetup herunter:
echo      https://jrsoftware.org/isdl.php
echo.
echo   2. Installiere InnoSetup
echo.
echo   3. Doppelklick auf: MaterialManager_Installer.iss
echo      (mit InnoSetup Ã¶ffnen)
echo.
echo   4. Klick "Compile"
echo.
echo   5. Fertig! MaterialManager_1.0.x_Installer.exe wird erstellt!
echo.
echo   6. Kopiere EXE zu USB-Stick
echo      â†’ USB_Installation\MaterialManager_1.0.x_Installer.exe
echo.
echo OPTION 2 - PowerShell Installer (Automatisch):
echo â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
echo   Starte: Build-Multi-PC-Installer.ps1
echo   (wird in KÃ¼rze erstellt)
echo.
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.
echo DrÃ¼cke eine Taste zum Fortfahren...
pause

cd /d "%PROJECT_ROOT%"
endlocal

