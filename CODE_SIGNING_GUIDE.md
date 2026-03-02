# Code-Signing für MaterialManager R03

## 📋 Übersicht

Dieses Dokument beschreibt, wie du das Programm digital mit einem Code-Signing-Zertifikat signierst, damit Windows und Antivirus-Software es als vertrauenswürdig erkennen.

## 🔐 Was ist Code-Signing?

- **Digitale Signatur** auf der .exe-Datei
- **Authentizität**: Beweist, dass du der Autor bist
- **Integrität**: Datei wurde nicht verändert
- **SmartScreen-Warnungen reduzieren**

## 📦 Optionen zum Signieren

### Option 1: Selbstsigniertes Zertifikat (kostenlos, lokal)

Gut für: Interne Verteilung, Tests, kleine Unternehmen

```powershell
# 1. Zertifikat erstellen (einmalig)
$cert = New-SelfSignedCertificate -CertStoreLocation "cert:\CurrentUser\My" `
    -Subject "CN=MaterialManager" `
    -FriendlyName "MaterialManager Code Signing" `
    -Type CodeSigningCert `
    -NotAfter (Get-Date).AddYears(5)

# 2. Zertifikat exportieren
$password = Read-Host -Prompt "Zertifikat-Passwort eingeben" -AsSecureString
Export-PfxCertificate -Cert $cert -FilePath "MaterialManager_CodeSign.pfx" -Password $password

# 3. .exe signieren
$signToolPath = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe"
& $signToolPath sign /f "MaterialManager_CodeSign.pfx" /p "YourPassword" /t "http://timestamp.comodoca.com/authenticode" "C:\Path\To\MaterialManager_V01.exe"
```

### Option 2: Externes Zertifikat (empfohlen für Produktion)

Gut für: Kommerzielle Verteilung, Vertrauen von Dritten

**Anbieter:**
- DigiCert: https://www.digicert.com/code-signing
- Sectigo: https://sectigo.com/code-signing
- GlobalSign: https://www.globalsign.com/

**Kosten:** ca. 100-500 EUR/Jahr

**Vorteil:**
- Windows erkennt Zertifikat automatisch als vertrauenswürdig
- Keine SmartScreen-Warnungen
- Professioneller Eindruck

## 🔨 Vollautomatisches Signing mit Build-Script

```powershell
# Publish + Sign in einem Schritt:
.\Build-USBVersion.ps1 -Action Package `
    -CertificatePath "MaterialManager_CodeSign.pfx" `
    -CertificatePassword "YourSecurePassword"
```

## ✅ Verifizierung der Signatur

```powershell
# Signatur prüfen
$file = "C:\Path\To\MaterialManager_V01.exe"
Get-AuthenticodeSignature $file | Format-List *

# Sollte zeigen:
# Status: Valid
# SignerCertificate: CN=MaterialManager
```

## 📝 Lizenz-Dateien Struktur

Die Lizenzierung funktioniert parallel zum Code-Signing:

```
USB_Stick/
├── MaterialManager_V01.exe    ← Digital signiert
├── *.dll                       ← Digital signiert
├── INSTALL.bat                 ← Installation
├── GENERATE_LICENSE.bat        ← Lizenzgenerierung
└── USB_README.txt             ← Dokumentation
```

## 🚀 Kompletter Prozess für USB-Distribution

```bash
# 1. Build erstellen
.\Build-USBVersion.ps1 -Action Build

# 2. Publish (self-contained)
.\Build-USBVersion.ps1 -Action Publish

# 3. Code-Signing (optional, mit gültigem Zertifikat)
.\Build-USBVersion.ps1 -Action Sign `
    -CertificatePath "MaterialManager_CodeSign.pfx" `
    -CertificatePassword "password"

# 4. Finales USB-Paket erstellen
.\Build-USBVersion.ps1 -Action Package

# 5. Datei auf USB kopieren
# → USB_Distribution Folder auf USB-Stick kopieren
```

## 🛡️ Sicherheit & Best Practices

✅ **DO:**
- Zertifikat-Passwort sicher lagern
- Timestamp verwenden (bei jedem Signing)
- Vor Distribution testen
- Checksum-Dateien erstellen

❌ **DON'T:**
- Passwort in Scripts hardcodieren
- Ohne Timestamp signieren
- Abgelaufene Zertifikate verwenden
- Zertifikat auf USB-Stick lagern

## 📊 Timing-Server (Timestamp)

Beim Signieren sollte ein Timestamp-Server verwendet werden:

```
http://timestamp.comodoca.com/authenticode
http://time.certum.pl
https://tsa.starfieldtech.com
```

**Warum?** Die Signatur bleibt gültig, auch wenn das Zertifikat später abläuft.

## 🔄 Automatische Lizenzschlüssel-Generierung

Für Kunden Hardware-IDs in Lizenzen umwandeln:

```batch
REM GENERATE_LICENSE.bat ausführen:
GENERATE_LICENSE.bat

REM Eingaben:
REM - Hardware-ID: CPU-DISK-MAC-Hash
REM - Kundenname: Firma XYZ
REM → Lizenzschlüssel: MM-XXXX-XXXX-XXXX-XXXX
```

## 📞 Support

Bei Problemen mit Code-Signing:
- Windows SDK installiert? → https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/
- SignTool.exe nicht gefunden? → Pfad in Build.ps1 anpassen
- Zertifikat ungültig? → Neue selbstsignierte erstellen oder extern kaufen

---

**Version:** 1.0.0  
**Stand:** 2025-02-28  
**Autor:** MaterialManager Development Team
