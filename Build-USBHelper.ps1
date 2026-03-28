# Build USB Installation Helper - Simple GUI
# Erstellt eine einfache EXE ohne Shell/CMD

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

# Pfade
$ProjectRoot = "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01"
$HelperProject = Join-Path $ProjectRoot "USB_InstallationHelper"
$ProjectFile = Join-Path $HelperProject "USB_InstallationHelper.csproj"

Write-Host "`n╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  🖥️  BUILD USB_INSTALLATIONHELPER (Simple GUI)               ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# Prüfe ob Projekt existiert
if (-not (Test-Path $ProjectFile)) {
    Write-Host "❌ Projekt nicht gefunden: $ProjectFile" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Projekt gefunden`n" -ForegroundColor Green

# Bereinige alte Build-Dateien
Write-Host "📦 Bereinige alte Build-Dateien..." -ForegroundColor Yellow
if (Test-Path (Join-Path $HelperProject "bin")) {
    Remove-Item -Path (Join-Path $HelperProject "bin") -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path (Join-Path $HelperProject "obj")) {
    Remove-Item -Path (Join-Path $HelperProject "obj") -Recurse -Force -ErrorAction SilentlyContinue
}

# Build
Write-Host "🔨 Starte Build ($Configuration)..." -ForegroundColor Yellow
$buildResult = & dotnet build $ProjectFile -c $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build fehlgeschlagen!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build erfolgreich!`n" -ForegroundColor Green

# Output Pfad
$outputPath = Join-Path $HelperProject "bin\$Configuration\net8.0-windows\USB_InstallationHelper.exe"

if (Test-Path $outputPath) {
    $fileSize = [math]::Round((Get-Item $outputPath).Length / 1MB, 2)
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║  ✅ EXE ERFOLGREICH ERSTELLT!                                 ║" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════════════════════╝`n" -ForegroundColor Green
    
    Write-Host "📄 Datei: USB_InstallationHelper.exe" -ForegroundColor White
    Write-Host "📁 Pfad: $outputPath" -ForegroundColor Gray
    Write-Host "📦 Größe: $fileSize MB`n" -ForegroundColor White
    
    Write-Host "✨ FEATURES:" -ForegroundColor Cyan
    Write-Host "  ✓ Keine Shell/CMD angezeigt" -ForegroundColor White
    Write-Host "  ✓ Windows-Forms GUI" -ForegroundColor White
    Write-Host "  ✓ USB-Laufwerke automatisch erkennen" -ForegroundColor White
    Write-Host "  ✓ Dateien auf USB kopieren" -ForegroundColor White
    Write-Host "  ✓ Installer starten" -ForegroundColor White
    Write-Host "  ✓ Anleitung öffnen" -ForegroundColor White
    Write-Host "  ✓ USB-Ordner im Explorer öffnen`n" -ForegroundColor White
    
    Write-Host "🚀 ZUM TESTEN:" -ForegroundColor Cyan
    Write-Host "  $outputPath`n" -ForegroundColor Yellow
    
} else {
    Write-Host "❌ EXE nicht gefunden: $outputPath" -ForegroundColor Red
    exit 1
}
