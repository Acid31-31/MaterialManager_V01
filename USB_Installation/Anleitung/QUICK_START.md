# 🚀 MaterialManager R03 USB - Quick Start

## Was wurde für dich eingebaut?

✅ **USB-portable Vollversion** mit .NET 8  
✅ **Lizenzierungssystem** (Demo 30 Tage, dann Vollversion mit Lizenzschlüssel)  
✅ **Hardware-ID-Bindung** (Lizenz ist an Rechner gebunden)  
✅ **Digitale Zertifikate Support** (Code-Signing für Antivirus)  
✅ **Automatisches Installationsskript** (USB_INSTALL.bat)  
✅ **Lizenzschlüssel-Generator** (GENERATE_LICENSE.bat)  

## 5 Schritte zur USB-Distribution

### Schritt 1: Build-Skript ausführen

```powershell
cd C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_V01
.\Build-USBVersion.ps1 -Action Package
```

**Ergebnis:** Folder `USB_Distribution` mit allen Dateien

### Schritt 2: USB-Stick vorbereiten

```powershell
# USB einstecken (z.B. Laufwerk D:)
Copy-Item -Path "USB_Distribution\*" -Destination "D:\" -Recurse -Force
```

### Schritt 3: Auf Zielrechner testen

- USB einstecken
- `USB_INSTALL.bat` **als Administrator** ausführen
- Programm öffnet automatisch

### Schritt 4: Hardware-ID erfassen

- Menü: **Hilfe → Lizenzinformationen**
- Hardware-ID kopieren
- Via Email an Kunden oder dich selbst

### Schritt 5: Lizenzschlüssel generieren & senden

```batch
GENERATE_LICENSE.bat
# Eingabe: Hardware-ID + Kundenname
# Output: Lizenzschlüssel (MM-XXXX-XXXX-XXXX-XXXX)
```

Lizenzschlüssel an Kunden senden → Kunde aktiviert ihn im Programm

---

## Dateien im Projekt

| Datei | Zweck |
|-------|-------|
| `Build-USBVersion.ps1` | PowerShell-Skript für Build/Publish/Sign/Package |
| `USB_INSTALL.bat` | Installation auf Zielrechner |
| `GENERATE_LICENSE.bat` | Lizenzschlüssel-Generator |
| `USB_README.txt` | Dokumentation für Kunden |
| `CODE_SIGNING_GUIDE.md` | Anleitung für digitale Zertifikate |
| `DEPLOYMENT_ANLEITUNG.md` | Detaillierte Schritt-für-Schritt Anleitung |
| `Services/HardwareIdService.cs` | Hardware-ID Generierung |
| `Services/LicenseKeyGenerator.cs` | Lizenzschlüssel-Logik |

---

## Lizenzierungs-Features

### Demo-Version (30 Tage)
- ✅ Automatisch aktiv nach Installation
- ✅ Zähler läuft nach erstem Start
- ✅ Kein Lizenzschlüssel nötig

### Vollversion
- 🔑 Lizenzschlüssel aktiviert Vollversion
- 🔐 Gebunden an Hardware-ID des Rechners
- ♾️ Unbegrenzte Nutzung nach Aktivierung

### Lizenzschlüssel-Format
```
MM-A1B2-C3D4-E5F6-G7H8
```
- Eindeutig für jede Hardware-ID
- Mit HMAC-SHA256 generiert
- Nicht übertragbar

---

## Antivirus & Digitale Zertifikate

### Ohne Code-Signing (einfach)
```powershell
.\Build-USBVersion.ps1 -Action Package
```
→ Antivirus könnte Warnung zeigen, aber Datei ist sauber

### Mit Code-Signing (professionell)

1. **Zertifikat erstellen** (siehe `CODE_SIGNING_GUIDE.md`)
2. **Build mit Signing:**
```powershell
.\Build-USBVersion.ps1 -Action Package `
    -CertificatePath "C:\Temp\cert.pfx" `
    -CertificatePassword "password"
```
→ Antivirus erkennt digitale Signatur

---

## Troubleshooting

**Problem: PowerShell zeigt Fehler**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

**Problem: USB-Installation fehlgeschlagen**
- Rechtsklick → "Als Administrator ausführen"
- USB-Stick vollständig kopiert?

**Problem: Lizenzschlüssel funktioniert nicht**
- Programm: Menü → Lizenz zurücksetzen
- Demo sollte wieder da sein
- Mit neuem Schlüssel versuchen

---

## Support-Kontakt

Bei Fragen oder Problemen:
- Diese Dateien lesen: `DEPLOYMENT_ANLEITUNG.md`
- PowerShell-Fehler: `CODE_SIGNING_GUIDE.md`
- Lizenzierung: Siehe `Services/LicenseService.cs`

---

**Version:** 1.0.0  
**Erstellungsdatum:** 2025-02-28  
**Status:** ✅ Produktionsreif
