# ═══════════════════════════════════════════════════════════════════════════════
# MATERIALMANAGER V01 - DEMO-RESET SCRIPT
# Setzt die Demo auf 30 Tage zurück
# © 2025 Alexander Hölzer
# ═══════════════════════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                        PHASE 3: DEMO-RESET (30 TAGE)                        ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Demo-Datei Pfade
$appDataPath = [Environment]::GetFolderPath("LocalApplicationData")
$oldDemoPath = Join-Path $appDataPath "MaterialManager_V01"
$newDemoPath = Join-Path $appDataPath "MaterialManager_V01"

Write-Host "📁 Demo-Pfade:" -ForegroundColor Yellow
Write-Host "  Alt (R03): $oldDemoPath" -ForegroundColor Gray
Write-Host "  Neu (V01): $newDemoPath" -ForegroundColor Gray
Write-Host ""

# Lösche alte Demo-Datei (R03)
if (Test-Path $oldDemoPath) {
    Write-Host "🗑️  Lösche alte Demo-Daten (R03)..." -ForegroundColor Yellow
    try {
        Remove-Item -Path $oldDemoPath -Recurse -Force -ErrorAction Stop
        Write-Host "✓ Alte Demo-Daten gelöscht" -ForegroundColor Green
    } catch {
        Write-Host "⚠️  Konnte nicht alle Dateien löschen: $_" -ForegroundColor Yellow
    }
}

Write-Host ""

# Erstelle neue V01-Demo-Struktur
Write-Host "📁 Erstelle neue V01-Demo-Struktur..." -ForegroundColor Yellow
try {
    if (-not (Test-Path $newDemoPath)) {
        New-Item -ItemType Directory -Path $newDemoPath -Force | Out-Null
    }
    
    # Erstelle LICENSE-Datei mit aktuellem Datum (Demo-Start)
    $demoStartFile = Join-Path $newDemoPath "demo_start.dat"
    $demoStartDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Set-Content -Path $demoStartFile -Value $demoStartDate -Force
    
    # Erstelle Config-Datei
    $configFile = Join-Path $newDemoPath "config.json"
    $config = @{
        "AppName" = "MaterialManager_V01"
        "Version" = "1.0.0"
        "DemoMode" = $true
        "DemoStartDate" = $demoStartDate
        "DemoDays" = 30
        "DemoExpireDate" = (Get-Date).AddDays(30).ToString("yyyy-MM-dd")
    }
    $config | ConvertTo-Json | Set-Content -Path $configFile -Force
    
    Write-Host "✓ V01-Demo-Struktur erstellt" -ForegroundColor Green
    Write-Host "  📄 demo_start.dat: $demoStartDate" -ForegroundColor Green
    Write-Host "  📄 config.json erstellt" -ForegroundColor Green
} catch {
    Write-Host "❌ Fehler beim Erstellen: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ PHASE 3 ABGESCHLOSSEN!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Demo wurde zurückgesetzt!" -ForegroundColor Green
Write-Host "  ✓ Neue Demo läuft ab: $(Get-Date).AddDays(30).ToString('dd.MM.yyyy')" -ForegroundColor Yellow
Write-Host ""
Write-Host "Nächster Schritt: TEST_V01_BUILD.bat ausführen" -ForegroundColor Yellow
Write-Host ""
