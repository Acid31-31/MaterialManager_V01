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

        public ObservableCollection<MaterialItem> RestMaterialien { get; } = new();
        public ObservableCollection<Auftrag> AuftraegeView { get; } = new();

        private string _workspaceTitle = "Laser – Auftragsübersicht";
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

        public LaserDemoWindow()
        {
            InitializeComponent();
            DataContext = this;
            WorkspaceTitle = "Laser – Auftragsübersicht";
            HeaderText = $"Angemeldet als {OperatorIdentityService.CurrentOperatorName} – Produktionssicht";
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
                _auftraegeCache = AuftragDataService.LoadAllAuftraege()
                    .Where(a => AuftragArbeitsplatzService.IsMatchForBereich(a.Arbeitsplatz, Arbeitsbereich))
                    .ToList();
                ApplyAuftragsKwFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Aufträge:\n{ex.Message}", "Laser", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyAuftragsKwFilter()
        {
            var gefilterteAuftraege = AuftragRulesService.FilterByIsoCalendarWeek(_auftraegeCache, _aktuellesJahr, _ausgewaehlteKalenderWoche);

            AuftraegeView.Clear();
            foreach (var auftrag in gefilterteAuftraege)
                AuftraegeView.Add(auftrag);

            AuftragsKwInfoText = $"{AuftraegeView.Count} Auftrag/Aufträge in KW {_ausgewaehlteKalenderWoche:D2} ({_aktuellesJahr})";
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

                return (m.MaterialArt ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Legierung ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Mass ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Restnummer ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.AuftragNr ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Form ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       m.Kategorie.ToString().ToLowerInvariant().Contains(query) ||
                       (m.Lagerort ?? string.Empty).ToLowerInvariant().Contains(query);
            }).ToList();

            RestMaterialien.Clear();
            foreach (var item in filtered)
                RestMaterialien.Add(item);

            SummaryText = $"{RestMaterialien.Count} gebuchte Material(ien)";
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
                MessageBox.Show("Es gibt keine Aktion zum Zurücksetzen.", "Laser", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            RestoreMaterials(materialien);
        }

        private void ExecuteRedo()
        {
            var materialien = UndoService.Redo(_alleMaterialien);
            if (materialien == null)
            {
                MessageBox.Show("Es gibt keine Aktion zum Vorwärtszetten.", "Laser", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            RestoreMaterials(materialien);
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
                MessageBox.Show($"Netzwerkordner konnte nicht geöffnet werden:\n{ex.Message}", "Laser", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                MessageBox.Show("Bitte zuerst reserviertes Material auswählen oder markieren.", "Laser", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirmDialog = new BestaetigungsDialog(
                "Reserviertes Material löschen",
                items.Count == 1
                    ? $"Reserviertes Material '{items[0].MaterialArt} {items[0].Mass}' wirklich löschen?"
                    : $"{items.Count} reservierte Materialien wirklich löschen?",
                confirmText: "Löschen",
                cancelText: "Abbrechen",
                confirmColorHex: "#8B1E1E")
            { Owner = this };

            if (confirmDialog.ShowDialog() != true)
                return;

            PushUndoSnapshot(items.Count == 1 ? "Reserviertes Material löschen" : "Reservierte Materialien löschen");
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
                    MessageBox.Show($"Update-Prüfung fehlgeschlagen:\n{result.ErrorMessage}", "Update", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            var dlg = new ProduktionVerfolgungDialog(auftrag) { Owner = this };
            dlg.ShowDialog();
            LoadAuftraege();
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

        private void UpdateAuftragsKwText()
        {
            AuftragsKwText = $"KW {_ausgewaehlteKalenderWoche:D2} â–¾";
        }

        private void OnAuftragKwAuswahlClick(object sender, RoutedEventArgs e)
        {
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
                        ? $"â–¶ KW {kw:D2} ({_aktuellesJahr})"
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
            ApplyAuftragsKwFilter();

            var archivDialog = new ArchivAuftraegeDialog(_ausgewaehlteKalenderWoche, _aktuellesJahr)
            {
                Owner = this
            };
            archivDialog.ShowDialog();
        }

        private void OnGridPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dep = e.OriginalSource as DependencyObject;
            if (dep == null) return;

            var cell = FindVisualParent<DataGridCell>(dep);
            if (cell == null) return;

            if (cell.DataContext is not MaterialItem item) return;

            if (cell.Column is DataGridBoundColumn boundColumn &&
                boundColumn.Binding is Binding binding &&
                binding.Path?.Path == nameof(MaterialItem.PdfDateiname))
            {
                OpenPdfPreviewForItem(item);
                e.Handled = true;
                return;
            }

            if (RestMaterialGrid.Columns.Count == 0 || cell.Column != RestMaterialGrid.Columns[0]) return;

            item.IsSelected = !item.IsSelected;
            RestMaterialGrid.SelectedItem = item;
            e.Handled = true;
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T t) return t;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
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
                    : Brushes.Gray;
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
    }
}
