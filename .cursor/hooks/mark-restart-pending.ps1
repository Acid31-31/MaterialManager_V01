# Setzt Flag: nach Agent-Ende App neu starten (nur bei echten Code-Aenderungen).
$projectRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
$flag = Join-Path $projectRoot '.cursor\.app-restart-pending'
New-Item -ItemType File -Path $flag -Force | Out-Null
exit 0
