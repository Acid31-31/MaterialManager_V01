# MaterialManager R03 - MASTER SETUP SCRIPT (mit Admin-Rechten)
# Erstellt alles: Installer.exe, USB-Paket, vollständiges Installation-Set

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Full", "InstallerOnly", "USBOnly", "CleanBuild")]
    [string]$Action = "Full"
)

$ErrorActionPreference = "Stop"

# ===== Farben & Styling =====
function Write-Title {
    param([string]$Message)
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║  $Message" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Step {
    param([string]$Message)
    Write-Host "▶ $Message" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

# ===== Admin-Check =====
function Check-Admin {
    $currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object System.Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)
}

# ===== Pfade =====
$ProjectRoot = "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01"
$InstallerSourcePath = Join-Path $ProjectRoot "Installer_Source"
$USBInstallationPath = Join-Path $ProjectRoot "USB_Installation"
$BackupPath = Join-Path (Split-Path $ProjectRoot) "Backups"

Write-Title "MATERIALMANAGER R03 - MASTER SETUP"

# Check Admin
if (-not (Check-Admin)) {
    Write-Error "Dieses Skript benötigt Admin-Rechte!"
    Write-Host "Bitte als Administrator ausführen:" -ForegroundColor Yellow
    Write-Host "  Rechtsklick auf PowerShell → 'Als Administrator ausführen'" -ForegroundColor Gray
    exit 1
}

Write-Success "Admin-Rechte bestätigt"

# ===== SCHRITT 1: VORBEREITUNG =====
if ($Action -in @("Full", "InstallerOnly")) {
    Write-Title "SCHRITT 1: VORBEREITUNG - Installer bauen"
    
    Write-Step "Wechsel zu Installer_Source..."
    Push-Location $InstallerSourcePath
    
    Write-Step "Prüfe .NET 8 SDK..."
    $dotnetCheck = dotnet --version 2>$null
    if (-not $dotnetCheck) {
        Write-Error ".NET 8 SDK nicht gefunden!"
        Write-Host "Download: https://dotnet.microsoft.com/en-us/download/dotnet/8.0" -ForegroundColor Yellow
        exit 1
    }
    Write-Success ".NET 8 SDK gefunden: $dotnetCheck"
    
    Write-Step "Bereinige alte Build-Dateien..."
    if (Test-Path "bin") { Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue }
    if (Test-Path "obj") { Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue }
    Write-Success "Alte Dateien gelöscht"
}

# ===== SCHRITT 2: INSTALLER BAUEN =====
if ($Action -in @("Full", "InstallerOnly", "CleanBuild")) {
    Write-Title "SCHRITT 2: INSTALLER.EXE BAUEN"
    
    Write-Step "Starte Build-Prozess..."
    $buildCmd = & dotnet build "$InstallerSourcePath\MaterialManager_Installer.csproj" -c Release 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build fehlgeschlagen!"
        Write-Host $buildCmd
        exit 1
    }
    Write-Success "Build erfolgreich"
    
    Write-Step "Starte Publish-Prozess (self-contained)..."
    $publishCmd = & dotnet publish "$InstallerSourcePath\MaterialManager_Installer.csproj" -c Release --self-contained 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Publish fehlgeschlagen!"
        Write-Host $publishCmd
        exit 1
    }
    Write-Success "Publish erfolgreich"
    
    Write-Step "Benenne MaterialManager_Installer.exe zu Installer.exe um..."
    $exePath = Join-Path $InstallerSourcePath "bin\Release\net8.0-windows\win-x64\publish\MaterialManager_Installer.exe"
    $installerPath = Join-Path $InstallerSourcePath "bin\Release\net8.0-windows\win-x64\publish\Installer.exe"
    
    if (Test-Path $exePath) {
        Move-Item -Path $exePath -Destination $installerPath -Force
        Write-Success "Installer.exe erstellt"
    } else {
        Write-Error "MaterialManager_Installer.exe nicht gefunden!"
        exit 1
    }
    
    Write-Step "Prüfe Dateigröße..."
    $fileSize = [math]::Round((Get-Item $installerPath).Length / 1MB, 2)
    Write-Success "Installer.exe: $fileSize MB"
    
    Pop-Location
}

