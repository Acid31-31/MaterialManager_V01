# 📦 MaterialManager R03 - USB Deployment Schritt-für-Schritt

## Phase 1: Vorbereitung (einmalig)

### 1.1 Zertifikat erstellen (optional, für Antivirus)

```powershell
# PowerShell als Admin öffnen

# Selbstsigniertes Zertifikat erstellen
$cert = New-SelfSignedCertificate -CertStoreLocation "cert:\CurrentUser\My" `
    -Subject "CN=MaterialManager_Developer" `
    -FriendlyName "MaterialManager Code Signing Cert" `
    -Type CodeSigningCert `
    -NotAfter (Get-Date).AddYears(5)

# Exportieren als .pfx
$password = ConvertTo-SecureString -String "SuperSecurePassword123!" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "C:\Temp\MaterialManager_Cert.pfx" -Password $password

# Zertifikat-Pfad notieren für später:
# C:\Temp\MaterialManager_Cert.pfx
# Passwort: SuperSecurePassword123!
```

### 1.2 Windows SDK installieren (für Code-Signing)

Falls noch nicht installiert:
- Download: https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/
- Installieren und SignTool.exe bestätigen

## Phase 2: Build & Packaging

### 2.1 Terminal in Projektverzeichnis öffnen

```powershell
cd C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01

# PowerShell ExecutionPolicy aktivieren
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### 2.2 Vollständiges Package erstellen

**OHNE Code-Signing:**
```powershell
.\Build-USBVersion.ps1 -Action Package
```

**MIT Code-Signing:**
```powershell
.\Build-USBVersion.ps1 -Action Package `
    -CertificatePath "C:\Temp\MaterialManager_Cert.pfx" `
    -CertificatePassword "SuperSecurePassword123!"
