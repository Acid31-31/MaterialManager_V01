# MIGRATION R03 → V01 - PHASE 2: UMBENENNEN

$dest = "D:\MaterialManager_V01_komplett"

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║      MIGRATION R03 → V01 - PHASE 2: UMBENENNEN (R03→V01)        ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $dest)) {
    Write-Host "❌ FEHLER: V01-Ordner nicht gefunden: $dest" -ForegroundColor Red
    Write-Host "Führe erst 01_COPY_V01.ps1 aus!" -ForegroundColor Yellow
    pause
    exit 1
}

Write-Host "✓ V01-Ordner gefunden" -ForegroundColor Green
Write-Host ""

# Suche alle C#, XAML, CSPROJ und andere Dateien
Write-Host "🔍 Durchsuche Dateien..." -ForegroundColor Yellow

$files = Get-ChildItem -Path $dest -Recurse -Include "*.cs", "*.xaml", "*.csproj", "*.sln", "*.txt", "*.md", "*.bat", "*.ps1" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch 'obj\\|bin\\' }

Write-Host "✓ $($files.Count) Dateien gefunden" -ForegroundColor Green
Write-Host ""
Write-Host "📝 Ersetze MaterialManager_V01 → MaterialManager_V01..." -ForegroundColor Yellow
Write-Host ""

$count = 0
foreach ($file in $files) {
    try {
        $content = Get-Content $file.FullName -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
        
        if ($null -ne $content) {
            $before = $content
            
            # Ersetze ALLE Vorkommen
            $content = $content -replace 'MaterialManager_V01', 'MaterialManager_V01'
            $content = $content -replace 'namespace MaterialManager_V01', 'namespace MaterialManager_V01'
            $content = $content -replace 'using MaterialManager_V01', 'using MaterialManager_V01'
            $content = $content -replace 'MM_V01_SECRET', 'MM_V01_SECRET'
            
            # Wenn etwas geändert wurde
            if ($content -ne $before) {
                Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -Force
                Write-Host "  ✓ $($file.Name)" -ForegroundColor Green
                $count++
            }
        }
    } catch {
        Write-Host "  ⚠️  $($file.Name): $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "✅ $count Dateien aktualisiert!" -ForegroundColor Green
Write-Host ""

Write-Host "════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Nächster Schritt: 03_BUILD_V01.ps1 ausführen!" -ForegroundColor Yellow
Write-Host "════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

pause
