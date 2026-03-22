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
- Die Menüzeile 'Datei, Bearbeiten, Ansicht, Hilfe' soll in die oberste Fensterleiste verschoben werden – also in den Bereich, in dem aktuell die Demo-Version im Fenstertitel steht – nicht nur in die dunkle Kopfzeile des Inhalts.
- Die App soll sich auf jedem PC und Tablet automatisch an die jeweilige Bildschirmgröße anpassen und immer die verfügbare Fläche vollständig ausfüllen; auf kleinen 10-Zoll-Tablets darf seitlich kein Desktop sichtbar bleiben.
- Die App soll benutzer- bzw. rollenbasiert gestaltet werden: An der Lasermaschine soll der Benutzer nur Restmaterial suchen, reservieren und sehen können; vollständige Lagerverwaltung ist dort nicht nötig. Der Lasermaschinen-Programmierer sucht und reserviert Restmaterial, damit der Lasermaschinen-Bediener es sieht. Zusätzlich soll eine Rolle für die Tafelbelegung und die Reservierung von Restmaterial in der App berücksichtigt werden.
- Die Laser-Bediener sollen nicht bei jedem Start neu angelegt werden müssen. Mehrere Bediener werden einmal festgelegt, dauerhaft gespeichert und später per Auswahl ausgewählt oder gelöscht.
- Die Lager-Sicht soll für gelieferte neue Materialien, Regalauslastung und Inventur gedacht sein; sie braucht keine Funktion 'Reservierte Reste'. Sichtbare Texte mit 'Demo' sollen entfernt werden.
- In der Laser-Sicht müssen gebuchte Reste trotz reduzierter Auftragsansicht per Doppelklick bearbeitbar bleiben, damit nach der Bearbeitung verbleibende Restmaße gespeichert werden können. Nach Änderung von reservierten Resten im Laser soll die Auftragsnummer gelöscht werden und das Material nicht mehr in der Laser-Sicht erscheinen. Der Laser soll nur reservierte Tafeln und Reste sehen und diese ändern oder löschen können.

## License Key Generation
- Die beiden Lizenzschlüssel-Mechanismen müssen vereinheitlicht bleiben:
  - `LicenseKeyGenerator.cs` verwendet HMAC-SHA256 mit `MM_V01_MASTER_SECRET_2025_PRODUCTION` und erzeugt das Format `MM-XXXX-XXXX-XXXX-XXXX`.
  - `LicenseService.cs` darf keine abweichende eigene Schlüsselgenerierung mehr verwenden.
- `LicenseService.ActivateFullLicense()` muss `LicenseKeyGenerator.GenerateLicenseKey()` verwenden.
