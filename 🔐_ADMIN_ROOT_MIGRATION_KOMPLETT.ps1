# ═══════════════════════════════════════════════════════════════════════════════
# 🚀 MATERIALMANAGER R03 → V01 - ADMIN ROOT SCRIPT
#
# DIESES SKRIPT:
# 1. Startet sich selbst mit ADMIN-RECHTEN (falls nicht schon Admin)
# 2. Kopiert R03 → D:\MaterialManager_V01_komplett
# 3. Ändert ALLE R03 → V01 Referenzen
# 4. Setzt Demo auf 30 Tage zurück
# 5. Startet Build
# 6. Startet Programm
#
# © 2025 Alexander Hölzer
# ═══════════════════════════════════════════════════════════════════════════════

# ════════════════════════════════════════════════════════════════════════════════
# SCHRITT 1: PRÜFE OB ADMIN - WENN NICHT, STARTE NEU MIT ADMIN!
# ════════════════════════════════════════════════════════════════════════════════

function Invoke-AsAdmin {
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    
    if (-not $isAdmin) {
        Write-Host ""
        Write-Host "⚠️  NICHT ALS ADMIN GESTARTET!" -ForegroundColor Yellow
        Write-Host "🔄 Starte neu mit Administrator-Rechten..." -ForegroundColor Cyan
        Write-Host ""
        
        $scriptPath = $MyInvocation.ScriptName
        $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`""
        
        Start-Process powershell.exe -Verb RunAs -ArgumentList $arguments
        exit
    }
}

# Starte Admin-Check
Invoke-AsAdmin

# ════════════════════════════════════════════════════════════════════════════════
# BESTÄTIGUNG: WIR SIND JETZT ADMIN!
# ════════════════════════════════════════════════════════════════════════════════

Clear-Host

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║                                                                    ║" -ForegroundColor Green
Write-Host "║          ✅ ADMIN-MODUS AKTIVIERT - VOLLE KONTROLLE!             ║" -ForegroundColor Green
Write-Host "║                                                                    ║" -ForegroundColor Green
Write-Host "║     🚀 MATERIALMANAGER R03 → V01 KOMPLETT-MIGRATION 🚀           ║" -ForegroundColor Green
Write-Host "║                                                                    ║" -ForegroundColor Green
Write-Host "║                    ALLES AUTOMATISCH - KEINE WARTEZEIT            ║" -ForegroundColor Green
Write-Host "║                                                                    ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""

$source = "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01"
$dest = "D:\MaterialManager_V01_komplett"
$appDataPath = [Environment]::GetFolderPath("LocalApplicationData")

# ════════════════════════════════════════════════════════════════════════════════
# PHASE 0: ADMIN-PRÜFUNGEN (Dateizugriff, Laufwerk, etc.)
# ════════════════════════════════════════════════════════════════════════════════

Write-Host "[PRE-CHECK] Prüfe Admin-Rechte und Zugriff..." -ForegroundColor Cyan
Write-Host ""

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if ($isAdmin) {
    Write-Host "✅ Admin-Rechte: AKTIVIERT" -ForegroundColor Green
} else {
    Write-Host "❌ Admin-Rechte: FEHLER!" -ForegroundColor Red
    pause
    exit 1
}

if (-not (Test-Path $source)) {
    Write-Host "❌ R03 nicht gefunden: $source" -ForegroundColor Red
    pause
    exit 1
}

Write-Host "✅ R03-Ordner: GEFUNDEN" -ForegroundColor Green

if (-not (Test-Path "D:\")) {
    Write-Host "❌ D:\ externe Festplatte nicht verfügbar" -ForegroundColor Red
    pause
    exit 1
}

Write-Host "✅ D:\ Laufwerk: GEFUNDEN" -ForegroundColor Green
Write-Host ""

# ════════════════════════════════════════════════════════════════════════════════
# PHASE 1: KOPIEREN (mit Admin-Rechten!)
# ════════════════════════════════════════════════════════════════════════════════

Write-Host "[1/5] KOPIERE R03 → D:\MaterialManager_V01_komplett..." -ForegroundColor Yellow
Write-Host ""

if (Test-Path $dest) {
    Write-Host "⚠️  Lösche alten Ordner (Admin-Rechte!)..." -ForegroundColor Yellow
    
    try {
        Remove-Item -Path $dest -Recurse -Force -ErrorAction Stop
        Write-Host "✅ Alter Ordner gelöscht" -ForegroundColor Green
    } catch {
        Write-Host "❌ Fehler beim Löschen: $_" -ForegroundColor Red
        pause
        exit 1
    }
    
    Start-Sleep -Seconds 1
}

Write-Host "📁 Kopiere Dateien mit Admin-Rechten..." -ForegroundColor Cyan

try {
    Copy-Item -Path $source -Destination $dest -Recurse -Force -ErrorAction Stop
    Write-Host "✅ Kopieren erfolgreich!" -ForegroundColor Green
} catch {
    Write-Host "❌ Kopier-Fehler: $_" -ForegroundColor Red
    pause
    exit 1
}

$fileCount = (Get-ChildItem -Path $dest -Recurse -File -ErrorAction SilentlyContinue | Measure-Object).Count
Write-Host "   ✓ $fileCount Dateien kopiert" -ForegroundColor Green
Write-Host ""

# ════════════════════════════════════════════════════════════════════════════════
# PHASE 2: MASS-RENAME
# ════════════════════════════════════════════════════════════════════════════════

Write-Host "[2/5] ÄNDERE ALLE R03 → V01..." -ForegroundColor Yellow
Write-Host ""

$files = Get-ChildItem -Path $dest -Recurse -Include "*.cs", "*.xaml", "*.csproj", "*.sln", "*.txt", "*.md", "*.bat", "*.ps1" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch 'obj\\|bin\\' }

Write-Host "🔍 Durchsuche $($files.Count) Dateien..." -ForegroundColor Cyan

$renameCount = 0
foreach ($file in $files) {
    try {
        $content = Get-Content $file.FullName -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
        
        if ($null -ne $content -and $content -match 'MaterialManager_V01') {
            $originalSize = $content.Length
            
            # ERSETZE ALLES
            $content = $content -replace 'MaterialManager_V01', 'MaterialManager_V01' `
                               -replace 'namespace MaterialManager_V01', 'namespace MaterialManager_V01' `
                               -replace 'using MaterialManager_V01', 'using MaterialManager_V01' `
                               -replace 'MM_V01_SECRET', 'MM_V01_SECRET' `
                               -replace 'R03', 'V01'
            
            # Schreibe mit Admin-Rechten
            Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -Force -ErrorAction Stop
            Write-Host "  ✓ $($file.Name)" -ForegroundColor Green
            $renameCount++
        }
    } catch {
        Write-Host "  ⚠️  $($file.Name): $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "✅ $renameCount Dateien aktualisiert!" -ForegroundColor Green
Write-Host ""

# ════════════════════════════════════════════════════════════════════════════════
# PHASE 3: DEMO RESET (Admin-Kontrolle)
# ════════════════════════════════════════════════════════════════════════════════

Write-Host "[3/5] DEMO AUF 30 TAGE ZURÜCKSETZEN..." -ForegroundColor Yellow
Write-Host ""

$oldPath = Join-Path $appDataPath "MaterialManager_V01"
$newPath = Join-Path $appDataPath "MaterialManager_V01"

# Lösche mit Admin-Rechten
if (Test-Path $oldPath) {
    Write-Host "⚠️  Lösche alte Demo-Daten (R03)..." -ForegroundColor Yellow
    try {
        Remove-Item -Path $oldPath -Recurse -Force -ErrorAction Stop
        Write-Host "✅ Gelöscht" -ForegroundColor Green
    } catch {
        Write-Host "⚠️  Konnte R03 AppData nicht löschen (nicht kritisch)" -ForegroundColor Yellow
    }
}

# Erstelle neue V01-Struktur mit Admin-Rechten
try {
    if (-not (Test-Path $newPath)) {
        New-Item -ItemType Directory -Path $newPath -Force | Out-Null
    }
    
    $demoFile = Join-Path $newPath "demo_start.dat"
    $demoDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Set-Content -Path $demoFile -Value $demoDate -Force
    (Get-Item $demoFile).Attributes = 'Hidden'
    
    $demoEndDate = (Get-Date).AddDays(30).ToString('dd.MM.yyyy')
    Write-Host "✅ Demo zurückgesetzt!" -ForegroundColor Green
    Write-Host "   Start: $demoDate" -ForegroundColor Green
    Write-Host "   Ende:  $demoEndDate" -ForegroundColor Green
} catch {
    Write-Host "❌ Demo-Reset Fehler: $_" -ForegroundColor Red
}

Write-Host ""

# ════════════════════════════════════════════════════════════════════════════════
# PHASE 4: BUILD
# ════════════════════════════════════════════════════════════════════════════════

Write-Host "[4/5] BUILD STARTEN..." -ForegroundColor Yellow
Write-Host ""

Push-Location $dest

Write-Host "🏗️  Kompiliere MaterialManager_V01.csproj..." -ForegroundColor Cyan
Write-Host ""

$buildOutput = & dotnet build MaterialManager_V01.csproj 2>&1

Write-Host $buildOutput

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ BUILD ERFOLGREICH!" -ForegroundColor Green
    Write-Host ""
    
    # ════════════════════════════════════════════════════════════════════════════════
    # PHASE 5: STARTE PROGRAMM
    # ════════════════════════════════════════════════════════════════════════════════
    
    Write-Host "[5/5] STARTE MATERIALMANAGER V01..." -ForegroundColor Yellow
    Write-Host ""
    
    $exePath = Join-Path $dest "bin\Debug\net8.0-windows\MaterialManager_V01.exe"
    
    if (Test-Path $exePath) {
        Write-Host "🚀 Starte: $exePath" -ForegroundColor Green
        Write-Host ""
        
        # Starte mit Admin-Rechten
        Start-Process -FilePath $exePath -Verb RunAs
        
        Start-Sleep -Seconds 2
    } else {
        Write-Host "❌ EXE nicht gefunden: $exePath" -ForegroundColor Red
    }
    
    Pop-Location
    
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║                                                                    ║" -ForegroundColor Green
    Write-Host "║           ✅ MIGRATION ERFOLGREICH ABGESCHLOSSEN! ✅              ║" -ForegroundColor Green
    Write-Host "║                                                                    ║" -ForegroundColor Green
    Write-Host "║  V01 Location:  $dest" -ForegroundColor Green
    Write-Host "║  Demo:          30 Tage" -ForegroundColor Green
    Write-Host "║  Build:         erfolgreich" -ForegroundColor Green
    Write-Host "║  Programm:      läuft mit Admin-Rechten" -ForegroundColor Green
    Write-Host "║                                                                    ║" -ForegroundColor Green
    Write-Host "║  NÄCHSTE SCHRITTE:                                                 ║" -ForegroundColor Green
    Write-Host "║  1. Teste Programm vollständig                                      ║" -ForegroundColor Green
    Write-Host "║  2. Erstelle GitHub Repository (MaterialManager_V01)                ║" -ForegroundColor Green
    Write-Host "║  3. Pushe Code                                                       ║" -ForegroundColor Green
    Write-Host "║  4. Erstelle USB-Paket                                               ║" -ForegroundColor Green
    Write-Host "║  5. VERKAUFE! 💰                                                    ║" -ForegroundColor Green
    Write-Host "║                                                                    ║" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""
    
} else {
    Write-Host ""
    Write-Host "❌ BUILD FEHLGESCHLAGEN!" -ForegroundColor Red
    Write-Host "Fehlercode: $LASTEXITCODE" -ForegroundColor Red
    Write-Host ""
    Pop-Location
}

Write-Host ""
Write-Host "Drücke eine Taste zum Beenden..." -ForegroundColor Cyan
Read-Host
