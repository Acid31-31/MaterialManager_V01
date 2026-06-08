$scriptDir = Split-Path -Parent $PSCommandPath
$candidates = @(
    (Join-Path $scriptDir 'USB_Installation\UNINSTALL_GUI.ps1'),
    (Join-Path $scriptDir 'UNINSTALL_GUI.ps1')
)

$targetScript = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $targetScript) {
    [System.Windows.Forms.MessageBox]::Show('UNINSTALL_GUI.ps1 wurde nicht gefunden.', 'Deinstallation', 'OK', 'Error')
    exit 1
}

Start-Process powershell.exe -WindowStyle Hidden -ArgumentList @(
    '-NoProfile',
    '-ExecutionPolicy', 'Bypass',
    '-File', "`"$targetScript`""
)
