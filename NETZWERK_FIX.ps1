# Einfacher Netzwerk-Server Setup für MaterialManager

$SharePath = "C:\MaterialManager_TestShare"
$ShareName = "MaterialManager_TestShare"
$ComputerName = $env:COMPUTERNAME

Write-Host ""
Write-Host "🚀 NETZWERK-SERVER SETUP" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Admin-Check
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $isAdmin) {
    Write-Host "❌ Dieses Script benötigt Admin-Rechte!" -ForegroundColor Red
    Write-Host "Starte PowerShell als Administrator!" -ForegroundColor Yellow
    Read-Host "Drücke Enter"
    exit 1
}

Write-Host "✅ Admin-Rechte: OK" -ForegroundColor Green
Write-Host ""

# Schritt 1: Alte Freigabe löschen
Write-Host "[1] Entferne alte Freigabe..." -ForegroundColor White
cmd /c net share $ShareName /delete /yes 2>&1 | Out-Null
Start-Sleep -Milliseconds 500

# Schritt 2: Ordner erstellen
Write-Host "[2] Erstelle Ordner..." -ForegroundColor White
if (-not (Test-Path $SharePath)) {
    New-Item -Path $SharePath -ItemType Directory -Force | Out-Null
    Write-Host "    ✅ $SharePath" -ForegroundColor Green
} else {
    Write-Host "    ℹ️  Ordner existiert bereits" -ForegroundColor Cyan
}

# Schritt 3: Freigabe erstellen
Write-Host "[3] Erstelle Netzwerk-Freigabe..." -ForegroundColor White
$result = cmd /c net share $ShareName="$SharePath" /grant:Everyone,FULL /remark:"MaterialManager Test Server" 2>&1
if ($result -like "*freigegeben*" -or $result -like "*shared*") {
    Write-Host "    ✅ Freigabe erstellt" -ForegroundColor Green
} else {
    Write-Host "    ⚠️  Prüfe manuell..." -ForegroundColor Yellow
}

Start-Sleep -Seconds 1

# Schritt 4: Verifikation
Write-Host "[4] Verifiziere Freigabe..." -ForegroundColor White
$share = cmd /c net share $ShareName 2>&1
if ($share -like "*$ShareName*") {
    Write-Host "    ✅ Freigabe aktiv" -ForegroundColor Green
} else {
    Write-Host "    ❌ Freigabe nicht aktiv" -ForegroundColor Red
}

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ NETZWERK-SERVER BEREIT!" -ForegroundColor Green
Write-Host ""
Write-Host "📍 Zugriffsadressen:" -ForegroundColor Cyan
Write-Host "   \\localhost\$ShareName" -ForegroundColor Yellow
Write-Host "   \\$ComputerName\$ShareName" -ForegroundColor Yellow
Write-Host ""
Write-Host "📁 Lokaler Pfad:" -ForegroundColor Cyan
Write-Host "   $SharePath" -ForegroundColor Yellow
Write-Host ""
Write-Host "🚀 Im Programm eingeben:" -ForegroundColor Cyan
Write-Host "   Einstellungen → Netzwerk → Pfad aktivieren" -ForegroundColor White
Write-Host "   Pfad: \\localhost\$ShareName" -ForegroundColor Yellow
Write-Host ""
Write-Host "🧪 Zum Testen:" -ForegroundColor Cyan
Write-Host "   Terminal 1: dotnet run" -ForegroundColor White
Write-Host "   Terminal 2: dotnet run" -ForegroundColor White
Write-Host "   Beide: Netzwerk-Pfad eingeben → Test-Button" -ForegroundColor White
Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Read-Host "Drücke Enter zum Beenden"
