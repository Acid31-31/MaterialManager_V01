param(
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$csproj = Join-Path $root 'MaterialManager_V01.csproj'
$vdproj = Join-Path $root 'Installer_Source\MaterialManager_Setup.vdproj'

if (-not (Test-Path $csproj)) { throw "csproj nicht gefunden: $csproj" }
if (-not (Test-Path $vdproj)) { throw "Setup Project (.vdproj) nicht gefunden: $vdproj" }

if ([string]::IsNullOrWhiteSpace($Version)) {
    $build = Get-Date -Format 'yyDDD'
    $Version = "1.0.$build"
}

if (-not [Version]::TryParse($Version, [ref]([Version]::new()))) {
    throw "Ungültige Version: $Version"
}

$parts = $Version.Split('.')
if ($parts.Length -ne 3) {
    throw "Version muss 3-teilig sein, z.B. 1.0.123"
}

$fileVersion = "$Version.0"

Write-Host "Setze Version auf: $Version"

# csproj aktualisieren
$xml = Get-Content $csproj -Raw
$xml = [regex]::Replace($xml, '<Version>.*?</Version>', "<Version>$Version</Version>")
$xml = [regex]::Replace($xml, '<AssemblyVersion>.*?</AssemblyVersion>', "<AssemblyVersion>$fileVersion</AssemblyVersion>")
$xml = [regex]::Replace($xml, '<FileVersion>.*?</FileVersion>', "<FileVersion>$fileVersion</FileVersion>")
if ($xml -match '<InformationalVersion>') {
    $xml = [regex]::Replace($xml, '<InformationalVersion>.*?</InformationalVersion>', "<InformationalVersion>$Version</InformationalVersion>")
} else {
    $xml = $xml -replace '</PropertyGroup>', "  <InformationalVersion>$Version</InformationalVersion>`r`n  </PropertyGroup>"
}
Set-Content -Path $csproj -Value $xml -Encoding UTF8

# vdproj aktualisieren
$vd = Get-Content $vdproj -Raw
$vd = [regex]::Replace($vd, '"ProductVersion"\s*=\s*"8:[^"]+"', "\"ProductVersion\" = \"8:$Version\"")
$vd = [regex]::Replace($vd, '"PackageCode"\s*=\s*"8:\{[^\}]+\}"', "\"PackageCode\" = \"8:{$([guid]::NewGuid().ToString().ToUpper())}\"")
$vd = [regex]::Replace($vd, '"ProductCode"\s*=\s*"8:\{[^\}]+\}"', "\"ProductCode\" = \"8:{$([guid]::NewGuid().ToString().ToUpper())}\"")
if ($vd -notmatch '"RemovePreviousVersions"\s*=\s*"11:TRUE"') {
    $vd = $vd -replace '"RemovePreviousVersions"\s*=\s*"11:FALSE"', '"RemovePreviousVersions" = "11:TRUE"'
}
Set-Content -Path $vdproj -Value $vd -Encoding UTF8

Write-Host "\nGeänderte Dateien (git diff):"
git -C $root diff -- $csproj $vdproj

Write-Host "\nFertig. Neue Version: $Version"