```

→ **Output:** `USB_Distribution` Ordner wird erstellt

### 2.3 Größe prüfen

```powershell
# Größe der USB-Distribution prüfen
[Math]::Round((Get-ChildItem USB_Distribution -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB, 2)
# → sollte ca. 200-400 MB sein
```

## Phase 3: USB-Stick vorbereiten

### 3.1 USB-Stick formatieren

```powershell
# USB-Stick einstecken
# In Explorer Rechtsklick auf USB → Formatieren

# ODER PowerShell:
Format-Volume -DriveLetter D -FileSystem NTFS -NewFileSystemLabel "MaterialManager"
```

### 3.2 Dateien auf USB kopieren

```powershell
# Alle Dateien aus USB_Distribution auf USB-Stick kopieren
Copy-Item -Path "USB_Distribution\*" -Destination "D:\" -Recurse -Force

# Prüfen:
Get-ChildItem "D:\MaterialManager_V01.exe"
```

## Phase 4: Installation auf Zielrechner testen

### 4.1 USB-Stick einstecken

### 4.2 Installationsskript ausführen

```powershell
# Explorer öffnen, zu USB navigieren
# USB_INSTALL.bat RECHTSKLICK → "Als Administrator ausführen"

# ODER PowerShell als Admin:
& "D:\USB_INSTALL.bat"
```

### 4.3 Programm starten

- Desktop-Verknüpfung doppelklicken
- ODER StartMenu durchsuchen: "MaterialManager"

### 4.4 Demo-Lizenz testen

```
- Programm öffnet
- Menü: Hilfe → Lizenzinformationen
- Hardware-ID ist sichtbar
- Demo läuft 30 Tage
```

## Phase 5: Lizenzschlüssel ausstellen

### 5.1 Hardware-ID vom Kunden erhalten

Kunde führt folgendes aus:
```
1. MaterialManager öffnen
2. Menü: Hilfe → Lizenzinformationen
3. Hardware-ID kopieren (z.B. "ABC123XYZ...") 
4. Via Email senden
```

### 5.2 Lizenzschlüssel generieren

```batch
REM GENERATE_LICENSE.bat ausführen (auf deinem Rechner)

REM Eingaben:
Hardware-ID: ABC123XYZ...
Kundenname: Acme Corporation

REM Output:
Lizenzschlüssel: MM-A1B2-C3D4-E5F6-G7H8
```

### 5.3 Lizenzschlüssel an Kunden senden

```
Betreff: Lizenzschlüssel MaterialManager R03

Hallo,

anbei finden Sie Ihren Lizenzschlüssel zur Aktivierung der Vollversion:

MM-A1B2-C3D4-E5F6-G7H8

Aktivierung:
1. MaterialManager R03 öffnen
2. Menü: Lizenzierung → Lizenzschlüssel aktivieren
3. Den obigen Schlüssel eingeben
4. Speichern

Der Schlüssel ist an die Hardware-ID Ihres Rechners gebunden.

Viele Grüße,
Support Team
```

## Phase 6: Troubleshooting

### Problem: Windows Defender/Antivirus blockiert Installation

**Lösung:**
- ✅ **Mit Code-Signing:** Zertifikat sollte vertrauenswürdig sein
- ⚠️ **Ohne Signing:** Nutzer klickt "Trotzdem ausführen"

```powershell
# Zertifikat im vertrauenswürdigen Speicher installieren:
Import-PfxCertificate -FilePath "C:\Temp\MaterialManager_Cert.pfx" `
    -CertStoreLocation "Cert:\CurrentUser\Root" `
    -Password (ConvertTo-SecureString -String "password" -Force -AsPlainText)
```

### Problem: SmartScreen warnt vor "unbekanntem Herausgeber"

- Normal bei selbstsigniertem Zertifikat
- Nutzer kann "Weitere Informationen → Trotzdem ausführen" klicken
- Mit kauftem EV-Zertifikat würde dies nicht erscheinen

### Problem: Installation fehlgeschlagen

```powershell
# Logs prüfen:
Get-EventLog -LogName Application -Newest 10 -Source "*MaterialManager*"

# Oder in Event Viewer:
# Applications and Services Logs → MaterialManager
```

### Problem: Lizenzschlüssel wird nicht akzeptiert

```powershell
# Lokale Lizenzinfo löschen:
Remove-Item "$env:LOCALAPPDATA\MaterialManager_V01\.license" -Force

# Programm neustarten
# → Demo-Lizenz sollte wieder da sein
# → Mit neuem Lizenzschlüssel versuchen
```

## 🎯 Checkliste vor Kundenversand

- [ ] USB-Distribution lokal erfolgreich getestet
- [ ] Auf USB kopiert und getestet
- [ ] Antivirus erkennt es nicht als Malware
- [ ] INSTALL.bat funktioniert
- [ ] Programm startet und zeigt Demo-Lizenz
- [ ] Menü "Lizenzinformationen" zeigt Hardware-ID
- [ ] USB_README.txt ist lesbar
- [ ] GENERATE_LICENSE.bat Test: Lizenzschlüssel kann generiert werden

## 📞 Für Kunden: Support-Seite

```
Häufige Fragen:

F: Wie lange läuft die Demo?
A: 30 Tage ab erstem Start

F: Was ist die Hardware-ID?
A: Eindeutige ID deines Rechners
   Menü: Hilfe → Lizenzinformationen

F: Wie aktiviere ich die Vollversion?
A: Lizenzschlüssel erhalten → 
   Menü: Lizenzierung → Aktivieren

F: Kann ich die Lizenz auf einen anderen Rechner übertragen?
A: Nein, Hardware-ID ist gebunden. Neue Lizenz erforderlich.

F: Was wenn ich Windows neu installiere?
A: Hardware-ID bleibt gleich (CPU/Disk ID). Lizenz bleibt gültig.
```

---

**Benötigte Dateien:**
- ✅ Build-USBVersion.ps1
- ✅ USB_INSTALL.bat
- ✅ GENERATE_LICENSE.bat
- ✅ USB_README.txt
- ✅ CODE_SIGNING_GUIDE.md (dieses Dokument)

**Viel Erfolg! 🚀**
