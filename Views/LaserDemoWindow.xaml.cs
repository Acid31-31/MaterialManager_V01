using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MaterialManager_V01.Models;
using MaterialManager_V01.Services;
using Microsoft.Win32;
using System.Windows.Threading;

namespace MaterialManager_V01.Views
{
    public partial class LaserDemoWindow : Window, INotifyPropertyChanged
    {
        private sealed class MaterialSelectionSnapshot
        {
            public HashSet<string> CheckedKeys { get; } = new(StringComparer.Ordinal);
            public string? SelectedKey { get; set; }
        }

        protected virtual string Arbeitsbereich => AuftragArbeitsplatzService.Laser;
        protected virtual bool ShowReservedMaterialArea => true;

        private List<MaterialItem> _alleMaterialien = new();
        private List<MaterialItem> _restMaterialienCache = new();
        private List<Auftrag> _auftraegeCache = new();
        private readonly int _aktuellesJahr = DateTime.Now.Year;
        private int _ausgewaehlteKalenderWoche = ISOWeek.GetWeekOfYear(DateTime.Now);
        private DateTime _lastAutoReloadUtc = DateTime.MinValue;
        private DateTime _lastObservedMaterialWriteUtc = DateTime.MinValue;
        private DateTime _lastObservedAuftraegeWriteUtc = DateTime.MinValue;
        private readonly DispatcherTimer _autoRefreshTimer = new() { Interval = TimeSpan.FromSeconds(2) };
        private int _autoSyncStatusVersion;
        private bool _nachproduktionModus;

        public ObservableCollection<MaterialItem> RestMaterialien { get; } = new();
        public ObservableCollection<Auftrag> AuftraegeView { get; } = new();

