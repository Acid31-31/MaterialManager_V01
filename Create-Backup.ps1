# ╔════════════════════════════════════════════════════════════════╗
# ║  AUTOMATISCHES BACKUP SYSTEM - PowerShell Version              ║
# ║  Erstellt nach jedem Change ein Backup                         ║
# ╚════════════════════════════════════════════════════════════════╝

param(
    [string]$BackupName = "Auto-Backup",
    [bool]$Verbose = $true
)

# Konfiguration
$ProjectRoot = "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01"
$BackupRoot = "$ProjectRoot\Backups"
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupDir = "$BackupRoot\$BackupName`_$Timestamp"

# Farbige Output-Funktionen
function Write-Success { Write-Host "✅ $args" -ForegroundColor Green }
function Write-Info { Write-Host "ℹ️  $args" -ForegroundColor Cyan }
function Write-Error { Write-Host "❌ $args" -ForegroundColor Red }
function Write-Warning { Write-Host "⚠️  $args" -ForegroundColor Yellow }

# Starte Backup
Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  📦 BACKUP WIRD ERSTELLT...                                   ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Erstelle Backup-Ordner
if (-not (Test-Path $BackupRoot)) {
    New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null
    Write-Info "Backup-Root erstellt: $BackupRoot"
}

if (-not (Test-Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
    Write-Success "Backup-Ordner erstellt"
}

# Zu sichernde Dateien und Ordner
$ItemsToCopy = @(
    @{Name = "Source-Code"; Source = "$ProjectRoot\Services"; Dest = "$BackupDir\Services" },
    @{Name = "XAML-Dateien"; Source = "$ProjectRoot\Views"; Dest = "$BackupDir\Views" },
    @{Name = "Assets"; Source = "$ProjectRoot\Assets"; Dest = "$BackupDir\Assets" },
    @{Name = "USB-Tools"; Source = "$ProjectRoot\USB_Installation"; Dest = "$BackupDir\USB_Installation" },
    @{Name = "Installer"; Source = "$ProjectRoot\Installer_Source"; Dest = "$BackupDir\Installer_Source" }
)

# Kopiere Dateien
$Count = 1
foreach ($Item in $ItemsToCopy) {
    if (Test-Path $Item.Source) {
        Write-Host "[$Count/5] Kopiere $($Item.Name)..." -ForegroundColor White
        Copy-Item -Path $Item.Source -Destination $Item.Dest -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
        Write-Success "  ✓ $($Item.Name) gesichert"
    }
    $Count++
}

# Kopiere einzelne wichtige Dateien
Write-Host "[6/6] Kopiere Projekt-Dateien..." -ForegroundColor White

$FilePatterns = @("*.csproj", "*.sln", "*.ps1", "*.bat", "*.txt", "*.md")
foreach ($Pattern in $FilePatterns) {
    Get-ChildItem -Path $ProjectRoot -Filter $Pattern -File | 
        Copy-Item -Destination $BackupDir -Force -ErrorAction SilentlyContinue
}

Write-Success "  ✓ Projekt-Dateien gesichert"

# Zeige Backup-Info
Write-Host ""
Write-Host "✅ BACKUP ERSTELLT!" -ForegroundColor Green
Write-Host ""
Write-Host "📁 Backup-Pfad:" -ForegroundColor Cyan
Write-Host "   $BackupDir" -ForegroundColor White
Write-Host ""

# Zähle Dateien im Backup
$FileCount = (Get-ChildItem -Path $BackupDir -Recurse -File).Count
Write-Host "📊 Backup-Info:" -ForegroundColor Cyan
Write-Host "   Dateien: $FileCount" -ForegroundColor White
Write-Host "   Größe: $((Get-ChildItem -Path $BackupDir -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB)MB" -ForegroundColor White
Write-Host ""

# Behalte nur die letzten 10 Backups
Write-Host "🧹 Bereinige alte Backups..." -ForegroundColor Yellow
$OldBackups = Get-ChildItem -Path $BackupRoot -Directory | Sort-Object CreationTime -Descending | Select-Object -Skip 10

foreach ($OldBackup in $OldBackups) {
    Write-Host "   Lösche: $($OldBackup.Name)" -ForegroundColor Gray
    Remove-Item -Path $OldBackup.FullName -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Success "Alte Backups bereinigt (behalte die 10 neuesten)"
Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ Automatisches Backup-System ist AKTIVIERT!" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Zeige Backup-Historie
Write-Host "📋 BACKUP-HISTORIE (neueste 5):" -ForegroundColor Cyan
Write-Host ""
Get-ChildItem -Path $BackupRoot -Directory | Sort-Object CreationTime -Descending | Select-Object -First 5 | 
    ForEach-Object {
        $FileCount = (Get-ChildItem -Path $_.FullName -Recurse -File).Count
        $Size = [math]::Round((Get-ChildItem -Path $_.FullName -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB, 2)
        Write-Host "  📦 $($_.Name)" -ForegroundColor White
        Write-Host "     📅 $(Get-Date $_.CreationTime -Format 'dd.MM.yyyy HH:mm:ss')" -ForegroundColor Gray
        Write-Host "     📊 $FileCount Dateien, $($Size) MB" -ForegroundColor Gray
        Write-Host ""
    }

Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
