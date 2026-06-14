# Beendet laufende MaterialManager-Instanz vor neuer Programmierarbeit.
$ErrorActionPreference = 'SilentlyContinue'

Get-Process -Name 'MaterialManager_V01' | Stop-Process -Force

# dotnet run haengt manchmal am Host-Prozess – kurz warten und erneut pruefen
Start-Sleep -Milliseconds 300
Get-Process -Name 'MaterialManager_V01' | Stop-Process -Force

exit 0
