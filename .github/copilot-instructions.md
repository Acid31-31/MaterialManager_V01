# Copilot Instructions

## Project Guidelines
- Antworte immer auf Deutsch.
- Vor jeder Codeänderung immer ein vollständiges Projekt-Backup im `Backup`-Verzeichnis erstellen; keine Einzeldatei-Backups als Ersatz verwenden.
- **Backup-Regel:** Maximal 3 Backup-Ordner im `Backup`-Verzeichnis. Vor jedem neuen Backup zuerst prüfen, ob bereits 3 vorhanden sind – wenn ja, den ältesten löschen, dann das neue erstellen. Niemals mehr als 3 Backups anlegen. Beim Kopieren immer folgende Ordner ausschließen: `Backup`, `bin`, `obj`, `.git`, `dist`, `publish`, `publish_release`, `update-installer`, `update-installer-release`, `_ARCHIVE_TO_REVIEW`, `_Multi`, sowie alle alten Backup-Ordner im Projektstamm (Muster: `BACKUP_*`, `Backup_*`).
- Bei riskanten Änderungen zuerst kurz den Plan nennen; keine funktionierenden Dateien ohne ausdrückliche Zustimmung löschen oder ersetzen.
- Nach Änderungen standardmäßig sowohl lokal als auch auf GitHub aktualisieren. Bei `komplett aktualisieren` den lokalen USB-Ordner inklusive `USB_Installation` und GitHub vollständig synchronisieren.
- Immer den aktuellen `COPILOTWORKSPACE CONTEXT` und `IDESTATE CONTEXT` berücksichtigen.
- Neue vom Benutzer genannte Arbeitsregeln nach Möglichkeit in diese Datei übernehmen, damit sie nicht wiederholt werden müssen.
- Verbindliche UI-Regel: Bei allen neu erstellten UI-Elementen immer ein dunkles Design verwenden (Fenster, Dialoge, Dropdowns, ContextMenus, Popups); keine hellen Standard-Hintergründe oder helle Titelleisten verwenden.
- **Neue UI-Regel:** Bei allen neu erstellten UI-Elementen (Fenster, Dialoge, Dropdowns, ContextMenus, Popups) immer durchgehend dunkles Design verwenden; keine hellen Standard-Elemente zulassen.
- Keine neuen Verzeichnisse oder zusätzlichen Git-Branches ohne ausdrückliche Zustimmung anlegen.
- Das bestehende `USB_Installation`-Layout beibehalten. Vorhandene Installationsdateien, Erklärungen und sonstige Inhalte nicht durch Publish ersetzen oder entfernen, außer mit ausdrücklicher Zustimmung.
- Der `UpdateInstaller` soll den Installationspfad automatisch erkennen, damit Updates auf mehreren Rechnern und in unterschiedlichen Installationsordnern ohne manuelle Pfadangabe funktionieren.
- Die Menüzeile 'Datei, Bearbeiten, Ansicht, Hilfe' soll in die oberste Fensterleiste verschoben werden – also in den Bereich, in dem aktuell die Demo-Version im Fenstertitel steht – nicht nur in die dunkle Kopfzeile des Inhalts.
- Die App soll sich auf jedem PC und Tablet automatisch an die jeweilige Bildschirmgröße anpassen und immer die verfügbare Fläche vollständig ausfüllen; auf kleinen 10-Zoll-Tablets darf seitlich kein Desktop sichtbar bleiben.
- Die App soll benutzer- bzw. rollenbasiert gestaltet werden: An der Lasermaschine soll der Benutzer nur Restmaterial suchen, reservieren und sehen können; vollständige Lagerverwaltung ist dort nicht nötig. Der Lasermaschinen-Programmierer sucht und reserviert Restmaterial, damit der Lasermaschinen-Bediener es sieht. Zusätzlich soll eine Rolle für die Tafelbelegung und die Reservierung von Restmaterial in der App berücksichtigt werden.
- Die Laser-Bediener sollen nicht bei jedem Start neu angelegt werden müssen. Mehrere Bediener werden einmal festgelegt, dauerhaft gespeichert und später per Auswahl ausgewählt oder gelöscht.
- Die Lager-Sicht soll für gelieferte neue Materialien, Regalauslastung und Inventur gedacht sein; sie braucht keine Funktion 'Reservierte Reste'. Sichtbare Texte mit 'Demo' sollen entfernt werden.
- In der Laser-Sicht müssen gebuchte Reste trotz reduzierter Auftragsansicht per Doppelklick bearbeitbar bleiben, damit nach der Bearbeitung verbleibende Restmaße gespeichert werden können. Nach Änderung von reservierten Resten im Laser soll die Auftragsnummer gelöscht werden und das Material nicht mehr in der Laser-Sicht erscheinen. Der Laser soll nur reservierte Tafeln und Reste sehen und diese ändern oder löschen können.
- Bei der Materialsuche sollen zuerst exakte Maßtreffer angezeigt werden; nur wenn kein exakter Treffer vorhanden ist, sollen größere Materialien aufsteigend von klein nach groß angezeigt werden – überall, wo Materialsuche verwendet wird.
- Änderungen und notwendige Speichervorgänge sollen automatisch ausgeführt werden, ohne den Benutzer zu manuellen Speicherschritten aufzufordern.
- Sichtbare UI-Texte dürfen keine Encoding-/Sonderzeichenfehler enthalten; sie sollen in deutscher Sprache korrekt mit Umlauten angezeigt werden und Umlaute dürfen nicht durch Ersatzschreibweisen wie ae/oe/ue ersetzt werden. Bei Meldungen und Beschriftungen sollen fehleranfällige Sonderzeichen vermieden bzw. robust dargestellt werden.
- Netzwerk-Synchronisation soll beim Start nicht erzwungen werden; zuerst Programm testen, Einrichtung später manuell über Einstellungen.
- **Installer-/USB-Stand:** Der Installer- und USB-Stand soll nach Änderungen immer aktualisiert sein, damit in der Firma keine veraltete Installation verwendet wird.

