$defaultSource = $PSScriptRoot
$repoSource = "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01"
$source = if (Test-Path $repoSource) { $repoSource } else { $defaultSource }

$backupRoot = "D:\MaterialManager_V01_Backups"
$fullRoot = Join-Path $backupRoot "Full"
$incrementalRoot = Join-Path $backupRoot "Incremental"
$manifestPath = Join-Path $incrementalRoot "manifest.json"

$maxFullBackups = 7
$maxIncrementalBackups = 30
$excludeDirs = @(".vs", "bin", "obj", "Backup", "_ARCHIVE_TO_REVIEW", ".git")

function Ensure-Directory([string]$path) {
    if (-not (Test-Path $path)) {
        New-Item -ItemType Directory -Path $path -Force | Out-Null
    }
}

function Get-FilteredFiles([string]$rootPath) {
    Get-ChildItem -Path $rootPath -Recurse -File | Where-Object {
        $rel = $_.FullName.Substring($rootPath.Length + 1)
        -not ($excludeDirs | Where-Object { $rel.StartsWith($_ + "\") })
    }
}

Ensure-Directory $backupRoot
Ensure-Directory $fullRoot
Ensure-Directory $incrementalRoot

$report = @()
$today = (Get-Date).Date

# --- 1) Tägliches Voll-Backup (max. 7) ---
$existingFull = Get-ChildItem -Path $fullRoot -Directory -Filter "FULL_*" -ErrorAction SilentlyContinue | Sort-Object Name
$hasTodayFull = $false

foreach ($dir in $existingFull) {
    if ($dir.Name -match '^FULL_(\d{8})_\d{6}$') {
        $d = [datetime]::ParseExact($Matches[1], 'yyyyMMdd', $null)
        if ($d.Date -eq $today) { $hasTodayFull = $true; break }
    }
}

if (-not $hasTodayFull) {
    $fullName = "FULL_" + (Get-Date -Format "yyyyMMdd_HHmmss")
    $fullPath = Join-Path $fullRoot $fullName
    Ensure-Directory $fullPath

    robocopy "$source" "$fullPath" /E /XD .vs bin obj Backup _ARCHIVE_TO_REVIEW .git /R:1 /W:1 /NFL /NDL /NP | Out-Null
    $report += "FULL: erstellt ($fullName)"
}
else {
    $report += "FULL: heute bereits vorhanden"
}

$existingFull = Get-ChildItem -Path $fullRoot -Directory -Filter "FULL_*" -ErrorAction SilentlyContinue | Sort-Object Name -Descending
$fullToDelete = $existingFull | Select-Object -Skip $maxFullBackups
foreach ($old in $fullToDelete) {
    Remove-Item -Path $old.FullName -Recurse -Force -ErrorAction SilentlyContinue
}
if ($fullToDelete.Count -gt 0) {
    $report += "FULL: bereinigt ($($fullToDelete.Count) alte gelöscht)"
}

# --- 2) Inkrementelles Backup (max. 30) ---
$manifest = @{}
if (Test-Path $manifestPath) {
    try {
        $json = Get-Content $manifestPath -Raw
        $obj = ConvertFrom-Json $json -ErrorAction Stop
        if ($obj -is [System.Collections.IDictionary]) {
            $manifest = $obj
        } else {
            $manifest = @{}
            $obj.PSObject.Properties | ForEach-Object { $manifest[$_.Name] = $_.Value }
        }
    } catch {
        $manifest = @{}
    }
}

$changed = @()
$files = Get-FilteredFiles -rootPath $source
foreach ($file in $files) {
    $rel = $file.FullName.Substring($source.Length + 1)
    $hash = (Get-FileHash -Path $file.FullName -Algorithm SHA256).Hash
    if (-not $manifest.ContainsKey($rel) -or $manifest[$rel] -ne $hash) {
        $changed += @{ Path = $file.FullName; Rel = $rel; Hash = $hash }
    }
}

$incName = "INC_" + (Get-Date -Format "yyyyMMdd_HHmmss")
$incPath = Join-Path $incrementalRoot $incName
Ensure-Directory $incPath

foreach ($entry in $changed) {
    $dest = Join-Path $incPath $entry.Rel
    $destDir = Split-Path $dest -Parent
    Ensure-Directory $destDir
    Copy-Item -Path $entry.Path -Destination $dest -Force
    $manifest[$entry.Rel] = $entry.Hash
}

$manifest | ConvertTo-Json | Set-Content -Path $manifestPath -Encoding UTF8
$report += "INC: erstellt ($incName) | geänderte Dateien: $($changed.Count)"

$existingInc = Get-ChildItem -Path $incrementalRoot -Directory -Filter "INC_*" -ErrorAction SilentlyContinue | Sort-Object Name -Descending
$incToDelete = $existingInc | Select-Object -Skip $maxIncrementalBackups
foreach ($old in $incToDelete) {
    Remove-Item -Path $old.FullName -Recurse -Force -ErrorAction SilentlyContinue
}
if ($incToDelete.Count -gt 0) {
    $report += "INC: bereinigt ($($incToDelete.Count) alte gelöscht)"
}

Write-Host "Hybrid-Backup abgeschlossen" -ForegroundColor Green
$report | ForEach-Object { Write-Host " - $_" }
Write-Host "BackupRoot: $backupRoot"
