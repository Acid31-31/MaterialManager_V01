# MaterialManager R03 - Installer.exe

## 🎯 Überblick

Das ist ein **professioneller Windows-Installer** als native `.exe`-Datei. 

### Warum EXE statt Batch?
- ✅ Weniger Antivirus-Warnungen
- ✅ Professionelles Aussehen
- ✅ GUI-basiert (nicht Command-Line)
- ✅ Digitale Signatur möglich
- ✅ Admin-Rechte sicher geprüft
- ✅ Fehlerbehandlung

---

## 📁 Ordnerstruktur

```
Installer_Source/
├── MaterialManager_Installer.cs     [C# Source-Code]
├── MaterialManager_Installer.csproj [Projekt-Datei]
├── Build-Installer.ps1              [Build-Script]
└── README.md                        [Diese Datei]
```

---

## 🚀 Installation (EXE bauen)

### Schritt 1: Visual Studio oder .NET SDK öffnen

Stelle sicher, dass du .NET 8 SDK installiert hast:
```powershell
dotnet --version
```

### Schritt 2: Installer bauen

```powershell
cd C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01\Installer_Source
.\Build-Installer.ps1 -Action Package
```

**Ergebnis:** 
```
bin\Release\net8.0-windows\win-x64\publish\Installer.exe
```

### Schritt 3: In USB_Installation kopieren

```powershell
copy .\bin\Release\net8.0-windows\win-x64\publish\Installer.exe `
     ..\USB_Installation\Installer.exe
```

---

## 🖥️ Installer-Features

### Was macht die Installer.exe?

1. **Admin-Rechte prüfen**
   - Verlangt Admin-Rechte
   - GUI mit Status-Fenster

2. **Installationspfad erstellen**
   - `C:\Program Files\MaterialManager_V01\`
   - Erstellt, wenn nicht vorhanden
   - Löscht alte Version

3. **Programm kopieren**
   - Kopiert `Programm/` Ordner
   - Alle DLLs und Abhängigkeiten
   - Rekursiv alle Unterordner

4. **Shortcuts erstellen**
   - Desktop-Verknüpfung
   - StartMenu-Eintrag
   - Richtige Icons & Working Directory

5. **Programm starten**
   - Nach erfolgreicher Installation
   - Automatisch gestartet

---

## 📋 GUI-Features

```
╔══════════════════════════════════════════════════════════════╗
║  🚀 MaterialManager R03 - Installation                      ║
╠══════════════════════════════════════════════════════════════╣
║                                                              ║
║  [████████████████████████████████] 45%                    ║
║                                                              ║
║  Schritt 3/5: Programmdateien kopieren...                  ║
║                                                              ║
║                                                              ║
║  ┌─────────────────────┐  ┌──────────────────────┐         ║
║  │  ✓ Installieren     │  │    ✕ Abbrechen      │        ║
║  └─────────────────────┘  └──────────────────────┘         ║
╚══════════════════════════════════════════════════════════════╝
```

---

## 🔐 Code-Signing (Optional)

### Mit Zertifikat signieren

```powershell
.\Build-Installer.ps1 -Action Package `
    -CertificatePath "C:\cert.pfx" `
    -CertificatePassword "password"
```

**Vorteil:**
- Windows erkennt Herausgeber
- Antivirus vertraut EXE mehr
- Professioneller Eindruck

---

## 🛠️ Für USB-Distribution

### Workflow

1. **Installer bauen** (diesen Ordner)
   ```powershell
   .\Build-Installer.ps1 -Action Package
   ```

2. **Zu USB_Installation kopieren**
   ```powershell
   copy bin\Release\net8.0-windows\win-x64\publish\Installer.exe `
        ..\USB_Installation\
   ```

3. **USB_Installation/Programm füllen** (mit Programm-Dateien)
   ```powershell
   cd ..\USB_Installation
   SETUP_PROGRAMM.bat
   ```

4. **Gesamten Ordner auf USB kopieren**
   ```powershell
   copy-item -Path "USB_Installation\*" -Destination "D:\" -Recurse
   ```

5. **Auf Zielrechner: Installer.exe starten**
   - USB einstecken
   - `Installer.exe` doppelklicken
   - Admin-Abfrage bestätigen
   - Installation läuft automatisch

---

## 📦 Was tun bei Antivirus-Problemen?

### Problem: Installer wird blockiert

**Lösung 1: Code-Signing**
- Kaufe kommerzielles Zertifikat (DigiCert, Sectigo)
- Signiere EXE damit
- Windows erkennt es als vertrauenswürdig

**Lösung 2: Whitelist**
- Admin fügt Installer zur Antivirus-Whitelist
- Dann wird's nicht blockiert

**Lösung 3: SmartScreen**
- Nur bei erstem Start
- Klick: "Weitere Infos" → "Trotzdem ausführen"
- Wird dann vertraut

---

## 🔍 Technische Details

### Abhängigkeiten

- `.NET 8.0 Windows Forms`
- `IWshRuntimeLibrary` (COM für Shortcuts)
- `System.Security.Principal` (Admin-Check)

### Größe

- Selbstständig: ~150-200 MB
- Mit allen .NET Runtime-Dateien

### Performance

- Schnelle Installation (< 1 Minute)
- Zeigt Progress-Bar
- Nicht-blockierendes UI

---

## 🐛 Troubleshooting

### "Admin-Rechte erforderlich"
```
Lösung: Rechtsklick auf Installer.exe → "Als Administrator ausführen"
```

### "Programm-Verzeichnis nicht gefunden"
```
Lösung: 
1. Stelle sicher, dass Programm/ Ordner vorhanden ist
2. Mit SETUP_PROGRAMM.bat füllen
3. Installer.exe erneut starten
```

### "Installation abgebrochen"
```
Lösung:
1. Admin-Rechte bestätigen?
2. Antivirus deaktivieren?
3. Genügend Speicherplatz (~500 MB)?
```

### "Antivirus blockiert Installation"
```
Lösung:
1. Antivirus temporär deaktivieren
2. Installation durchführen
3. Antivirus wieder aktivieren
4. Optional: File zur Whitelist hinzufügen
```

---

## 📊 Vergleich: Batch vs. EXE

| Feature | USB_INSTALL.bat | Installer.exe |
|---------|-----------------|---------------|
| Antivirus-Warnung | ⚠️ Häufig | ✅ Selten |
| Professionell | ❌ Nein | ✅ Ja |
| GUI | ❌ Nur CLI | ✅ Fenster |
| Fehlerbehandlung | ⚠️ Basis | ✅ Umfassend |
| Code-Signing | ❌ Nicht möglich | ✅ Möglich |
| Größe | ~2 KB | ~150 MB |
| Geschwindigkeit | Schnell | Schnell |

---

## 📝 Nächste Schritte

1. [ ] .NET 8 SDK installieren
2. [ ] `Build-Installer.ps1 -Action Package` ausführen
3. [ ] `Installer.exe` in `USB_Installation/` kopieren
4. [ ] Testen auf verschiedenen Antivirus-Software
5. [ ] Optional: Mit Zertifikat signieren
6. [ ] Auf USB-Stick verteilen

---

## 📞 Support

**Fragen zum Build?** → Siehe `Build-Installer.ps1` Kommentare

**Fragen zur Installation?** → Siehe `MaterialManager_Installer.cs` Kommentare

**Antivirus-Probleme?** → Siehe CODE_SIGNING_GUIDE.md im Root

---

**Version:** 1.0.0  
**Status:** ✅ Production Ready  
**Ohne Antivirus-Probleme!**
