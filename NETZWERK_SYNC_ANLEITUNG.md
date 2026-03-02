# MaterialManager R03 - Netzwerk-Synchronisation

## 🔄 AUTOMATISCHE SYNC zwischen mehreren PCs

Die App ist bereits für **Echtzeit-Synchronisation** vorbereitet!

---

## 📋 SETUP FÜR MULTI-PC-SYNCHRONISATION

### **METHODE 1: Netzlaufwerk (empfohlen für Firma)**

1. **Erstelle gemeinsamen Ordner auf Server:**
   ```
   \\IHR-SERVER\MaterialManager\
   ```

2. **Mappe Netzlaufwerk auf allen PCs:**
   - Windows Explorer öffnen
   - Rechtsklick auf "Dieser PC" → "Netzlaufwerk verbinden"
   - Laufwerk: `Z:`
   - Pfad: `\\IHR-SERVER\MaterialManager\`
   - ✅ "Verbindung bei Anmeldung wiederherstellen"

3. **Konfiguriere MaterialManager (EINMALIG):**
   - Erstelle Datei: `C:\ProgramData\MaterialManager_V01\netzwerk_config.json`
   ```json
   {
     "Aktiviert": true,
     "NetzwerkPfad": "Z:\\MaterialManager"
   }
   ```

4. **Fertig! Alle PCs synchronisieren automatisch!** ✅

---

### **METHODE 2: Gemeinsamer Ordner (für kleine Teams)**

1. **Erstelle Ordner auf PC-HAUPT:**
   ```
   C:\MaterialManager_Shared\
   ```

2. **Teile den Ordner im Netzwerk:**
   - Rechtsklick auf Ordner → "Eigenschaften" → "Freigabe"
   - Klicke "Erweiterte Freigabe"
   - ✅ "Diesen Ordner freigeben"
   - Berechtigungen: Alle = "Vollzugriff"

3. **Verbinde andere PCs:**
   - Öffne `\\PC-HAUPT\MaterialManager_Shared\` auf anderen PCs
   - Mappe als Laufwerk `Z:`

4. **Konfiguriere MaterialManager (wie oben)**

---

## ⚙️ WIE ES FUNKTIONIERT

### **Automatische Synchronisation:**
1. ✅ **PC-X:** User erstellt neues Material → Speichert in `materialbestand.xlsx`
2. ✅ **PC-Y:** FileWatcher erkennt Änderung → Lädt Daten automatisch neu
3. ✅ **PC-Z:** FileWatcher erkennt Änderung → Lädt Daten automatisch neu
4. ✅ **Alle PCs zeigen sofort das neue Material!**

### **Konflikt-Vermeidung:**
- ✅ **File-Locking:** Nur ein User kann gleichzeitig editieren
- ✅ **Auto-Reload:** Andere sehen Änderungen sofort nach Speichern
- ✅ **Backup:** Automatische Backups alle 5 Minuten

---

## 🧪 TESTEN

1. **Starte App auf PC-1**
2. **Erstelle neues Material** (z.B. "Test-Material")
3. **Starte App auf PC-2** (oder aktualisiere)
4. **✅ "Test-Material" erscheint automatisch auf PC-2!**

---

## ⚠️ WICHTIG

- **Alle PCs müssen auf DIE GLEICHE Datei zugreifen!**
- **Netzwerk muss stabil sein** (LAN empfohlen, kein WLAN)
- **Backups werden automatisch erstellt** in `Z:\MaterialManager\Backups\`

---

## 🆘 TROUBLESHOOTING

**Problem: "Datei wird von anderem Prozess verwendet"**
- Lösung: Warte 5 Sekunden, App versucht erneut

**Problem: Änderungen erscheinen nicht automatisch**
- Prüfe: Alle PCs verwenden gleichen Pfad?
- Prüfe: FileWatcher aktiv? (ist standardmäßig aktiv)
- Manuell aktualisieren: `Datei → Export → Import`

**Problem: Keine Verbindung zum Server**
- Prüfe: Netzlaufwerk erreichbar?
- Teste: Öffne `\\IHR-SERVER\MaterialManager\` im Explorer

---

✅ **Alles bereits implementiert und aktiv!**
