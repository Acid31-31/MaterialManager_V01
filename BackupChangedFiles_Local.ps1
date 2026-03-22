$source = "C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01"
$backupRoot = "D:\MaterialManager_V01_Backups"
$maxVersions = 10

if (-not (Test-Path $backupRoot)) {
    New-Item -ItemType Directory -Path $backupRoot | Out-Null
}

$manifestPath = Join-Path $backupRoot "manifest.json"
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
    } catch { $manifest = @{} }
}

$versionFolders = Get-ChildItem -Path $backupRoot -Directory -Filter "Version_*" -ErrorAction SilentlyContinue
$nextIndex = 1
if ($versionFolders.Count -gt 0) {
    $indices = $versionFolders | ForEach-Object {
        if ($_.Name -match "Version_(\d{3})") { [int]$Matches[1] }
    }
    $last = ($indices | Measure-Object -Maximum).Maximum
    $nextIndex = $last + 1
}
if ($nextIndex -gt $maxVersions) { $nextIndex = 1 }
$versionName = "Version_{0:D3}" -f $nextIndex
$versionPath = Join-Path $backupRoot $versionName

if (Test-Path $versionPath) {
    Remove-Item -Path $versionPath -Recurse -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $versionPath | Out-Null

$excludeDirs = @("bin","obj",".vs")
$files = Get-ChildItem -Path $source -Recurse -File | Where-Object {
    $rel = $_.FullName.Substring($source.Length + 1)
    -not ($excludeDirs | Where-Object { $rel.StartsWith($_ + "\") })
}

$changed = @()
foreach ($file in $files) {
    $rel = $file.FullName.Substring($source.Length + 1)
    $hash = (Get-FileHash -Path $file.FullName -Algorithm SHA256).Hash
    if (-not $manifest.ContainsKey($rel) -or $manifest[$rel] -ne $hash) {
        $changed += @{ Path = $file.FullName; Rel = $rel; Hash = $hash }
    }
}

foreach ($entry in $changed) {
    $dest = Join-Path $versionPath $entry.Rel
    $destDir = Split-Path $dest -Parent
    if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
    Copy-Item -Path $entry.Path -Destination $dest -Force
    $manifest[$entry.Rel] = $entry.Hash
}

$manifest | ConvertTo-Json | Set-Content -Path $manifestPath -Encoding UTF8

Write-Host "Backup erstellt: $versionPath"
Write-Host ("Geaenderte Dateien: {0}" -f $changed.Count)
