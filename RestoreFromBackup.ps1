param(
    [string]$Version = "Version_001"
)

$backupRoot = "D:\MaterialManager_V01_Backups"
$target = "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01"

$source = Join-Path $backupRoot $Version
if (-not (Test-Path $source)) {
    Write-Host "Backup nicht gefunden: $source"
    exit 1
}

if (Test-Path $target) {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $old = "$target`_ALT_$timestamp"
    Move-Item -Path $target -Destination $old
    Write-Host "Aktueller Ordner verschoben nach: $old"
}

New-Item -ItemType Directory -Path $target | Out-Null
Copy-Item -Path (Join-Path $source '*') -Destination $target -Recurse -Force

Write-Host "Wiederherstellung abgeschlossen: $source -> $target"
