# ╔════════════════════════════════════════════════════════════════════════╗
# ║  KOMPLETTE USB-INSTALLATION - PowerShell Version                       ║
# ║  Erstellt das komplette MaterialManager_V01 Programm für USB           ║
# ║  KEINE Shell - KEINE alleinstehenden Dateien                           ║
# ╚════════════════════════════════════════════════════════════════════════╝

param(
    [switch]$SkipBackup = $false,
    [switch]$Verbose = $false
)

# ═══════════════════════════════════════════════════════════════════════════
# KONFIGURATION
# ═══════════════════════════════════════════════════════════════════════════

$ProjectRoot = "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01"
$UsbInstallation = "$ProjectRoot\USB_Installation"
$BuildOutput = "$ProjectRoot\bin\Release\net8.0-windows\win-x64"
$BackupRoot = "$ProjectRoot\Backups"
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupDir = "$BackupRoot\USB_Build_$Timestamp"

# Farben
function Write-Success { Write-Host "✅ $args" -ForegroundColor Green }
function Write-Error { Write-Host "❌ $args" -ForegroundColor Red }
function Write-Warning { Write-Host "⚠️  $args" -ForegroundColor Yellow }
function Write-Info { Write-Host "ℹ️  $args" -ForegroundColor Cyan }
function Write-Step { Write-Host "`n[SCHRITT $args]" -ForegroundColor Cyan }

Clear-Host

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                                                                        ║" -ForegroundColor Cyan
Write-Host "║     🚀 MATERIALMANAGER R03 - KOMPLETTE USB INSTALLATION              ║" -ForegroundColor Cyan
Write-Host "║                                                                        ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ═══════════════════════════════════════════════════════════════════════════
# SCHRITT 1: BACKUP ERSTELLEN
# ═══════════════════════════════════════════════════════════════════════════

Write-Step "1/5 - Erstelle Backup..."

if (-not $SkipBackup) {
    if (-not (Test-Path $BackupRoot)) {
        New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null
    }
    
    if (-not (Test-Path $BackupDir)) {
        New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
    }

    Write-Host "Kopiere Source-Code..." -ForegroundColor White
    @(
        @{Source = "$ProjectRoot\Services"; Dest = "$BackupDir\Services" },
        @{Source = "$ProjectRoot\Views"; Dest = "$BackupDir\Views" },
        @{Source = "$ProjectRoot\Assets"; Dest = "$BackupDir\Assets" }
    ) | ForEach-Object {
        if (Test-Path $_.Source) {
            Copy-Item -Path $_.Source -Destination $_.Dest -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
        }
    }

    Get-ChildItem -Path $ProjectRoot -Filter "*.csproj" | Copy-Item -Destination $BackupDir -Force -ErrorAction SilentlyContinue
    Get-ChildItem -Path $ProjectRoot -Filter "*.sln" | Copy-Item -Destination $BackupDir -Force -ErrorAction SilentlyContinue

    Write-Success "Backup erstellt: $BackupDir"
} else {
    Write-Info "Backup übersprungen"
}

Write-Host ""

# ═══════════════════════════════════════════════════════════════════════════
# SCHRITT 2: CLEANUP
# ═══════════════════════════════════════════════════════════════════════════

Write-Step "2/5 - Cleanup alte Build-Dateien..."

@("$ProjectRoot\bin", "$ProjectRoot\obj") | ForEach-Object {
    if (Test-Path $_) {
        Write-Host "Lösche $_..." -ForegroundColor White
        Remove-Item -Path $_ -Recurse -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 500
    }
}

Write-Success "Cleanup abgeschlossen"
Write-Host ""

# ═══════════════════════════════════════════════════════════════════════════
# SCHRITT 3: BUILD
# ═══════════════════════════════════════════════════════════════════════════

Write-Step "3/5 - Baue Programm (Release-Modus)..."
Write-Host ""
Write-Host "Bitte warten... Dies kann 2-5 Minuten dauern!" -ForegroundColor Yellow
Write-Host ""

Push-Location $ProjectRoot

$BuildArgs = @(
    "build"
    "-c", "Release"
    "-p:Platform=x64"
    "-p:SelfContained=true"
    "-p:RuntimeIdentifier=win-x64"
    "--no-restore"
    "--verbosity", "minimal"
)

& dotnet @BuildArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "BUILD FEHLER! Der Build ist fehlgeschlagen."
    Pop-Location
    Read-Host "Drücke Enter zum Beenden"
    exit 1
}

Pop-Location

Write-Success "Build erfolgreich abgeschlossen"
Write-Host ""

# ═══════════════════════════════════════════════════════════════════════════
# SCHRITT 4: KOPIERE ZU USB_INSTALLATION
# ═══════════════════════════════════════════════════════════════════════════

Write-Step "4/5 - Kopiere Dateien zu USB_Installation..."

if (-not (Test-Path "$UsbInstallation\Programm")) {
    New-Item -ItemType Directory -Path "$UsbInstallation\Programm" -Force | Out-Null
}

