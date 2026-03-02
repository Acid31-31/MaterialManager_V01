$defaultSource = $PSScriptRoot
$repoSource = "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01"
$source = if (Test-Path $repoSource) { $repoSource } else { $defaultSource }
$backupRoot = "D:\MaterialManager_V01_Backups"
$maxVersions = 10

if (-not (Test-Path $backupRoot)) {
    New-Item -ItemType Directory -Path $backupRoot | Out-Null
}

$versionFolders = Get-ChildItem -Path $backupRoot -Directory -Filter "Version_*" -ErrorAction SilentlyContinue | Sort-Object Name
$nextIndex = 1
if ($versionFolders.Count -gt 0) {
    $last = $versionFolders[-1].Name
    if ($last -match "Version_(\d{3})") {
        $nextIndex = [int]$Matches[1] + 1
    }
}
if ($nextIndex -gt $maxVersions) { $nextIndex = 1 }
$versionName = "Version_" + $nextIndex.ToString().PadLeft(3, '0')
$versionPath = Join-Path $backupRoot $versionName

if (Test-Path $versionPath) {
    Remove-Item -Path $versionPath -Recurse -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $versionPath | Out-Null

robocopy "$source" "$versionPath" /E /XD .vs /R:1 /W:1 /NFL /NDL /NP | Out-Null

Write-Host "Voll-Backup erstellt: $versionPath"
