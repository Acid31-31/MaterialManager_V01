# ═══════════════════════════════════════════════════════════════════════════════
# MATERIALMANAGER V01 UMBENENN-SCRIPT
# Ersetzt alle R03 Referenzen durch V01
# © 2025 Alexander Hölzer
# ═══════════════════════════════════════════════════════════════════════════════

param(
    [string]$TargetPath = "D:\MaterialManager_V01_komplett"
)

Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                      PHASE 2: UMBENENNUNG R03 → V01                         ║" -ForegroundColor Cyan
Write-Host "║                                                                               ║" -ForegroundColor Cyan
Write-Host "║                   Ersetze alle R03-Referenzen mit V01                        ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Prüfe Ziel
if (-not (Test-Path $TargetPath)) {
    Write-Host "❌ FEHLER: Zielordner nicht gefunden!" -ForegroundColor Red
    Write-Host "   Erwartet: $TargetPath" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ Zielordner gefunden: $TargetPath" -ForegroundColor Green
Write-Host ""

# Definiere Dateitypen für Suche
$fileTypes = @("*.cs", "*.csproj", "*.xaml", "*.txt", "*.md", "*.bat", "*.ps1")

Write-Host "🔍 Suche nach Dateien..." -ForegroundColor Yellow
$files = Get-ChildItem -Path $TargetPath -Recurse -Include $fileTypes -ErrorAction SilentlyContinue

Write-Host "✓ $($files.Count) Dateien gefunden" -ForegroundColor Green
Write-Host ""

Write-Host "📝 Ersetze Referenzen..." -ForegroundColor Yellow
Write-Host ""

$replacedCount = 0
$replacementPairs = @(
    @{ Old = "MaterialManager_V01"; New = "MaterialManager_V01" },
    @{ Old = "namespace MaterialManager_V01"; New = "namespace MaterialManager_V01" },
    @{ Old = "R03"; New = "V01" }
)

foreach ($file in $files) {
    try {
        $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
        if ($null -eq $content) { continue }
        
        $modified = $false
        foreach ($pair in $replacementPairs) {
            if ($content -contains $pair.Old) {
                $newContent = $content -replace [regex]::Escape($pair.Old), $pair.New
                if ($newContent -ne $content) {
                    Set-Content -Path $file.FullName -Value $newContent -Force
                    $modified = $true
                    break
                }
            }
        }
        
        if ($modified) {
            Write-Host "  ✓ $($file.Name)" -ForegroundColor Green
            $replacedCount++
        }
    } catch {
        Write-Host "  ⚠️  Fehler bei $($file.Name): $_" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "✓ $replacedCount Dateien aktualisiert!" -ForegroundColor Green
Write-Host ""

# Benenne .csproj Datei um
Write-Host "📋 Benenne Projektdatei um..." -ForegroundColor Yellow
$oldCsproj = Get-ChildItem -Path $TargetPath -Name "MaterialManager_V01.csproj" -ErrorAction SilentlyContinue
if ($oldCsproj) {
    $oldPath = Join-Path $TargetPath $oldCsproj
    $newPath = Join-Path $TargetPath "MaterialManager_V01.csproj"
    Rename-Item -Path $oldPath -NewName "MaterialManager_V01.csproj" -Force
    Write-Host "  ✓ Benannt: MaterialManager_V01.csproj → MaterialManager_V01.csproj" -ForegroundColor Green
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ PHASE 2 ABGESCHLOSSEN!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Alle Referenzen wurden erfolgreich aktualisiert!" -ForegroundColor Green
Write-Host ""
