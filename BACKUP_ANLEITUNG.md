# 🔄 MaterialManager R03 - Backup-System

## 📋 ÜBERBLICK

Das Backup-System schützt dich vor Datenverlust während der Programmierung!

**Merkmale:**
- ✅ **Automatische Versionierung** (10 Versionen)
- ✅ **Automatische Rotation** (alte Version wird durch neue ersetzt)
- ✅ **Zeitstempel** (sehe wann jede Version erstellt wurde)
- ✅ **Einfache Bedienung** (Doppelklick)

---

## 🚀 VERWENDUNG

### **OPTION 1: Schnell-Backup (Einfach)**

```
Doppelklick: RunBackup.bat
     ↓
Backup wird erstellt
     ↓
Fertig! ✅
```

### **OPTION 2: PowerShell (Manuell)**

```powershell
.\CreateAutoBackup.ps1
```

---

## 📂 ORDNERSTRUKTUR

Nach dem ersten Backup:

```
Backups/
├── Version_001/  [2024-01-15 14:30:00]
│   ├── Views/
│   ├── Services/
│   ├── Models/
│   ├── Converters/
│   ├── MainWindow.xaml.cs
│   ├── App.xaml.cs
│   ├── MaterialManager_V01.csproj
│   └── BACKUP_INFO.txt
├── Version_002/  [2024-01-15 15:45:00]
├── Version_003/  [2024-01-15 16:20:00]
└── ...
```

---

## 🔄 WIE ES FUNKTIONIERT

### **Bei weniger als 10 Versionen:**
```
Version_001 → Version_002 → Version_003 → ...
```

### **Bei 10 Versionen (VOLL):**
```
ROTATION aktiviert!
Version_001 (ALT) wird gelöscht
Neue Version_001 (NEU) wird erstellt mit aktuellem Code
Die anderen rücken nach vorne:
Version_002, Version_003, ..., Version_010
```

---

## 📝 BEISPIEL-WORKFLOW

### **15:00 Uhr - Erste Änderung**
```
→ RunBackup.bat (Doppelklick)
→ Version_001 erstellt
→ Code gespeichert ✅
```

### **16:00 Uhr - Weitere Änderung**
```
→ RunBackup.bat (Doppelklick)
→ Version_002 erstellt
→ Code gespeichert ✅
```

### **Oops! Code ist kaputt 💥**
```
→ Gehe zu: Backups/Version_001/
→ Kopiere alle Dateien zurück
→ Projekt lädt wieder ✅
```

---

## ✅ BEST PRACTICE

### **Nach jeder Programmänderung:**

1. **Test:** Starte das Programm
2. **OK?** → **RunBackup.bat** ausführen
3. **FEHLER?** → **Aus Backups/Version_XYZ/ wiederherstellen**

### **Zeitersparnis:**
- Ohne Backup: 30-60 Min manuell Fehler finden/fixen
- Mit Backup: 5 Sec Rollback + Erneut programmieren ✨

---

## 🎯 WICHTIG

### **Das Backup speichert:**
- ✅ `Views/` (alle Dialoge)
- ✅ `Services/` (alle Business-Logik)
- ✅ `Models/` (Datenmodelle)
- ✅ `Converters/` (WPF-Converter)
- ✅ `MainWindow.xaml.cs` (Hauptfenster)
- ✅ `App.xaml.cs` (App-Konfiguration)
- ✅ `.csproj` (Projekt-Einstellungen)

### **Das Backup speichert NICHT:**
- ❌ `bin/` und `obj/` (Compiled Output)
- ❌ `.vs/` (Visual Studio Cache)
- ❌ Datenbank-Dateien
- ❌ Große Dependencies

---

## 📊 VERSIONENLISTE ANZEIGEN

**Aktuell erstellte Versionen:**
```
Das Skript zeigt dir alle verfügbaren Versionen mit Datum/Uhrzeit an!
```

---

## 💡 TIPPS

### **Automatisches Backup nach jedem Build:**

Falls gewünscht, kann ich `RunBackup.bat` ins Build-System integrieren:
```
Dann läuft nach jedem Build automatisch ein Backup!
```

### **Alte Versionen ansehen:**

```
Windows Explorer → Backups/ → Version_001/ → Rechtsklick → "Öffnen"
```

### **Mehrere Backup-Punkte pro Tag:**

Einfach mehrmals hintereinander ausführen:
```
14:00 → Backup 1
15:30 → Backup 2
17:00 → Backup 3
```

---

## 🆘 NOTFALL-RECOVERY

### **Program ist kaputt - was tun?**

1. **Öffne:** `Backups/Version_001/` (älteste stabile Version)
2. **Kopiere:** Alle Dateien von dort
3. **Einfügen:** In `MaterialManager_V01/` zurück
4. **Visual Studio:** Projekt neu laden (`STRG+SHIFT+B`)
5. **Test:** Läuft wieder? ✅

---

## ✨ VOLLER WORKFLOW (EMPFOHLEN)

```
JEDEN TAG:

1. Programmieren
   ↓
2. Testen (läuft es?)
   ↓
   JA → RunBackup.bat
   NEIN → git revert oder aus Backups wiederherstellen
   ↓
3. Weiter programmieren
```

---

**🎉 JETZT BIS DU GESCHÜTZT VOR DATENVERLUST! 🎉**

Viel Erfolg beim Programmieren! 💪✨
