# MaterialManager V01 - Lizenzgenerator Tools

Vollständiges Toolset zum Generieren und Verwalten von Lizenzen für MaterialManager V01.

## 📋 Inhalt

### Tools

1. **LicenseGenerator.csproj + LicenseGenerator.cs**
   - Kommandozeilen-Tool für die Lizenzgenerierung
   - Automatische Zwischenablage-Unterstützung
   - Für Batch-Verarbeitung und Automatisierung

2. **LicenseGeneratorForm.cs**
   - GUI-Anwendung mit grafischer Oberfläche
   - Einfache und benutzerfreundliche Bedienung
   - **Empfohlen für normale Nutzung**

3. **license_manager.py**
   - Python-Skript zur Verwaltung ausgegebener Lizenzen
   - Datenbank mit allen Lizenzinformationen
   - CSV-Export, Ablauf-Warnung, Detailansicht

4. **START_LICENSE_GENERATOR.bat**
   - Windows Batch-Skript zum Starten der Tools
   - Interaktives Menü
   - Schneller Zugriff auf alle Funktionen

5. **LIZENZGENERATOR_ANLEITUNG.txt**
   - Ausführliche deutsche Anleitung
   - Schritt-für-Schritt Erklärungen
   - FAQ und technische Details

## 🚀 Schnellstart

### Option 1: GUI-Version (einfach)

```bash
# Doppelklick auf: START_LICENSE_GENERATOR.bat
# oder:
cd Tools
dotnet run --project LicenseGenerator.csproj
```

### Option 2: Kommandozeile (schnell)

```bash
cd Tools
dotnet run "ABC123DEF456GHI789JKL012" "Musterfirma GmbH" 3
```

**Parameter:**
- Argument 1: Hardware-ID (von Kunde kopiert)
- Argument 2: Firmenname
- Argument 3: Jahre Gültigkeit (optional, Standard: 1)

**Beispiel-Ausgabe:**
```
✓ Lizenzschlüssel erfolgreich generiert!

Hardware-ID:        ABC123DEF456GHI789JKL012
Registriert auf:    Musterfirma GmbH
Lizenzlaufzeit:     3 Jahr(e)
Ablaufdatum:        15.01.2028

────────────────────────────────────────────────────
LIZENZSCHLÜSSEL:    MM-4A7F-9B2E-C5D1-8K3L
────────────────────────────────────────────────────

✓ Lizenzschlüssel wurde in die Zwischenablage kopiert!
```

### Option 3: Verwaltung (für mehrere Lizenzen)

```bash
python license_manager.py
```

Menu:
- Lizenz hinzufügen
- Alle Lizenzen anzeigen
- Details anzeigen
- Ablauf-Warnung
- CSV-Export

## 📝 Workflow zum Ausstellen einer Lizenz

### Schritt 1: Hardware-ID vom Kunden erhalten

Der Kunde öffnet MaterialManager und:
- Menü → "Hilfe" → "Lizenzinformationen"
- oder
- Lizenzaktivierungs-Dialog → "Hardware-ID kopieren"

Die Hardware-ID sieht so aus: `ABC123DEF456GHI789JKL012MNO345PQR`

### Schritt 2: Lizenzschlüssel generieren

**GUI-Version:**
1. `START_LICENSE_GENERATOR.bat` doppelklicken
2. Option 1 wählen
3. Hardware-ID eingeben
4. Firmenname eingeben
5. Lizenzlaufzeit eingeben
6. "Lizenz generieren" klicken
7. "Kopieren" klicken

**Kommandozeile:**
```bash
cd Tools
dotnet run "ABC123DEF456GHI789JKL012MNO345PQR" "Musterfirma GmbH" 1
```

### Schritt 3: Lizenzschlüssel an Kunden übermitteln

Der generierte Lizenzschlüssel kann sofort verwendet werden:
- Per Email
- Per Telefon
- Über Support-Portal
- Im Lizenzaktiverungsdialog

### Schritt 4: Optional - Dokumentieren

```bash
python license_manager.py
```
- Option 1: Lizenz hinzufügen
- Automatische Datenbank-Verwaltung
- CSV-Export für Excel/Verwaltung

