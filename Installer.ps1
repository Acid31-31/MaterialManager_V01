# MaterialManager R03 Installer - PowerShell Version
# Installiert MaterialManager auf dem Zielrechner

param(
    [string]$SourcePath = $PSScriptRoot,
    [string]$InstallPath = "C:\Program Files\MaterialManager_V01"
)

Write-Host "`n╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  🚀 MaterialManager R03 - Installationsprogramm              ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# Admin-Check
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $isAdmin) {
    Write-Host "❌ Fehler: Admin-Rechte erforderlich!" -ForegroundColor Red
    Write-Host "`nBitte:" -ForegroundColor Yellow
    Write-Host "1. Rechtsklick auf diese Datei" -ForegroundColor White
    Write-Host "2. 'Mit PowerShell ausführen (Admin)'" -ForegroundColor White
    Read-Host "Drücke Enter zum Beenden"
    exit 1
}

Write-Host "✓ Admin-Rechte bestätigt`n" -ForegroundColor Green

# Prüfe Quellordner
Write-Host "1️⃣  Prüfe USB_Distribution..." -ForegroundColor Cyan
$usbDistPath = Join-Path $SourcePath "USB_Distribution"

if (-not (Test-Path $usbDistPath)) {
    Write-Host "⚠️  USB_Distribution nicht gefunden" -ForegroundColor Yellow
    Write-Host "   Nutze Quellpfad: $SourcePath`n" -ForegroundColor Gray
    $sourceDir = $SourcePath
} else {
    Write-Host "✓ USB_Distribution gefunden`n" -ForegroundColor Green
    $sourceDir = $usbDistPath
}

# Erstelle Installationspfad
Write-Host "2️⃣  Erstelle Installationspfad..." -ForegroundColor Cyan
if (-not (Test-Path $InstallPath)) {
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
    Write-Host "✓ Ordner erstellt: $InstallPath`n" -ForegroundColor Green
} else {
    Write-Host "✓ Ordner existiert bereits: $InstallPath`n" -ForegroundColor Green
}

# Kopiere Dateien
Write-Host "3️⃣  Kopiere Dateien..." -ForegroundColor Cyan
$files = Get-ChildItem -Path $sourceDir -Recurse -File
$total = $files.Count
$copied = 0

foreach ($file in $files) {
    $relativePath = $file.FullName.Substring($sourceDir.Length + 1)
    $destPath = Join-Path $InstallPath $relativePath
    $destDir = Split-Path $destPath

    if (-not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }

    Copy-Item -Path $file.FullName -Destination $destPath -Force
    $copied++
    $percent = [math]::Round(($copied / $total) * 100)
    Write-Progress -Activity "Kopiere Dateien" -Status "$copied von $total" -PercentComplete $percent
}

Write-Host "`n✓ $copied Dateien kopiert`n" -ForegroundColor Green

# Starte Anwendung
Write-Host "4️⃣  Starte Anwendung..." -ForegroundColor Cyan
$exePath = Join-Path $InstallPath "MaterialManager_V01.exe"

if (Test-Path $exePath) {
    Start-Process $exePath
    Write-Host "✓ MaterialManager wird gestartet...`n" -ForegroundColor Green
} else {
    Write-Host "⚠️  Anwendung nicht gefunden: $exePath`n" -ForegroundColor Yellow
}

Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ Installation abgeschlossen!`n" -ForegroundColor Green

Read-Host "Drücke Enter zum Beenden"
