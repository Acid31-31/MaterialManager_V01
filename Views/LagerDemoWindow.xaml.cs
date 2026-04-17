using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MaterialManager_V01.Models;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class LagerDemoWindow : Window, INotifyPropertyChanged
    {
        private sealed class MaterialSelectionSnapshot
        {
            public HashSet<string> CheckedKeys { get; } = new(StringComparer.Ordinal);
            public string? SelectedKey { get; set; }
        }

        private List<MaterialItem> _alleMaterialien = new();
        private DateTime _lastAutoReloadUtc = DateTime.MinValue;
        private DateTime _lastObservedMaterialWriteUtc = DateTime.MinValue;
        private DateTime _lastObservedAuftraegeWriteUtc = DateTime.MinValue;
        private readonly DispatcherTimer _autoRefreshTimer = new() { Interval = TimeSpan.FromSeconds(2) };
        private int _autoSyncStatusVersion;

        public ObservableCollection<MaterialItem> GefilterteMaterialien { get; } = new();

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

        public event PropertyChangedEventHandler? PropertyChanged;

        public LagerDemoWindow()
        {
            InitializeComponent();
            DataContext = this;
            HeaderText = $"Angemeldet als {OperatorIdentityService.CurrentOperatorName} – Lager";
            RefreshLicenseBanner();
            RefreshNetworkStatusBanner();
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

        private void LoadMaterials()
        {
            var selectionSnapshot = CaptureMaterialSelection();

            try
            {
                _alleMaterialien = MaterialDataService.LoadAllMaterials();
                RefreshManualFilterOptions();
                ApplyFilter();
                RestoreMaterialSelection(selectionSnapshot);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Materialien:\n{ex.Message}", "Lager", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private MaterialSelectionSnapshot CaptureMaterialSelection()
        {
            var snapshot = new MaterialSelectionSnapshot();

            foreach (var item in GefilterteMaterialien.Where(m => m.IsSelected))
                snapshot.CheckedKeys.Add(GetMaterialSelectionKey(item));

            if (MaterialGrid?.SelectedItem is MaterialItem selectedItem)
                snapshot.SelectedKey = GetMaterialSelectionKey(selectedItem);

            return snapshot;
        }

        private void RestoreMaterialSelection(MaterialSelectionSnapshot snapshot)
        {
            MaterialItem? selectedItem = null;

            foreach (var item in GefilterteMaterialien)
            {
                var key = GetMaterialSelectionKey(item);
                item.IsSelected = snapshot.CheckedKeys.Contains(key);
                if (snapshot.SelectedKey == key)
                    selectedItem = item;
            }

            if (MaterialGrid != null)
                MaterialGrid.SelectedItem = selectedItem ?? GefilterteMaterialien.FirstOrDefault(m => m.IsSelected);
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

        private static string NormalizeFilterValue(string? value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static bool ContainsFilter(string? source, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            return (source ?? string.Empty)
                .ToLowerInvariant()
                .Contains(filter);
        }

        private static bool ContainsNumericFilter(double value, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            var numeric = value.ToString("0.###", CultureInfo.InvariantCulture).ToLowerInvariant();
            var filterNormalized = filter.Replace(',', '.');
            return numeric.Contains(filterNormalized);
        }

        private static bool MatchesNumericExactFilter(double value, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            var normalized = filter.Replace(',', '.').Trim();

            // Sonderfall: Eingabe "0" soll alle Stärken < 1 mm zeigen (z. B. 0.2, 0.3, 0.5, 0.8)
            if (string.Equals(normalized, "0", StringComparison.Ordinal))
                return value > 0 && value < 1;

            if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var target))
                return false;

            return Math.Abs(value - target) < 0.0001;
        }

        private static bool ContainsIntFilter(int value, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            return value.ToString(CultureInfo.InvariantCulture).Contains(filter);
        }

        private static bool MatchesMassFilter(string? mass, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            var source = (mass ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(source))
                return false;

            var normalizedFilter = filter.Replace(',', '.').Trim().ToLowerInvariant();

            if (normalizedFilter.Contains('x') || normalizedFilter.Contains('×'))
            {
                var normalizedSource = source.Replace('×', 'x');
                var nf = normalizedFilter.Replace('×', 'x');
                return normalizedSource.Contains(nf, StringComparison.OrdinalIgnoreCase);
            }

            var parts = source
                .Replace('×', 'x')
                .Split(new[] { 'x', '*', '/', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();

            if (parts.Count == 0)
                return false;

            return parts.Any(p => p.StartsWith(normalizedFilter, StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryParseMass(string? mass, out double minSide, out double maxSide)
        {
            minSide = 0;
            maxSide = 0;

            if (string.IsNullOrWhiteSpace(mass))
                return false;

            var normalized = mass
                .ToLowerInvariant()
                .Replace("mm", string.Empty)
                .Replace('×', 'x')
                .Replace(',', '.')
                .Trim();

            var parts = normalized.Split(new[] { 'x' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();

            if (parts.Length != 2)
                return false;

            if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var a))
                return false;
            if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var b))
                return false;

            minSide = Math.Min(a, b);
            maxSide = Math.Max(a, b);
            return true;
        }

        private static double GetMassArea(double minSide, double maxSide)
        {
            return minSide * maxSide;
        }

        private static bool MatchesSelection(string? source, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            return string.Equals((source ?? string.Empty).Trim(), filter, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetComboFilterValue(ComboBox? comboBox)
        {
            var value = comboBox?.SelectedItem switch
            {
                ComboBoxItem item => item.Content?.ToString(),
                string text => text,
                _ => comboBox?.Text
            };

            value = (value ?? string.Empty).Trim();
            return string.Equals(value, "Alle", StringComparison.OrdinalIgnoreCase) ? string.Empty : value;
        }

        private static ComboBoxItem CreateFilterComboItem(string content)
        {
            return new ComboBoxItem
            {
                Content = content,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#102018")),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D2D8CF"))
            };
        }

        private static void PopulateFilterComboBox(ComboBox? comboBox, IEnumerable<string> values, string selectedValue)
        {
            if (comboBox == null)
                return;

            comboBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#102018"));
            comboBox.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D2D8CF"));
            comboBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F8A7F"));

            comboBox.Items.Clear();
            comboBox.Items.Add(CreateFilterComboItem("Alle"));

            foreach (var value in values.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase))
                comboBox.Items.Add(CreateFilterComboItem(value));

            var target = string.IsNullOrWhiteSpace(selectedValue) ? "Alle" : selectedValue;
            comboBox.SelectedItem = comboBox.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(i => string.Equals(i.Content?.ToString(), target, StringComparison.OrdinalIgnoreCase));

            comboBox.SelectedIndex = comboBox.SelectedIndex >= 0 ? comboBox.SelectedIndex : 0;
        }

        private void RefreshManualFilterOptions()
        {
            var selectedMaterial = GetComboFilterValue(FilterMaterialBox);
            var selectedStaerke = GetComboFilterValue(FilterStaerkeBox);

            var materialien = _alleMaterialien
                .Select(m => (m.MaterialArt ?? string.Empty).Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .OrderBy(v => v, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            var staerken = _alleMaterialien
                .Where(m => m.Staerke > 0)
                .Select(m => m.Staerke.ToString("0.###", CultureInfo.InvariantCulture).Replace('.', ','))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => double.TryParse(v.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : double.MaxValue)
                .ToList();

            PopulateFilterComboBox(FilterMaterialBox, materialien, selectedMaterial);
            PopulateFilterComboBox(FilterStaerkeBox, staerken, selectedStaerke);
        }

        private void ApplyFilter()
        {
            var fMaterial = NormalizeFilterValue(GetComboFilterValue(FilterMaterialBox));
            var fStaerke = NormalizeFilterValue(GetComboFilterValue(FilterStaerkeBox));
            var fMass = NormalizeFilterValue(FilterMassBox?.Text);
            var fLaenge = NormalizeFilterValue(FilterLaengeBox?.Text);
            var fLagerort = NormalizeFilterValue(FilterLagerortBox?.Text);
            var fRestnummer = NormalizeFilterValue(FilterRestnummerBox?.Text);
            var fAuftrag = NormalizeFilterValue(FilterAuftragBox?.Text);

            var baseFiltered = _alleMaterialien.Where(m =>
                MatchesSelection(m.MaterialArt, fMaterial)
                && MatchesNumericExactFilter(m.Staerke, fStaerke)
                && ContainsNumericFilter(m.Laenge, fLaenge)
                && ContainsFilter(m.Lagerort, fLagerort)
                && ContainsFilter(m.Restnummer, fRestnummer)
                && ContainsFilter(m.AuftragNr, fAuftrag)
            );

            List<MaterialItem> filtered;

            if (TryParseMass(fMass, out var queryMin, out var queryMax))
            {
                var parsedItems = baseFiltered
                    .Select(m =>
                    {
                        var ok = TryParseMass(m.Mass, out var minSide, out var maxSide);
                        return new { Item = m, Ok = ok, MinSide = minSide, MaxSide = maxSide };
                    })
                    .Where(x => x.Ok)
                    .ToList();

                var exact = parsedItems
                    .Where(x => Math.Abs(x.MinSide - queryMin) < 0.001 && Math.Abs(x.MaxSide - queryMax) < 0.001)
                    .OrderBy(x => GetMassArea(x.MinSide, x.MaxSide))
                    .Select(x => x.Item)
                    .ToList();

                if (exact.Count > 0)
                {
                    filtered = exact;
                }
                else
                {
                    filtered = parsedItems
                        .Where(x => x.MinSide >= queryMin && x.MaxSide >= queryMax)
                        .OrderBy(x => GetMassArea(x.MinSide, x.MaxSide))
                        .Select(x => x.Item)
                        .ToList();
                }
            }
            else
            {
                filtered = baseFiltered
                    .Where(m => MatchesMassFilter(m.Mass, fMass))
                    .ToList();
            }

            GefilterteMaterialien.Clear();
            foreach (var item in filtered)
                GefilterteMaterialien.Add(item);

            SummaryText = $"{filtered.Count} Material(ien)";
        }

        private void SaveAllMaterials()
        {
            MaterialDataService.SaveAllMaterials(_alleMaterialien);
        }

        private void PushUndoSnapshot(string beschreibung)
        {
            UndoService.PushSnapshot($"Lager: {beschreibung}", _alleMaterialien);
        }

        private void RestoreMaterials(List<MaterialItem> materialien)
        {
            _alleMaterialien = materialien;
            SaveAllMaterials();
            LoadMaterials();
        }

        private void ExecuteUndo()
        {
            var materialien = UndoService.Undo(_alleMaterialien);
            if (materialien == null)
            {
                MessageBox.Show("Es gibt keine Aktion zum Zurücksetzen.", "Lager", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            RestoreMaterials(materialien);
        }

        private void ExecuteRedo()
        {
            var materialien = UndoService.Redo(_alleMaterialien);
            if (materialien == null)
            {
                MessageBox.Show("Es gibt keine Aktion zum Vorwärtssetzen.", "Lager", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show($"Netzwerkordner konnte nicht geöffnet werden:\n{ex.Message}", "Lager", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnNewMaterialClick(object sender, RoutedEventArgs e)
        {
            var dlg = new MaterialDialog(_alleMaterialien) { Owner = this };
            if (dlg.ShowDialog() != true)
                return;

            PushUndoSnapshot("Material neu");
            _alleMaterialien.Add(dlg.Material);
            SaveAllMaterials();
            LoadMaterials();
        }

        private MaterialItem? GetPrimarySelectedMaterial()
        {
            return GefilterteMaterialien.FirstOrDefault(m => m.IsSelected)
                ?? MaterialGrid.SelectedItem as MaterialItem;
        }

        private List<MaterialItem> GetMarkedMaterials()
        {
            var marked = GefilterteMaterialien.Where(m => m.IsSelected).ToList();
            if (marked.Count > 0)
                return marked;

            return MaterialGrid.SelectedItem is MaterialItem selected
                ? new List<MaterialItem> { selected }
                : new List<MaterialItem>();
        }

        private void OnEditMaterialClick(object sender, RoutedEventArgs e)
        {
            var item = GetPrimarySelectedMaterial();
            if (item == null)
                return;

            var dlg = new MaterialDialog(_alleMaterialien) { Owner = this };
            dlg.SetEditMode(item);
            if (dlg.ShowDialog() != true)
                return;

            var index = _alleMaterialien.IndexOf(item);
            if (index < 0)
                return;

            PushUndoSnapshot("Material bearbeiten");
            dlg.Material.IsSelected = item.IsSelected;
            _alleMaterialien[index] = dlg.Material;
            SaveAllMaterials();
            LoadMaterials();
        }

        private void OnReserveMaterialClick(object sender, RoutedEventArgs e)
        {
            var items = GetMarkedMaterials();
            if (items.Count == 0)
                return;

            var existingAuftrag = items.Count == 1 ? items[0].AuftragNr : string.Empty;
            var dlg = new ResteReservierungDialog(existingAuftrag) { Owner = this };
            if (dlg.ShowDialog() != true)
                return;

            if (dlg.DeleteMaterialFromLager)
            {
                var confirmDialog = new BestaetigungsDialog(
                    "Material aus dem Lager entfernen",
                    items.Count == 1
                        ? "Material nach Eintrag-Löschung wirklich aus dem Lager entfernen?"
                        : $"{items.Count} Materialien nach Eintrag-Löschung wirklich aus dem Lager entfernen?",
                    confirmText: "Entfernen",
                    cancelText: "Abbrechen",
                    confirmColorHex: "#8B1E1E")
                { Owner = this };

                if (confirmDialog.ShowDialog() != true)
                    return;

                PushUndoSnapshot(items.Count == 1 ? "Eintrag löschen + Material löschen" : "Einträge löschen + Materialien löschen");
                foreach (var item in items)
                    _alleMaterialien.Remove(item);

                SaveAllMaterials();
                LoadMaterials();
                return;
            }

            var auftragNr = dlg.AuftragNr?.Trim() ?? string.Empty;
            PushUndoSnapshot(items.Count == 1 ? "Reservierung ändern" : "Reservierung ändern (mehrere)");

            foreach (var item in items)
            {
                item.AuftragNr = auftragNr;
                item.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
                item.AenderungsDatum = DateTime.Now;
            }

            SaveAllMaterials();
            LoadMaterials();
        }

        private void OnReleaseReservationClick(object sender, RoutedEventArgs e)
        {
            var items = GetMarkedMaterials().Where(m => !string.IsNullOrWhiteSpace(m.AuftragNr)).ToList();
            if (items.Count == 0)
            {
                MessageBox.Show("Bitte reservierte Materialien auswählen oder markieren.", "Lager", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirmDialog = new BestaetigungsDialog(
                "Reservierung global aufheben",
                items.Count == 1
                    ? $"Reservierung für '{items[0].MaterialArt} {items[0].Mass}' global aufheben?"
                    : $"Reservierung für {items.Count} markierte Materialien global aufheben?",
                confirmText: "Aufheben",
                cancelText: "Abbrechen",
                confirmColorHex: "#1976D2")
            { Owner = this };

            if (confirmDialog.ShowDialog() != true)
                return;

            PushUndoSnapshot(items.Count == 1 ? "Reservierung global aufheben" : "Reservierungen global aufheben");

            MaterialItem? releasedItem = items.Count == 1 ? items[0] : null;
            var auftragsNummern = items.Select(i => i.AuftragNr).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().ToList();

            foreach (var item in items)
            {
                item.AuftragNr = string.Empty;
                item.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
                item.AenderungsDatum = DateTime.Now;
                item.IsSelected = false;
            }

            AuditLogService.LogAction(
                OperatorIdentityService.CurrentOperatorName,
                "RELEASE",
                "MaterialItem",
                string.Join(",", auftragsNummern),
                oldValue: "Reserviert",
                newValue: "Verfügbar",
                reason: $"Reservierung im Lager global aufgehoben ({items.Count} Positionen)");

            if (releasedItem != null)
            {
                var materialText = $"{releasedItem.MaterialArt} {releasedItem.Mass}".Trim();
                var actionDialog = new ReservierungAufhebenAktionDialog(materialText) { Owner = this };
                if (actionDialog.ShowDialog() == true)
                {
                    if (actionDialog.SelectedAction == ReservierungAufhebenAktion.Bearbeiten)
                    {
                        var editDialog = new MaterialDialog(_alleMaterialien) { Owner = this };
                        editDialog.SetEditMode(releasedItem);
                        if (editDialog.ShowDialog() == true)
                        {
                            var index = _alleMaterialien.IndexOf(releasedItem);
                            if (index >= 0)
                            {
                                editDialog.Material.IsSelected = false;
                                _alleMaterialien[index] = editDialog.Material;
                            }
                        }
                    }
                    else if (actionDialog.SelectedAction == ReservierungAufhebenAktion.Loeschen)
                    {
                        _alleMaterialien.Remove(releasedItem);
                    }
                }
            }

            SaveAllMaterials();
            LoadMaterials();
        }

        private void OnDeleteMaterialClick(object sender, RoutedEventArgs e)
        {
            var items = GetMarkedMaterials();
            if (items.Count == 0)
                return;

            var confirmDialog = new BestaetigungsDialog(
                "Material löschen",
                items.Count == 1
                    ? $"Material '{items[0].MaterialArt} {items[0].Mass}' wirklich löschen?"
                    : $"{items.Count} markierte Materialien wirklich löschen?",
                confirmText: "Löschen",
                cancelText: "Abbrechen",
                confirmColorHex: "#8B1E1E")
            { Owner = this };

            if (confirmDialog.ShowDialog() != true)
                return;

            PushUndoSnapshot(items.Count == 1 ? "Material löschen" : "Materialien löschen");
            foreach (var item in items)
            {
                _alleMaterialien.Remove(item);
            }

            SaveAllMaterials();
            LoadMaterials();
        }

        private void OnGridMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MaterialGrid.SelectedItem is not MaterialItem item)
                return;

            var pdfPfad = !string.IsNullOrWhiteSpace(item.PdfPfadAngefangeneTafel)
                ? item.PdfPfadAngefangeneTafel
                : item.PdfPfad;

            if (!string.IsNullOrWhiteSpace(pdfPfad) && System.IO.File.Exists(pdfPfad))
            {
                var dlg = new PdfPreviewDialog(pdfPfad) { Owner = this };
                dlg.ShowDialog();
                return;
            }

            OnEditMaterialClick(sender, null);
        }

        private void OnGridPreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var dep = e.OriginalSource as System.Windows.DependencyObject;
            if (dep == null) return;

            var cell = FindVisualParent<System.Windows.Controls.DataGridCell>(dep);
            if (cell == null) return;

            if (MaterialGrid.Columns.Count == 0) return;
            if (cell.DataContext is not MaterialItem item) return;

            if (cell.Column is DataGridBoundColumn boundColumn &&
                boundColumn.Binding is Binding binding &&
                binding.Path?.Path == nameof(MaterialItem.PdfDateiname))
            {
                OpenPdfPreview(item.PdfPfad);
                e.Handled = true;
                return;
            }

            var isSelectionColumn = cell.Column == MaterialGrid.Columns[0];
            if (!isSelectionColumn) return;

            item.IsSelected = !item.IsSelected;
            MaterialGrid.SelectedItem = item;
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

        private static T FindVisualParent<T>(System.Windows.DependencyObject child) where T : System.Windows.DependencyObject
        {
            while (child != null)
            {
                if (child is T t) return t;
                child = System.Windows.Media.VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private void OnShelfUtilizationClick(object sender, RoutedEventArgs e)
        {
            var dlg = new RegalauslastungDialog(_alleMaterialien) { Owner = this };
            dlg.ShowDialog();
            LoadMaterials();
        }

        private void OnInventoryClick(object sender, RoutedEventArgs e)
        {
            var dlg = new InventurDialog(_alleMaterialien) { Owner = this };
            dlg.ShowDialog();
            LoadMaterials();
        }

        private void OnLowStockClick(object sender, RoutedEventArgs e)
        {
            var materialien = new ObservableCollection<MaterialItem>(_alleMaterialien);
            var dlg = new NiedrigeBestaendeDialog(materialien) { Owner = this };
            dlg.ShowDialog();
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

        private void OnOpenMainProgramClick(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
            Close();
        }

        private void OnOpenLaserProgramClick(object sender, RoutedEventArgs e)
        {
            var laserWindow = new LaserDemoWindow();
            Application.Current.MainWindow = laserWindow;
            laserWindow.Show();
            Close();
        }

        private void OnOpenTafelplanungProgramClick(object sender, RoutedEventArgs e)
        {
            var tafWindow = new TafelplanungWindow();
            Application.Current.MainWindow = tafWindow;
            tafWindow.Show();
            Close();
        }

        private void OnColumnFilterChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void OnColumnFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private static DateTime GetObservedWriteTimeUtc(string? path)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path)
                    ? System.IO.File.GetLastWriteTimeUtc(path)
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
            var directory = System.IO.Path.GetDirectoryName(materialPath);
            return string.IsNullOrWhiteSpace(directory) ? string.Empty : System.IO.Path.Combine(directory, "auftraege.json");
        }
    }
}