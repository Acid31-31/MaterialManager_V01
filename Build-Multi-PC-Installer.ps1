# ╔════════════════════════════════════════════════════════════════════════╗
# ║  MATERIALMANAGER R03 - MULTI-PC INSTALLER CREATOR                     ║
# ║  Erstellt EXE für Installation auf mehreren PCs per USB               ║
# ╚════════════════════════════════════════════════════════════════════════╝

param(
    [switch]$CreateInstallerEXE = $false,
    [switch]$SignExecutable = $false
)

# ═══════════════════════════════════════════════════════════════════════════
# KONFIGURATION
# ═══════════════════════════════════════════════════════════════════════════

$ProjectRoot = "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01"
$UsbInstallation = "$ProjectRoot\USB_Installation"
$InstallerSource = "$ProjectRoot\Installer_Source"
$OutputEXE = "$ProjectRoot\MaterialManager_V01_Installer.exe"
$InstallerScript = "$InstallerSource\Installer.ps1"

# Farben
function Write-Success { Write-Host "✅ $args" -ForegroundColor Green }
function Write-Error { Write-Host "❌ $args" -ForegroundColor Red }
function Write-Warning { Write-Host "⚠️  $args" -ForegroundColor Yellow }
function Write-Info { Write-Host "ℹ️  $args" -ForegroundColor Cyan }
function Write-Step { Write-Host "`n[SCHRITT $args]" -ForegroundColor Cyan }

Clear-Host

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                                                                        ║" -ForegroundColor Cyan
Write-Host "║  🔧 MATERIALMANAGER R03 - MULTI-PC INSTALLER CREATOR                 ║" -ForegroundColor Cyan
Write-Host "║     Installation per USB-Stick auf mehreren PCs                      ║" -ForegroundColor Cyan
Write-Host "║                                                                        ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ═══════════════════════════════════════════════════════════════════════════
# SCHRITT 1: INSTALLER SETUP SCRIPT ERSTELLEN
# ═══════════════════════════════════════════════════════════════════════════

Write-Step "1/3 - Erstelle Installer-Setup Script..."

$InstallerScriptContent = @'
# ╔════════════════════════════════════════════════════════════════════════╗
# ║  MATERIALMANAGER R03 - INSTALLER SCRIPT                              ║
# ║  Installiert das Programm auf einem Windows-PC                       ║
# ╚════════════════════════════════════════════════════════════════════════╝

param(
    [string]$InstallPath = "C:\Program Files\MaterialManager_V01",
    [bool]$CreateDesktopShortcut = $true,
    [bool]$LaunchAfterInstall = $true
)

# ═══════════════════════════════════════════════════════════════════════════
# FUNKTIONEN
# ═══════════════════════════════════════════════════════════════════════════

function Write-Header {
    Clear-Host
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║  🔧 MATERIALMANAGER R03 INSTALLATION                         ║" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
}

function Test-Admin {
    $CurrentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $Principal = New-Object Security.Principal.WindowsPrincipal($CurrentUser)
    return $Principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Create-InstallationDirectory {
    param([string]$Path)
    
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
        Write-Host "✅ Installationsordner erstellt: $Path" -ForegroundColor Green
    } else {
        Write-Host "ℹ️  Installationsordner existiert bereits" -ForegroundColor Cyan
    }
}

function Copy-ProgrammFiles {
    param([string]$SourcePath, [string]$DestPath)
    
    Write-Host ""
    Write-Host "Kopiere Programmfiles..." -ForegroundColor White
    
    # Kopiere Programm-Ordner
    if (Test-Path "$SourcePath\Programm") {
        Copy-Item -Path "$SourcePath\Programm\*" -Destination "$DestPath\" -Recurse -Force
        Write-Host "✅ Programmfiles kopiert" -ForegroundColor Green
    } else {
        Write-Host "❌ Programmordner nicht gefunden!" -ForegroundColor Red
        return $false
    }
    
    # Kopiere Anleitung
    if (Test-Path "$SourcePath\Anleitung") {
        Copy-Item -Path "$SourcePath\Anleitung" -Destination "$DestPath\Anleitung" -Recurse -Force
        Write-Host "✅ Anleitung kopiert" -ForegroundColor Green
    }
    
    # Kopiere Tools
    if (Test-Path "$SourcePath\Tools") {
        Copy-Item -Path "$SourcePath\Tools" -Destination "$DestPath\Tools" -Recurse -Force
        Write-Host "✅ Tools kopiert" -ForegroundColor Green
    }
    
    return $true
}

