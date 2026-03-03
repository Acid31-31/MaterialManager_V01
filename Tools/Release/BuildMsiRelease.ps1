param(
    [string]$Version,
    [switch]$SkipBump,
    [switch]$CreateGitHubRelease
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $root

$dist = Join-Path $root 'dist'
if (-not (Test-Path $dist)) { New-Item -ItemType Directory -Path $dist | Out-Null }

if (-not $SkipBump) {
    & (Join-Path $PSScriptRoot 'BumpVersion.ps1') -Version $Version
}

Write-Host 'Baue App (Release)...'
dotnet build .\MaterialManager_V01.csproj -c Release

Write-Host 'Baue Setup Project (vdproj) via devenv.com ...'
$devenv = Get-Command devenv.com -ErrorAction SilentlyContinue
if (-not $devenv) {
    throw 'devenv.com nicht gefunden. Bitte Visual Studio Installer Projects + Developer PowerShell verwenden.'
}

& $devenv.Source .\MaterialManager_V01.sln /Build Release

$msiCandidates = Get-ChildItem -Recurse -Filter *.msi | Where-Object {
    $_.FullName -notmatch '\\Backups\\' -and $_.FullName -notmatch '\\dist\\'
} | Sort-Object LastWriteTime -Descending

if (-not $msiCandidates) {
    throw 'Kein MSI gefunden nach Build.'
}

$msi = $msiCandidates | Select-Object -First 1
$targetMsi = Join-Path $dist $msi.Name
Copy-Item $msi.FullName $targetMsi -Force

$hash = Get-FileHash $targetMsi -Algorithm SHA256
$hashFile = "$targetMsi.sha256.txt"
Set-Content -Path $hashFile -Value "$($hash.Hash)  $($msi.Name)" -Encoding UTF8

Write-Host "MSI bereit: $targetMsi"
Write-Host "SHA256: $($hash.Hash)"

if ($CreateGitHubRelease) {
    $gh = Get-Command gh -ErrorAction SilentlyContinue
    if ($gh) {
        $tag = "v$((Get-Content .\MaterialManager_V01.csproj -Raw | Select-String -Pattern '<Version>(.*?)</Version>' -AllMatches).Matches[0].Groups[1].Value)"
        gh release create $tag $targetMsi --title $tag --notes "MSI Release $tag"
    }
    else {
        Write-Host 'gh CLI nicht gefunden. Manuell uploaden:'
        Write-Host "1) Release Tag anlegen"
        Write-Host "2) Datei hochladen: $targetMsi"
        Write-Host "3) SHA256 bereitstellen: $hashFile"
    }
}
