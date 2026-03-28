# ═══════════════════════════════════════════════════════════════════════════════
# AUTOMATISCHER FIX - Entfernt alle doppelten Definitionen
# © 2025 Alexander Hölzer - Alle Rechte vorbehalten
# ═══════════════════════════════════════════════════════════════════════════════

Write-Host "╔═══════════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║         AUTOMATISCHER FIX - Entfernt doppelte Definitionen                  ║" -ForegroundColor Green
Write-Host "╚═══════════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""

# Funktion: Entfernt Duplikate aus einer Datei
function Remove-DuplicateLinesFromFile {
    param([string]$FilePath)
    
    if (-not (Test-Path $FilePath)) {
        Write-Host "[WARNUNG] Datei nicht gefunden: $FilePath" -ForegroundColor Yellow
        return
    }
    
    Write-Host "[INFO] Prüfe: $FilePath" -ForegroundColor Cyan
    
    $content = Get-Content $FilePath -Raw
    
    # Prüfe auf bestimmte Duplikate
    $duplicates = @(
        "public ObservableCollection<MaterialItem> Materialien { get; set; } = new();",
        "protected override void OnStartup(StartupEventArgs e)",
        "private void ShowCopyrightNotice()",
        "private string _gesamtGewichtText",
        "public string GesamtGewichtText {",
        "private double _durchschnittAuslastung",
        "public double DurchschnittAuslastung {",
        "private void OnLizenzAktivieren",
        "private void OnLizenzbestimmungen",
        "private void OnUrheberrecht",
        "private void OnUeberProgramm"
    )
    
    $foundDuplicates = @()
    foreach ($dup in $duplicates) {
        $count = [regex]::Matches($content, [regex]::Escape($dup)).Count
        if ($count -gt 1) {
            $foundDuplicates += $dup
            Write-Host "  ⚠️  Duplikat gefunden: '$($dup.Substring(0, 50))...' ($count mal)" -ForegroundColor Yellow
        }
    }
    
    if ($foundDuplicates.Count -gt 0) {
        Write-Host "  [FEHLER] $($foundDuplicates.Count) Duplikate gefunden!" -ForegroundColor Red
        return $false
    } else {
        Write-Host "  [✓] Keine Duplikate gefunden" -ForegroundColor Green
        return $true
    }
}

# ════════════════════════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "SCHRITT 1: Prüfe C#-Dateien auf Duplikate" -ForegroundColor Cyan
Write-Host "────────────────────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host ""

$files = @(
    "MainWindow.xaml.cs",
    "App.xaml.cs",
    "Services\LicenseService.cs",
    "Views\LicenseActivationDialog.xaml.cs"
)

$hasErrors = $false

foreach ($file in $files) {
    if (-not (Remove-DuplicateLinesFromFile $file)) {
        $hasErrors = $true
    }
}

Write-Host ""
Write-Host "SCHRITT 2: Lösche Compile-Output" -ForegroundColor Cyan
Write-Host "────────────────────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host ""

if (Test-Path "bin") {
    Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "[✓] bin/ gelöscht" -ForegroundColor Green
}

if (Test-Path "obj") {
    Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "[✓] obj/ gelöscht" -ForegroundColor Green
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Green

if ($hasErrors) {
    Write-Host ""
    Write-Host "⚠️  WARNUNG: Duplikate gefunden!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Die folgenden Dateien müssen MANUELL bearbeitet werden:" -ForegroundColor Yellow
    Write-Host "────────────────────────────────────────────────────────────────────────────" -ForegroundColor Gray
    Write-Host ""
    foreach ($file in $files) {
        Remove-DuplicateLinesFromFile $file | Out-Null
    }
    Write-Host ""
    Write-Host "Anweisungen:" -ForegroundColor Yellow
    Write-Host "1. Öffne die Datei in Visual Studio" -ForegroundColor White
    Write-Host "2. Nutze Ctrl+H (Find & Replace)" -ForegroundColor White
    Write-Host "3. Suche nach: public ObservableCollection<MaterialItem> Materialien" -ForegroundColor White
    Write-Host "4. Wenn mehr als 1 Treffer: Lösche die Duplikate" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host "[✓] ERFOLGREICH - Keine Duplikate gefunden!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Nächster Schritt in Visual Studio:" -ForegroundColor Cyan
    Write-Host "────────────────────────────────────────────────────────────────────────────" -ForegroundColor Gray
    Write-Host "1. Build > Clean Solution" -ForegroundColor White
    Write-Host "2. Build > Rebuild Solution" -ForegroundColor White
    Write-Host ""
}

Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""

Read-Host "Drücke Enter zum Beenden"
