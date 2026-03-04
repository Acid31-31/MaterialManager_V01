param(
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $root

if ([string]::IsNullOrWhiteSpace($Version)) {
    $csproj = Join-Path $root 'MaterialManager_V01.csproj'
    $raw = Get-Content $csproj -Raw
    $m = [regex]::Match($raw, '<Version>(.*?)</Version>')
    if (-not $m.Success) { throw 'Version in csproj nicht gefunden.' }
    $Version = $m.Groups[1].Value
}

Write-Host "Zielversion: $Version"

Write-Host "[1/6] App Build (Release)"
dotnet build .\MaterialManager_V01.csproj -c Release

Write-Host "[2/6] Hinweis: Setup-Projekt (.vdproj) muss in Visual Studio gebaut werden."
Write-Host "Bitte in VS: MaterialManager_Setup -> Build (Release)"
Write-Host "Wenn gefragt: ProductCode aktualisieren = JA"

$expectedMsi = Join-Path $root 'Installer_Source\Release\MaterialManager_V01.msi'
if (-not (Test-Path $expectedMsi)) {
    throw "MSI nicht gefunden: $expectedMsi`nBitte zuerst in Visual Studio das Setup-Projekt bauen."
}

Write-Host "[3/6] MSI gefunden: $expectedMsi"

$dist = Join-Path $root 'dist\msi_test'
if (-not (Test-Path $dist)) { New-Item -ItemType Directory -Path $dist | Out-Null }

$outMsi = Join-Path $dist ("MaterialManager_V01_{0}.msi" -f $Version)
Copy-Item $expectedMsi $outMsi -Force

Write-Host "[4/6] Kopiert nach: $outMsi"

Write-Host "[5/6] GitHub Release erstellen (gh, latest)"
$tag = "v$Version"
$title = "MaterialManager v$Version"
$notes = "MSI Update"

gh release create $tag "$outMsi" --title "$title" --notes "$notes" --latest

Write-Host "[6/6] Fertig"
Write-Host "Release URL prüfen mit: gh release view $tag --repo Acid31-31/MaterialManager_V01"
Write-Host "curl-Test: curl.exe -L -H \"User-Agent: MaterialManager\" \"https://api.github.com/repos/Acid31-31/MaterialManager_V01/releases/latest\""
