using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Globalization;

namespace MaterialManager_V01.Views
{
    public partial class TafelplanungWindow : Window, INotifyPropertyChanged
    {
        private List<MaterialItem> _alleMaterialien = new();
        private List<MaterialItem> _materialienCache = new();
        private readonly int _aktuellesJahr = DateTime.Now.Year;
        private int _ausgewaehlteKalenderWoche = ISOWeek.GetWeekOfYear(DateTime.Now);
        private Point _auftragDragStartPoint;
        private Auftrag? _draggedAuftrag;

        public ObservableCollection<MaterialItem> RestMaterialien { get; } = new();
        public ObservableCollection<Auftrag> AuftraegeView { get; } = new();
        public ObservableCollection<AuftragFilterItem> AuftragFilterItems { get; } = new();

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

        private string _auftragOverviewText = "Keine Aufträge geladen";
        public string AuftragOverviewText
        {
            get => _auftragOverviewText;
            set
            {
                _auftragOverviewText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AuftragOverviewText)));
            }
        }

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

        private AuftragFilterItem? _selectedAuftragFilter;
        public AuftragFilterItem? SelectedAuftragFilter
        {
            get => _selectedAuftragFilter;
            set
            {
                _selectedAuftragFilter = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedAuftragFilter)));
                ApplyFilter();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public TafelplanungWindow()
        {
            InitializeComponent();
            DataContext = this;
            HeaderText = $"Angemeldet als {OperatorIdentityService.CurrentOperatorName}";
            UpdateAuftragsKwText();
            FitToWorkArea();
            Loaded += (_, _) => LoadMaterials();
            PreviewKeyDown += OnWindowPreviewKeyDown;
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

        private async void LoadMaterials()
        {
            try
            {
                var items = await MaterialDataService.LoadAllMaterialsAsync();
                _alleMaterialien = items;
                _materialienCache = _alleMaterialien.ToList();
                RefreshAuftragFilter();
                LoadAuftraegeGridForSelectedKw();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Materialien:\n{ex.Message}", "Auftragssteuerung", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshAuftragFilter()
        {
            var bisherigeAuswahl = SelectedAuftragFilter?.Auftragsnummer ?? string.Empty;
            var auftraege = AuftragDataService.LoadAllAuftraege();

            AuftragFilterItems.Clear();
            AuftragFilterItems.Add(new AuftragFilterItem(string.Empty, "Alle Aufträge"));

            foreach (var auftrag in auftraege)
            {
                AuftragFilterItems.Add(new AuftragFilterItem(
                    auftrag.Auftragsnummer,
                    $"{auftrag.Auftragsnummer} - {auftrag.Status} ({auftrag.MaterialPositionen} Pos. / {auftrag.GesamtStueckzahl} Stk.)"));
            }

            SelectedAuftragFilter = AuftragFilterItems.FirstOrDefault(a => a.Auftragsnummer == bisherigeAuswahl)
                ?? AuftragFilterItems.FirstOrDefault();

            var offen = auftraege.Count(a => a.Status == AuftragStatus.Offen);
            var inBearbeitung = auftraege.Count(a => a.Status == AuftragStatus.InBearbeitung);
            AuftragOverviewText = auftraege.Count == 0
                ? "Keine aktiven Aufträge"
                : $"{auftraege.Count} Aufträge - {offen} offen, {inBearbeitung} in Bearbeitung";
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
            var selectedAuftrag = SelectedAuftragFilter?.Auftragsnummer ?? string.Empty;

            var filtered = _materialienCache.Where(m =>
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

                if (!string.IsNullOrWhiteSpace(selectedAuftrag) && !string.Equals(m.AuftragNr, selectedAuftrag, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (string.IsNullOrWhiteSpace(query))
                    return true;

                return (m.MaterialArt ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Legierung ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Form ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       m.Kategorie.ToString().ToLowerInvariant().Contains(query) ||
                       (m.Mass ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Restnummer ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.AuftragNr ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Lagerort ?? string.Empty).ToLowerInvariant().Contains(query);
            }).ToList();

            RestMaterialien.Clear();
            foreach (var item in filtered)
                RestMaterialien.Add(item);

            var gebucht = filtered.Count(m => !string.IsNullOrWhiteSpace(m.AuftragNr));
            SummaryText = $"{RestMaterialien.Count} Material(ien), {gebucht} gebucht";
        }

        private void SaveAllMaterials()
        {
            MaterialDataService.SaveAllMaterials(_alleMaterialien);
        }

        private void PushUndoSnapshot(string beschreibung)
        {
            UndoService.PushSnapshot($"Auftragssteuerung: {beschreibung}", _alleMaterialien);
        }

        private void RestoreMaterials(List<MaterialItem> materialien)
        {
            _alleMaterialien = materialien;
            _materialienCache = _alleMaterialien.ToList();
            SaveAllMaterials();
            LoadMaterials();
        }

        private void ExecuteUndo()
        {
            var materialien = UndoService.Undo(_alleMaterialien);
            if (materialien == null)
            {
                MessageBox.Show("Es gibt keine Aktion zum Zurücksetzen.", "Auftragssteuerung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            RestoreMaterials(materialien);
        }

        private void ExecuteRedo()
        {
            var materialien = UndoService.Redo(_alleMaterialien);
            if (materialien == null)
            {
                MessageBox.Show("Es gibt keine Aktion zum Vorwärtssetzen.", "Auftragssteuerung", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void OnFormFilterChanged(object sender, SelectionChangedEventArgs e)
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
                MessageBox.Show($"Netzwerkordner konnte nicht geöffnet werden:\n{ex.Message}", "Auftragssteuerung", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnReservedRestsClick(object sender, RoutedEventArgs e)
        {
            var dlg = new ReservierteResteDialog { Owner = this };
            dlg.ShowDialog();
            LoadMaterials();
        }

        private void OnLowStockClick(object sender, RoutedEventArgs e)
        {
            var materialien = new ObservableCollection<MaterialItem>(_alleMaterialien);
            var dlg = new NiedrigeBestaendeDialog(materialien) { Owner = this };
            dlg.ShowDialog();
        }

        private void OnOrderListClick(object sender, RoutedEventArgs e)
        {
            var dlg = new AuftragslisteWindow { Owner = this };
            var result = dlg.ShowDialog();

            RefreshAuftragFilter();
            LoadAuftraegeGridForSelectedKw();

            if (result == true && !string.IsNullOrWhiteSpace(dlg.SelectedAuftragsnummer))
            {
                SelectedAuftragFilter = AuftragFilterItems.FirstOrDefault(a =>
                    string.Equals(a.Auftragsnummer, dlg.SelectedAuftragsnummer, StringComparison.OrdinalIgnoreCase))
                    ?? AuftragFilterItems.FirstOrDefault();
                return;
            }

            ApplyFilter();
        }

        private void OnAssignToLaserClick(object sender, RoutedEventArgs e)
        {
            var auftragsnummer = GetSelectedAuftragsnummerForFreigabe();
            if (string.IsNullOrWhiteSpace(auftragsnummer))
            {
                MessageBox.Show("Bitte zuerst einen Auftrag auswählen oder ein gebuchtes Material markieren.", "Auftragssteuerung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AuftragArbeitsplatzService.SetArbeitsplatz(auftragsnummer, AuftragArbeitsplatzService.Laser);

            RefreshAuftragFilter();
            LoadAuftraegeGridForSelectedKw();
            ApplyFilter();

            MessageBox.Show($"Auftrag {auftragsnummer} wurde für den Laser freigegeben.", "Auftragssteuerung", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnAssignToKantbankClick(object sender, RoutedEventArgs e)
        {
            var auftragsnummer = GetSelectedAuftragsnummerForFreigabe();
            if (string.IsNullOrWhiteSpace(auftragsnummer))
            {
                MessageBox.Show("Bitte zuerst einen Auftrag auswählen oder ein gebuchtes Material markieren.", "Auftragssteuerung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AuftragArbeitsplatzService.SetArbeitsplatz(auftragsnummer, AuftragArbeitsplatzService.Kantbank);

            RefreshAuftragFilter();
            LoadAuftraegeGridForSelectedKw();
            ApplyFilter();

            MessageBox.Show($"Auftrag {auftragsnummer} wurde für die Kantbank freigegeben.", "Auftragssteuerung", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string GetSelectedAuftragsnummerForFreigabe()
        {
            if (AuftraegeGrid.SelectedItem is Auftrag auftrag && !string.IsNullOrWhiteSpace(auftrag.Auftragsnummer))
                return auftrag.Auftragsnummer.Trim();

            var ausMaterial = GetMarkedMaterials()
                .Select(m => m.AuftragNr)
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

            return (ausMaterial ?? string.Empty).Trim();
        }

        private void LoadAuftraegeGridForSelectedKw()
        {
            var gefilterteAuftraege = AuftragRulesService.FilterByIsoCalendarWeek(
                AuftragDataService.LoadAllAuftraege(),
                _aktuellesJahr,
                _ausgewaehlteKalenderWoche);

            AuftraegeView.Clear();
            foreach (var auftrag in gefilterteAuftraege)
                AuftraegeView.Add(auftrag);

            AuftragsKwInfoText = $"{AuftraegeView.Count} Auftrag/Aufträge in KW {_ausgewaehlteKalenderWoche:D2} ({_aktuellesJahr})";
        }

        private void UpdateAuftragsKwText()
        {
            AuftragsKwText = $"KW {_ausgewaehlteKalenderWoche:D2} ▾";
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
                        ? $"▶ KW {kw:D2} ({_aktuellesJahr})"
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
            LoadAuftraegeGridForSelectedKw();
            RefreshAuftragFilter();
            ApplyFilter();

            var archivDialog = new ArchivAuftraegeDialog(_ausgewaehlteKalenderWoche, _aktuellesJahr)
            {
                Owner = this
            };
            archivDialog.ShowDialog();
        }

        private void OnAuftraegeGridDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AuftraegeGrid.SelectedItem is not Auftrag auftrag)
                return;

            var dlg = new ProduktionVerfolgungDialog(auftrag) { Owner = this };
            dlg.ShowDialog();
            LoadAuftraegeGridForSelectedKw();
        }

        private void OnAuftragPdfButtonClick(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not Auftrag auftrag)
                return;

            var pdfPfad = !string.IsNullOrWhiteSpace(auftrag.PdfPfadAngefangeneTafel)
                ? auftrag.PdfPfadAngefangeneTafel
                : auftrag.PdfPfad;

            OpenPdfPreview(pdfPfad);
            e.Handled = true;
        }

        private void OnAuftragKantPdfButtonClick(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not Auftrag auftrag)
                return;

            if (string.IsNullOrWhiteSpace(auftrag.PdfPfadKantzeichnung))
            {
                MessageBox.Show("Für diesen Auftrag wurde keine Kant-PDF gefunden.", "PDF-Vorschau", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            OpenPdfPreview(auftrag.PdfPfadKantzeichnung);
            e.Handled = true;
        }

        private void OnSearchRestsClick(object sender, RoutedEventArgs e)
        {
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
                requireRest: false)
                .Where(m => string.IsNullOrWhiteSpace(m.AuftragNr))
                .ToList();

            foreach (var m in _alleMaterialien)
                m.IsHighlighted = gefunden.Contains(m);

            if (!gefunden.Any())
            {
                MessageBox.Show("Keine passenden Materialien gefunden.", "Material-Suche Ergebnis", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"{gefunden.Count} passende Materialien gefunden.\n\nDie Materialien sind grün markiert.",
                "Material-Suche Ergebnis", MessageBoxButton.OK, MessageBoxImage.Information);

            var auswahlDlg = new ResteAuswahlDialog(gefunden) { Owner = this };
            if (auswahlDlg.ShowDialog() != true || auswahlDlg.SelectedMaterial == null)
                return;

            var selectedMaterial = auswahlDlg.SelectedMaterial;
            var buchungDlg = new AuftragBuchungDialog(
                selectedMaterial.Stueckzahl,
                selectedMaterial.AuftragNr,
                selectedMaterial.PdfPfad,
                requirePdf: IsTafelMaterial(selectedMaterial))
            { Owner = this };

            if (buchungDlg.ShowDialog() != true)
                return;

            PushUndoSnapshot(IsTafelMaterial(selectedMaterial) ? "Tafel aus Suche buchen" : "Rest aus Suche reservieren");
            BookMaterialForOrder(selectedMaterial, buchungDlg.AuftragNr, buchungDlg.Menge, buchungDlg.PdfPfad);

            SaveAllMaterials();
            LoadMaterials();
        }

        private bool IsTafelMaterial(MaterialItem item)
        {
            return item.Form == "GF" || item.Form == "MF" || item.Form == "KF";
        }

        private void OnBookForOrderClick(object sender, RoutedEventArgs e)
        {
            var items = GetMarkedMaterials().Where(m => string.IsNullOrWhiteSpace(m.AuftragNr)).ToList();
            if (items.Count == 0)
            {
                var verfuegbareMaterialien = _alleMaterialien
                    .Where(m => string.IsNullOrWhiteSpace(m.AuftragNr) && m.Stueckzahl > 0)
                    .OrderBy(m => m.Kategorie)
                    .ThenBy(m => m.MaterialArt)
                    .ThenBy(m => m.Legierung)
                    .ThenBy(m => m.Form)
                    .ThenBy(m => m.Mass)
                    .ToList();

                if (verfuegbareMaterialien.Count == 0)
                {
                    MessageBox.Show("Es sind keine verfügbaren Materialien für eine Reservierung vorhanden.", "Auftragssteuerung", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var auswahlDlg = new ResteAuswahlDialog(verfuegbareMaterialien) { Owner = this };
                if (auswahlDlg.ShowDialog() != true || auswahlDlg.SelectedMaterial == null)
                    return;

                items.Add(auswahlDlg.SelectedMaterial);
            }

            string auftragNrForLog = string.Empty;
            var gebuchteMenge = 0;

            if (items.Count == 1)
            {
                var item = items[0];
                var dlg = new AuftragBuchungDialog(item.Stueckzahl, item.AuftragNr, item.PdfPfad, requirePdf: false) { Owner = this };
                if (dlg.ShowDialog() != true)
                    return;

                PushUndoSnapshot("Für Auftrag buchen");
                BookMaterialForOrder(item, dlg.AuftragNr, dlg.Menge, dlg.PdfPfad);
                auftragNrForLog = dlg.AuftragNr;
                gebuchteMenge = dlg.Menge;
            }
            else
            {
                var dlg = new ResteReservierungDialog(string.Empty) { Owner = this };
                if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.AuftragNr))
                    return;

                PushUndoSnapshot("Mehrere Materialien für Auftrag buchen");
                foreach (var item in items.ToList())
                {
                    BookMaterialForOrder(item, dlg.AuftragNr.Trim(), item.Stueckzahl, string.Empty);
                    gebuchteMenge += item.Stueckzahl;
                }
                auftragNrForLog = dlg.AuftragNr.Trim();
            }

            AuditLogService.LogAction(
                OperatorIdentityService.CurrentOperatorName,
                "RESERVE",
                "MaterialItem",
                string.IsNullOrWhiteSpace(auftragNrForLog) ? "MULTI" : auftragNrForLog,
                oldValue: "Verfügbar",
                newValue: $"Gebucht für Auftrag {auftragNrForLog}, Stück: {gebuchteMenge}",
                reason: $"Reservierung in Auftragssteuerung ({items.Count} Positionen)");

            if (!string.IsNullOrWhiteSpace(auftragNrForLog))
                AuftragArbeitsplatzService.SetDefaultArbeitsplatzIfMissing(auftragNrForLog, AuftragArbeitsplatzService.Laser);

            SaveAllMaterials();
            LoadMaterials();
        }

        private void OnReleaseReservationClick(object sender, RoutedEventArgs e)
        {
            var items = GetMarkedMaterials().Where(m => !string.IsNullOrWhiteSpace(m.AuftragNr)).ToList();
            if (items.Count == 0)
            {
                MessageBox.Show("Bitte gebuchte Materialien auswählen oder markieren.", "Auftragssteuerung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var auftragsNummern = items.Select(i => i.AuftragNr).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().ToList();

            var confirm = MessageBox.Show(
                items.Count == 1
                    ? $"Reservierung für '{items[0].MaterialArt} {items[0].Mass}' aufheben?"
                    : $"Reservierung für {items.Count} markierte Materialien aufheben?",
                "Auftragssteuerung",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            PushUndoSnapshot(items.Count == 1 ? "Reservierung aufheben" : "Reservierungen aufheben");
            foreach (var item in items.ToList())
            {
                if (IsAngefangeneTafel(item))
                {
                    var restoredStartedPlate = CloneMaterial(item);
                    restoredStartedPlate.AuftragNr = string.Empty;
                    restoredStartedPlate.Lagerort = "Angefangene Tafel";
                    restoredStartedPlate.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
                    restoredStartedPlate.AenderungsDatum = DateTime.Now;
                    restoredStartedPlate.IsSelected = false;
                    _alleMaterialien.Add(restoredStartedPlate);
                    _alleMaterialien.Remove(item);
                    continue;
                }

                var existing = FindAvailableMaterial(item);
                if (existing != null)
                {
                    existing.Stueckzahl += item.Stueckzahl;
                    existing.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
                    existing.AenderungsDatum = DateTime.Now;
                    if (string.IsNullOrWhiteSpace(existing.PdfPfad))
                        existing.PdfPfad = item.PdfPfad;
                }
                else
                {
                    var restored = CloneMaterial(item);
                    restored.AuftragNr = string.Empty;
                    restored.PdfPfadAngefangeneTafel = string.Empty;
                    restored.Lagerort = RegalService.DetermineLagerort(restored.MaterialArt, restored.Legierung, restored.Form, restored.Staerke, restored.Mass, _alleMaterialien);
                    restored.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
                    restored.AenderungsDatum = DateTime.Now;
                    restored.IsSelected = false;
                    _alleMaterialien.Add(restored);
                }

                _alleMaterialien.Remove(item);
            }

            AuditLogService.LogAction(
                OperatorIdentityService.CurrentOperatorName,
                "RELEASE",
                "MaterialItem",
                string.Join(",", auftragsNummern),
                oldValue: "Reserviert",
                newValue: "Verfügbar",
                reason: $"Reservierung aufgehoben ({items.Count} Positionen)");

            SaveAllMaterials();
            LoadMaterials();
        }

        private void OnCompleteProductionClick(object sender, RoutedEventArgs e)
        {
            var items = GetMarkedMaterials().Where(m => !string.IsNullOrWhiteSpace(m.AuftragNr)).ToList();
            if (items.Count == 0)
            {
                MessageBox.Show("Bitte gebuchte Materialien auswählen oder markieren.", "Auftragssteuerung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var auftragsNummern = items.Select(i => i.AuftragNr).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().ToList();

            var confirm = MessageBox.Show(
                items.Count == 1
                    ? $"Produktion für '{items[0].MaterialArt} {items[0].Mass}' abschließen und gebuchter Material entfernen?"
                    : $"Produktion für {items.Count} markierte Materialien abschließen und gebuchtes Material entfernen?",
                "Auftragssteuerung",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            PushUndoSnapshot(items.Count == 1 ? "Produktion abschließen" : "Produktionen abschließen");
            foreach (var item in items.ToList())
            {
                BuchungsService.BucheAusgang(item, item.AuftragNr, OperatorIdentityService.CurrentOperatorName);
                _alleMaterialien.Remove(item);
            }

            AuditLogService.LogAction(
                OperatorIdentityService.CurrentOperatorName,
                "COMPLETE",
                "Auftrag",
                string.Join(",", auftragsNummern),
                oldValue: "Reserviert/In Bearbeitung",
                newValue: "Abgeschlossen",
                reason: $"Produktion abgeschlossen ({items.Count} Materialpositionen)");

            SaveAllMaterials();
            LoadMaterials();
        }

        private MaterialItem? FindAvailableMaterial(MaterialItem bookedItem)
        {
            return _alleMaterialien.FirstOrDefault(m =>
                string.IsNullOrWhiteSpace(m.AuftragNr) &&
                !IsAngefangeneTafel(m) &&
                m.MaterialArt == bookedItem.MaterialArt &&
                m.Legierung == bookedItem.Legierung &&
                m.Oberflaeche == bookedItem.Oberflaeche &&
                m.Guete == bookedItem.Guete &&
                m.Form == bookedItem.Form &&
                Math.Abs(m.Staerke - bookedItem.Staerke) < 0.0001 &&
                m.Mass == bookedItem.Mass);
        }

        private static bool IsAngefangeneTafel(MaterialItem item)
        {
            return item.Kategorie == MaterialKategorie.Blech
                && (string.Equals(item.Lagerort, "Angefangene Tafel", StringComparison.OrdinalIgnoreCase)
                    || !string.IsNullOrWhiteSpace(item.PdfPfadAngefangeneTafel));
        }

        private static MaterialItem CloneMaterial(MaterialItem source)
        {
            return new MaterialItem
            {
                MaterialArt = source.MaterialArt,
                Legierung = source.Legierung,
                Oberflaeche = source.Oberflaeche,
                Guete = source.Guete,
                Form = source.Form,
                Staerke = source.Staerke,
                Mass = source.Mass,
                Stueckzahl = source.Stueckzahl,
                Restnummer = source.Restnummer,
                Datum = source.Datum,
                AenderungsDatum = source.AenderungsDatum,
                Lagerort = source.Lagerort,
                AngelegtVon = source.AngelegtVon,
                GeaendertVon = source.GeaendertVon,
                Lieferant = source.Lieferant,
                LieferscheinNr = source.LieferscheinNr,
                AuftragNr = source.AuftragNr,
                PdfPfad = source.PdfPfad,
                PdfPfadAngefangeneTafel = source.PdfPfadAngefangeneTafel
            };
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            var startWindow = new StartModeWindow();
            Application.Current.MainWindow = startWindow;
            startWindow.Show();
            Close();
        }

        private void OnOpenMainProgramClick(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
            Close();
        }

        private void OnGridPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dep = e.OriginalSource as DependencyObject;
            if (dep == null) return;

            var cell = FindVisualParent<DataGridCell>(dep);
            if (cell == null) return;

            if (cell.DataContext is not MaterialItem item) return;

            if (cell.Column is DataGridBoundColumn boundColumn &&
                boundColumn.Binding is Binding binding)
            {
                if (binding.Path?.Path == nameof(MaterialItem.PdfDateiname))
                {
                    OpenPdfPreview(item.PdfPfad);
                    e.Handled = true;
                    return;
                }

                if (binding.Path?.Path == nameof(MaterialItem.PdfDateinameAngefangeneTafel))
                {
                    OpenPdfPreview(item.PdfPfadAngefangeneTafel);
                    e.Handled = true;
                    return;
                }
            }

            if (RestMaterialGrid.Columns.Count == 0 || cell.Column != RestMaterialGrid.Columns[0]) return;

            item.IsSelected = !item.IsSelected;
            RestMaterialGrid.SelectedItem = item;
            e.Handled = true;
        }

        private void OpenPdfPreview(string? pdfPfad)
        {
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

        private void OnEditRestClick(object sender, RoutedEventArgs e)
        {
            var item = GetPrimarySelectedMaterial();
            if (item == null)
                return;

            var dlg = new MaterialDialog(_alleMaterialien) { Owner = this };
            dlg.SetEditMode(item);
            if (dlg.ShowDialog() != true)
                return;

            if (!EnsureAngefangeneTafelPdf(item, dlg.Material))
                return;

            var index = _alleMaterialien.IndexOf(item);
            if (index < 0)
                return;

            PushUndoSnapshot("Material bearbeiten");
            if (!string.IsNullOrWhiteSpace(item.AuftragNr) && item.Kategorie == MaterialKategorie.Blech)
                dlg.Material.Lagerort = "Angefangene Tafel";
            dlg.Material.IsSelected = false;
            _alleMaterialien[index] = dlg.Material;
            SaveAllMaterials();
            LoadMaterials();
        }

        private void OnGridMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OnEditRestClick(sender, e);
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

        private void OnAttachPdfClick(object sender, RoutedEventArgs e)
        {
            var item = GetPrimarySelectedMaterial();
            if (item == null)
            {
                MessageBox.Show("Bitte zuerst ein Material auswählen.", "PDF anhängen", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var title = GetPdfAttachTitle(item);
            var aktuellerPfad = title == "PDF-Datei für angefangene Tafel auswählen" ? item.PdfPfadAngefangeneTafel : item.PdfPfad;
            var pdfPfad = WaehlePdfDatei(title, aktuellerPfad);
            if (string.IsNullOrWhiteSpace(pdfPfad))
                return;

            PushUndoSnapshot("PDF anhängen");
            if (title == "PDF-Datei für angefangene Tafel auswählen")
                item.PdfPfadAngefangeneTafel = pdfPfad;
            else
                item.PdfPfad = pdfPfad;

            item.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
            item.AenderungsDatum = DateTime.Now;
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

        private void OnDeleteMaterialClick(object sender, RoutedEventArgs e)
        {
            var items = GetMarkedMaterials();
            if (items.Count == 0)
            {
                MessageBox.Show("Bitte zuerst Material auswählen oder markieren.", "Auftragssteuerung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                items.Count == 1
                    ? $"Material '{items[0].MaterialArt} {items[0].Mass}' wirklich löschen?"
                    : $"{items.Count} markierte Materialien wirklich löschen?",
                "Auftragssteuerung",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            PushUndoSnapshot(items.Count == 1 ? "Material löschen" : "Materialien löschen");
            foreach (var item in items.ToList())
                _alleMaterialien.Remove(item);

            SaveAllMaterials();
            LoadMaterials();
        }

        private string? WaehlePdfDatei(string titel, string vorhandenerPfad = "")
        {
            var dlg = new OpenFileDialog
            {
                Title = titel,
                Filter = "PDF-Dateien (*.pdf)|*.pdf|Alle Dateien (*.*)|*.*",
                CheckFileExists = true
            };

            if (!string.IsNullOrWhiteSpace(vorhandenerPfad) && System.IO.File.Exists(vorhandenerPfad))
                dlg.InitialDirectory = System.IO.Path.GetDirectoryName(vorhandenerPfad);

            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

        private bool EnsureAngefangeneTafelPdf(MaterialItem originalItem, MaterialItem editedItem)
        {
            if (string.IsNullOrWhiteSpace(originalItem.AuftragNr) || originalItem.Kategorie != MaterialKategorie.Blech)
                return true;

            if (!string.IsNullOrWhiteSpace(editedItem.PdfPfadAngefangeneTafel))
                return true;

            var pdfPfad = WaehlePdfDatei("PDF-Datei für angefangene Tafel auswählen", originalItem.PdfPfad);
            if (string.IsNullOrWhiteSpace(pdfPfad))
            {
                MessageBox.Show("Bitte eine PDF-Datei für die angefangene Tafel auswählen.", "Angefangene Tafel", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            editedItem.PdfPfadAngefangeneTafel = pdfPfad;
            return true;
        }

        private string GetPdfAttachTitle(MaterialItem item)
        {
            return !string.IsNullOrWhiteSpace(item.AuftragNr) && item.Kategorie == MaterialKategorie.Blech && !string.IsNullOrWhiteSpace(item.PdfPfad)
                ? "PDF-Datei für angefangene Tafel auswählen"
                : "PDF-Datei für Auftrag auswählen";
        }

        private void BookMaterialForOrder(MaterialItem item, string auftragNr, int menge, string pdfPfad = "")
        {
            if (string.IsNullOrWhiteSpace(auftragNr) || menge <= 0 || menge > item.Stueckzahl)
                return;

            var bookedItem = CloneMaterial(item);
            bookedItem.Stueckzahl = menge;
            bookedItem.AuftragNr = auftragNr.Trim();
            bookedItem.PdfPfad = string.IsNullOrWhiteSpace(pdfPfad) ? bookedItem.PdfPfad : pdfPfad.Trim();
            bookedItem.Lagerort = "Gebucht";
            bookedItem.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
            bookedItem.AenderungsDatum = DateTime.Now;
            bookedItem.IsSelected = false;

            item.Stueckzahl -= menge;
            item.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
            item.AenderungsDatum = DateTime.Now;
            item.IsSelected = false;

            if (item.Stueckzahl <= 0)
                _alleMaterialien.Remove(item);

            _alleMaterialien.Add(bookedItem);
        }

        private Auftrag? GetSelectedAuftrag()
        {
            return AuftraegeGrid.SelectedItem as Auftrag;
        }

        private List<Auftrag> GetCurrentWeekAuftraegeSorted()
        {
            var alle = AuftragDataService.LoadAllAuftraege();
            return AuftragRulesService.FilterByIsoCalendarWeek(alle, _aktuellesJahr, _ausgewaehlteKalenderWoche)
                .OrderBy(a => a.SortIndex)
                .ThenBy(a => a.Auftragsnummer)
                .ToList();
        }

        private void PersistWeekSortOrder(List<Auftrag> weekOrders)
        {
            for (var i = 0; i < weekOrders.Count; i++)
            {
                var zielSortIndex = (i + 1) * 10;
                var nr = weekOrders[i].Auftragsnummer;
                AuftragDataService.UpdateAuftrag(nr, a =>
                {
                    a.SortIndex = zielSortIndex;
                    a.GeaendertAm = DateTime.Now;
                    a.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
                });
            }
        }

        private void ReloadAuftraegeKeepingSelection(string? auftragsnummer)
        {
            RefreshAuftragFilter();
            LoadAuftraegeGridForSelectedKw();
            if (!string.IsNullOrWhiteSpace(auftragsnummer))
            {
                var selected = AuftraegeView.FirstOrDefault(a => string.Equals(a.Auftragsnummer, auftragsnummer, StringComparison.OrdinalIgnoreCase));
                if (selected != null)
                    AuftraegeGrid.SelectedItem = selected;
            }
            ApplyFilter();
        }

        private void OnMoveAuftragUpClick(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedAuftrag();
            if (selected == null)
            {
                MessageBox.Show("Bitte zuerst einen Auftrag auswählen.", "Auftragssteuerung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var list = GetCurrentWeekAuftraegeSorted();
            var idx = list.FindIndex(a => string.Equals(a.Auftragsnummer, selected.Auftragsnummer, StringComparison.OrdinalIgnoreCase));
            if (idx <= 0)
                return;

            (list[idx - 1], list[idx]) = (list[idx], list[idx - 1]);
            PersistWeekSortOrder(list);
            ReloadAuftraegeKeepingSelection(selected.Auftragsnummer);
        }

        private void OnMoveAuftragDownClick(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedAuftrag();
            if (selected == null)
            {
                MessageBox.Show("Bitte zuerst einen Auftrag auswählen.", "Auftragssteuerung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var list = GetCurrentWeekAuftraegeSorted();
            var idx = list.FindIndex(a => string.Equals(a.Auftragsnummer, selected.Auftragsnummer, StringComparison.OrdinalIgnoreCase));
            if (idx < 0 || idx >= list.Count - 1)
                return;

            (list[idx], list[idx + 1]) = (list[idx + 1], list[idx]);
            PersistWeekSortOrder(list);
            ReloadAuftraegeKeepingSelection(selected.Auftragsnummer);
        }

        private void OnMarkEiltClick(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedAuftrag();
            if (selected == null)
            {
                MessageBox.Show("Bitte zuerst einen Auftrag auswählen.", "Auftragssteuerung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AuftragDataService.UpdateAuftrag(selected.Auftragsnummer, a =>
            {
                a.IsEilt = true;
                a.GeaendertAm = DateTime.Now;
                a.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
            });
            ReloadAuftraegeKeepingSelection(selected.Auftragsnummer);
        }

        private void OnUnmarkEiltClick(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedAuftrag();
            if (selected == null)
            {
                MessageBox.Show("Bitte zuerst einen Auftrag auswählen.", "Auftragssteuerung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AuftragDataService.UpdateAuftrag(selected.Auftragsnummer, a =>
            {
                a.IsEilt = false;
                a.GeaendertAm = DateTime.Now;
                a.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
            });
            ReloadAuftraegeKeepingSelection(selected.Auftragsnummer);
        }

        private void OnStartNowClick(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedAuftrag();
            if (selected == null)
            {
                MessageBox.Show("Bitte zuerst einen Auftrag auswählen.", "Auftragssteuerung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AuftragDataService.UpdateAuftrag(selected.Auftragsnummer, a =>
            {
                a.ProduktionStartDatum ??= DateTime.Now;
                a.Status = AuftragStatus.InBearbeitung;
                a.GeaendertAm = DateTime.Now;
                a.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
            });
            ReloadAuftraegeKeepingSelection(selected.Auftragsnummer);
        }

        private void OnEndNowClick(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedAuftrag();
            if (selected == null)
            {
                MessageBox.Show("Bitte zuerst einen Auftrag auswählen.", "Auftragssteuerung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AuftragDataService.UpdateAuftrag(selected.Auftragsnummer, a =>
            {
                a.ProduktionStartDatum ??= DateTime.Now;
                a.ProduktionEndDatum = DateTime.Now;
                a.Status = AuftragStatus.Abgeschlossen;
                a.GeaendertAm = DateTime.Now;
                a.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
            });
            ReloadAuftraegeKeepingSelection(selected.Auftragsnummer);
        }

        private void OnAuftraegeGridPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _auftragDragStartPoint = e.GetPosition(null);
            _draggedAuftrag = null;

            if (e.OriginalSource is not DependencyObject dep)
                return;

            var row = FindVisualParent<DataGridRow>(dep);
            if (row?.Item is Auftrag auftrag)
                _draggedAuftrag = auftrag;
        }

        private void OnAuftraegeGridMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _draggedAuftrag == null)
                return;

            var current = e.GetPosition(null);
            if (Math.Abs(current.X - _auftragDragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(current.Y - _auftragDragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            var dragData = new DataObject(typeof(Auftrag), _draggedAuftrag);
            DragDrop.DoDragDrop(AuftraegeGrid, dragData, DragDropEffects.Move);
        }

        private void OnAuftraegeGridDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(typeof(Auftrag)) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnAuftraegeGridDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(Auftrag)))
                return;

            var source = e.Data.GetData(typeof(Auftrag)) as Auftrag;
            if (source == null)
                return;

            if (e.OriginalSource is not DependencyObject dep)
                return;

            var row = FindVisualParent<DataGridRow>(dep);
            var target = row?.Item as Auftrag;
            if (target == null || string.Equals(target.Auftragsnummer, source.Auftragsnummer, StringComparison.OrdinalIgnoreCase))
                return;

            var list = GetCurrentWeekAuftraegeSorted();
            var srcIndex = list.FindIndex(a => string.Equals(a.Auftragsnummer, source.Auftragsnummer, StringComparison.OrdinalIgnoreCase));
            var dstIndex = list.FindIndex(a => string.Equals(a.Auftragsnummer, target.Auftragsnummer, StringComparison.OrdinalIgnoreCase));
            if (srcIndex < 0 || dstIndex < 0 || srcIndex == dstIndex)
                return;

            var moved = list[srcIndex];
            list.RemoveAt(srcIndex);
            list.Insert(dstIndex, moved);

            PersistWeekSortOrder(list);
            ReloadAuftraegeKeepingSelection(source.Auftragsnummer);
        }
    }

    public sealed class AuftragFilterItem
    {
        public AuftragFilterItem(string auftragsnummer, string displayText)
        {
            Auftragsnummer = auftragsnummer;
            DisplayText = displayText;
        }

        public string Auftragsnummer { get; }
        public string DisplayText { get; }
    }
}