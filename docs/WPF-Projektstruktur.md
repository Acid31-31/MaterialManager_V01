# WPF-Projektstruktur: MaterialManager V01

## Überblick

**MaterialManager V01** ist eine Windows-Desktop-Anwendung zur Verwaltung von Blechlager, Rohren und Profilen in der Fertigung.

| Eigenschaft | Wert |
|-------------|------|
| Framework | .NET 8.0 (`net8.0-windows`), WPF, self-contained `win-x64` |
| Namespace | `MaterialManager_V01` + `GeoArbeitsvorbereitung` (Submodul) |
| Version | 1.2.1 |
| NuGet | EF Core + SQLite, ClosedXML, QRCoder, PdfPig |

**Solution:** `MaterialManager_V01.sln`

- `MaterialManager_V01` – Hauptanwendung
- `MaterialManager_Setup` – MSI-Installer

---

## Architekturmuster

**Code-Behind + Static-Service** (kein striktes MVVM in der Hauptapp):

| Schicht | Ort | Muster |
|---------|-----|--------|
| UI | `MainWindow`, `Views/*` | Code-Behind, Event-Handler, teils `INotifyPropertyChanged` im Window |
| Domäne | `Models/*` | POCOs/Entities |
| Logik | `Services/*` | Statische Service-Klassen (kein DI) |
| Konvertierung | `Converters/*` | WPF Value Converter |

**Ausnahme:** `GeoSuche/` nutzt MVVM (`MainViewModel`, `RelayCommand`).

`MainWindow` ist als Partial Class aufgeteilt:

- `MainWindow.xaml.cs` – Kernlogik
- `MainWindow.Filtering.cs` – Filter/Suche
- `MainWindow.Help.cs` – Hilfe-Dialog

---

## Verzeichnisstruktur

```
MaterialManager_V01_komplett/
├── App.xaml / App.xaml.cs          # Startup, Theme, Lizenz, Updates
├── MainWindow.xaml / .cs           # Haupt-Materialverwaltung
├── Models/                         # Domänenmodelle
├── Views/                          # Fenster & Dialoge
├── Services/                       # Geschäftslogik (~45 Klassen)
├── Converters/                     # WPF Converter
├── GeoSuche/                       # GEO-Datei-Suche (eigenes MVVM)
├── Assets/                         # Icons, Logos
├── USB_Installation/               # USB-Deployment-Skripte
├── Tools/                          # LicenseGenerator (nicht im Build)
├── Installer_Source/               # MSI-Setup
└── .vscode/                        # Cursor/VS Code Run & Debug
```

---

## Models/

| Datei | Zweck |
|-------|-------|
| `MaterialItem.cs` | Zentrale Entität: Blech/Rohr/Profil |
| `MaterialKategorie.cs` | Enum: Blech, Rohr, Profil |
| `MaterialDefinitions.cs` | Materialarten, Dichten |
| `Auftrag.cs` | Produktionsauftrag |
| `User.cs` | Benutzer/Rollen |
| `GroupedMaterialItem.cs` | UI-Gruppierung |

---

## Services/ (Auswahl)

**Daten:** `MaterialManagerDbContext`, `MaterialDataService`, `ExcelService`, `PathService`, `DatabaseBootstrapService`

**Netzwerk:** `NetzwerkService`, `AutoSyncManager`, `FileWatcherService`, `OnlineUserService`

**Material:** `LagerService`, `RegalService`, `RestMaterialSearchService`, `RohrZuschnittService`, `PdfRohrParser`

**Aufträge:** `AuftragDataService`, `AuftragArchivService`, `BuchungsService`, `BestellService`

**System:** `LicenseService`, `TrialService`, `GitHubUpdateService`, `ThemeService`, `UndoService`

---

## Views/

**Einstieg:** `StartModeWindow` (Modus-Auswahl nach App-Start)

**Hauptmodule:** `MainWindow`, `LagerDemoWindow`, `TafelplanungWindow`, `LaserDemoWindow`, `KundenMaterialWindow`, `RohrZuschnittDialog`

**Dialoge:** Material, Aufträge, Netzwerk, Lizenz, Update, Hilfe (siehe `Views/`)

---

## Anwendungsstart

1. Single-Instance-Mutex
2. Theme initialisieren
3. SQLite-DB bootstrap
4. Lizenzprüfung (Trial 60 Tage oder Vollversion)
5. Netzwerk-Healthcheck (optional)
6. `StartModeWindow` öffnen
7. Update-Checks via GitHub

Details: `App.xaml.cs`

---

## Datenfluss

- **SQLite** primär: `%LocalAppData%\MaterialManager_V01\materialmanager.db`
- **Excel** (`materialbestand.xlsx`) für Multi-PC-Sync im Netzwerkmodus
- `MaterialDataService` synchronisiert DB ↔ Excel anhand Zeitstempel
- `NetzwerkService` verwaltet Lock-Dateien

---

## GeoSuche/

Namespace `GeoArbeitsvorbereitung` – GEO-Datei-Suche mit MVVM, eingebunden über `App.xaml` → `GeoResources.xaml`.

---

## Konventionen

1. Kein Admin nötig – Daten unter `%LocalAppData%`
2. Self-contained Build für USB-Verteilung
3. Partial Classes für große Fenster
4. Gemischter UI-Stil (XAML + reines C# für einige Dialoge)
5. Tests/Backups/Tools per `<Compile Remove>` ausgeschlossen
