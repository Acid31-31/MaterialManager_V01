# ═══════════════════════════════════════════════════════════════════════════════
# MATERIALMANAGER R03 → V01 KOPIER-SCRIPT
# Kopiert essenzielle Dateien zu D:\MaterialManager_V01_komplett
# © 2025 Alexander Hölzer
# ═══════════════════════════════════════════════════════════════════════════════

param(
    [string]$SourcePath = "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01",
    [string]$DestPath = "D:\MaterialManager_V01_komplett"
)

Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    MATERIALMANAGER R03 → V01 KOPIEREN                       ║" -ForegroundColor Cyan
Write-Host "║                         PHASE 1: ESSENZIELLE DATEIEN                        ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Prüfe Quelle
if (-not (Test-Path $SourcePath)) {
    Write-Host "❌ FEHLER: Quellordner nicht gefunden!" -ForegroundColor Red
    Write-Host "   Erwartet: $SourcePath" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ Quellordner gefunden: $SourcePath" -ForegroundColor Green
Write-Host ""

# Erstelle Zielordner
Write-Host "📁 Erstelle Zielordner: $DestPath" -ForegroundColor Yellow
if (Test-Path $DestPath) {
    Write-Host "⚠️  Zielordner existiert bereits. Überschreibe..." -ForegroundColor Yellow
} else {
    New-Item -ItemType Directory -Path $DestPath -Force | Out-Null
}
Write-Host "✓ Zielordner bereit" -ForegroundColor Green
Write-Host ""

# Definiere was kopiert werden soll
$includePatterns = @(
    "*.cs",
    "*.xaml",
    "*.xaml.cs",
    "*.csproj",
    "*.sln",
    "*.xaml",
    "*.md",
    "*.txt",
    "*.bat",
    "*.ps1"
)

$excludePatterns = @(
    "bin\*",
    "obj\*",
    ".git\*",
    ".vs\*",
    "*.vsdbg*",
    "*.tmp",
    "USB_Package\*",
    "Backup_*\*",
    "BUILD_FEHLER*",
    ".gitignore",
    "*.lock"
)

Write-Host "📋 Kopiere essenzielle Dateien..." -ForegroundColor Yellow
Write-Host ""

# Kopiere Struktur
$itemsToCopy = @(
    "Views",
    "Services",
    "Models",
    "Converters",
    "Assets",
    "Docs",
    "App.xaml",
    "App.xaml.cs",
    "MainWindow.xaml",
    "MainWindow.xaml.cs",
    "*.csproj",
    "*.sln",
    "LICENSE.txt",
    "COPYRIGHT.txt"
)

$copiedCount = 0
foreach ($item in $itemsToCopy) {
    $sourcePath = Join-Path $SourcePath $item
    if (Test-Path $sourcePath) {
        $destPath = Join-Path $DestPath $item
        
        if ((Get-Item $sourcePath).PSIsContainer) {
            # Ordner
            Copy-Item -Path $sourcePath -Destination $destPath -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "  ✓ Ordner: $item" -ForegroundColor Green
        } else {
            # Datei
            Copy-Item -Path $sourcePath -Destination $destPath -Force -ErrorAction SilentlyContinue
            Write-Host "  ✓ Datei: $item" -ForegroundColor Green
        }
        $copiedCount++
    }
}

Write-Host ""
Write-Host "✓ $copiedCount Elemente kopiert!" -ForegroundColor Green
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ PHASE 1 ABGESCHLOSSEN!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Nächster Schritt: RENAME_TO_V01.ps1 ausführen" -ForegroundColor Yellow
Write-Host ""
