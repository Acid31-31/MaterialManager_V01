# ═══════════════════════════════════════════════════════════════════════════════
# MASS-RENAME SCRIPT - Ändert ALLE R03 → V01 Referenzen
# © 2025 Alexander Hölzer
# ═══════════════════════════════════════════════════════════════════════════════

param(
    [string]$TargetPath = "D:\MaterialManager_V01_komplett"
)

Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                  MASS-RENAME: Ändere ALLE R03 → V01                         ║" -ForegroundColor Cyan
Write_Host "╚═══════════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Alle Dateitypen die geändert werden sollen
$fileTypes = @("*.cs", "*.xaml", "*.csproj", "*.sln", "*.txt", "*.md", "*.bat", "*.ps1", "*.json")

$replacements = @(
    @{ Old = "MaterialManager_V01"; New = "MaterialManager_V01" },
    @{ Old = "namespace MaterialManager_V01"; New = "namespace MaterialManager_V01" },
    @{ Old = "using MaterialManager_V01"; New = "using MaterialManager_V01" },
    @{ Old = "R03"; New = "V01" }
)

Write-Host "🔍 Suche Dateien..." -ForegroundColor Yellow
$files = Get-ChildItem -Path $TargetPath -Recurse -Include $fileTypes -ErrorAction SilentlyContinue

Write-Host "✓ $($files.Count) Dateien gefunden" -ForegroundColor Green
Write-Host ""

$changedCount = 0
foreach ($file in $files) {
    try {
        $content = Get-Content $file.FullName -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
        if ($null -eq $content) { continue }
        
        $newContent = $content
        foreach ($replacement in $replacements) {
            $newContent = $newContent -replace [regex]::Escape($replacement.Old), $replacement.New
        }
        
        if ($newContent -ne $content) {
            Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8 -Force
            Write-Host "  ✓ $($file.Name)" -ForegroundColor Green
            $changedCount++
        }
    } catch {
        Write-Host "  ⚠️  $($file.Name): $_" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "✓ $changedCount Dateien aktualisiert!" -ForegroundColor Green
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ MASS-RENAME ABGESCHLOSSEN!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
