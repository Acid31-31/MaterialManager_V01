# Code-Signing für MaterialManager 1.0.x

## 📋 Übersicht

Dieses Dokument beschreibt, wie du das Programm digital mit einem Code-Signing-Zertifikat signierst, damit Windows und Antivirus-Software es als vertrauenswürdig erkennen.

## 🔐 Was ist Code-Signing?

- **Digitale Signatur** auf der .exe-Datei
- **Authentizität**: Beweist, dass du der Autor bist
- **Integrität**: Datei wurde nicht verändert
- **SmartScreen-Warnungen reduzieren**

## 🚨 Wichtig für Updates auf andere PCs

- Die `.pfx` enthält den **privaten Schlüssel** und bleibt nur auf dem Build-PC.
- Auf Ziel-PCs wird **nicht** die `.pfx` verteilt.
- Verteilt werden:
  1. die **signierten Update-Dateien** (`.exe`, `.dll`),
  2. das **öffentliche Zertifikat** (`.cer`) für Vertrauen auf Ziel-PCs.

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
- Weniger SmartScreen-Warnungen
- Professioneller Eindruck

## ✅ Neuer Standard-Workflow (Release + USB)

1. Software bauen/publishen.
2. Signieren mit:

```bat
SIGN_RELEASE.bat "D:\Pfad\zu\deinem.pfx" "DEIN_PASSWORT"
```

3. Dabei wird automatisch exportiert:

- `USB_Installation\MaterialManager_CodeSigning_PUBLIC.cer`

4. Auf Ziel-PC (einmalig pro Zertifikat):

- `USB_Installation\INSTALL_CERTIFICATE.bat` als Administrator ausführen.

5. Danach Update/Installation normal starten.

## ✅ Verifizierung der Signatur

```powershell
$file = "D:\MaterialManager_V01_komplett\USB_Installation\MaterialManager\MaterialManager_V01.exe"
Get-AuthenticodeSignature $file | Format-List Status,StatusMessage,SignerCertificate
```

## 🛡️ Sicherheit & Best Practices

✅ **DO:**
- `.pfx` nur auf Build-PC behalten
- Immer Timestamp verwenden
- Signatur nach jedem Release prüfen
- Öffentliches `.cer` mit USB-Updatepaket mitgeben

❌ **DON'T:**
- `.pfx` auf USB oder Ziel-PCs verteilen
- Passwort in Scripts hardcodieren
- Ohne Timestamp signieren

## 📊 Timing-Server (Timestamp)

Beim Signieren sollte ein Timestamp-Server verwendet werden:

- `http://timestamp.digicert.com`
- `http://timestamp.comodoca.com/authenticode`
- `http://time.certum.pl`

---

**Version:** 1.1.0  
**Stand:** 2026-04-09  
**Autor:** MaterialManager Development Team
