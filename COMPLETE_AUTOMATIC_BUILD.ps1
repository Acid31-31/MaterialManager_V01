# MaterialManager R03 - VOLLSTÄNDIGER AUTOMATIC BUILD
# Mit Admin-Rechten - Baut ALLES automatisch in einem Durchgang!

param(
    [Parameter(Mandatory=$false)]
    [switch]$SkipTests = $false
)

$ErrorActionPreference = "Stop"

# ===== FARBEN & STYLING =====
function Write-Title {
    param([string]$Message, [string]$Color = "Cyan")
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor $Color
    Write-Host "║  $Message" -ForegroundColor $Color
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor $Color
    Write-Host ""
}

function Write-Step {
    param([string]$Message, [int]$Step, [int]$Total)
    Write-Host "[$Step/$Total] $Message" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

# ===== ADMIN-CHECK =====
function Check-Admin {
    $currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object System.Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)
}

# ===== PFADE =====
$ProjectRoot = "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01"
$InstallerSourcePath = Join-Path $ProjectRoot "Installer_Source"
$USBInstallationPath = Join-Path $ProjectRoot "USB_Installation"
$BackupPath = Join-Path (Split-Path $ProjectRoot) "Backups"

Write-Title "MATERIALMANAGER R03 - VOLLSTÄNDIGER AUTOMATIC BUILD (MIT ADMIN-RECHTEN)" "Cyan"

# ===== ADMIN-CHECK =====
Write-Step "Admin-Rechte prüfen..." 1 6
if (-not (Check-Admin)) {
    Write-Error "Dieses Skript benötigt Admin-Rechte!"
    Write-Host "Bitte als Administrator ausführen!" -ForegroundColor Yellow
    exit 1
}
Write-Success "Admin-Rechte bestätigt"

# ===== SCHRITT 1: VORBEREITUNG =====
Write-Step "Vorbereitung & Validierung..." 1 6
Write-Host "  ├─ Prüfe Projekt-Struktur..." -ForegroundColor Gray
if (-not (Test-Path $ProjectRoot)) {
    Write-Error "Projekt-Verzeichnis nicht gefunden: $ProjectRoot"
    exit 1
}
Write-Host "  ├─ Prüfe .NET 8 SDK..." -ForegroundColor Gray
$dotnetCheck = & dotnet --version 2>$null
if (-not $dotnetCheck) {
    Write-Error ".NET 8 SDK nicht gefunden!"
    exit 1
}
Write-Success ".NET 8 SDK gefunden: $dotnetCheck"

# ===== SCHRITT 2: MASTER_SETUP (INSTALLER.EXE) =====
Write-Step "Starte MASTER_SETUP.ps1 (Installer.exe bauen)..." 2 6
Write-Host "  ├─ Wechsel zu Installer_Source..." -ForegroundColor Gray
Push-Location $InstallerSourcePath

Write-Host "  ├─ Bereinige alte Build-Dateien..." -ForegroundColor Gray
if (Test-Path "bin") { Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path "obj") { Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue }

Write-Host "  ├─ Starte dotnet build..." -ForegroundColor Gray
$buildResult = & dotnet build "$InstallerSourcePath\MaterialManager_Installer.csproj" -c Release 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build fehlgeschlagen!"
    Write-Host $buildResult
    exit 1
}

Write-Host "  ├─ Starte dotnet publish (self-contained)..." -ForegroundColor Gray
$publishResult = & dotnet publish "$InstallerSourcePath\MaterialManager_Installer.csproj" -c Release --self-contained 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish fehlgeschlagen!"
    Write-Host $publishResult
    exit 1
}

Write-Host "  └─ Benenne MaterialManager_Installer.exe zu Installer.exe um..." -ForegroundColor Gray
$exePath = Join-Path $InstallerSourcePath "bin\Release\net8.0-windows\win-x64\publish\MaterialManager_Installer.exe"
$installerPath = Join-Path $InstallerSourcePath "bin\Release\net8.0-windows\win-x64\publish\Installer.exe"
if (Test-Path $exePath) {
    Move-Item -Path $exePath -Destination $installerPath -Force
    $fileSize = [math]::Round((Get-Item $installerPath).Length / 1MB, 2)
    Write-Success "Installer.exe erstellt ($fileSize MB)"
} else {
    Write-Error "MaterialManager_Installer.exe nicht gefunden!"
    exit 1
}

# ===== SCHRITT 3: USB_DISTRIBUTION =====
Write-Step "Starte Build-USBVersion.ps1 (USB_Distribution erstellen)..." 3 6
Pop-Location
Push-Location $ProjectRoot

Write-Host "  ├─ Bereinige alte USB_Distribution..." -ForegroundColor Gray
if (Test-Path "USB_Distribution") { 
    Remove-Item -Path "USB_Distribution" -Recurse -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
}

Write-Host "  ├─ Starte Build-USBVersion.ps1..." -ForegroundColor Gray
if (-not (Test-Path "Build-USBVersion.ps1")) {
    Write-Error "Build-USBVersion.ps1 nicht gefunden!"
    exit 1
}