Write-Host "Kopiere Hauptprogramm..." -ForegroundColor White

$FilesToCopy = @(
    @{Name = "MaterialManager_V01.exe"; Pattern = "MaterialManager_V01.exe" },
    @{Name = "DLL-Dateien"; Pattern = "*.dll" },
    @{Name = "Config-Dateien"; Pattern = "*.config" },
    @{Name = "JSON-Dateien"; Pattern = "*.json" }
)

foreach ($FileType in $FilesToCopy) {
    $Files = Get-ChildItem -Path $BuildOutput -Filter $FileType.Pattern -ErrorAction SilentlyContinue
    
    if ($Files) {
        $Files | Copy-Item -Destination "$UsbInstallation\Programm\" -Force -ErrorAction SilentlyContinue
        Write-Success "  ✓ $($FileType.Name) kopiert"
    }
}

# Kopiere Runtime-Ordner und -Dateien
Write-Host "Kopiere Runtime-Dateien..." -ForegroundColor White

Get-ChildItem -Path $BuildOutput -Directory | ForEach-Object {
    if ($_.Name -ne "publish") {
        Copy-Item -Path $_.FullName -Destination "$UsbInstallation\Programm\" -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Success "Dateien zu USB_Installation kopiert"
Write-Host ""

# ═══════════════════════════════════════════════════════════════════════════
# SCHRITT 5: VERIFIZIERE
# ═══════════════════════════════════════════════════════════════════════════

Write-Step "5/5 - Verifiziere USB_Installation..."

$HasErrors = $false

# Prüfe Hauptprogramm
if (Test-Path "$UsbInstallation\Programm\MaterialManager_V01.exe") {
    Write-Success "MaterialManager_V01.exe vorhanden"
} else {
    Write-Error "MaterialManager_V01.exe fehlt!"
    $HasErrors = $true
}

# Prüfe DLLs
$DllCount = (Get-ChildItem -Path "$UsbInstallation\Programm\*.dll" -ErrorAction SilentlyContinue).Count
if ($DllCount -gt 0) {
    Write-Success "$DllCount DLL-Dateien vorhanden"
} else {
    Write-Warning "Keine DLL-Dateien gefunden"
}

# Prüfe Anleitung
if (Test-Path "$UsbInstallation\Anleitung\QUICK_START.md") {
    Write-Success "Anleitung vorhanden"
} else {
    Write-Warning "Anleitung fehlt"
}

# Berechne Größe
$ProgrammSize = (Get-ChildItem -Path "$UsbInstallation\Programm" -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Info "Programm-Größe: $([math]::Round($ProgrammSize, 2)) MB"

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

if (-not $HasErrors) {
    Write-Host ""
    Write-Success "KOMPLETTE USB-INSTALLATION ERSTELLT!"
    Write-Host ""
    Write-Host "📁 Programm-Dateien:" -ForegroundColor Cyan
    Write-Host "   $UsbInstallation\Programm\" -ForegroundColor White
    Write-Host ""
    Write-Host "📋 Ordnerstruktur:" -ForegroundColor Cyan
    Write-Host "   USB_Installation/" -ForegroundColor White
    Write-Host "   ├─ Programm/" -ForegroundColor Gray
    Write-Host "   │  ├─ MaterialManager_V01.exe  ✅" -ForegroundColor Green
    Write-Host "   │  ├─ *.dll Dateien           ✅" -ForegroundColor Green
    Write-Host "   │  └─ Runtime-Dateien         ✅" -ForegroundColor Green
    Write-Host "   ├─ Anleitung/" -ForegroundColor Gray
    Write-Host "   ├─ Tools/" -ForegroundColor Gray
    Write-Host "   ├─ Installer.exe (später)" -ForegroundColor Gray
    Write-Host "   └─ README.md" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🚀 NÄCHSTER SCHRITT: USB-Stick kopieren!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "💾 So kopierst du auf USB:" -ForegroundColor Cyan
    Write-Host "   1. USB-Stick einstecken" -ForegroundColor White
    Write-Host "   2. Öffne Windows Explorer" -ForegroundColor White
    Write-Host "   3. Gehe zu: $UsbInstallation" -ForegroundColor White
    Write-Host "   4. Markiere ALLES (Ctrl+A)" -ForegroundColor White
    Write-Host "   5. Kopiere (Ctrl+C)" -ForegroundColor White
    Write-Host "   6. Gehe zu USB-Stick" -ForegroundColor White
    Write-Host "   7. Einfügen (Ctrl+V)" -ForegroundColor White
    Write-Host "   8. ✅ FERTIG!" -ForegroundColor White
    Write-Host ""
    Write-Host "🎉 Installation auf USB abgeschlossen!" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Error "Es gab Fehler bei der Installation!"
}

Write-Host "════════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📦 BACKUP INFORMATIONEN:" -ForegroundColor Cyan
Write-Host "   Backup-Pfad: $BackupDir" -ForegroundColor White
Write-Host ""

Read-Host "Drücke Enter zum Beenden"
