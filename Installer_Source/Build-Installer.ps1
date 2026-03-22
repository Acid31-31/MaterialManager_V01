# MaterialManager R03 - Installer.exe Builder
# Erstellt eine standalone EXE-Datei ohne Antivirus-Probleme

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Build", "Package")]
    [string]$Action = "Package",
    
    [Parameter(Mandatory=$false)]
    [string]$CertificatePath = "",
    
    [Parameter(Mandatory=$false)]
    [string]$CertificatePassword = ""
)

$ErrorActionPreference = "Stop"
$ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectPath = $ScriptPath

Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  MaterialManager R03 - Installer.exe Builder                  ║" -ForegroundColor Cyan
Write-Host "║  Action: $Action" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

function Build-Installer {
    Write-Host "[1/2] Building Installer.exe..." -ForegroundColor Yellow
    
    $buildOutput = & dotnet build "$ProjectPath\MaterialManager_Installer.csproj" -c Release
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Build successful!" -ForegroundColor Green
    Write-Host ""
}

function Publish-Installer {
    Write-Host "[2/2] Publishing self-contained installer..." -ForegroundColor Yellow
    
    $outputDir = Join-Path $ProjectPath "bin\Release\net8.0-windows\win-x64\publish"
    
    $publishOutput = & dotnet publish `
        "$ProjectPath\MaterialManager_Installer.csproj" `
        -c Release `
        --self-contained
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Publish failed!" -ForegroundColor Red
        exit 1
    }
    
    # Rename to Installer.exe
    $exePath = Join-Path $outputDir "MaterialManager_Installer.exe"
    $installerPath = Join-Path $outputDir "Installer.exe"
    
    if (Test-Path $exePath) {
        Move-Item -Path $exePath -Destination $installerPath -Force
    }
    
    Write-Host "✅ Publish successful!" -ForegroundColor Green
    Write-Host "   Output: $installerPath" -ForegroundColor Gray
    Write-Host ""
}

function Sign-Installer {
    Write-Host "[3/3] Signing installer..." -ForegroundColor Yellow
    
    if ([string]::IsNullOrEmpty($CertificatePath)) {
        Write-Host "⚠️  No certificate specified. Skipping signing..." -ForegroundColor Yellow
        return
    }
    
    if (!(Test-Path $CertificatePath)) {
        Write-Host "❌ Certificate not found: $CertificatePath" -ForegroundColor Red
        exit 1
    }
    
    $outputDir = Join-Path $ProjectPath "bin\Release\net8.0-windows\win-x64\publish"
    $exePath = Join-Path $outputDir "Installer.exe"
    
    if (!(Test-Path $exePath)) {
        Write-Host "❌ Installer.exe not found. Run 'Build' first!" -ForegroundColor Red
        exit 1
    }
    
    $signToolPath = Get-ChildItem "C:\Program Files*\Windows Kits\*\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    
    if ($null -eq $signToolPath) {
        Write-Host "⚠️  SignTool.exe not found. Skipping signing..." -ForegroundColor Yellow
        return
    }
    
    & $signToolPath sign /f $CertificatePath /p $CertificatePassword /t http://timestamp.comodoca.com/authenticode $exePath
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Installer signed successfully!" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Signing failed (non-critical)" -ForegroundColor Yellow
    }
    
    Write-Host ""
}

# Execute action
switch ($Action) {
    "Build" { Build-Installer }
    "Package" { Build-Installer; Publish-Installer; Sign-Installer }
}

Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║  ✅ Installer Build Complete!                                ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Green

# Show result
$outputDir = Join-Path $ProjectPath "bin\Release\net8.0-windows\win-x64\publish"
$exePath = Join-Path $outputDir "Installer.exe"

if (Test-Path $exePath) {
    Write-Host ""
    Write-Host "📦 Installer.exe erstellt:" -ForegroundColor Cyan
    Write-Host "   $exePath" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Dateigröße:" -ForegroundColor Cyan
    $size = [math]::Round((Get-Item $exePath).Length / 1MB, 2)
    Write-Host "   $size MB" -ForegroundColor Green
}