## 🔒 Sicherheit

### Master Secret schützen

Das Master Secret ist in `Services/LicenseKeyGenerator.cs` definiert:

```csharp
private const string MasterSecret = "MM_V01_MASTER_SECRET_2025_PRODUCTION";
```

**Wichtig:**
- Ändern Sie es NICHT nach Ausstellung von Lizenzen
- Halten Sie es sicher und geheim
- Dokumentieren Sie Backup-Kopien
- Verwenden Sie es in Produktionsumgebung

### Lizenzierungssicherheit

Das System verwendet:
- **HMAC-SHA256** Algorithmus
- **Hardware-gebundene Lizenzen** (Computer-spezifisch)
- **Ablaufdatum-Verifikation** (kryptographisch)
- **Manipulations-Erkennung** (lokale Datei mit Hash)

## 📊 Lizenzformate

### Lizenzschlüssel-Format
```
MM-XXXX-XXXX-XXXX-XXXX
├─ Präfix: MM (MaterialManager)
└─ 16 Zeichen: Base64-kodierter Hash
```

Beispiel: `MM-4A7F-9B2E-C5D1-8K3L`

### Hardware-ID-Format
```
Base64-String (24 Zeichen)
Basiert auf:
- Computername
- Prozessoranzahl
- Betriebssystem-Version
- Benutzername
```

Beispiel: `ABC123DEF456GHI789JKL012MNO345PQR`

## 🐍 Python-Skript Verwendung

### Installation
```bash
pip install python  # Standard in Windows
```

### Befehle
```bash
python license_manager.py
```

### Ausgaben
- Interaktives Menü
- `licenses_issued.json` - Datenbank mit allen Lizenzen
- `licenses_export.csv` - Export für Excel/Verwaltung

### Beispiel-Datenbank
```json
[
  {
    "id": 1,
    "hardware_id": "ABC123DEF456GHI789JKL012",
    "company_name": "Musterfirma GmbH",
    "license_key": "MM-4A7F-9B2E-C5D1-8K3L",
    "issued_date": "2025-01-15 14:30:00",
    "expiry_date": "2026-01-15",
    "years": 1,
    "notes": "Standardlizenz",
    "status": "active"
  }
]
```

## 🔄 Integrationsoptionen

### In Website einbinden
```php
// Lizenzschlüssel generieren via API
$cmd = "dotnet run '{$hardwareId}' '{$company}' 1";
exec($cmd, $output);
$licenseKey = trim($output[count($output)-1]);
```

### In Email-System integrieren
```bash
# Automatisch Lizenzen per Email versenden
python -c "from license_manager import LicenseManager; m = LicenseManager(); m.add_license(...)"
```

### In Backend-System einbinden
- Dateibasiert: `licenses_issued.json` auslesen
- Database: SQL-Import aus CSV
- API: Custom REST-Endpoint schreiben

## ⚠️ Häufige Fehler

### "HMAC-SHA256 Hash mismatch"
- **Ursache:** Unterschiedliche Master Secrets oder Hardware-IDs
- **Lösung:** Hardware-ID korrekt kopieren, Master Secret nicht ändern

### "Hardware-ID ungültig"
- **Ursache:** Falsche Hardware-ID eingegeben
- **Lösung:** Vom Kunden neu kopieren, Leerzeichen/Typos prüfen

### "Lizenzschlüssel funktioniert nicht"
- **Ursache:** Falsche Kombinationen oder Ablaufdatum überschritten
- **Lösung:** Neuen Schlüssel generieren

## 📞 Support

Bei Fragen oder Problemen:
1. Anleitung lesen: `LIZENZGENERATOR_ANLEITUNG.txt`
2. Skript-Logs überprüfen
3. Hardware-ID validieren
4. Master Secret überprüfen

## 📄 Rechtliche Hinweise

- Diese Tools gehören zu MaterialManager V01
- Das Lizenzierungssystem ist proprietär
- Geben Sie das Master Secret nicht an Dritte weiter
- Dokumentieren Sie alle ausgestellten Lizenzen für Compliance

---

**Version:** 1.0  
**Zuletzt aktualisiert:** Januar 2025  
**Autor:** MaterialManager Development Team
