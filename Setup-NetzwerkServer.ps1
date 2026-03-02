# ╔════════════════════════════════════════════════════════════════════════╗
# ║  🖥️  NETZWERK-SERVER AUTOMATISCH EINRICHTEN                            ║
# ║  Ein Script zum Einrichten der Test-Freigabe                          ║
# ╚════════════════════════════════════════════════════════════════════════╝

param(
    [switch]$Remove = $false
)

# Farben
function Write-Success { Write-Host "✅ $args" -ForegroundColor Green }
function Write-Error { Write-Host "❌ $args" -ForegroundColor Red }
function Write-Info { Write-Host "ℹ️  $args" -ForegroundColor Cyan }
function Write-Warning { Write-Host "⚠️  $args" -ForegroundColor Yellow }

Clear-Host

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  🖥️  NETZWERK-SERVER EINRICHTUNG                             ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Admin-Rechte prüfen
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $isAdmin) {
    Write-Error "Dieses Script benötigt Admin-Rechte!"
    Write-Warning "Starte PowerShell als Administrator und versuche erneut."
    Read-Host "Drücke Enter zum Beenden"
    exit 1
}

Write-Success "Admin-Rechte bestätigt"
Write-Host ""

# Pfade
$SharePath = "C:\MaterialManager_TestShare"
$ShareName = "MaterialManager_TestShare"

if ($Remove) {
    # SHARE ENTFERNEN
    Write-Host "Entferne Netzwerk-Share..." -ForegroundColor Yellow
    
    try {
        Remove-SmbShare -Name $ShareName -Force -ErrorAction SilentlyContinue
        Write-Success "Share entfernt"
    } catch {
        Write-Warning "Share war nicht aktiv"
    }
    
    # Ordner löschen?
    $response = Read-Host "Lösche auch den Ordner? (j/n)"
    if ($response -eq "j") {
        Remove-Item -Path $SharePath -Recurse -Force -ErrorAction SilentlyContinue
        Write-Success "Ordner gelöscht"
    }
    
    Write-Host ""
    Write-Success "Entfernung abgeschlossen!"
    exit 0
}

# ═══════════════════════════════════════════════════════════════════════════
# SHARE ERSTELLEN
# ═══════════════════════════════════════════════════════════════════════════

Write-Info "Erstelle Netzwerk-Freigabe für Testing..."
Write-Host ""

# Ordner erstellen
Write-Host "Erstelle Ordner: $SharePath" -ForegroundColor White
if (-not (Test-Path $SharePath)) {
    New-Item -Path $SharePath -ItemType Directory -Force | Out-Null
    Write-Success "Ordner erstellt"
} else {
    Write-Info "Ordner existiert bereits"
}

# Share erstellen
Write-Host "Erstelle SMB-Share: $ShareName" -ForegroundColor White
try {
    Remove-SmbShare -Name $ShareName -Force -ErrorAction SilentlyContinue
} catch {}

try {
    New-SmbShare -Name $ShareName `
        -Path $SharePath `
        -FullAccess "Everyone" `
        -Description "MaterialManager_V01 Test-Freigabe" | Out-Null
    
    Write-Success "Share erstellt!"
} catch {
    Write-Error "Share-Erstellung fehlgeschlagen!"
    Write-Error $_.Exception.Message
    exit 1
}

Write-Host ""

# ═══════════════════════════════════════════════════════════════════════════
# BERECHTIGUNGEN SETZEN
# ═══════════════════════════════════════════════════════════════════════════

Write-Info "Setze Berechtigungen..."

# NTFS Berechtigungen
$acl = Get-Acl $SharePath
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "Everyone",
    "Modify",
    "ContainerInherit,ObjectInherit",
    "None",
    "Allow"
)
$acl.AddAccessRule($accessRule)
Set-Acl -Path $SharePath -AclObject $acl | Out-Null

Write-Success "Berechtigungen konfiguriert"
Write-Host ""

# ═══════════════════════════════════════════════════════════════════════════
# TESTNETZWERK VERBINDUNG
# ═══════════════════════════════════════════════════════════════════════════

Write-Info "Teste Netzwerk-Verbindung..."

$testPaths = @(
    "\\localhost\$ShareName",
    "\\$env:COMPUTERNAME\$ShareName"
)

$connected = $false
foreach ($path in $testPaths) {
    if (Test-Path $path) {
        Write-Success "Verbindung OK: $path"
        $connected = $true
        $accessPath = $path
        break
    }
}

if (-not $connected) {
    Write-Warning "Share ist erstellt, aber nicht sofort erreichbar (normal nach Erstellung)"
    Write-Info "Versuche in 5 Sekunden erneut..."
    Start-Sleep -Seconds 5
    
    if (Test-Path $testPaths[0]) {
        Write-Success "Jetzt erreichbar!"
        $accessPath = $testPaths[0]
    } else {
        Write-Warning "Starte Programm und versuche manuell: $testPaths[0]"
        $accessPath = $testPaths[0]
    }
}

Write-Host ""

# ═══════════════════════════════════════════════════════════════════════════
# ZUSAMMENFASSUNG
# ═══════════════════════════════════════════════════════════════════════════

Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Success "NETZWERK-SERVER IST BEREIT!"
Write-Host ""
Write-Host "📍 Share-Informationen:" -ForegroundColor Cyan
Write-Host "   Name: $ShareName" -ForegroundColor White
Write-Host "   Pfad: $SharePath" -ForegroundColor White
Write-Host "   Adresse: $accessPath" -ForegroundColor Green
Write-Host ""
Write-Host "🚀 Nächste Schritte:" -ForegroundColor Cyan
Write-Host ""
Write-Host "   1. Starte das Programm (2x für Multi-User Test)" -ForegroundColor White
Write-Host "      dotnet run" -ForegroundColor Yellow
Write-Host ""
Write-Host "   2. Im Programm: Einstellungen → Netzwerk" -ForegroundColor White
Write-Host "      Pfad: $accessPath" -ForegroundColor Yellow
Write-Host ""
Write-Host "   3. Test-Button klicken" -ForegroundColor White
Write-Host "      ✅ 'Erfolgreich verbunden'" -ForegroundColor Green
Write-Host ""
Write-Host "   4. Multi-User Test durchführen" -ForegroundColor White
Write-Host "      Fenster 1: Ändere Material" -ForegroundColor Yellow
Write-Host "      Fenster 2: Reload" -ForegroundColor Yellow
Write-Host "      ✅ Änderungen sichtbar?" -ForegroundColor Green
Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Share Status prüfen
Write-Host "📊 Share-Status:" -ForegroundColor Cyan
Get-SmbShare -Name $ShareName | Select-Object Name, Path, Description | Format-Table -AutoSize

Write-Host ""
Write-Host "💡 Tip: Um Share zu entfernen, führe aus:" -ForegroundColor Cyan
Write-Host "   .\Setup-NetzwerkServer.ps1 -Remove" -ForegroundColor Yellow
Write-Host ""

Read-Host "Drücke Enter zum Beenden"
