using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using GeoArbeitsvorbereitung.Dialogs;
using GeoArbeitsvorbereitung.Models;
using GeoArbeitsvorbereitung.Services;

namespace GeoArbeitsvorbereitung.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ISettingsService _settingsService;
    private readonly IGeoFileService _geoFileService;
    private readonly IDialogService _dialogService;
    private readonly ILagerBestandService _lagerBestandService;

    private string _searchRoot = string.Empty;
    private string _outputRoot = string.Empty;
    private string _drawingNumber = string.Empty;
    private GeoFileInfo? _foundFile;
    private string _newQuantity = string.Empty;
    private string _statusLog = string.Empty;
    private string _previewTitle = "Vorschau";
    private string _previewContent = "Keine GEO-Datei ausgewählt.";
    private string _lagerBestandExcelPath = string.Empty;
    private string _lagerBestandStatus = "Lagerbestand noch nicht geprüft.";
    private List<string> _searchFolderOptions = [];
    private List<string> _outputFolderOptions = [];

    public MainViewModel(
        ISettingsService settingsService,
        IGeoFileService geoFileService,
        IDialogService dialogService)
    {
        _settingsService = settingsService;
        _geoFileService = geoFileService;
        _dialogService = dialogService;
        _lagerBestandService = new LagerBestandService();

        var settings = _settingsService.Load();
        _searchRoot = settings.SearchRoot;
        _outputRoot = settings.OutputRoot;
        _searchFolderOptions = NormalizeFolderOptions(settings.SearchFolderOptions);
        _outputFolderOptions = NormalizeFolderOptions(settings.OutputFolderOptions);
        _lagerBestandExcelPath = settings.LagerBestandExcelPath;

        SaveSettingsCommand = new RelayCommand(_ => SaveSettings());
        SearchCommand = new RelayCommand(
            _ => Search(),
            _ => !string.IsNullOrWhiteSpace(DrawingNumber));
        CreateCopyCommand = new RelayCommand(
            _ => CreateCopy(),
            _ => FoundFile != null && int.TryParse(NewQuantity, out int q) && q > 0);
        OpenInGeoViewerCommand = new RelayCommand(_ => OpenInGeoViewer(), _ => FoundFile != null);
        OpenSearchFoldersCommand = new RelayCommand(_ => OpenSearchFoldersDialog());
        OpenOutputFoldersCommand = new RelayCommand(_ => OpenOutputFoldersDialog());
        OpenLagerBestandCommand = new RelayCommand(_ => OpenLagerBestandDialog());
        SelectLagerBestandExcelCommand = new RelayCommand(_ => SelectLagerBestandExcelFile());
    }

    // ── Settings ─────────────────────────────────────────────────────────────

    public string SearchRoot
    {
        get => _searchRoot;
        set { _searchRoot = value; OnPropertyChanged(); }
    }

    public string OutputRoot
    {
        get => _outputRoot;
        set { _outputRoot = value; OnPropertyChanged(); }
    }

    // ── Search ────────────────────────────────────────────────────────────────

    public string DrawingNumber
    {
        get => _drawingNumber;
        set { _drawingNumber = value; OnPropertyChanged(); }
    }

    public ObservableCollection<GeoFileInfo> FoundFiles { get; } = [];
    public ObservableCollection<LagerBestandItem> LagerBestandTreffer { get; } = [];

    public GeoFileInfo? FoundFile
    {
        get => _foundFile;
        set
        {
            _foundFile = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasFoundFile));
            UpdatePreview();
        }
    }

    public bool HasFoundFile => _foundFile != null;
    public bool HasFoundFiles => FoundFiles.Count > 0;
    public bool HasLagerBestandTreffer => LagerBestandTreffer.Count > 0;

    // ── Modify ────────────────────────────────────────────────────────────────

    public string NewQuantity
    {
        get => _newQuantity;
        set { _newQuantity = value; OnPropertyChanged(); }
    }

    // ── Log ───────────────────────────────────────────────────────────────────

    public string StatusLog
    {
        get => _statusLog;
        set { _statusLog = value; OnPropertyChanged(); }
    }

    public string PreviewTitle
    {
        get => _previewTitle;
        set { _previewTitle = value; OnPropertyChanged(); }
    }

    public string PreviewContent
    {
        get => _previewContent;
        set { _previewContent = value; OnPropertyChanged(); }
    }

    public string LagerBestandStatus
    {
        get => _lagerBestandStatus;
        set { _lagerBestandStatus = value; OnPropertyChanged(); }
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public ICommand SaveSettingsCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand CreateCopyCommand { get; }
    public ICommand OpenInGeoViewerCommand { get; }
    public ICommand OpenSearchFoldersCommand { get; }
    public ICommand OpenOutputFoldersCommand { get; }
    public ICommand OpenLagerBestandCommand { get; }
    public ICommand SelectLagerBestandExcelCommand { get; }

    // ── Implementations ───────────────────────────────────────────────────────

    private void SaveSettings()
    {
        _settingsService.Save(new AppSettings
        {
            SearchRoot = SearchRoot,
            OutputRoot = OutputRoot,
            SearchFolderOptions = NormalizeFolderOptions(_searchFolderOptions),
            OutputFolderOptions = NormalizeFolderOptions(_outputFolderOptions),
            LagerBestandExcelPath = _lagerBestandExcelPath,
        });
        Log("Einstellungen gespeichert.");
    }

    private void OpenSearchFoldersDialog()
    {
        var dlg = new SearchFoldersDialog(
            _searchFolderOptions,
            SearchRoot,
            dialogTitle: "Suchordner-Auswahl",
            headerText: "Suchordner auswählen (10 Plätze)",
            slotPrefix: "Suchordner")
        {
            Owner = Application.Current.MainWindow
        };

        if (dlg.ShowDialog() != true)
            return;

        _searchFolderOptions = NormalizeFolderOptions(dlg.SelectedFolders);

        SearchRoot = _searchFolderOptions.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p)) ?? string.Empty;
        SaveSettings();
        Log($"Suchordner übernommen: {dlg.SelectedFolders.Count} ausgewählt.");
    }

    private void OpenOutputFoldersDialog()
    {
        var dlg = new SearchFoldersDialog(
            _outputFolderOptions,
            OutputRoot,
            dialogTitle: "Speicherordner-Auswahl",
            headerText: "Speicherordner auswählen (10 Plätze)",
            slotPrefix: "Speicherordner")
        {
            Owner = Application.Current.MainWindow
        };

        if (dlg.ShowDialog() != true)
            return;

        _outputFolderOptions = NormalizeFolderOptions(dlg.SelectedFolders);

        OutputRoot = _outputFolderOptions.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p)) ?? string.Empty;
        SaveSettings();
        Log($"Speicherordner übernommen: {dlg.SelectedFolders.Count} ausgewählt.");
    }

    private static List<string> NormalizeFolderOptions(IEnumerable<string>? options)
    {
        var normalized = (options ?? Enumerable.Empty<string>())
            .Take(10)
            .Select(s => s?.Trim() ?? string.Empty)
            .ToList();

        while (normalized.Count < 10)
            normalized.Add(string.Empty);

        return normalized;
    }

    private void SelectLagerBestandExcelFile()
    {
        var ofd = new OpenFileDialog
        {
            Title = "Excel Lagerbestand auswählen",
            Filter = "Excel-Dateien (*.xlsx;*.xlsm;*.xls)|*.xlsx;*.xlsm;*.xls",
            CheckFileExists = true,
            Multiselect = false
        };

        if (ofd.ShowDialog() != true)
            return;

        _lagerBestandExcelPath = ofd.FileName;
        SaveSettings();
        LagerBestandStatus = $"Excel gesetzt: {_lagerBestandExcelPath}";
        Log($"Lagerbestand-Excel gesetzt: {_lagerBestandExcelPath}");
    }

    private bool EnsureLagerBestandExcelPath(bool interactive)
    {
        if (!string.IsNullOrWhiteSpace(_lagerBestandExcelPath) && File.Exists(_lagerBestandExcelPath))
            return true;

        if (!interactive)
            return false;

        SelectLagerBestandExcelFile();
        return !string.IsNullOrWhiteSpace(_lagerBestandExcelPath) && File.Exists(_lagerBestandExcelPath);
    }

    private void Search()
    {
        try
        {
            FoundFile = null;
            FoundFiles.Clear();
            LagerBestandTreffer.Clear();
            LagerBestandStatus = "Lagerbestand noch nicht geprüft.";
            OnPropertyChanged(nameof(HasFoundFiles));
            OnPropertyChanged(nameof(HasLagerBestandTreffer));

            var searchRoots = NormalizeFolderOptions(_searchFolderOptions)
                .Where(p => !string.IsNullOrWhiteSpace(p) && Directory.Exists(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (searchRoots.Count == 0 && !string.IsNullOrWhiteSpace(SearchRoot) && Directory.Exists(SearchRoot))
                searchRoots.Add(SearchRoot);

            if (searchRoots.Count == 0)
            {
                Log("Fehler: Kein gültiger Suchordner angegeben.");
                return;
            }

            var term = DrawingNumber.Trim();
            var merged = new Dictionary<string, GeoFileInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var root in searchRoots)
            {
                IReadOnlyList<GeoFileInfo> results;
                try
                {
                    results = _geoFileService.FindAll(root, term);
                }
                catch (Exception ex)
                {
                    Log($"Suchordner übersprungen ({root}): {ex.Message}");
                    continue;
                }

                foreach (var file in results)
                {
                    if (!merged.ContainsKey(file.FullPath))
                        merged[file.FullPath] = file;
                }
            }

            foreach (var file in merged.Values.OrderByDescending(f => f.LastWriteTime).ThenBy(f => f.FileName, StringComparer.OrdinalIgnoreCase))
                FoundFiles.Add(file);

            OnPropertyChanged(nameof(HasFoundFiles));

            if (FoundFiles.Count == 0)
            {
                Log($"Keine GEO-Datei für Zeichnungsnummer/Suchbegriff \"{DrawingNumber}\" gefunden.");
            }
            else
            {
                FoundFile = FoundFiles[0];
                Log($"{FoundFiles.Count} GEO-Datei(en) gefunden in {searchRoots.Count} Suchordnern. Auswahl: {FoundFile.FileName}");
            }

            PruefeLagerBestand(term);
        }
        catch (Exception ex)
        {
            Log($"Fehler bei der Suche: {ex.Message}");
        }
    }

    private void PruefeLagerBestand(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            LagerBestandStatus = "Keine Zeichnungsnummer für Lagerprüfung vorhanden.";
            return;
        }

        if (!EnsureLagerBestandExcelPath(interactive: true))
        {
            LagerBestandStatus = "Keine Excel-Datei für Lagerbestand ausgewählt.";
            Log("Lagerbestand wurde übersprungen: keine Excel-Datei ausgewählt.");
            return;
        }

        var items = _lagerBestandService.SucheNachZeichnungsnummer(_lagerBestandExcelPath, term);
        foreach (var item in items)
            LagerBestandTreffer.Add(item);

        OnPropertyChanged(nameof(HasLagerBestandTreffer));

        if (items.Count == 0)
        {
            LagerBestandStatus = $"Kein Lager-Treffer für \"{term}\" in Excel gefunden.";
            Log(LagerBestandStatus);
            return;
        }

        var vorhanden = items.Count(i => string.Equals(i.Status, "Vorhanden", StringComparison.OrdinalIgnoreCase));
        LagerBestandStatus = $"Lager-Treffer: {items.Count} | Vorhanden: {vorhanden}";
        Log($"Lagerbestand geprüft: {items.Count} Treffer in Excel.");
    }

    private void CreateCopy()
    {
        try
        {
            if (FoundFile == null) return;

            if (!int.TryParse(NewQuantity, out int qty) || qty <= 0)
            {
                Log("Fehler: Bitte eine gültige Stückzahl (> 0) eingeben.");
                return;
            }

            if (string.IsNullOrWhiteSpace(OutputRoot))
            {
                Log("Fehler: Kein Speicherordner angegeben.");
                return;
            }

            var newFileName = _geoFileService.BuildNewFileName(FoundFile, qty);
            var targetPath = _geoFileService.BuildTargetPath(OutputRoot, FoundFile, newFileName);

            if (File.Exists(targetPath))
            {
                var targetFolder = Path.GetDirectoryName(targetPath) ?? OutputRoot;
                var confirmedName = _dialogService.ShowTargetExistsDialog(targetFolder, newFileName);
                if (confirmedName == null)
                {
                    Log("Abgebrochen.");
                    return;
                }

                newFileName = confirmedName;
                targetPath = _geoFileService.BuildTargetPath(OutputRoot, FoundFile, newFileName);
            }

            _geoFileService.CopyFile(FoundFile.FullPath, targetPath);
            Log($"Kopie erstellt: {targetPath}");
        }
        catch (Exception ex)
        {
            Log($"Fehler beim Erstellen der Kopie: {ex.Message}");
        }
    }

    private void UpdatePreview()
    {
        if (FoundFile == null)
        {
            PreviewTitle = "Vorschau";
            PreviewContent = "Keine GEO-Datei ausgewählt.";
            return;
        }

        PreviewTitle = $"GEO Viewer: {FoundFile.FileName}";
        PreviewContent =
            $"Datei: {FoundFile.FileName}{Environment.NewLine}" +
            $"Pfad: {FoundFile.FullPath}{Environment.NewLine}{Environment.NewLine}" +
            "Für die echte Vorschau bitte auf 'Im GEO Viewer öffnen' klicken.";
    }

    private void OpenInGeoViewer()
    {
        try
        {
            if (FoundFile == null)
            {
                Log("Keine GEO-Datei für Viewer ausgewählt.");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = FoundFile.FullPath,
                UseShellExecute = true
            });

            Log($"GEO Viewer geöffnet: {FoundFile.FileName}");
        }
        catch (Exception ex)
        {
            Log($"GEO Viewer konnte nicht geöffnet werden: {ex.Message}");
            PreviewContent =
                "Kein GEO-Viewer als Standard-App registriert oder Start fehlgeschlagen." +
                Environment.NewLine +
                "Bitte GEO Viewer installieren und .geo damit verknüpfen.";
        }
    }

    private void OpenLagerBestandDialog()
    {
        try
        {
            var term = DrawingNumber?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(term))
            {
                Log("Bitte zuerst eine Zeichnungsnummer / einen Suchbegriff eingeben.");
                return;
            }

            if (!EnsureLagerBestandExcelPath(interactive: true))
            {
                Log("Lagerbestand wurde abgebrochen: keine Excel-Datei ausgewählt.");
                return;
            }

            var items = _lagerBestandService.SucheNachZeichnungsnummer(_lagerBestandExcelPath, term);

            var dlg = new LagerBestandDialog(term, _lagerBestandExcelPath, items)
            {
                Owner = Application.Current.MainWindow
            };
            dlg.ShowDialog();

            Log($"Lagerbestand geprüft: {items.Count} Treffer in Excel.");
        }
        catch (Exception ex)
        {
            Log($"Fehler bei Lagerbestand-Prüfung: {ex.Message}");
        }
    }

    private void Log(string message)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}]  {message}";
        StatusLog = string.IsNullOrEmpty(StatusLog) ? entry : entry + Environment.NewLine + StatusLog;
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
