# 🚀 MaterialManager R03 - MASTER SETUP (Visuelle Anleitung)

## Was ist MASTER_SETUP?

Ein automatisierter Script mit **Admin-Rechten**, der in wenigen Minuten alles erstellt:

```
┌─────────────────────────────────┐
│  MASTER_SETUP (Admin-Rechte)    │
├─────────────────────────────────┤
│                                 │
│  [1] Installer.exe bauen        │
│      └─ dotnet build            │
│      └─ dotnet publish          │
│      └─ Rename → Installer.exe  │
│                                 │
│  [2] USB-Paket vorbereiten      │
│      └─ Kopiere Installer.exe   │
│      └─ Struktur prüfen         │
│                                 │
│  [3] Backup erstellen           │
│      └─ ZIP-Datei generieren    │
│                                 │
│  [4] Zusammenfassung            │
│      └─ Status anzeigen         │
│                                 │
└─────────────────────────────────┘
        ↓        ↓       ↓
    Ready     USB    Backup
    .exe      Pkg    Created
```

---

## 🎯 So startest du es:

### Methode 1: PowerShell (empfohlen)

```powershell
# 1. PowerShell öffnen
# 2. Rechtsklick → "Als Administrator ausführen"
# 3. Eingeben:

cd C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01
.\MASTER_SETUP.ps1 -Action Full
```

### Methode 2: Batch-Datei (einfach)

```
1. Explorer öffnen
2. Zu C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01 navigieren
3. Rechtsklick auf MASTER_SETUP.bat
4. "Als Administrator ausführen"
```

---

## 📊 Schritt-für-Schritt Ablauf:

```
START
  │
  ├─→ [1] Admin-Check
  │   └─ ✓ Admin? → Weiter
  │   └─ ✗ Admin? → ERROR, Exit
  │
  ├─→ [2] .NET SDK Check
  │   └─ ✓ .NET 8? → Weiter
  │   └─ ✗ .NET 8? → ERROR, Exit
  │
  ├─→ [3] Alte Build-Dateien löschen
  │   └─ bin/ und obj/ Ordner gelöscht
  │
  ├─→ [4] BUILD PHASE
  │   ├─ dotnet build -c Release
  │   │  └─ Kompiliert C# Source-Code
  │   │     ├─ MaterialManager_Installer.cs
  │   │     └─ Mit Windows-Forms
  │   │
  │   └─ dotnet publish --self-contained
  │      └─ Erstellt standalone .exe
  │         ├─ Mit .NET 8 Runtime
  │         ├─ ~150-200 MB
  │         └─ Läuft überall
  │
  ├─→ [5] INSTALLER.EXE UMBENENNEN
  │   └─ MaterialManager_Installer.exe → Installer.exe
  │
  ├─→ [6] USB-PAKET VORBEREITEN
  │   └─ Kopiere Installer.exe nach USB_Installation/
  │
  ├─→ [7] BACKUP ERSTELLEN
  │   └─ ZIP mit gesamtem Projekt
  │
  ├─→ [8] ZUSAMMENFASSUNG
  │   ├─ Dateien anzeigen
  │   ├─ Größen anzeigen
  │   └─ Nächste Schritte
  │
END (Status: READY FOR DISTRIBUTION)
```

---

## ✅ Was danach passiert:

### Terminal zeigt:

```
╔════════════════════════════════════════════════════════════════╗
║  SETUP ABGESCHLOSSEN ✅                                        ║
╚════════════════════════════════════════════════════════════════╝

📦 ERSTELLTE DATEIEN:

  ✓ Installer.exe (175.42 MB)
    Location: C:\Users\...\USB_Installation\Installer.exe

📁 USB_INSTALLATION STRUKTUR:

  📁 Programm/
  📁 Tools/
  📁 Lizenzen/
  📁 Anleitung/
  📄 Installer.exe (175.42 MB)
  📄 USB_INSTALL.bat (2.5 KB)
  📄 USB_README.txt (5.3 KB)
  📄 SETUP_PROGRAMM.bat (1.2 KB)
  📄 INDEX.txt (4.1 KB)
  📄 README.md (3.8 KB)

🎯 NÄCHSTE SCHRITTE:

1️⃣  Programm-Dateien einfügen:
   cd USB_Installation
   .\SETUP_PROGRAMM.bat

2️⃣  Auf USB-Stick kopieren:
   copy-item -Path '...' -Destination 'D:\' -Recurse

3️⃣  Auf Zielrechner:
   USB einstecken → Installer.exe doppelklicken

✅ Installation läuft automatisch!
```

