# Startet MaterialManager nach abgeschlossener Programmierung (wenn Code geaendert wurde).
$ErrorActionPreference = 'Stop'

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
$flag = Join-Path $projectRoot '.cursor\.app-restart-pending'

if (-not (Test-Path $flag)) {
    exit 0
}

Remove-Item $flag -Force

Get-Process -Name 'MaterialManager_V01' -ErrorAction SilentlyContinue | Stop-Process -Force

Set-Location $projectRoot
dotnet build MaterialManager_V01.csproj -c Debug --nologo -v q
if ($LASTEXITCODE -ne 0) {
    exit 1
}

$exeCandidates = @(
    (Join-Path $projectRoot 'bin\Debug\net8.0-windows\win-x64\MaterialManager_V01.exe'),
    (Join-Path $projectRoot 'bin\Debug\net8.0-windows\MaterialManager_V01.exe')
)

$exe = $exeCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($exe) {
    $workDir = Split-Path $exe -Parent
    Start-Process -FilePath $exe -WorkingDirectory $workDir
    exit 0
}

Start-Process -FilePath 'dotnet' -ArgumentList 'run', '--project', 'MaterialManager_V01.csproj', '--no-launch-profile' -WorkingDirectory $projectRoot
exit 0
