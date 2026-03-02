# USB_Installation - Kompletter Überblick

## 📁 Ordnerstruktur

```
USB_Installation/
│
├─ 📄 INDEX.txt                 ← LESEN: Übersicht der Struktur
├─ 📄 USB_INSTALL.bat           ← Installation auf Zielrechner
├─ 📄 USB_README.txt            ← Für Kunden
├─ 📄 SETUP_PROGRAMM.bat        ← Programm-Dateien kopieren
│
├─ 📁 Programm/                 ← Hauptprogramm
│   ├─ MaterialManager_V01.exe
│   └─ *.dll (Abhängigkeiten)
│
├─ 📁 Tools/                    ← Verwaltungstools
│   └─ GENERATE_LICENSE.bat
│
├─ 📁 Lizenzen/                 ← Kundenlizenzen dokumentieren
│   └─ (beispiel: Kunde_XYZ.txt)
│
└─ 📁 Anleitung/                ← Dokumentation
    ├─ QUICK_START.md
    └─ DEPLOYMENT_ANLEITUNG.md
```

---

## 🚀 Schritt für Schritt

### Schritt 1: Programm-Dateien einfügen

```powershell
# PowerShell öffnen, zum Projekt-Verzeichnis wechseln
cd C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01

# USB-Package erstellen
.\Build-USBVersion.ps1 -Action Package

# USB_Installation/Programm mit Dateien füllen
copy USB_Distribution\* USB_Installation\Programm\ /Y /S
```

**ODER einfacher: Das Batch-Skript verwenden:**
```
cd USB_Installation
SETUP_PROGRAMM.bat
```

### Schritt 2: Auf USB-Stick kopieren

```powershell
# USB-Stick einstecken (z.B. D:)
copy-item -Path "USB_Installation\*" -Destination "D:\" -Recurse -Force
```

### Schritt 3: Auf Zielrechner Installation

- USB-Stick einstecken
- `USB_INSTALL.bat` **Rechtsklick** → **"Als Administrator ausführen"**
- Installation läuft automatisch

### Schritt 4: Demo-Lizenz testen

- Programm öffnet sich automatisch
- Menü: **Hilfe → Lizenzinformationen**
- Demo sollte 30 Tage laufen

### Schritt 5: Lizenzschlüssel später

- Nach 30 Tagen: Kunde fordert Lizenz an
- Hardware-ID kopieren (siehe Schritt 4)
- Ausführen: `USB_Installation/Tools/GENERATE_LICENSE.bat`
- Lizenzschlüssel an Kunde senden

---

## 📋 Datei-Details

### USB_INSTALL.bat
- **Zweck**: Installation auf Zielrechner
- **Was macht es**:
  - Prüft Admin-Rechte
  - Erstellt `C:\Program Files\MaterialManager_V01\`
  - Kopiert Programm + DLLs
  - Desktop-Shortcut
  - StartMenu-Eintrag
- **Startet auf**: Zielrechner (von USB)

### SETUP_PROGRAMM.bat
- **Zweck**: Programm-Dateien vorbereiten (auf deinem Rechner!)
- **Was macht es**:
  - Ruft `Build-USBVersion.ps1 -Action Package` auf
  - Kopiert Dateien in `Programm/` Ordner
  - Macht alles bereit für USB-Stick
- **Startet auf**: Dein Entwickler-Rechner

### GENERATE_LICENSE.bat
- **Zweck**: Lizenzschlüssel generieren
- **Input**: Hardware-ID + Kundenname
- **Output**: Lizenzschlüssel (MM-XXXX-XXXX-XXXX-XXXX)
- **Startet auf**: Dein Rechner (bei Lizenzanfrage)

---

## ⚠️ Wichtig!

### Vor USB-Versand checken:

- [ ] `SETUP_PROGRAMM.bat` oder `Build-USBVersion.ps1` ausgeführt?
- [ ] `Programm/` Ordner mit .exe + DLLs gefüllt?
- [ ] Alle Dateien kopiert?
- [ ] USB-Stick formatiert?
- [ ] Gesamter `USB_Installation` Ordner auf USB kopiert?
- [ ] `USB_INSTALL.bat` getestet (optional auf Test-PC)?

### Nach USB-Empfang checken (Kunde):

- [ ] USB einstecken
- [ ] `USB_INSTALL.bat` ausgeführt (als Admin)
- [ ] Programm öffnet sich
- [ ] Menü: Hilfe → Lizenzinformationen (Hardware-ID sichtbar)
- [ ] Demo läuft (sollte 30 Tage zeigen)

---

## 🔐 Lizenzierungs-Ablauf

```
Tag 1:
├─ USB empfangen
├─ USB_INSTALL.bat ausführen
├─ Programm startet
└─ Demo: 30 Tage automatisch

