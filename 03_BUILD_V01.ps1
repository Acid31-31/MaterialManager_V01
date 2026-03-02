# MIGRATION R03 → V01 - PHASE 3: DEMO RESET + BUILD + START

$dest = "D:\MaterialManager_V01_komplett"

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   MIGRATION R03 → V01 - PHASE 3: DEMO RESET + BUILD + START    ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $dest)) {
    Write-Host "❌ FEHLER: V01-Ordner nicht gefunden!" -ForegroundColor Red
    pause
    exit 1
}

# DEMO RESET
Write-Host "[PHASE 1] DEMO AUF 30 TAGE ZURÜCKSETZEN..." -ForegroundColor Yellow
Write-Host ""

$appDataPath = [Environment]::GetFolderPath("LocalApplicationData")
$oldPath = Join-Path $appDataPath "MaterialManager_V01"
$newPath = Join-Path $appDataPath "MaterialManager_V01"

# Lösche alte Demo-Daten
if (Test-Path $oldPath) {
    Write-Host "⚠️  Lösche alte Demo-Daten (R03)..." -ForegroundColor Yellow
    Remove-Item -Path $oldPath -Recurse -Force -ErrorAction SilentlyContinue
}

# Erstelle neue V01-Struktur
if (-not (Test-Path $newPath)) {
    New-Item -ItemType Directory -Path $newPath -Force | Out-Null
}

$demoFile = Join-Path $newPath "demo_start.dat"
$demoDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
Set-Content -Path $demoFile -Value $demoDate -Force
(Get-Item $demoFile).Attributes = 'Hidden'

Write-Host "✅ Demo zurückgesetzt!" -ForegroundColor Green
Write-Host "   Start-Datum: $demoDate" -ForegroundColor Green
Write-Host "   Ablauf: $((Get-Date).AddDays(30).ToString('dd.MM.yyyy'))" -ForegroundColor Green
Write-Host ""

# BUILD
Write-Host "[PHASE 2] BUILD STARTEN..." -ForegroundColor Yellow
Write-Host ""

cd $dest

Write-Host "🏗️  Kompiliere MaterialManager_V01..." -ForegroundColor Yellow
Write-Host ""

& dotnet build MaterialManager_V01.csproj

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ BUILD ERFOLGREICH!" -ForegroundColor Green
    Write-Host ""
    
    # START
    Write-Host "[PHASE 3] STARTE PROGRAMM..." -ForegroundColor Yellow
    Write-Host ""
    
    $exePath = Join-Path $dest "bin\Debug\net8.0-windows\MaterialManager_V01.exe"
    
    if (Test-Path $exePath) {
        Write-Host "🚀 Starte $exePath..." -ForegroundColor Green
        Write-Host ""
        
        & $exePath
    } else {
        Write-Host "❌ EXE nicht gefunden: $exePath" -ForegroundColor Red
    }
} else {
    Write-Host ""
    Write-Host "❌ BUILD FEHLGESCHLAGEN!" -ForegroundColor Red
    Write-Host "Fehlercode: $LASTEXITCODE" -ForegroundColor Red
    pause
    exit 1
}

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ MIGRATION ERFOLGREICH ABGESCHLOSSEN!" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "V01 Location: $dest" -ForegroundColor Cyan
Write-Host "Demo: 30 Tage" -ForegroundColor Cyan
Write-Host ""

pause