$usbBuildResult = & powershell -NoProfile -ExecutionPolicy Bypass -Command `
    "cd '$ProjectRoot'; .\Build-USBVersion.ps1 -Action Package" 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build-USBVersion.ps1 fehlgeschlagen!"
    Write-Host $usbBuildResult
    exit 1
}

# Prüfe ob USB_Distribution erstellt wurde
if (-not (Test-Path "USB_Distribution")) {
    Write-Error "USB_Distribution wurde nicht erstellt!"
    exit 1
}

$usbSize = [math]::Round((Get-ChildItem "USB_Distribution" -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB, 2)
Write-Success "USB_Distribution erstellt ($usbSize MB)"

# ===== SCHRITT 4: KOPIERE INSTALLER ZU USB_INSTALLATION =====
Write-Step "Kopiere Installer.exe zu USB_Installation..." 4 6
Write-Host "  ├─ Quelle: $installerPath" -ForegroundColor Gray
Write-Host "  ├─ Ziel: $USBInstallationPath\Installer.exe" -ForegroundColor Gray

if (Test-Path $installerPath) {
    Copy-Item -Path $installerPath -Destination "$USBInstallationPath\Installer.exe" -Force
    Write-Success "Installer.exe kopiert"
} else {
    Write-Error "Installer.exe nicht gefunden!"
    exit 1
}

# ===== SCHRITT 5: SETUP_PROGRAMM (PROGRAM-DATEIEN KOPIEREN) =====
Write-Step "Starte SETUP_PROGRAMM.bat (Programm-Dateien kopieren)..." 5 6
Write-Host "  ├─ Wechsel zu USB_Installation..." -ForegroundColor Gray
Push-Location $USBInstallationPath

Write-Host "  ├─ Bereinige alte Programm-Dateien..." -ForegroundColor Gray
if (Test-Path "Programm") { 
    Remove-Item -Path "Programm" -Recurse -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
}
New-Item -ItemType Directory -Path "Programm" -Force | Out-Null

Write-Host "  ├─ Kopiere Programm-Dateien..." -ForegroundColor Gray
if (Test-Path "..\USB_Distribution") {
    Copy-Item -Path "..\USB_Distribution\*" -Destination "Programm\" -Recurse -Force
    Write-Success "Programm-Dateien kopiert"
} else {
    Write-Error "USB_Distribution nicht gefunden!"
    exit 1
}

$programSize = [math]::Round((Get-ChildItem "Programm" -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB, 2)
Write-Host "  └─ Programm-Ordner: $programSize MB" -ForegroundColor Gray

# ===== SCHRITT 6: BACKUP & ZUSAMMENFASSUNG =====
Write-Step "Erstelle Backup & Zusammenfassung..." 6 6
Pop-Location
Push-Location $ProjectRoot

Write-Host "  ├─ Erstelle Projekt-Backup..." -ForegroundColor Gray
if (-not (Test-Path $BackupPath)) {
    New-Item -ItemType Directory -Path $BackupPath -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupZip = Join-Path $BackupPath "MaterialManager_V01_COMPLETE_BUILDED_$timestamp.zip"
Compress-Archive -Path (Join-Path $ProjectRoot "*") -DestinationPath $backupZip -Force
$backupSize = [math]::Round((Get-Item $backupZip).Length / 1MB, 2)
Write-Success "Backup erstellt ($backupSize MB)"

# ===== ZUSAMMENFASSUNG =====
Write-Title "✅ BUILD ERFOLGREICH ABGESCHLOSSEN!" "Green"

Write-Host "📦 ERSTELLTE DATEIEN:" -ForegroundColor Green
Write-Host ""
Write-Host "  ✓ Installer.exe" -ForegroundColor White
Write-Host "    Location: $InstallerSourcePath\bin\Release\...\publish\Installer.exe" -ForegroundColor Gray
Write-Host "    + Kopiert nach: $USBInstallationPath\Installer.exe" -ForegroundColor Gray
Write-Host ""
Write-Host "  ✓ USB_Distribution" -ForegroundColor White
Write-Host "    Location: $ProjectRoot\USB_Distribution\" -ForegroundColor Gray
Write-Host "    Size: $usbSize MB" -ForegroundColor Gray
Write-Host ""
Write-Host "  ✓ Programm-Dateien" -ForegroundColor White
Write-Host "    Location: $USBInstallationPath\Programm\" -ForegroundColor Gray
Write-Host "    Size: $programSize MB" -ForegroundColor Gray
Write-Host ""

Write-Host "🎯 NÄCHSTE SCHRITTE:" -ForegroundColor Cyan
Write-Host ""
Write-Host "  1️⃣  USB-Stick formatieren (500 MB empfohlen)" -ForegroundColor White
Write-Host "  2️⃣  Kopiere USB_Installation auf USB-Stick:" -ForegroundColor White
Write-Host "      copy-item -Path '$USBInstallationPath\*' -Destination 'D:\' -Recurse" -ForegroundColor Gray
Write-Host "  3️⃣  Auf Zielrechner testen:" -ForegroundColor White
Write-Host "      USB einstecken → Installer.exe doppelklicken" -ForegroundColor Gray
Write-Host "  4️⃣  Installation wird automatisch gestartet ✅" -ForegroundColor White
Write-Host ""

Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  🎉 MATERIALMANAGER R03 - READY FOR DISTRIBUTION 🎉" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""

Pop-Location
