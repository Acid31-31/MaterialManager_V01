# =============================================================================
# MaterialManager R03 - Automatisches Backup-System
# Erstellt versionierte Backups in einen Ordner
# =============================================================================

# Pfade definieren
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$backupBaseDir = Join-Path $projectRoot "Backups"
$maxVersions = 10

# Erstelle Backup-Basis-Ordner falls nicht vorhanden
if (!(Test-Path $backupBaseDir)) {
    New-Item -ItemType Directory -Path $backupBaseDir -Force | Out-Null
    Write-Host "✅ Backup-Ordner erstellt: $backupBaseDir"
}

# Finde alle vorhandenen Versionen
$existingVersions = Get-ChildItem -Path $backupBaseDir -Directory -Filter "Version_*" | 
    Sort-Object Name | 
    Select-Object -ExpandProperty Name

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════"
Write-Host "  MaterialManager R03 - Automatisches Backup"
Write-Host "════════════════════════════════════════════════════════════════"
Write-Host ""

# Bestimme die nächste Versionsnummer
if ($existingVersions.Count -lt $maxVersions) {
    # Noch nicht alle 10 Versionen erstellt
    $nextVersion = $existingVersions.Count + 1
    $nextVersionFolder = "Version_{0:D3}" -f $nextVersion
    $targetDir = Join-Path $backupBaseDir $nextVersionFolder
    
    Write-Host "[1/3] Erstelle neue Version: $nextVersionFolder"
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
} else {
    # 10 Versionen voll - ersetze die älteste (Version_001)
    $oldestVersion = $existingVersions[0]
    $targetDir = Join-Path $backupBaseDir $oldestVersion
    
    Write-Host "[1/3] ROTATION: Ersetze älteste Version ($oldestVersion) mit neuen Daten"
    Remove-Item -Path $targetDir -Recurse -Force | Out-Null
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
}

# Kopiere Source-Code
Write-Host "[2/3] Kopiere Source-Code..."
$sourceDir = Join-Path $projectRoot "Views"
$servicesDir = Join-Path $projectRoot "Services"
$modelsDir = Join-Path $projectRoot "Models"
$convertersDir = Join-Path $projectRoot "Converters"

# Erstelle Ordnerstruktur
foreach ($dir in @("Views", "Services", "Models", "Converters")) {
    $targetSubDir = Join-Path $targetDir $dir
    New-Item -ItemType Directory -Path $targetSubDir -Force | Out-Null
}

# Kopiere Dateien
Copy-Item -Path "$sourceDir\*" -Destination "$targetDir\Views\" -Exclude "*.xaml" -ErrorAction SilentlyContinue
Copy-Item -Path "$servicesDir\*" -Destination "$targetDir\Services\" -ErrorAction SilentlyContinue
Copy-Item -Path "$modelsDir\*" -Destination "$targetDir\Models\" -ErrorAction SilentlyContinue
Copy-Item -Path "$convertersDir\*" -Destination "$targetDir\Converters\" -ErrorAction SilentlyContinue

# Kopiere Hauptdateien
Copy-Item -Path "$projectRoot\MainWindow.xaml.cs" -Destination "$targetDir\" -ErrorAction SilentlyContinue
Copy-Item -Path "$projectRoot\App.xaml.cs" -Destination "$targetDir\" -ErrorAction SilentlyContinue
Copy-Item -Path "$projectRoot\*.csproj" -Destination "$targetDir\" -ErrorAction SilentlyContinue

Write-Host "     ✓ Dateien kopiert"

# Erstelle Info-Datei mit Metadata
Write-Host "[3/3] Erstelle Backup-Info..."
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$backupInfo = @"
╔════════════════════════════════════════════════════════════════╗
║           MaterialManager R03 - Backup Information            ║
╚════════════════════════════════════════════════════════════════╝

📅 BACKUP-DATUM: $timestamp
🔢 VERSION: $($targetDir | Split-Path -Leaf)
📊 GESAMT-VERSIONEN: $([Math]::Min($existingVersions.Count + 1, $maxVersions)) / $maxVersions

✅ Backup erfolgreich erstellt!
   Alles in diesem Ordner ist wiederherstellbar.

🔄 VERSIONIERUNGS-SYSTEM:
   • Speichert max. 10 Versionen
   • Älteste Version wird durch neue ersetzt
   • Lasse `CreateAutoBackup.ps1` nach jeder Programmänderung laufen

📂 ORDNER-STRUKTUR:
   Backups/
   ├── Version_001/  (älteste)
   ├── Version_002/
   ├── ...
   └── Version_010/  (neueste)

💾 ACHTUNG:
   • Backups sind NUR für Quellcode!
   • Für komplette Backup: Auch bin/ und obj/ sichern
   • Git ist auch noch vorhanden als Fallback!

"@

$backupInfo | Set-Content -Path "$targetDir\BACKUP_INFO.txt" -Encoding UTF8

Write-Host "     ✓ Info-Datei erstellt"
Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════"
Write-Host "✅ BACKUP ERFOLGREICH!"
Write-Host "════════════════════════════════════════════════════════════════"
Write-Host ""
Write-Host "📍 Speicherort: $targetDir"
Write-Host ""

# Zeige alle Versionen an
Write-Host "📊 VERFÜGBARE VERSIONEN:"
Write-Host ""
$allVersions = Get-ChildItem -Path $backupBaseDir -Directory -Filter "Version_*" | 
    Sort-Object Name | 
    ForEach-Object {
        $infoFile = Join-Path $_.FullName "BACKUP_INFO.txt"
        if (Test-Path $infoFile) {
            $content = Get-Content $infoFile
            $dateLine = $content | Select-String "BACKUP-DATUM:" | Select-Object -ExpandProperty Line
            $date = if ($dateLine) { $dateLine.Replace("📅 BACKUP-DATUM: ", "") } else { "Unbekannt" }
            Write-Host "  $($_.Name)  →  $date"
        } else {
            Write-Host "  $($_.Name)  →  (keine Info)"
        }
    }

Write-Host ""
Write-Host "💡 TIPP: Nach jeder Programmänderung diese Datei ausführen:"
Write-Host "   PowerShell> .\CreateAutoBackup.ps1"
Write-Host ""
