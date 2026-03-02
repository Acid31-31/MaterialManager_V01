# MaterialManager R03 - USB Installation & Lizenzierung

## 🚀 Schnellstart

### Voraussetzungen
- Windows 10 oder höher
- Administrator-Rechte während Installation
- ca. 500 MB Speicherplatz
- Internet (optional, für Online-Features)

### Installation auf Zielrechner

1. **USB-Stick in Zielrechner einstecken**
2. **INSTALL.bat mit Rechtsklick öffnen**
3. **"Als Administrator ausführen" wählen**
4. Installation folgt automatisch
5. Nach Abschluss: Programm starten

## 📝 Lizenzierung

### Demo-Version (30 Tage)
- Automatisch aktiviert nach Installation
- Funktioniert sofort ohne Lizenzschlüssel
- Zähler läuft nach erstem Start

### Vollversion mit Lizenzschlüssel

#### Schritt 1: Hardware-ID abrufen
- Programm starten
- Menü: **Hilfe → Lizenzinformationen**
- Hardware-ID kopieren

#### Schritt 2: Lizenzschlüssel anfordern
- Hardware-ID an Administrator senden
- Erhalten Sie einen Lizenzschlüssel (Format: MM-XXXX-XXXX-XXXX-XXXX)

#### Schritt 3: Lizenz aktivieren
- Programm starten
- Menü: **Lizenzierung → Lizenzschlüssel aktivieren**
- Lizenzschlüssel eingeben
- Bestätigen

## 🔐 Digitale Zertifikate

Das Programm ist digital signiert:

```
Herausgeber: MaterialManager
Gültig von: 2025-01-01 bis 2026-01-01
Algorithmus: SHA256
```

**Wenn Windows "Unbekannter Herausgeber" warnt:**
1. Das ist normal bei selbstsignierten Zertifikaten
2. Die Anwendung ist sicher zu verwenden
3. Sie können Warnung ignorieren und "Trotzdem ausführen" klicken

## 📦 Dateienstruktur

```
USB_STICK/
├── MaterialManager_R03.exe         [Hauptprogramm, digital signiert]
├── *.dll                            [Abhängigkeiten]
├── INSTALL.bat                      [Installationsskript]
├── USB_README.txt                   [Diese Datei]
└── LICENSE.txt                      [Lizenzbestimmungen]
```

## 🛠️ Support

### Fehler während Installation?

**"Zugriff verweigert"**
- Administrator-Rechte erforderlich
- Rechtsklick → "Als Administrator ausführen"

**"Datei nicht gefunden"**
- USB-Stick vollständig kopiert?
- Alle .dll-Dateien vorhanden?
- Pfad ohne Sonderzeichen?

**Lizenzprobleme**
- Starten Sie: `Hilfe → Lizenz zurücksetzen`
- Starten Sie Programm neu
- Demo-Version sollte wieder verfügbar sein

### Kontakt
- Email: support@materialmanager.de
- Telefon: +49 (0) 123 456789

## ✅ Checkliste vor Versand

- [ ] Program als Vollversion mit Lizenzierung
- [ ] Digital signiert (SHA256 Code-Signing)
- [ ] INSTALL.bat Test-Lauf erfolgreich
- [ ] USB-Stick geprüft
- [ ] README.txt dabei
- [ ] Lizenzschlüssel-Anleitung beigelegt

## 📄 Lizenzbestimmungen

Die Lizenz ist nicht übertragbar und gebunden an die Hardware-ID des Zielrechners.

---
MaterialManager R03 © 2025 - Version 1.0.0
