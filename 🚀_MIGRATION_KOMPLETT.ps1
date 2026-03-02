# MIGRATION R03 → V01 - ALLES IN EINEM!
# MASTER-SCRIPT: Kopieren + Umbenennen + Demo Reset + Build + Start

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                                                                    ║" -ForegroundColor Cyan
Write-Host "║          🚀 MATERIALMANAGER R03 → V01 KOMPLETT-MIGRATION 🚀       ║" -ForegroundColor Cyan
Write-Host "║                                                                    ║" -ForegroundColor Cyan
Write-Host "║                     ALLES AUTOMATISCH - EINE DATEI!               ║" -ForegroundColor Cyan
Write-Host "║                                                                    ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$source = "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01"
$dest = "D:\MaterialManager_V01_komplett"
$appDataPath = [Environment]::GetFolderPath("LocalApplicationData")

# ════════════════════════════════════════════════════════════════════
# PHASE 1: KOPIEREN
# ════════════════════════════════════════════════════════════════════

Write-Host "[1/4] KOPIERE R03 → D:\MaterialManager_V01_komplett..." -ForegroundColor Yellow
Write-Host ""

if (-not (Test-Path $source)) {
    Write-Host "❌ FEHLER: R03 nicht gefunden!" -ForegroundColor Red
    pause
    exit 1
}

if (Test-Path $dest) {
    Write-Host "⚠️  Lösche alten Ordner..." -ForegroundColor Yellow
    Remove-Item -Path $dest -Recurse -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
}

Write-Host "📁 Kopiere Dateien..." -ForegroundColor Cyan
Copy-Item -Path $source -Destination $dest -Recurse -Force -ErrorAction Continue

$fileCount = (Get-ChildItem -Path $dest -Recurse -File -ErrorAction SilentlyContinue | Measure-Object).Count
Write-Host "✅ $fileCount Dateien kopiert!" -ForegroundColor Green
Write-Host ""

# ════════════════════════════════════════════════════════════════════
# PHASE 2: UMBENENNEN
# ════════════════════════════════════════════════════════════════════

Write-Host "[2/4] ÄNDERE ALLE R03 → V01..." -ForegroundColor Yellow
Write-Host ""

$files = Get-ChildItem -Path $dest -Recurse -Include "*.cs", "*.xaml", "*.csproj", "*.sln", "*.txt", "*.md", "*.bat", "*.ps1" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch 'obj\\|bin\\' }

$renameCount = 0
foreach ($file in $files) {
    try {
        $content = Get-Content $file.FullName -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
        
        if ($null -ne $content -and $content -match 'MaterialManager_V01') {
            $content = $content -replace 'MaterialManager_V01', 'MaterialManager_V01' `
                               -replace 'namespace MaterialManager_V01', 'namespace MaterialManager_V01' `
                               -replace 'using MaterialManager_V01', 'using MaterialManager_V01' `
                               -replace 'MM_V01_SECRET', 'MM_V01_SECRET'
            
            Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -Force
            $renameCount++
        }
    } catch { }
}

Write-Host "✅ $renameCount Dateien aktualisiert!" -ForegroundColor Green
Write-Host ""

# ════════════════════════════════════════════════════════════════════
# PHASE 3: DEMO RESET
# ════════════════════════════════════════════════════════════════════

Write-Host "[3/4] DEMO AUF 30 TAGE ZURÜCKSETZEN..." -ForegroundColor Yellow
Write-Host ""

$oldPath = Join-Path $appDataPath "MaterialManager_V01"
$newPath = Join-Path $appDataPath "MaterialManager_V01"

if (Test-Path $oldPath) {
    Remove-Item -Path $oldPath -Recurse -Force -ErrorAction SilentlyContinue
}

if (-not (Test-Path $newPath)) {
    New-Item -ItemType Directory -Path $newPath -Force | Out-Null
}

$demoFile = Join-Path $newPath "demo_start.dat"
$demoDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
Set-Content -Path $demoFile -Value $demoDate -Force
(Get-Item $demoFile).Attributes = 'Hidden'

Write-Host "✅ Demo zurückgesetzt!" -ForegroundColor Green
Write-Host "   Ablauf: $((Get-Date).AddDays(30).ToString('dd.MM.yyyy'))" -ForegroundColor Green
Write-Host ""

# ════════════════════════════════════════════════════════════════════
# PHASE 4: BUILD
# ════════════════════════════════════════════════════════════════════

Write-Host "[4/4] BUILD STARTEN..." -ForegroundColor Yellow
Write-Host ""

cd $dest

Write-Host "🏗️  Kompiliere..." -ForegroundColor Cyan
& dotnet build MaterialManager_V01.csproj

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "════════════════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host "✅ MIGRATION ERFOLGREICH ABGESCHLOSSEN!" -ForegroundColor Green
    Write-Host "════════════════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host ""
    Write-Host "✓ V01 liegt in: $dest" -ForegroundColor Cyan
    Write-Host "✓ Demo: 30 Tage" -ForegroundColor Cyan
    Write-Host "✓ Build: erfolgreich" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Starte Programm jetzt? (J/n)" -ForegroundColor Yellow
    $response = Read-Host
    
    if ($response -ne 'n') {
        $exePath = Join-Path $dest "bin\Debug\net8.0-windows\MaterialManager_V01.exe"
        if (Test-Path $exePath) {
            Write-Host ""
            Write-Host "🚀 Starte MaterialManager_V01..." -ForegroundColor Green
            & $exePath
        }
    }
} else {
    Write-Host ""
    Write-Host "❌ BUILD FEHLGESCHLAGEN!" -ForegroundColor Red
}

Write-Host ""
pause
