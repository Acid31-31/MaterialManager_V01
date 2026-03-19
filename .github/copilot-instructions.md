# Copilot Instructions

## Project Guidelines
- Antworte immer auf Deutsch.
- Vor jeder Codeänderung immer ein vollständiges Projekt-Backup im `Backup`-Verzeichnis erstellen; keine Einzeldatei-Backups als Ersatz verwenden.
- Bei riskanten Änderungen zuerst kurz den Plan nennen; keine funktionierenden Dateien ohne ausdrückliche Zustimmung löschen oder ersetzen.
- Nach Änderungen standardmäßig sowohl lokal als auch auf GitHub aktualisieren. Bei `komplett aktualisieren` den lokalen USB-Ordner inklusive `USB_Installation` und GitHub vollständig synchronisieren.
- Immer den aktuellen `COPILOTWORKSPACE CONTEXT` und `IDESTATE CONTEXT` berücksichtigen.
- Neue vom Benutzer genannte Arbeitsregeln nach Möglichkeit in diese Datei übernehmen, damit sie nicht wiederholt werden müssen.
- Keine neuen Verzeichnisse oder zusätzlichen Git-Branches ohne ausdrückliche Zustimmung anlegen.
- Das bestehende `USB_Installation`-Layout beibehalten. Vorhandene Installationsdateien, Erklärungen und sonstige Inhalte nicht durch Publish ersetzen oder entfernen, außer mit ausdrücklicher Zustimmung.
- Der `UpdateInstaller` soll den Installationspfad automatisch erkennen, damit Updates auf mehreren Rechnern und in unterschiedlichen Installationsordnern ohne manuelle Pfadangabe funktionieren.

## License Key Generation
- Die beiden Lizenzschlüssel-Mechanismen müssen vereinheitlicht bleiben:
  - `LicenseKeyGenerator.cs` verwendet HMAC-SHA256 mit `MM_V01_MASTER_SECRET_2025_PRODUCTION` und erzeugt das Format `MM-XXXX-XXXX-XXXX-XXXX`.
  - `LicenseService.cs` darf keine abweichende eigene Schlüsselgenerierung mehr verwenden.
- `LicenseService.ActivateFullLicense()` muss `LicenseKeyGenerator.GenerateLicenseKey()` verwenden.