function Create-DesktopShortcut {
    param([string]$ProgramPath, [string]$InstallPath)
    
    Write-Host ""
    Write-Host "Erstelle Desktop-Shortcut..." -ForegroundColor White
    
    $DesktopPath = [Environment]::GetFolderPath("Desktop")
    $ShortcutPath = "$DesktopPath\MaterialManager R03.lnk"
    
    $WshShell = New-Object -ComObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
    $Shortcut.TargetPath = $ProgramPath
    $Shortcut.WorkingDirectory = Split-Path $ProgramPath
    $Shortcut.Description = "MaterialManager R03 - Materialverwaltung für Blechlager"
    $Shortcut.Save()
    
    Write-Host "✅ Desktop-Shortcut erstellt" -ForegroundColor Green
}

function Create-StartMenuShortcut {
    param([string]$ProgramPath, [string]$InstallPath)
    
    Write-Host "Erstelle Start-Menü Shortcut..." -ForegroundColor White
    
    $StartMenuPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs"
    $CompanyFolder = "$StartMenuPath\MaterialManager"
    
    if (-not (Test-Path $CompanyFolder)) {
        New-Item -ItemType Directory -Path $CompanyFolder -Force | Out-Null
    }
    
    $ShortcutPath = "$CompanyFolder\MaterialManager R03.lnk"
    
    $WshShell = New-Object -ComObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
    $Shortcut.TargetPath = $ProgramPath
    $Shortcut.WorkingDirectory = Split-Path $ProgramPath
    $Shortcut.Description = "MaterialManager R03"
    $Shortcut.Save()
    
    Write-Host "✅ Start-Menü Shortcut erstellt" -ForegroundColor Green
}

function Create-UninstallScript {
    param([string]$InstallPath)
    
    Write-Host "Erstelle Deinstallations-Script..." -ForegroundColor White
    
    $UninstallScript = "$InstallPath\Uninstall.bat"
    
    @"
@echo off
REM Deinstallation von MaterialManager R03

if not "%1"=="am_admin" (
    powershell -ExecutionPolicy Bypass -Command "Start-Process cmd -ArgumentList '/c %0 am_admin' -Verb RunAs"
    exit /b
)

echo.
echo Deinstalliere MaterialManager R03...
echo.

REM Entferne Ordner
rmdir /s /q "$InstallPath" 2>nul

REM Entferne Desktop-Shortcut
del "%USERPROFILE%\Desktop\MaterialManager R03.lnk" 2>nul

REM Entferne Start-Menü Shortcuts
rmdir /s /q "%APPDATA%\Microsoft\Windows\Start Menu\Programs\MaterialManager" 2>nul

echo.
echo Deinstallation abgeschlossen!
echo.
pause
"@ | Set-Content $UninstallScript -Force
    
    Write-Host "✅ Deinstallations-Script erstellt" -ForegroundColor Green
}

function Initialize-Database {
    param([string]$InstallPath)
    
    Write-Host ""
    Write-Host "Initialisiere Datenbank..." -ForegroundColor White
    
    # Prüfe ob Programm Datenbank braucht
    $ProgramExe = "$InstallPath\MaterialManager_V01.exe"
    
    if (Test-Path $ProgramExe) {
        Write-Host "ℹ️  Datenbank wird beim ersten Start automatisch erstellt" -ForegroundColor Cyan
        Write-Host "✅ Datenbankinitialisierung vorbereitet" -ForegroundColor Green
    }
}

# ═══════════════════════════════════════════════════════════════════════════
# HAUPTPROGRAMM
# ═══════════════════════════════════════════════════════════════════════════

