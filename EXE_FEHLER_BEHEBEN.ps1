# EXE-Fehler Behebung - PowerShell Version
# Diagnotiziert und fixt Probleme mit EXE-Dateien

$ErrorActionPreference = "Continue"

Write-Host "`n╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  🔧 EXE-FEHLER BEHEBUNG - AUTOMATISCHER FIX                  ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# SCHRITT 1: Prüfe .NET 8
Write-Host "1️⃣  PRÜFE .NET 8 INSTALLATION..." -ForegroundColor Yellow
$dotnetCheck = & dotnet --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ .NET 8 NICHT INSTALLIERT!" -ForegroundColor Red
    Write-Host "`nLÖSUNG:" -ForegroundColor Yellow
    Write-Host "1. Gehe zu: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor White
    Write-Host "2. Lade '.NET 8 Desktop Runtime' herunter" -ForegroundColor White
    Write-Host "3. Installiere und starte neu" -ForegroundColor White
    exit 1
} else {
    Write-Host "✅ .NET 8 ist installiert: $dotnetCheck`n" -ForegroundColor Green
}

# SCHRITT 2: Prüfe ob EXE existiert
Write-Host "2️⃣  PRÜFE OB USB_INSTALLATIONHELPER.EXE EXISTIERT..." -ForegroundColor Yellow
$exePath = "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01\USB_InstallationHelper\bin\Release\net8.0-windows\USB_InstallationHelper.exe"

if (-not (Test-Path $exePath)) {
    Write-Host "❌ EXE NICHT GEFUNDEN!" -ForegroundColor Red
    Write-Host "`nLÖSUNG:" -ForegroundColor Yellow
    Write-Host "Baue die EXE jetzt automatisch..." -ForegroundColor White
    
    cd "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01"
    
    if (Test-Path "Build-USBHelper.ps1") {
        Write-Host "`n🔨 Starte Build-USBHelper.ps1..." -ForegroundColor Cyan
        & .\Build-USBHelper.ps1 -Configuration Release
        
        if (Test-Path $exePath) {
            Write-Host "`n✅ EXE ERFOLGREICH GEBAUT!" -ForegroundColor Green
        } else {
            Write-Host "`n❌ BUILD FEHLGESCHLAGEN!" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "❌ Build-USBHelper.ps1 nicht gefunden!" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "✅ EXE existiert`n" -ForegroundColor Green
    $size = [math]::Round((Get-Item $exePath).Length / 1MB, 2)
    Write-Host "   Größe: $size MB" -ForegroundColor White
    Write-Host "   Pfad: $exePath`n" -ForegroundColor Gray
}

# SCHRITT 3: Prüfe Berechtigungen
Write-Host "3️⃣  PRÜFE DATEIBERECHTIGUNGEN..." -ForegroundColor Yellow
try {
    $acl = Get-Acl $exePath
    Write-Host "✅ Berechtigungen OK`n" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Berechtigungsproblem erkannt" -ForegroundColor Yellow
    Write-Host "Versuche zu beheben..." -ForegroundColor Yellow
    
    try {
        $acl = Get-Acl $exePath
        $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            [System.Security.Principal.WindowsIdentity]::GetCurrent().User,
            "FullControl",
            "Allow"
        )
        $acl.AddAccessRule($rule)
        Set-Acl -Path $exePath -AclObject $acl
        Write-Host "✅ Berechtigungen repariert`n" -ForegroundColor Green
    } catch {
        Write-Host "❌ Konnte Berechtigungen nicht ändern" -ForegroundColor Red
    }
}

# SCHRITT 4: Prüfe abhängigkeiten
Write-Host "4️⃣  PRÜFE ABHÄNGIGKEITEN (DLLs)..." -ForegroundColor Yellow
$binPath = Split-Path $exePath
$dlls = @("System.Windows.Forms.dll", "System.Drawing.dll", "System.IO.dll")
$missingDlls = @()

foreach ($dll in $dlls) {
    if (-not (Test-Path "$binPath\$dll")) {
        $missingDlls += $dll
    }
}

if ($missingDlls.Count -gt 0) {
    Write-Host "⚠️  ABHÄNGIGKEITEN FEHLEN:" -ForegroundColor Yellow
    $missingDlls | ForEach-Object { Write-Host "   - $_" -ForegroundColor Yellow }
    
    Write-Host "`n🔨 Baue EXE nochmal neu..." -ForegroundColor Cyan
    cd "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01"
    
    # Lösche alte Build
    if (Test-Path "USB_InstallationHelper\bin") {
        Remove-Item "USB_InstallationHelper\bin" -Recurse -Force
    }
    if (Test-Path "USB_InstallationHelper\obj") {
        Remove-Item "USB_InstallationHelper\obj" -Recurse -Force
    }
    
    # Neuer Build
    & dotnet build "USB_InstallationHelper\USB_InstallationHelper.csproj" -c Release
    
    if (Test-Path $exePath) {
        Write-Host "✅ EXE NOCHMAL GEBAUT`n" -ForegroundColor Green
    } else {
        Write-Host "❌ BUILD FEHLGESCHLAGEN!" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "✅ Alle Abhängigkeiten vorhanden`n" -ForegroundColor Green
}

# SCHRITT 5: Starte die EXE
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "5️⃣  STARTEN DER EXE..." -ForegroundColor Yellow
Write-Host "`n🚀 Starte: USB_InstallationHelper.exe`n" -ForegroundColor Green

try {
    Start-Process $exePath
    Write-Host "✅ EXE GESTARTET ERFOLGREICH!" -ForegroundColor Green
    Write-Host "`nGUI-Fenster sollte jetzt öffnen..." -ForegroundColor White
    Write-Host "Falls nicht: Schau im Taskmanager nach dem Prozess" -ForegroundColor Gray
} catch {
    Write-Host "❌ FEHLER BEIM STARTEN:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host "`n════════════════════════════════════════════════════════════════`n" -ForegroundColor Cyan