---

## ⏱️ Dauer:

| Punkt | Dauer | Was passiert |
|-------|-------|--------------|
| Admin-Check | 1 Sec | Berechtigungen validieren |
| .NET Check | 1 Sec | SDK-Version prüfen |
| Clean Build | 10 Sec | Alte Dateien löschen |
| Build | 30-60 Sec | C# Code kompilieren |
| Publish | 30-90 Sec | Standalone EXE erstellen |
| Rename/Copy | 10 Sec | Dateien verschieben |
| Backup | 20-60 Sec | ZIP-Datei erstellen |
| Zusammenfassung | 5 Sec | Status anzeigen |
| **TOTAL** | **2-5 Min** | **Alles fertig!** |

*Erste Mal etwas länger (Downloads), danach schneller (Cache)*

---

## 🔒 Admin-Rechte Erklärung

Warum Admin-Rechte nötig?

- ✓ Build-Dateien im `obj/` und `bin/` Ordner (können geschützt sein)
- ✓ Publish zu `Program Files` Ordner (User-Rechte reichen meist nicht)
- ✓ Backup-Erstellung (einige Dateien könnten geschützt sein)

**Sicherheit:** Skript macht NUR:
- ✓ Kompilieren des eigenen Codes
- ✓ Kopieren/Verschieben von Dateien
- ✓ ZIP-Backup erstellen

**Macht NICHT:**
- ✗ Registry ändern
- ✗ System-Dateien ändern
- ✗ Andere Programme installieren
- ✗ Netzwerk-Zugriff

---

## 📋 Fehlerbehandlung

Falls Fehler auftritt:

```
❌ FEHLER: ".NET 8 SDK nicht gefunden!"

Lösung:
1. Terminal schließen
2. .NET 8 SDK von https://dotnet.microsoft.com/download/dotnet/8.0 laden
3. Installieren
4. Terminal neustarten
5. Erneut versuchen
```

---

## 🎯 Das Ergebnis:

Nach MASTER_SETUP:

```
USB_Installation/
│
├─ 📦 Installer.exe (175 MB)  ← FERTIG ZUM VERTEILEN!
│  └─ Professioneller Installer
│  └─ Mit GUI
│  └─ Admin-Check
│  └─ Fehlerbehandlung
│
├─ 📄 USB_INSTALL.bat (Backup - für Notfälle)
│
├─ 📁 Programm/
│  └─ (Wird mit SETUP_PROGRAMM.bat gefüllt)
│
├─ 📁 Tools/
│  └─ GENERATE_LICENSE.bat
│
└─ 📁 Anleitung/
   └─ Dokumentation
```

---

## 💡 Insider-Tipps:

### Tipp 1: Mehrfaches Build
```powershell
# Wenn du mehrmals builden musst
.\MASTER_SETUP.ps1 -Action InstallerOnly
# Baut nur Installer.exe, nicht USB + Backup
```

### Tipp 2: Clean Build bei Problemen
```powershell
# Löscht alles und baut neu
.\MASTER_SETUP.ps1 -Action CleanBuild
```

### Tipp 3: Nur USB-Vorbereitung
```powershell
# Wenn Installer.exe schon vorhanden
.\MASTER_SETUP.ps1 -Action USBOnly
```

---

## 🚀 Schnellversion:

```powershell
# Öffne PowerShell als Admin, dann:
cd C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01
.\MASTER_SETUP.ps1 -Action Full

# Fertig! In 2-5 Minuten ist alles bereit.
```

---

**Status:** ✅ Production Ready  
**Version:** 1.0.0  
**MaterialManager R03 - Installer Edition**
