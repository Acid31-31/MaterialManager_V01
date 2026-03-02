# ═══════════════════════════════════════════════════════════════════════════════
# AUTOMATISCHE PERSONALISIERUNG - MaterialManager R03
# Ersetzt alle Platzhalter mit den echten Daten
# © 2025 Alexander Hölzer
# ═══════════════════════════════════════════════════════════════════════════════

$ErrorActionPreference = "Stop"

# ═══════════════════════════════════════════════════════════════════════════════
# KONFIGURATION - DEINE DATEN
# ═══════════════════════════════════════════════════════════════════════════════

$PersonalData = @{
    Name        = "Alexander Hölzer"
    Adresse     = "Pfarrer-Rosenkranz-Strasse 9, 56642 Kruft"
    Email       = "hoelzer_alex@yahoo.de"
    Telefon     = "+49 170 8339993"
    Website     = "Auf Anfrage"
    Stadt       = "Kruft"
}

# Platzhalter-Mapping
$Replacements = @{
    '[DEIN NAME / FIRMENNAME]'  = $PersonalData.Name
    '[DEIN NAME]'                = $PersonalData.Name
    '[FIRMENNAME]'               = $PersonalData.Name
    '[DEINE ADRESSE]'            = $PersonalData.Adresse
    '[DEINE EMAIL]'              = $PersonalData.Email
    '[DEINE TELEFONNUMMER]'      = $PersonalData.Telefon
    '[DEINE WEBSITE]'            = $PersonalData.Website
    '[DEINE STADT]'              = $PersonalData.Stadt
}

# Dateien die personalisiert werden sollen
$FilesToProcess = @(
    "LICENSE.txt",
    "COPYRIGHT.txt",
    "VOLLVERSION_LEITFADEN.txt",
    "_START_HIER_VOLLVERSION.txt",
    "USB_Installer\README_VOLLVERSION.txt",
    "USB_Installer\INSTALL_VOLLVERSION.bat",
    "Build_USB_Vollversion.bat",
    "Services\LicenseService.cs",
    "App.xaml.cs",
    "MainWindow.xaml.cs",
    "MaterialManager_V01.csproj",
    "Views\LicenseActivationDialog.xaml",
    "Views\LicenseActivationDialog.xaml.cs"
)

# ═══════════════════════════════════════════════════════════════════════════════
# MAIN SCRIPT
# ═══════════════════════════════════════════════════════════════════════════════

Clear-Host

Write-Host "╔═══════════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║              AUTOMATISCHE PERSONALISIERUNG - MaterialManager R03              ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Zeige Konfiguration
Write-Host "IHRE DATEN:" -ForegroundColor Yellow
Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host "  Name:         $($PersonalData.Name)" -ForegroundColor White
Write-Host "  Adresse:      $($PersonalData.Adresse)" -ForegroundColor White
Write-Host "  E-Mail:       $($PersonalData.Email)" -ForegroundColor White
Write-Host "  Telefon:      $($PersonalData.Telefon)" -ForegroundColor White
Write-Host "  Website:      $($PersonalData.Website)" -ForegroundColor White
Write-Host "  Stadt:        $($PersonalData.Stadt)" -ForegroundColor White
Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host ""

# Erstelle Backup
$backupFolder = "Backup_Vor_Personalisierung_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Write-Host "[BACKUP] Erstelle Backup..." -ForegroundColor Yellow

New-Item -ItemType Directory -Path $backupFolder -Force | Out-Null

foreach ($file in $FilesToProcess) {
    if (Test-Path $file) {
        $destPath = Join-Path $backupFolder $file
        $destDir = Split-Path $destPath -Parent
        
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }
        
        Copy-Item $file $destPath -Force
    }
}

Write-Host "[BACKUP] Backup erstellt in: $backupFolder" -ForegroundColor Green
Write-Host ""

# Verarbeite Dateien
Write-Host "[VERARBEITUNG] Personalisiere Dateien..." -ForegroundColor Yellow
Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host ""

$totalChanges = 0
$filesProcessed = 0

foreach ($file in $FilesToProcess) {
    Write-Host "[$($filesProcessed + 1)/$($FilesToProcess.Count)] $file" -ForegroundColor White
    
    if (-not (Test-Path $file)) {
        Write-Host "  [!] Datei nicht gefunden" -ForegroundColor Red
        continue
    }
    
    # Lese Datei
    $content = Get-Content $file -Raw -Encoding UTF8
    $changeCount = 0
    
    # Ersetze alle Platzhalter
    foreach ($placeholder in $Replacements.Keys) {
        $replacement = $Replacements[$placeholder]
        $oldContent = $content
        $content = $content.Replace($placeholder, $replacement)
        
        if ($oldContent -ne $content) {
            $matches = ([regex]::Matches($oldContent, [regex]::Escape($placeholder))).Count
            $changeCount += $matches
            Write-Host "    → Ersetze '$placeholder' ($matches x)" -ForegroundColor Cyan
        }
    }
    
    # Speichere wenn Änderungen
    if ($changeCount -gt 0) {
        Set-Content $file $content -Encoding UTF8 -NoNewline
        Write-Host "  [✓] $changeCount Platzhalter ersetzt" -ForegroundColor Green
        $totalChanges += $changeCount
    } else {
        Write-Host "  [→] Keine Platzhalter gefunden" -ForegroundColor Gray
    }
    
    $filesProcessed++
    Write-Host ""
}

# Zusammenfassung
Write-Host "╔═══════════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║                              ZUSAMMENFASSUNG                                  ║" -ForegroundColor Green
Write-Host "╚═══════════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "Dateien verarbeitet:       $filesProcessed / $($FilesToProcess.Count)" -ForegroundColor White
Write-Host "Gesamt Ersetzungen:        $totalChanges" -ForegroundColor Cyan
Write-Host "Backup erstellt in:        $backupPath" -ForegroundColor Green
Write-Host ""
Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor Gray

if ($totalChanges -gt 0) {
    Write-Host "✓ PERSONALISIERUNG ERFOLGREICH!" -ForegroundColor Green
    Write-Host ""
    Write-Host "NÄCHSTE SCHRITTE:" -ForegroundColor Yellow
    Write-Host "  1. Prüfen Sie die geänderten Dateien" -ForegroundColor White
    Write-Host "  2. Kompilieren Sie das Projekt (Visual Studio > Build)" -ForegroundColor White
    Write-Host "  3. Führen Sie Build_USB_Vollversion.bat aus" -ForegroundColor White
} else {
    Write-Host "⚠ KEINE PLATZHALTER GEFUNDEN" -ForegroundColor Yellow
    Write-Host "Möglicherweise wurden die Dateien bereits personalisiert." -ForegroundColor Gray
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Gray
Write-Host ""
Write-Host "Drücken Sie eine beliebige Taste zum Beenden..." -ForegroundColor Gray
$null = Read-Host