# ===== SCHRITT 3: USB-PAKET VORBEREITEN =====
if ($Action -in @("Full", "USBOnly")) {
    Write-Title "SCHRITT 3: USB-PAKET VORBEREITEN"
    
    Write-Step "Prüfe USB_Installation Ordner..."
    if (-not (Test-Path $USBInstallationPath)) {
        Write-Error "USB_Installation Ordner existiert nicht!"
        exit 1
    }
    Write-Success "USB_Installation Ordner gefunden"
    
    Write-Step "Kopiere Installer.exe zu USB_Installation..."
    $sourceInstallerPath = Join-Path $InstallerSourcePath "bin\Release\net8.0-windows\win-x64\publish\Installer.exe"
    $destInstallerPath = Join-Path $USBInstallationPath "Installer.exe"
    
    if (Test-Path $sourceInstallerPath) {
        Copy-Item -Path $sourceInstallerPath -Destination $destInstallerPath -Force
        Write-Success "Installer.exe kopiert"
    } else {
        Write-Error "Installer.exe nicht gefunden in $sourceInstallerPath"
        exit 1
    }
}

# ===== SCHRITT 4: BACKUP ERSTELLEN =====
Write-Title "SCHRITT 4: BACKUP ERSTELLEN"

Write-Step "Erstelle Backup des gesamten Projekts..."
if (-not (Test-Path $BackupPath)) {
    New-Item -ItemType Directory -Path $BackupPath -Force | Out-Null
    Write-Success "Backup-Verzeichnis erstellt"
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupZip = Join-Path $BackupPath "MaterialManager_V01_complete_$timestamp.zip"

Compress-Archive -Path (Join-Path $ProjectRoot "*") -DestinationPath $backupZip -Force
$backupSize = [math]::Round((Get-Item $backupZip).Length / 1MB, 2)
Write-Success "Backup erstellt: $backupZip ($backupSize MB)"

# ===== SCHRITT 5: ZUSAMMENFASSUNG =====
Write-Title "SETUP ABGESCHLOSSEN ✅"

Write-Host "📦 ERSTELLTE DATEIEN:" -ForegroundColor Green
Write-Host ""

# Check Installer.exe
$installerCheck = Join-Path $USBInstallationPath "Installer.exe"
if (Test-Path $installerCheck) {
    $size = [math]::Round((Get-Item $installerCheck).Length / 1MB, 2)
    Write-Host "  ✓ Installer.exe ($size MB)" -ForegroundColor Green
    Write-Host "    Location: $installerCheck" -ForegroundColor Gray
} else {
    Write-Host "  ✗ Installer.exe (nicht gefunden)" -ForegroundColor Red
}

# Check USB_Installation Struktur
Write-Host ""
Write-Host "📁 USB_INSTALLATION STRUKTUR:" -ForegroundColor Green
Write-Host ""
Get-ChildItem -Path $USBInstallationPath | ForEach-Object {
    if ($_.PSIsContainer) {
        Write-Host "  📁 $($_.Name)/" -ForegroundColor Blue
    } else {
        $size = [math]::Round($_.Length / 1KB, 2)
        Write-Host "  📄 $($_.Name) ($size KB)" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "🎯 NÄCHSTE SCHRITTE:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1️⃣  Programm-Dateien einfügen:" -ForegroundColor Yellow
Write-Host "   cd $USBInstallationPath" -ForegroundColor Gray
Write-Host "   .\SETUP_PROGRAMM.bat" -ForegroundColor Gray
Write-Host ""
Write-Host "2️⃣  Auf USB-Stick kopieren:" -ForegroundColor Yellow
Write-Host "   copy-item -Path '$USBInstallationPath\*' -Destination 'D:\' -Recurse" -ForegroundColor Gray
Write-Host ""
Write-Host "3️⃣  Auf Zielrechner:" -ForegroundColor Yellow
Write-Host "   USB einstecken → Installer.exe doppelklicken" -ForegroundColor Gray
Write-Host ""
Write-Host "✅ Installation läuft automatisch!" -ForegroundColor Green

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  🎉 MATERIALMANAGER R03 - READY FOR DISTRIBUTION 🎉" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
