param(
    [int]$KeepBackups = 3,
    [switch]$ClearNuGetCache
)

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectRoot

$backupRoot = Join-Path $projectRoot 'Backup'
if (-not (Test-Path $backupRoot)) {
    New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
}

Write-Host "[1/4] Backups aufräumen (behalte: $KeepBackups)..." -ForegroundColor Cyan
$backupDirs = Get-ChildItem $backupRoot -Directory | Sort-Object LastWriteTime -Descending
$toRemove = $backupDirs | Select-Object -Skip $KeepBackups
foreach ($dir in $toRemove) {
    Remove-Item $dir.FullName -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host "  Entfernt: $($toRemove.Count)" -ForegroundColor DarkGray

Write-Host "[2/4] Build-Artefakte löschen (bin/obj/.vs)..." -ForegroundColor Cyan
$targets = Get-ChildItem $projectRoot -Directory -Recurse -Force -ErrorAction SilentlyContinue |
    Where-Object { @('bin','obj','.vs') -contains $_.Name -and $_.FullName -notlike '*\\Backup\\*' }
foreach ($t in $targets) {
    Remove-Item $t.FullName -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host "  Bereinigt: $($targets.Count) Ordner" -ForegroundColor DarkGray

if ($ClearNuGetCache) {
    Write-Host "[3/4] NuGet-Cache leeren..." -ForegroundColor Cyan
    dotnet nuget locals all --clear
} else {
    Write-Host "[3/4] NuGet-Cache übersprungen (mit -ClearNuGetCache aktivieren)." -ForegroundColor DarkGray
}

Write-Host "[4/4] Build starten..." -ForegroundColor Cyan
& dotnet build MaterialManager_V01.csproj

if ($LASTEXITCODE -eq 0) {
    Write-Host "Fertig: Build erfolgreich." -ForegroundColor Green
} else {
    Write-Host "Build fehlgeschlagen (ExitCode: $LASTEXITCODE)." -ForegroundColor Red
    exit $LASTEXITCODE
}
