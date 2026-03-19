# Copilot Instructions

## Project Guidelines
- Benutzer möchte ALLE Antworten auf Deutscher Sprache (Deutsch). NIEMALS auf Englisch antworten.
- User requires full project-level backups (complete folder snapshots) in the Backup directory before changes, not single-file backups in separate ad-hoc locations. This applies to all modifications to the codebase. Always create backups first, then make changes. Never delete or modify without a backup first. Always show what will be done and ask for confirmation on risky operations.
- Der Benutzer möchte nach jeder Änderung eine Aktualisierung sowohl lokal in einem PC-Ordner als auch auf GitHub durchführen lassen und erwartet diesen Ablauf standardmäßig immer. Wenn der Nutzer 'komplett aktualisieren' verlangt, sollen lokaler USB-Ordner (inkl. USB_Installation-Stamm) und GitHub sofort vollständig synchronisiert und sichtbar aktualisiert werden.
- Der Update-Installer soll den Installationspfad automatisch erkennen, damit Updates auf mehreren Rechnern und in unterschiedlichen Installationsordnern ohne manuelle Pfadangabe funktionieren.
- Aktuellen `COPILOTWORKSPACE CONTEXT` und `IDESTATE CONTEXT` immer berücksichtigen, ohne dass der Benutzer dies erneut erwähnen muss.
- Neue vom Benutzer genannte Arbeitsregeln sollen nach Möglichkeit in diese Anweisungen übernommen werden, damit sie nicht wiederholt werden müssen.
- Der Benutzer möchte keine neuen Verzeichnisse oder zusätzlichen Git-Branches ohne explizite vorherige Zustimmung; Änderungen sollen am erwarteten Ort erfolgen.
- Vorhandene Installationsdateien, Erklärungen, Urheberrechtsdateien und sonstige Inhalte im bestehenden USB-Installationslayout dürfen nicht durch Publish ersetzt oder entfernt werden; die bestehende Installationsart muss erhalten bleiben und Änderungen daran nur mit expliziter Zustimmung erfolgen.
- Der Benutzer will keine funktionierenden Dateien ohne ausdrückliche Erlaubnis gelöscht oder ersetzt haben; bei riskanten Änderungen zuerst Plan nennen und Zustimmung einholen.

## License Key Generation
- The project uses two different license key generation mechanisms that MUST be unified:
  - `LicenseKeyGenerator.cs` uses HMAC-SHA256 with "MM_V01_MASTER_SECRET_2025_PRODUCTION" and produces `MM-XXXX-XXXX-XXXX-XXXX` format.
  - `LicenseService.cs` had its own `GenerateLicenseKey()` using plain SHA256, which created incompatibility.
- To ensure consistency, `LicenseService.ActivateFullLicense()` must call `LicenseKeyGenerator.GenerateLicenseKey()`.
