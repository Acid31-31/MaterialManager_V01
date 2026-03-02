# MaterialManager R03 - Automatische Personalisierung
# (c) 2025 Alexander Hoelzer

$PersonalData = @{
    Name        = "Alexander Hölzer"
    Adresse     = "Pfarrer-Rosenkranz-Strasse 9, 56642 Kruft"
    Email       = "hoelzer_alex@yahoo.de"
    Telefon     = "+49 170 8339993"
    Website     = "Auf Anfrage"
    Stadt       = "Kruft"
}

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

Clear-Host
Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "   AUTOMATISCHE PERSONALISIERUNG - MaterialManager R03" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "IHRE DATEN:" -ForegroundColor Yellow
Write-Host "  Name:     $($PersonalData.Name)"
Write-Host "  Adresse:  $($PersonalData.Adresse)"
Write-Host "  E-Mail:   $($PersonalData.Email)"
Write-Host "  Telefon:  $($PersonalData.Telefon)"
Write-Host "  Website:  $($PersonalData.Website)"
Write-Host "  Stadt:    $($PersonalData.Stadt)"
Write-Host ""

$backupFolder = "Backup_Vor_Personalisierung_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Write-Host "[BACKUP] Erstelle Backup in: $backupFolder" -ForegroundColor Yellow
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
Write-Host "[OK] Backup erstellt" -ForegroundColor Green
Write-Host ""

Write-Host "[VERARBEITUNG] Personalisiere Dateien..." -ForegroundColor Yellow
Write-Host ""

$totalChanges = 0
$filesProcessed = 0

foreach ($file in $FilesToProcess) {
    Write-Host "[$($filesProcessed + 1)/$($FilesToProcess.Count)] $file" -ForegroundColor White
    
    if (-not (Test-Path $file)) {
        Write-Host "  [!] Datei nicht gefunden" -ForegroundColor Red
        continue
    }
    
    $content = Get-Content $file -Raw -Encoding UTF8
    $changeCount = 0
    
    foreach ($placeholder in $Replacements.Keys) {
        $replacement = $Replacements[$placeholder]
        $oldContent = $content
        $content = $content.Replace($placeholder, $replacement)
        
        if ($oldContent -ne $content) {
            $matches = ([regex]::Matches($oldContent, [regex]::Escape($placeholder))).Count
            $changeCount += $matches
        }
    }
    
    if ($changeCount -gt 0) {
        Set-Content $file $content -Encoding UTF8 -NoNewline
        Write-Host "  [OK] $changeCount Platzhalter ersetzt" -ForegroundColor Green
        $totalChanges += $changeCount
    } else {
        Write-Host "  [-] Keine Platzhalter gefunden" -ForegroundColor Gray
    }
    
    $filesProcessed++
}

Write-Host ""
Write-Host "================================================================" -ForegroundColor Green
Write-Host "   ZUSAMMENFASSUNG" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Dateien verarbeitet:  $filesProcessed / $($FilesToProcess.Count)"
Write-Host "Gesamt Ersetzungen:   $totalChanges" -ForegroundColor Cyan
Write-Host "Backup erstellt in:   $backupFolder" -ForegroundColor Green
Write-Host ""

if ($totalChanges -gt 0) {
    Write-Host "[ERFOLG] PERSONALISIERUNG ABGESCHLOSSEN!" -ForegroundColor Green
    Write-Host ""
    Write-Host "NAECHSTE SCHRITTE:" -ForegroundColor Yellow
    Write-Host "  1. Pruefen Sie die geaenderten Dateien"
    Write-Host "  2. Kompilieren Sie das Projekt (Visual Studio > Build)"
    Write-Host "  3. Fuehren Sie Build_USB_Vollversion.bat aus"
} else {
    Write-Host "[WARNUNG] KEINE PLATZHALTER GEFUNDEN" -ForegroundColor Yellow
    Write-Host "Moeglicherweise wurden die Dateien bereits personalisiert."
}

Write-Host ""
Write-Host "Druecken Sie Enter zum Beenden..."
$null = Read-Host
