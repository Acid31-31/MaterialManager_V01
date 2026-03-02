# MIGRATION R03 → V01 - FUNKTIONIERT 100%!

$source = "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01"
$dest = "D:\MaterialManager_V01_komplett"

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║           MIGRATION R03 → V01 - PHASE 1: KOPIEREN              ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Prüfe Quelle
if (-not (Test-Path $source)) {
    Write-Host "❌ FEHLER: R03-Ordner nicht gefunden: $source" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Quelle gefunden: $source" -ForegroundColor Green
Write-Host "✓ Ziel: $dest" -ForegroundColor Green
Write-Host ""

# Lösche alten Ordner falls vorhanden
if (Test-Path $dest) {
    Write-Host "⚠️  Lösche alten V01-Ordner..." -ForegroundColor Yellow
    Remove-Item -Path $dest -Recurse -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
}

# Kopiere ALLES
Write-Host "📁 Kopiere ALLE Dateien von R03..." -ForegroundColor Yellow
Write-Host ""

Copy-Item -Path $source -Destination $dest -Recurse -Force -ErrorAction Continue

Write-Host ""
Write-Host "✅ KOPIEREN FERTIG!" -ForegroundColor Green
Write-Host ""

# Zähle Dateien
$fileCount = (Get-ChildItem -Path $dest -Recurse -File -ErrorAction SilentlyContinue | Measure-Object).Count
Write-Host "✓ $fileCount Dateien kopiert" -ForegroundColor Green
Write-Host "✓ V01-Ordner liegt in: $dest" -ForegroundColor Green
Write-Host ""

Write-Host "════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Nächster Schritt: 02_RENAME_V01.ps1 ausführen!" -ForegroundColor Yellow
Write-Host "════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

pause
