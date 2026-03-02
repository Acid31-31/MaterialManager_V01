# MaterialManager R03 - USB Build & Deployment Script
# Verwendet werden: dotnet build, dotnet publish, Code-Signing

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Build", "Publish", "Sign", "Package")]
    [string]$Action = "Build",
    
    [Parameter(Mandatory=$false)]
    [string]$CertificatePath = "",
    
    [Parameter(Mandatory=$false)]
    [string]$CertificatePassword = ""
)

$ErrorActionPreference = "Stop"
$ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectPath = $ScriptPath

Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  MaterialManager R03 - USB Deployment Build Script            ║" -ForegroundColor Cyan
Write-Host "║  Action: $Action" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

function Build-Application {
    Write-Host "[1/3] Building application..." -ForegroundColor Yellow
    
    $buildOutput = & dotnet build "$ProjectPath\MaterialManager_V01.csproj" -c Release
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Build successful!" -ForegroundColor Green
    Write-Host ""
}

function Publish-Application {
    Write-Host "[2/3] Publishing self-contained application..." -ForegroundColor Yellow
    
    $usbPackagePath = Join-Path $ProjectPath "USB_Package"
    
    if (Test-Path $usbPackagePath) {
        Remove-Item -Path $usbPackagePath -Recurse -Force
    }
    
    $publishOutput = & dotnet publish `
        "$ProjectPath\MaterialManager_V01.csproj" `
        -c Release `
        -p:PublishProfile=USBVersion `
        -p:PublishDir="$usbPackagePath"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Publish failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Publish successful!" -ForegroundColor Green
    Write-Host "   Output: $usbPackagePath" -ForegroundColor Gray
    Write-Host ""
}

function Sign-Executable {
    Write-Host "[3/3] Signing executable with certificate..." -ForegroundColor Yellow
    
    if ([string]::IsNullOrEmpty($CertificatePath)) {
        Write-Host "⚠️  Kein Zertifikat angegeben. Skipping Signing..." -ForegroundColor Yellow
        Write-Host "   Usage: .\Build.ps1 -Action Sign -CertificatePath 'path\cert.pfx' -CertificatePassword 'password'" -ForegroundColor Gray
        Write-Host ""
        return
    }
    
    if (!(Test-Path $CertificatePath)) {
        Write-Host "❌ Certificate not found: $CertificatePath" -ForegroundColor Red
        exit 1
    }
    
    $exePath = Join-Path $ProjectPath "USB_Package\MaterialManager_V01.exe"
    
    if (!(Test-Path $exePath)) {
        Write-Host "❌ Executable not found. Run 'Publish' first!" -ForegroundColor Red
        exit 1
    }
    
    # Verwende SignTool (Teil von Windows SDK)
    $signToolPath = Get-ChildItem "C:\Program Files*\Windows Kits\*\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    
    if ($null -eq $signToolPath) {
        Write-Host "⚠️  SignTool.exe nicht gefunden. Windows SDK erforderlich!" -ForegroundColor Yellow
        Write-Host "   Download: https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/" -ForegroundColor Gray
        return
    }
    
    & $signToolPath sign /f $CertificatePath /p $CertificatePassword /t http://timestamp.comodoca.com/authenticode $exePath
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "⚠️  Signing fehlgeschlagen (non-critical)" -ForegroundColor Yellow
    } else {
        Write-Host "✅ Executable signed successfully!" -ForegroundColor Green
    }
    
    Write-Host ""
}

function Create-Package {
    Write-Host "[FINAL] Creating USB package..." -ForegroundColor Yellow
    
    $usbPackagePath = Join-Path $ProjectPath "USB_Package"
    $outputPath = Join-Path $ProjectPath "USB_Distribution"
    
    if (!(Test-Path $usbPackagePath)) {
        Write-Host "❌ USB_Package not found. Run 'Publish' first!" -ForegroundColor Red
        exit 1
    }
    
    # Erstelle Distribution-Verzeichnis
    if (Test-Path $outputPath) {
        Remove-Item -Path $outputPath -Recurse -Force
    }
    New-Item -ItemType Directory -Path $outputPath | Out-Null
    
    # Kopiere alle Dateien
    Copy-Item -Path "$usbPackagePath\*" -Destination $outputPath -Recurse -Force
    
    # Kopiere Installations-Skripte
    Copy-Item -Path "$ProjectPath\USB_INSTALL.bat" -Destination $outputPath -Force
    Copy-Item -Path "$ProjectPath\GENERATE_LICENSE.bat" -Destination $outputPath -Force
    Copy-Item -Path "$ProjectPath\USB_README.txt" -Destination $outputPath -Force
    
    # Erstelle LICENSE-Datei
    $licenseContent = @"
LIZENZBESTIMMUNGEN - MaterialManager R03
========================================

1. LIZENZGEWÄHRUNG
Diese Software wird unter einer nicht-exklusiven, nicht-übertragbaren Lizenz zur Nutzung gewährt.

2. HARDWARE-BINDUNG
Die Lizenz ist an die Hardware-ID des Installationsrechners gebunden und nicht auf andere Rechner übertragbar.

3. NUTZUNGSBESCHRÄNKUNGEN
- Nicht für kommerzielle Weitergabe ohne Genehmigung
- Keine Dekompilierung oder Reverse-Engineering
- Keine Netzwerk-Sharing ohne Enterprise-Lizenz

4. GEWÄHRLEISTUNG
Die Software wird "wie vorhanden" bereitgestellt, ohne Gewährleistung jeglicher Art.

5. LIZENZABLAUF
- Demo-Version: 30 Tage
- Vollversion: Unbegrenzt (nach Aktivierung)

Für Fragen kontakt: support@materialmanager.de

© 2025 MaterialManager - Alle Rechte vorbehalten
"@
    Set-Content -Path "$outputPath\LICENSE.txt" -Value $licenseContent -Encoding UTF8
    
    # Erstelle ZIP für Versand
    $zipPath = Join-Path (Split-Path $ProjectPath) "MaterialManager_V01_USB_Distribution.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    
    Compress-Archive -Path $outputPath -DestinationPath $zipPath -Force
    
    Write-Host "✅ Package created successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📦 Output locations:" -ForegroundColor Cyan
    Write-Host "   Folder: $outputPath" -ForegroundColor Green
    Write-Host "   ZIP   : $zipPath" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 USB-Inhalt:" -ForegroundColor Cyan
    Get-ChildItem -Path $outputPath | Format-Table Name, @{Label="Size"; Expression={if($_.PSIsContainer){"<Folder>"} else{[math]::Round($_.Length/1MB, 2).ToString() + " MB"}}} -AutoSize
    Write-Host ""
}

# Führe Aktion aus
switch ($Action) {
    "Build" { Build-Application }
    "Publish" { Build-Application; Publish-Application }
    "Sign" { Publish-Application; Sign-Executable }
    "Package" { Build-Application; Publish-Application; Sign-Executable; Create-Package }
}

Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║  ✅ Build Process Complete!                                   ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