Write-Header

# Prüfe Admin-Rechte
if (-not (Test-Admin)) {
    Write-Host "❌ Dieses Skript benötigt Admin-Rechte!" -ForegroundColor Red
    Write-Host "Starte PowerShell als Administrator und versuche es erneut." -ForegroundColor Yellow
    Read-Host "Drücke Enter zum Beenden"
    exit 1
}

Write-Host "✅ Admin-Rechte bestätigt" -ForegroundColor Green
Write-Host ""

# Installationspfad
Write-Host "Installationspfad: $InstallPath" -ForegroundColor White
Write-Host ""

# Erstelle Installationsverzeichnis
Create-InstallationDirectory $InstallPath

# Kopiere Dateien
if (-not (Copy-ProgrammFiles "$PSScriptRoot\..\USB_Installation" $InstallPath)) {
    Write-Host ""
    Write-Host "❌ Installation fehlgeschlagen!" -ForegroundColor Red
    Read-Host "Drücke Enter zum Beenden"
    exit 1
}

# Erstelle Shortcuts
if ($CreateDesktopShortcut) {
    Create-DesktopShortcut "$InstallPath\MaterialManager_V01.exe" $InstallPath
    Create-StartMenuShortcut "$InstallPath\MaterialManager_V01.exe" $InstallPath
}

# Erstelle Deinstallations-Script
Create-UninstallScript $InstallPath

# Initialisiere Datenbank
Initialize-Database $InstallPath

# Fertigstellung
Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ INSTALLATION ERFOLGREICH ABGESCHLOSSEN!" -ForegroundColor Green
Write-Host ""
Write-Host "📁 Installiert in: $InstallPath" -ForegroundColor White
Write-Host ""
Write-Host "🚀 Nächste Schritte:" -ForegroundColor Cyan
Write-Host "   1. Desktop-Shortcut: MaterialManager R03" -ForegroundColor White
Write-Host "   2. oder Start-Menü: MaterialManager > MaterialManager R03" -ForegroundColor White
Write-Host "   3. Beim ersten Start: Lizenzschlüssel eingeben" -ForegroundColor White
Write-Host ""

if ($LaunchAfterInstall) {
    Write-Host "⏳ Starte Programm in 5 Sekunden..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
    & "$InstallPath\MaterialManager_V01.exe"
}

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
'@

if (-not (Test-Path $InstallerSource)) {
    New-Item -ItemType Directory -Path $InstallerSource -Force | Out-Null
}

$InstallerScriptContent | Set-Content $InstallerScript -Force
Write-Success "Installer-Script erstellt: $InstallerScript"

# ═══════════════════════════════════════════════════════════════════════════
# SCHRITT 2: INNOSETUP SCRIPT ERSTELLEN
# ═══════════════════════════════════════════════════════════════════════════

Write-Step "2/3 - Erstelle InnoSetup Script..."

$InnoSetupScript = "$ProjectRoot\MaterialManager_Installer.iss"

$InnoSetupContent = @"
; Inno Setup Script für MaterialManager R03
; Professioneller Installer für Multi-PC Installation

[Setup]
AppName=MaterialManager R03
AppVersion=1.0.0
AppPublisher=MaterialManager
AppPublisherURL=https://www.materialmanager.de
AppSupportURL=https://support.materialmanager.de
AppUpdatesURL=https://www.materialmanager.de/updates
DefaultDirName={pf}\MaterialManager_V01
DefaultGroupName=MaterialManager R03
OutputDir=.
OutputBaseFilename=MaterialManager_V01_Installer
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
MinVersion=10.0.17763
WizardStyle=modern

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Desktop-Verknüpfung erstellen"; GroupDescription: "Zusätzliche Einstellungen"
Name: "startmenuicon"; Description: "Start-Menü Verknüpfung erstellen"; GroupDescription: "Zusätzliche Einstellungen"; Flags: checked