Tag 30:
├─ Programm fragt nach Lizenz
├─ Kunde sendet Hardware-ID
└─ Du generierst Lizenzschlüssel

Aktivierung:
├─ Lizenzschlüssel erhalten (MM-XXXX-XXXX-XXXX-XXXX)
├─ Programm: Menü → Lizenzierung → Aktivieren
├─ Lizenzschlüssel eingeben
└─ ✅ Vollversion läuft unbegrenzt
```

---

## 📚 Welche Datei öffne ich wann?

| Situation | Datei | Was ich dort mache |
|-----------|-------|-------------------|
| Übersicht der Struktur | `INDEX.txt` | Lese Struktur & Checkliste |
| Schnellstart | `Anleitung/QUICK_START.md` | 5 Schritte verstehen |
| Detaillierte Anleitung | `Anleitung/DEPLOYMENT_ANLEITUNG.md` | Schritt-für-Schritt mit Bildern |
| Programm vorbereiten | `SETUP_PROGRAMM.bat` | Doppelklick zum Füllen |
| Installation testen | `USB_INSTALL.bat` | Auf Zielrechner ausführen |
| Lizenzschlüssel generieren | `Tools/GENERATE_LICENSE.bat` | Bei Kundenanfrage ausführen |
| Dokumentation für Kunde | `USB_README.txt` | Zum Weitergeben |
| Kundenlizenzen notieren | `Lizenzen/` | Neue Datei erstellen |

---

## 🆘 Troubleshooting

**Problem: SETUP_PROGRAMM.bat funktioniert nicht**
```
Lösung:
1. Prüfe: Existiert USB_Distribution Ordner?
2. Führe aus: .\Build-USBVersion.ps1 -Action Package
3. Versuche nochmal: SETUP_PROGRAMM.bat
```

**Problem: USB_INSTALL.bat auf Zielrechner schlägt fehl**
```
Lösung:
1. Rechtsklick auf USB_INSTALL.bat
2. "Als Administrator ausführen" wählen
3. Alle Dateien komplett auf USB kopiert?
```

**Problem: Programm startet nicht nach Installation**
```
Lösung:
1. Programm manuelle starten: C:\Program Files\MaterialManager_V01\MaterialManager_V01.exe
2. Event Log checken
3. ggf. Windows .NET 8 Runtime installieren (ist aber self-contained)
```

---

## 🎯 Best Practices

✅ **DO:**
- USB mit allen Dateien testen vor Versand
- Backup des USB_Installation Ordners machen
- Kundendaten in `Lizenzen/` dokumentieren
- Lizenzschlüssel notieren (wann, für wen, welcher)

❌ **DON'T:**
- Programm-Dateien manuell kopieren (→ SETUP_PROGRAMM.bat nutzen)
- USB ohne Test zum Kunden senden
- Lizenzdaten nicht dokumentieren

---

## 📞 Support

**Fragen zur Installation?** → Siehe `Anleitung/DEPLOYMENT_ANLEITUNG.md`

**Fragen zur Lizenzierung?** → Siehe `Anleitung/QUICK_START.md`

**Technische Details?** → Siehe Projekt-Root: `CODE_SIGNING_GUIDE.md`

---

**MaterialManager R03 - USB Edition v1.0.0**  
**Status: ✅ Ready to Ship**