## Auftragsanforderungen
- Archivierung über Netzwerkpfad implementieren.
- Start eines Laser-Auftrags nur, wenn alle Materialpositionen PDFs haben.
- Bei Abschluss sowohl Auftragsdaten als auch PDF-Kopien in KW-Archiv (nur aktuelles Jahr) verschieben.
- Automatische Löschung/Überschreibung von Archivdaten nach 12 Monaten.
- KW-Auswahl im UI mit aktueller KW als Standard und Umschalten auf andere KWs des aktuellen Jahres ermöglichen.

## License Key Generation
- Die beiden Lizenzschlüssel-Mechanismen müssen vereinheitlicht bleiben:
  - `LicenseKeyGenerator.cs` verwendet HMAC-SHA256 mit `MM_V01_MASTER_SECRET_2025_PRODUCTION` und erzeugt das Format `MM-XXXX-XXXX-XXXX-XXXX`.
  - `LicenseService.cs` darf keine abweichende eigene Schlüsselgenerierung mehr verwenden.
- `LicenseService.ActivateFullLicense()` muss `LicenseKeyGenerator.GenerateLicenseKey()` verwenden.
- **Preisdarstellung in der Lizenzaktivierung** soll auf professionelles Software-Niveau angehoben werden; die bisherigen niedrigen Beispielpreise sind nicht passend. Lizenzpreise sollen marktgerecht bleiben und klar unter ca. 30.000 EUR/Jahr für Firmenlizenz liegen; Wettbewerberpreise sollen als Referenz genutzt werden. Lizenzpreise sollen nicht pauschal sein, sondern abhängig vom Software-Umfang und den enthaltenen Funktionen bewertet und daraus berechnet werden.