[Files]
Source: "USB_Installation\Programm\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "USB_Installation\Anleitung\*"; DestDir: "{app}\Anleitung"; Flags: ignoreversion recursesubdirs
Source: "USB_Installation\Tools\*"; DestDir: "{app}\Tools"; Flags: ignoreversion recursesubdirs
Source: "LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\MaterialManager R03"; Filename: "{app}\MaterialManager_V01.exe"; WorkingDir: "{app}"
Name: "{group}\Anleitung"; Filename: "{app}\Anleitung\QUICK_START.md"
Name: "{group}\Uninstall MaterialManager R03"; Filename: "{uninstallexe}"
Name: "{commondesktop}\MaterialManager R03"; Filename: "{app}\MaterialManager_V01.exe"; Tasks: desktopicon
Name: "{commonstartmenu}\MaterialManager R03"; Filename: "{app}\MaterialManager_V01.exe"; Tasks: startmenuicon

[Run]
Filename: "{app}\MaterialManager_V01.exe"; Description: "MaterialManager R03 starten"; Flags: nowait postinstall skipifsilent

[InstallDelete]
Type: files; Name: "{app}\*.tmp"
"@

$InnoSetupContent | Set-Content $InnoSetupScript -Force
Write-Success "InnoSetup Script erstellt: $InnoSetupScript"

# ═══════════════════════════════════════════════════════════════════════════
# SCHRITT 3: INFORMATIONEN ANZEIGEN
# ═══════════════════════════════════════════════════════════════════════════

Write-Step "3/3 - Zusammenfassung"

Write-Host ""
Write-Host "✅ INSTALLER-SCRIPTS ERFOLGREICH ERSTELLT!" -ForegroundColor Green
Write-Host ""
Write-Host "📁 Erstellte Dateien:" -ForegroundColor Cyan
Write-Host "   1. $InstallerScript" -ForegroundColor White
Write-Host "   2. $InnoSetupScript" -ForegroundColor White
Write-Host ""

Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "🚀 NÄCHSTE SCHRITTE FÜR MULTI-PC INSTALLATION:" -ForegroundColor Yellow
Write-Host ""
Write-Host "OPTION 1: Mit InnoSetup (PROFESSIONELL) ⭐ EMPFOHLEN" -ForegroundColor Green
Write-Host "──────────────────────────────────────────────────────" -ForegroundColor Green
Write-Host "  1. Lade InnoSetup herunter (kostenlos):" -ForegroundColor White
Write-Host "     https://jrsoftware.org/isdl.php" -ForegroundColor Cyan
Write-Host ""
Write-Host "  2. Installiere InnoSetup" -ForegroundColor White
Write-Host ""
Write-Host "  3. Doppelklick auf:" -ForegroundColor White
Write-Host "     MaterialManager_Installer.iss" -ForegroundColor Cyan
Write-Host ""
Write-Host "  4. Klick 'Compile'" -ForegroundColor White
Write-Host ""
Write-Host "  5. Fertig! EXE wird erstellt:" -ForegroundColor White
Write-Host "     MaterialManager_V01_Installer.exe" -ForegroundColor Green
Write-Host ""
Write-Host "OPTION 2: Mit PowerShell (Manuell)" -ForegroundColor Cyan
Write-Host "──────────────────────────────────" -ForegroundColor Cyan
Write-Host "  PowerShell als Admin starten:" -ForegroundColor White
Write-Host "  .\Installer_Source\Installer.ps1" -ForegroundColor Green
Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📦 FÜR MULTI-PC INSTALLATION (USB-STICK):" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Kopiere folgende Datei auf USB-Stick:" -ForegroundColor White
Write-Host "    MaterialManager_V01_Installer.exe" -ForegroundColor Green
Write-Host ""
Write-Host "  Auf jedem PC:" -ForegroundColor White
Write-Host "    1. USB einstecken" -ForegroundColor Cyan
Write-Host "    2. MaterialManager_V01_Installer.exe starten" -ForegroundColor Cyan
Write-Host "    3. Installation läuft automatisch" -ForegroundColor Cyan
Write-Host "    4. ✅ FERTIG!" -ForegroundColor Cyan
Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Read-Host "Drücke Enter zum Beenden"