        private string _workspaceTitle = "Laser ÔÇô Auftrags├╝bersicht";
        public string WorkspaceTitle
        {
            get => _workspaceTitle;
            set
            {
                _workspaceTitle = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WorkspaceTitle)));
            }
        }

        private string _headerText = string.Empty;
        public string HeaderText
        {
            get => _headerText;
            set
            {
                _headerText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HeaderText)));
            }
        }

        private string _summaryText = string.Empty;
        public string SummaryText
        {
            get => _summaryText;
            set
            {
                _summaryText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SummaryText)));
            }
        }

        public bool CanManageRestMaterials => false;

        public event PropertyChangedEventHandler? PropertyChanged;

        private string _auftragsKwText = string.Empty;
        public string AuftragsKwText
        {
            get => _auftragsKwText;
            set
            {
                _auftragsKwText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AuftragsKwText)));
            }
        }

        private string _auftragsKwInfoText = string.Empty;
        public string AuftragsKwInfoText
        {
            get => _auftragsKwInfoText;
            set
            {
                _auftragsKwInfoText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AuftragsKwInfoText)));
            }
        }

        private string _nachproduktionButtonText = "Nachproduktion";
        public string NachproduktionButtonText
        {
            get => _nachproduktionButtonText;
            set
            {
                _nachproduktionButtonText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NachproduktionButtonText)));
            }
        }

        public LaserDemoWindow()
        {
            InitializeComponent();
            DataContext = this;
            WorkspaceTitle = "Laser ÔÇô Auftrags├╝bersicht";
            HeaderText = $"Angemeldet als {OperatorIdentityService.CurrentOperatorName} ÔÇô Produktionssicht";
            RefreshLicenseBanner();
            RefreshNetworkStatusBanner();
            UpdateAuftragsKwText();
            FitToWorkArea();
            Loaded += OnWindowLoaded;
            Closed += OnWindowClosed;
            PreviewKeyDown += OnWindowPreviewKeyDown;
        }

        private void OnWindowLoaded(object? sender, RoutedEventArgs e)
        {
            InitializeAutoSync();
            LoadMaterials();
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            _autoRefreshTimer.Stop();
            _autoRefreshTimer.Tick -= OnAutoRefreshTimerTick;
        }

        private void InitializeAutoSync()
        {
            _lastObservedMaterialWriteUtc = GetObservedWriteTimeUtc(NetzwerkService.GetSavePath());
            _lastObservedAuftraegeWriteUtc = GetObservedWriteTimeUtc(GetSharedAuftraegePath());
            _autoRefreshTimer.Tick -= OnAutoRefreshTimerTick;
            _autoRefreshTimer.Tick += OnAutoRefreshTimerTick;
            _autoRefreshTimer.Start();
        }

        private void OnAutoRefreshTimerTick(object? sender, EventArgs e)
        {
            if (!IsLoaded)
                return;

            if (Mouse.LeftButton == MouseButtonState.Pressed)
                return;

            var materialWriteUtc = GetObservedWriteTimeUtc(NetzwerkService.GetSavePath());
            var auftraegeWriteUtc = GetObservedWriteTimeUtc(GetSharedAuftraegePath());
            var hasChanged = materialWriteUtc > _lastObservedMaterialWriteUtc || auftraegeWriteUtc > _lastObservedAuftraegeWriteUtc;

            _lastObservedMaterialWriteUtc = materialWriteUtc;
            _lastObservedAuftraegeWriteUtc = auftraegeWriteUtc;

            if (!hasChanged)
                return;

            var now = DateTime.UtcNow;
            if ((now - _lastAutoReloadUtc).TotalMilliseconds < 800)
                return;

            _lastAutoReloadUtc = now;
            LoadMaterials();
            ShowAutoSyncStatus();
        }

        private static DateTime GetObservedWriteTimeUtc(string? path)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(path) && File.Exists(path)
                    ? File.GetLastWriteTimeUtc(path)
                    : DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private static string GetSharedAuftraegePath()
        {
            var materialPath = NetzwerkService.GetSavePath();
            var directory = Path.GetDirectoryName(materialPath);
            return string.IsNullOrWhiteSpace(directory) ? string.Empty : Path.Combine(directory, "auftraege.json");
        }

        private async void LoadMaterials()
        {
            var selectionSnapshot = CaptureMaterialSelection();

            try
            {
                var items = await MaterialDataService.LoadAllMaterialsAsync();
                _alleMaterialien = items;
                _restMaterialienCache = _alleMaterialien
                    .Where(m => !string.IsNullOrWhiteSpace(m.AuftragNr))
                    .Where(m => MatchesArbeitsbereich(m.AuftragNr))
                    .ToList();

                LoadAuftraege();
                ApplyFilter();
                RestoreMaterialSelection(selectionSnapshot);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Auftragsmaterialien:\n{ex.Message}", "Laser", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private MaterialSelectionSnapshot CaptureMaterialSelection()
        {
            var snapshot = new MaterialSelectionSnapshot();

            foreach (var item in RestMaterialien.Where(m => m.IsSelected))
                snapshot.CheckedKeys.Add(GetMaterialSelectionKey(item));

            if (RestMaterialGrid?.SelectedItem is MaterialItem selectedItem)
                snapshot.SelectedKey = GetMaterialSelectionKey(selectedItem);

            return snapshot;
        }

        private void RestoreMaterialSelection(MaterialSelectionSnapshot snapshot)
        {
            MaterialItem? selectedItem = null;

            foreach (var item in RestMaterialien)
            {
                var key = GetMaterialSelectionKey(item);
                item.IsSelected = snapshot.CheckedKeys.Contains(key);
                if (snapshot.SelectedKey == key)
                    selectedItem = item;
            }

            if (RestMaterialGrid != null)
                RestMaterialGrid.SelectedItem = selectedItem ?? RestMaterialien.FirstOrDefault(m => m.IsSelected);
        }

        private static string GetMaterialSelectionKey(MaterialItem item)
        {
            if (item.Id > 0)
                return $"ID:{item.Id}";

            return string.Join("|",
                item.Restnummer ?? string.Empty,
                item.AuftragNr ?? string.Empty,
                item.MaterialArt ?? string.Empty,
                item.Legierung ?? string.Empty,
                item.Form ?? string.Empty,
                item.Mass ?? string.Empty,
                item.Staerke.ToString(CultureInfo.InvariantCulture));
        }

        private bool MatchesArbeitsbereich(string? auftragsnummer)
        {
            var arbeitsplatz = AuftragArbeitsplatzService.GetArbeitsplatz(auftragsnummer);
            return AuftragArbeitsplatzService.IsMatchForBereich(arbeitsplatz, Arbeitsbereich);
        }

        private void LoadAuftraege()
        {
            try
            {
                if (_nachproduktionModus)
                {
                    try
                    {
                        AuftragArchivService.BackfillArchiveMetadataForYear(_aktuellesJahr);
                    }
                    catch
                    {
                    }

                    _auftraegeCache = AuftragArchivService.GetArchivedOrdersForYear(_aktuellesJahr)
                        .Where(x => !string.IsNullOrWhiteSpace(x.Auftragsnummer))
                        .Select(x => new Auftrag
                        {
                            Auftragsnummer = x.Auftragsnummer,
                            Arbeitsplatz = "Archiv",
                            Status = AuftragStatus.Abgeschlossen,
                            ErstelltAm = x.ProduktionStartDatum ?? x.ArchiviertAm,
                            GeaendertAm = x.ProduktionEndDatum ?? x.ArchiviertAm,
                            ProduktionStartDatum = x.ProduktionStartDatum,
                            ProduktionEndDatum = x.ProduktionEndDatum,
                            MaterialPositionen = x.MaterialPositionen,
                            MaterialArtStaerkeText = x.MaterialArtStaerkeText,
                            GesamtStueckzahl = x.GesamtStueckzahl,
                            GesamtGewichtKg = x.GesamtGewichtKg,
                            AngelegtVon = x.AngelegtVon,
                            GeaendertVon = x.GeaendertVon,
                            PdfPfad = AuftragArchivService.ResolveAccessiblePdfPath(x.Auftragsnummer, x.ErstePdfPfad)
                        })
                        .ToList();
                }
                else
                {
                    _auftraegeCache = AuftragDataService.LoadAllAuftraege()
                        .Where(a => a.Status != AuftragStatus.Abgeschlossen)
                        .ToList();
                }

                ApplyAuftragsKwFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Aufträge:\n{ex.Message}", "Laser", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyAuftragsKwFilter()
        {
            List<Auftrag> gefilterteAuftraege;

            if (_nachproduktionModus)
            {
                var selectedWeekStart = GetIsoWeekStartDate(_aktuellesJahr, _ausgewaehlteKalenderWoche);
                var windowStart = selectedWeekStart.AddDays(-28);
                var windowEndExclusive = selectedWeekStart.AddDays(7);

                gefilterteAuftraege = _auftraegeCache
                    .Where(a => a.Status == AuftragStatus.Abgeschlossen)
                    .Where(a =>
                    {
                        var relevantDate = GetRelevantAuftragDate(a);
                        return relevantDate >= windowStart && relevantDate < windowEndExclusive;
                    })
                    .OrderByDescending(GetRelevantAuftragDate)
                    .ToList();

                AuftragsKwInfoText =
                    $"{gefilterteAuftraege.Count} abgeschlossene Auftrag/Aufträge im 5-Wochen-Fenster ({windowStart:dd.MM.yyyy} - {windowEndExclusive.AddDays(-1):dd.MM.yyyy})";
            }
            else
            {
                gefilterteAuftraege = _auftraegeCache
                    .OrderByDescending(GetRelevantAuftragDate)
                    .ToList();

                var offen = gefilterteAuftraege.Count(a => a.Status == AuftragStatus.Offen);
                var inBearbeitung = gefilterteAuftraege.Count(a => a.Status == AuftragStatus.InBearbeitung);
                AuftragsKwInfoText =
                    $"{gefilterteAuftraege.Count} aktive Auftrag/Aufträge ({offen} offen, {inBearbeitung} in Bearbeitung)";
            }

            AuftraegeView.Clear();
            foreach (var auftrag in gefilterteAuftraege)
                AuftraegeView.Add(auftrag);
        }

        private static DateTime GetRelevantAuftragDate(Auftrag auftrag)
        {
            return auftrag.GeaendertAm != default ? auftrag.GeaendertAm : auftrag.ErstelltAm;
        }

        private static DateTime GetIsoWeekStartDate(int year, int week)
        {
            var jan4 = new DateTime(year, 1, 4);
            var jan4IsoDay = jan4.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)jan4.DayOfWeek;
            var firstIsoWeekMonday = jan4.AddDays(1 - jan4IsoDay);
            return firstIsoWeekMonday.AddDays((week - 1) * 7);
        }

        private void UpdateAuftragsKwText()
        {
            AuftragsKwText = _nachproduktionModus
                ? $"Nachproduktion bis KW {_ausgewaehlteKalenderWoche:D2}"
                : "Aktive Aufträge";

            NachproduktionButtonText = _nachproduktionModus ? "Aktive Aufträge" : "Nachproduktion";
        }

        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                return;

            if (e.Key == Key.Z)
            {
                ExecuteUndo();
                e.Handled = true;
            }
            else if (e.Key == Key.Y)
            {
                ExecuteRedo();
                e.Handled = true;
            }
        }

        private void OnUndoClick(object sender, RoutedEventArgs e)
        {
            ExecuteUndo();
        }

        private void OnRedoClick(object sender, RoutedEventArgs e)
        {
            ExecuteRedo();
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            LoadMaterials();
        }

        private void OnOpenNetworkFolder(object sender, RoutedEventArgs e)
        {
            try
            {
                NetzwerkService.OpenAktivenDatenordnerImExplorer();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Netzwerkordner konnte nicht ge├Âffnet werden:\n{ex.Message}", "Laser", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnReservedRestsClick(object sender, RoutedEventArgs e)
        {
            var dlg = new ReservierteResteDialog { Owner = this };
            dlg.ShowDialog();
            LoadMaterials();
        }

        private void OnSearchReserveClick(object sender, RoutedEventArgs e)
        {
            if (!CanManageRestMaterials)
                return;

            var dlg = new ResteSucheDialog { Owner = this };
            if (dlg.ShowDialog() != true)
                return;

            var gefunden = RestMaterialSearchService.SearchBestMatches(
                _alleMaterialien,
                dlg.Material,
                dlg.Legierung,
                dlg.Staerke,
                dlg.Laenge,
                dlg.Breite,
                dlg.ToleranzProzent,
                dlg.Form,
                requireRest: true);

            if (!gefunden.Any())
            {
                MessageBox.Show("Keine passenden Restmaterialien gefunden.", "Laser", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var auswahlDlg = new ResteAuswahlDialog(gefunden) { Owner = this };
            if (auswahlDlg.ShowDialog() != true || auswahlDlg.SelectedMaterial == null)
                return;

            var reservierungDlg = new ResteReservierungDialog(auswahlDlg.SelectedMaterial.AuftragNr) { Owner = this };
            if (reservierungDlg.ShowDialog() != true || string.IsNullOrWhiteSpace(reservierungDlg.AuftragNr))
                return;

            PushUndoSnapshot("Rest reservieren");
            auswahlDlg.SelectedMaterial.AuftragNr = reservierungDlg.AuftragNr.Trim();
            auswahlDlg.SelectedMaterial.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
            auswahlDlg.SelectedMaterial.AenderungsDatum = DateTime.Now;
            SaveAllMaterials();
            LoadMaterials();
        }

        private void OnNewRestClick(object sender, RoutedEventArgs e)
        {
            if (!CanManageRestMaterials)
                return;

            var dlg = new MaterialDialog(_alleMaterialien) { Owner = this };
            dlg.SelectedForm = "Rest";
            if (dlg.ShowDialog() != true)
                return;

            PushUndoSnapshot("Rest neu");
            dlg.Material.Form = "Rest";
            _alleMaterialien.Add(dlg.Material);
            SaveAllMaterials();
            LoadMaterials();
        }

        private MaterialItem? GetPrimarySelectedMaterial()
        {
            return RestMaterialien.FirstOrDefault(m => m.IsSelected)
                ?? RestMaterialGrid.SelectedItem as MaterialItem;
        }

        private List<MaterialItem> GetMarkedMaterials()
        {
            var marked = RestMaterialien.Where(m => m.IsSelected).ToList();
            if (marked.Count > 0)
                return marked;

            return RestMaterialGrid.SelectedItem is MaterialItem selected
                ? new List<MaterialItem> { selected }
                : new List<MaterialItem>();
        }

        private void OnEditRestClick(object sender, RoutedEventArgs e)
        {
            var item = GetPrimarySelectedMaterial();
            if (item == null)
                return;

            var dlg = new MaterialDialog(_alleMaterialien) { Owner = this, PreserveOriginalAuftragOnEdit = false };
            dlg.SetEditMode(item);
            if (dlg.ShowDialog() != true)
                return;

            var index = _alleMaterialien.IndexOf(item);
            if (index < 0)
                return;

            PushUndoSnapshot("Reserviertes Material bearbeiten");
            dlg.Material.AuftragNr = string.Empty;
            if (string.IsNullOrWhiteSpace(dlg.Material.Lagerort) || string.Equals(dlg.Material.Lagerort, "Gebucht", StringComparison.OrdinalIgnoreCase))
            {
                dlg.Material.Lagerort = RegalService.DetermineLagerort(
                    dlg.Material.MaterialArt,
                    dlg.Material.Legierung,
                    dlg.Material.Form,
                    dlg.Material.Staerke,
                    dlg.Material.Mass,
                    _alleMaterialien.Where(m => !ReferenceEquals(m, item)).ToList());
            }
            dlg.Material.IsSelected = false;
            _alleMaterialien[index] = dlg.Material;
            SaveAllMaterials();
            LoadMaterials();
        }

        private void OnDeleteReservedClick(object sender, RoutedEventArgs e)
        {
            var items = GetMarkedMaterials().Where(m => !string.IsNullOrWhiteSpace(m.AuftragNr)).ToList();
            if (items.Count == 0)
            {
                MessageBox.Show("Bitte zuerst reserviertes Material ausw├ñhlen oder markieren.", "Laser", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirmDialog = new BestaetigungsDialog(
                "Reserviertes Material l├Âschen",
                items.Count == 1
                    ? $"Reserviertes Material '{items[0].MaterialArt} {items[0].Mass}' wirklich l├Âschen?"
                    : $"{items.Count} reservierte Materialien wirklich l├Âschen?",
                confirmText: "L├Âschen",
                cancelText: "Abbrechen",
                confirmColorHex: "#8B1E1E")
            { Owner = this };

            if (confirmDialog.ShowDialog() != true)
                return;

            PushUndoSnapshot(items.Count == 1 ? "Reserviertes Material l├Âschen" : "Reservierte Materialien l├Âschen");
            foreach (var item in items)
            {
                BuchungsService.BucheAusgang(item, item.AuftragNr, OperatorIdentityService.CurrentOperatorName);
                _alleMaterialien.Remove(item);
            }

            SaveAllMaterials();
            LoadMaterials();
        }

        private async void OnCheckForUpdates(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var result = await GitHubUpdateService.CheckForUpdatesAsync();

                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    MessageBox.Show($"Update-Pr├╝fung fehlgeschlagen:\n{result.ErrorMessage}", "Update", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!result.IsUpdateAvailable || string.IsNullOrWhiteSpace(result.DownloadUrl))
                {
                    MessageBox.Show($"Sie haben die neueste Version ({result.CurrentVersion}).", "Update", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dlg = new UpdateDialog(result) { Owner = this };
                dlg.ShowDialog();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void OnProgrammHilfe(object sender, RoutedEventArgs e)
        {
            var dlg = new ProgrammHilfeDialog { Owner = this };
            dlg.ShowDialog();
        }

        private void OnMinimizeWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnMaximizeRestoreWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void OnCloseWindow(object sender, RoutedEventArgs e)
        {
            OnCloseClick(sender, e);
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            var startWindow = new StartModeWindow();
            Application.Current.MainWindow = startWindow;
            startWindow.Show();
            Close();
        }

        private void OnOpenStartProgramClick(object sender, RoutedEventArgs e)
        {
            var startWindow = new StartModeWindow();
            Application.Current.MainWindow = startWindow;
            startWindow.Show();
            Close();
        }

        private void OnOpenLagerProgramClick(object sender, RoutedEventArgs e)
        {
            var lagerWindow = new LagerDemoWindow();
            Application.Current.MainWindow = lagerWindow;
            lagerWindow.Show();
            Close();
        }

        private void OnOpenTafelplanungProgramClick(object sender, RoutedEventArgs e)
        {
            var tafelWindow = new TafelplanungWindow();
            Application.Current.MainWindow = tafelWindow;
            tafelWindow.Show();
            Close();
        }

        private void OnOpenMainProgramClick(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
            Close();
        }

        private void OnAuftraegeGridDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AuftraegeGrid.SelectedItem is not Auftrag auftrag)
                return;

            if (_nachproduktionModus)
            {
                if (CopyArchivedOrderToActiveNachproduktion(auftrag, out var neueNummer))
                {
                    MessageBox.Show($"Nachproduktion erstellt: {neueNummer}\nDer Auftrag ist jetzt aktiv und kann gestartet/beendet werden.",
                        "Laser", MessageBoxButton.OK, MessageBoxImage.Information);

                    _nachproduktionModus = false;
                    UpdateAuftragsKwText();
                    LoadAuftraege();

                    var neu = AuftraegeView.FirstOrDefault(a => string.Equals(a.Auftragsnummer, neueNummer, StringComparison.OrdinalIgnoreCase));
                    if (neu != null)
                    {
                        AuftraegeGrid.SelectedItem = neu;
                        var dlgNeu = new ProduktionVerfolgungDialog(neu) { Owner = this };
                        dlgNeu.ShowDialog();
                        LoadAuftraege();
                    }
                }

                return;
            }

            var dlg = new ProduktionVerfolgungDialog(auftrag) { Owner = this };
            dlg.ShowDialog();
            LoadAuftraege();
        }

        private bool CopyArchivedOrderToActiveNachproduktion(Auftrag archivAuftrag, out string neueAuftragsnummer)
        {
            neueAuftragsnummer = string.Empty;
            if (archivAuftrag == null || string.IsNullOrWhiteSpace(archivAuftrag.Auftragsnummer))
                return false;

            var basis = archivAuftrag.Auftragsnummer.Trim();
            for (var i = 0; i < 50; i++)
            {
                var suffix = DateTime.Now.ToString("yyMMddHHmm");
                var candidate = i == 0 ? $"{basis}-NP-{suffix}" : $"{basis}-NP-{suffix}-{i}";

                var neuerAuftrag = new Auftrag
                {
                    Auftragsnummer = candidate,
                    Arbeitsplatz = AuftragArbeitsplatzService.Laser,
                    Status = AuftragStatus.Offen,
                    ErstelltAm = DateTime.Now,
                    GeaendertAm = DateTime.Now,
                    AngelegtVon = OperatorIdentityService.CurrentOperatorName,
                    GeaendertVon = OperatorIdentityService.CurrentOperatorName,
                    MaterialPositionen = archivAuftrag.MaterialPositionen,
                    GesamtStueckzahl = archivAuftrag.GesamtStueckzahl,
                    GesamtGewichtKg = archivAuftrag.GesamtGewichtKg,
                    PdfPfad = archivAuftrag.PdfPfad,
                    PdfPfadAngefangeneTafel = archivAuftrag.PdfPfadAngefangeneTafel,
                    ProduktionStartDatum = null,
                    ProduktionEndDatum = null,
                    IsEilt = false,
                    SortIndex = 0
                };

                if (!AuftragDataService.AddAuftrag(neuerAuftrag))
                    continue;

                AuftragArbeitsplatzService.SetArbeitsplatz(candidate, AuftragArbeitsplatzService.Laser);
                neueAuftragsnummer = candidate;
                return true;
            }

            MessageBox.Show("Nachproduktion konnte nicht erstellt werden (Auftragsnummernkonflikt).", "Laser", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private void ToggleNachproduktionModus()
        {
            _nachproduktionModus = !_nachproduktionModus;
            if (_nachproduktionModus && (_ausgewaehlteKalenderWoche < 1 || _ausgewaehlteKalenderWoche > 53))
                _ausgewaehlteKalenderWoche = ISOWeek.GetWeekOfYear(DateTime.Now);

            UpdateAuftragsKwText();
            LoadAuftraege();
        }

        private void OnToggleNachproduktionClick(object sender, RoutedEventArgs e)
        {
            ToggleNachproduktionModus();
        }

        private void SaveAllMaterials()
        {
            MaterialDataService.SaveAllMaterials(_alleMaterialien);
        }

        private void PushUndoSnapshot(string beschreibung)
        {
            UndoService.PushSnapshot($"Laser: {beschreibung}", _alleMaterialien);
        }

        private void RestoreMaterials(List<MaterialItem> materialien)
        {
            _alleMaterialien = materialien;
            _restMaterialienCache = _alleMaterialien.Where(m => !string.IsNullOrWhiteSpace(m.AuftragNr)).ToList();
            SaveAllMaterials();
            LoadMaterials();
        }

        private void ExecuteUndo()
        {
            var materialien = UndoService.Undo(_alleMaterialien);
            if (materialien == null)
            {
                MessageBox.Show("Es gibt keine Aktion zum Zur├╝cksetzen.", "Laser", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            RestoreMaterials(materialien);
        }

        private void ExecuteRedo()
        {
            var materialien = UndoService.Redo(_alleMaterialien);
            if (materialien == null)
            {
                MessageBox.Show("Es gibt keine Aktion zum Vorw├ñrtszetten.", "Laser", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            RestoreMaterials(materialien);
        }

        private void OnAuftragPdfButtonClick(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not Auftrag auftrag)
                return;

            var pdfPfad = !string.IsNullOrWhiteSpace(auftrag.PdfPfadAngefangeneTafel)
                ? auftrag.PdfPfadAngefangeneTafel
                : auftrag.PdfPfad;

            pdfPfad = AuftragArchivService.ResolveAccessiblePdfPath(auftrag.Auftragsnummer, pdfPfad);
            OpenPdfPreview(pdfPfad, "Diesem Auftrag ist keine Original-PDF zugeordnet.");
            e.Handled = true;
        }

        private void OpenPdfPreviewForAuftrag(Auftrag auftrag)
        {
            var pdfPfad = !string.IsNullOrWhiteSpace(auftrag.PdfPfadAngefangeneTafel)
                ? auftrag.PdfPfadAngefangeneTafel
                : auftrag.PdfPfad;

            pdfPfad = AuftragArchivService.ResolveAccessiblePdfPath(auftrag.Auftragsnummer, pdfPfad);
            OpenPdfPreview(pdfPfad, "Diesem Auftrag ist keine PDF-Datei zugeordnet.");
        }

        private void OpenPdfPreview(string pdfPfad, string emptyMessage)
        {
            if (string.IsNullOrWhiteSpace(pdfPfad))
            {
                MessageBox.Show(emptyMessage, "PDF-Vorschau", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!System.IO.File.Exists(pdfPfad))
            {
                MessageBox.Show($"PDF-Datei nicht gefunden:\n{pdfPfad}", "PDF-Vorschau", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new PdfPreviewDialog(pdfPfad) { Owner = this };
            dlg.ShowDialog();
        }

        private void RefreshLicenseBanner()
        {
            if (LicenseService.IsFullLicenseActive())
            {
                LicenseBannerTextBlock.Text = "Vollversion aktiv";
                LicenseBannerTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                return;
            }

            var remainingDays = LicenseService.GetRemainingTrialDays();
            var expiration = LicenseService.GetExpirationDate();
            var expiryText = expiration.HasValue ? $" (bis {expiration.Value:dd.MM.yyyy})" : string.Empty;
            LicenseBannerTextBlock.Text = $"Pilotbetrieb: {remainingDays} Tage{expiryText}";
            LicenseBannerTextBlock.Foreground = remainingDays <= 7
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC107"));
        }

        private async void ShowAutoSyncStatus()
        {
            if (AutoSyncStatusTextBlock == null)
                return;

            var version = ++_autoSyncStatusVersion;
            AutoSyncStatusTextBlock.Text = $"Daten automatisch aktualisiert ({DateTime.Now:HH:mm:ss})";
            AutoSyncStatusTextBlock.Opacity = 1;

            await Task.Delay(2500);
            if (version == _autoSyncStatusVersion)
                AutoSyncStatusTextBlock.Opacity = 0;
        }

        private void RefreshNetworkStatusBanner()
        {
            var status = NetzwerkService.GetNetzwerkStatusText();
            NetworkModeTextBlock.Text = status;
            NetworkExcelTextBlock.Text = NetzwerkService.GetExcelStatusText();

            NetworkModeTextBlock.Foreground = status.Contains("Server verbunden", StringComparison.OrdinalIgnoreCase)
                ? Brushes.LimeGreen
                : status.Contains("nicht erreichbar", StringComparison.OrdinalIgnoreCase)
                    ? Brushes.OrangeRed
                    : Brushes.DeepSkyBlue;
        }

        private void FitToWorkArea()
        {
            var wa = SystemParameters.WorkArea;
            Left = wa.Left;
            Top = wa.Top;
            Width = wa.Width;
            Height = wa.Height;
            MaxWidth = wa.Width;
            MaxHeight = wa.Height;
        }

        private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && WindowState != WindowState.Maximized)
            {
                DragMove();
            }
        }

        private string GetSelectedFilter()
        {
            var selected = (FormFilterBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (!string.IsNullOrWhiteSpace(selected))
                return selected.Trim();

            if (!string.IsNullOrWhiteSpace(FormFilterBox?.Text))
                return FormFilterBox.Text.Trim();

            return "Alle";
        }

        private void ApplyFilter()
        {
            var query = SearchBox?.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            var selectedFilter = GetSelectedFilter();

            var filtered = _restMaterialienCache.Where(m =>
            {
                var filterMatch = selectedFilter switch
                {
                    "Alle" => true,
                    "Blech" => m.Kategorie == MaterialKategorie.Blech,
                    "Rohr" => m.Kategorie == MaterialKategorie.Rohr,
                    "Profil" => m.Kategorie == MaterialKategorie.Profil,
                    "GF" or "MF" or "KF" or "Rest" => string.Equals(m.Form, selectedFilter, StringComparison.OrdinalIgnoreCase),
                    _ => true
                };

                if (!filterMatch)
                    return false;

                if (string.IsNullOrWhiteSpace(query))
                    return true;

                return (m.MaterialArt ?? string.Empty).ToLowerInvariant().Contains(query)
                    || (m.Legierung ?? string.Empty).ToLowerInvariant().Contains(query)
                    || (m.Mass ?? string.Empty).ToLowerInvariant().Contains(query)
                    || (m.Restnummer ?? string.Empty).ToLowerInvariant().Contains(query)
                    || (m.AuftragNr ?? string.Empty).ToLowerInvariant().Contains(query)
                    || (m.Form ?? string.Empty).ToLowerInvariant().Contains(query)
                    || m.Kategorie.ToString().ToLowerInvariant().Contains(query)
                    || (m.Lagerort ?? string.Empty).ToLowerInvariant().Contains(query);
            }).ToList();

            RestMaterialien.Clear();
            foreach (var item in filtered)
                RestMaterialien.Add(item);

            SummaryText = $"{RestMaterialien.Count} gebuchte Material(ien)";
        }

        private void OnAuftragKwAuswahlClick(object sender, RoutedEventArgs e)
        {
            if (!_nachproduktionModus)
                return;

            var menu = new ContextMenu
            {
                Style = (Style)FindResource("DarkContextMenuStyle"),
                ItemContainerStyle = (Style)FindResource("DarkContextMenuItemStyle")
            };

            for (var kw = 1; kw <= 53; kw++)
            {
                var istAktiv = kw == _ausgewaehlteKalenderWoche;
                var item = new MenuItem
                {
                    Header = istAktiv
                        ? $"> KW {kw:D2} ({_aktuellesJahr})"
                        : $"KW {kw:D2} ({_aktuellesJahr})",
                    Tag = kw
                };
                item.Click += OnAuftragKwAuswahlItemClick;
                menu.Items.Add(item);
            }

            if (sender is Button button)
            {
                button.ContextMenu = menu;
                menu.PlacementTarget = button;
            }

            menu.IsOpen = true;
        }

        private void OnAuftragKwAuswahlItemClick(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item || item.Tag is not int kw)
                return;

            _ausgewaehlteKalenderWoche = kw;
            UpdateAuftragsKwText();
            LoadAuftraege();
        }

        private void OnOpenPdfClick(object sender, RoutedEventArgs e)
        {
            var item = GetPrimarySelectedMaterial();
            if (item == null)
            {
                MessageBox.Show("Bitte zuerst ein Material auswählen.", "PDF-Vorschau", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            OpenPdfPreviewForItem(item);
        }

        private void OpenPdfPreviewForItem(MaterialItem item)
        {
            var pdfPfad = !string.IsNullOrWhiteSpace(item.PdfPfadAngefangeneTafel)
                ? item.PdfPfadAngefangeneTafel
                : item.PdfPfad;

            if (string.IsNullOrWhiteSpace(pdfPfad))
            {
                MessageBox.Show("Diesem Material ist keine PDF-Datei zugeordnet.", "PDF-Vorschau", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!File.Exists(pdfPfad))
            {
                MessageBox.Show($"PDF-Datei nicht gefunden:\n{pdfPfad}", "PDF-Vorschau", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new PdfPreviewDialog(pdfPfad) { Owner = this };
            dlg.ShowDialog();
        }

        private void OnGridPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dep = e.OriginalSource as DependencyObject;
            if (dep == null)
                return;

            var cell = FindVisualParent<DataGridCell>(dep);
            if (cell == null || cell.DataContext is not MaterialItem item)
                return;

            if (cell.Column is DataGridBoundColumn boundColumn
                && boundColumn.Binding is Binding binding
                && binding.Path?.Path == nameof(MaterialItem.PdfDateiname))
            {
                OpenPdfPreviewForItem(item);
                e.Handled = true;
                return;
            }

            if (RestMaterialGrid.Columns.Count == 0 || cell.Column != RestMaterialGrid.Columns[0])
                return;

            item.IsSelected = !item.IsSelected;
            RestMaterialGrid.SelectedItem = item;
            e.Handled = true;
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T t)
                    return t;
                child = VisualTreeHelper.GetParent(child);
            }

            return null;
        }

    }
}
