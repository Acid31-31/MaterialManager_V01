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
using System.Windows.Threading;

namespace MaterialManager_V01.Views
{
    public partial class TafelplanungWindow : Window, INotifyPropertyChanged
    {
        private sealed class MaterialSelectionSnapshot
        {
            public HashSet<string> CheckedKeys { get; } = new(StringComparer.Ordinal);
            public string? SelectedKey { get; set; }
        }

        private List<MaterialItem> _alleMaterialien = new();
        private List<MaterialItem> _materialienCache = new();
        private readonly int _aktuellesJahr = DateTime.Now.Year;
        private int _ausgewaehlteKalenderWoche = ISOWeek.GetWeekOfYear(DateTime.Now);
        private Point _auftragDragStartPoint;
        private Auftrag? _draggedAuftrag;
        private DateTime _lastAutoReloadUtc = DateTime.MinValue;
        private DateTime _lastObservedMaterialWriteUtc = DateTime.MinValue;
        private DateTime _lastObservedAuftraegeWriteUtc = DateTime.MinValue;
        private readonly DispatcherTimer _autoRefreshTimer = new() { Interval = TimeSpan.FromSeconds(2) };
        private int _autoSyncStatusVersion;

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

        private async void LoadMaterials()
        {
            var selectionSnapshot = CaptureMaterialSelection();

            try
            {
                var items = await MaterialDataService.LoadAllMaterialsAsync();
                _alleMaterialien = items;
                _materialienCache = _alleMaterialien.ToList();
                RefreshManualFilterOptions();
                RefreshAuftragFilter();
                LoadAuftraegeGridForSelectedKw();
                ApplyFilter();
                RestoreMaterialSelection(selectionSnapshot);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Materialien:\n{ex.Message}", "Auftragssteuerung", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private static bool MatchesSelection(string? source, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            return string.Equals((source ?? string.Empty).Trim(), filter, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetComboBoxItemContentText(ComboBoxItem item)
        {
            return item.Content switch
            {
                string text => text,
                TextBlock textBlock => textBlock.Text,
                _ => item.Content?.ToString() ?? string.Empty
            };
        }

        private static string GetComboFilterValue(ComboBox? comboBox)
        {
            var value = comboBox?.SelectedItem switch
            {
                ComboBoxItem item => GetComboBoxItemContentText(item),
                string text => text,
                _ => comboBox?.Text
            };

            value = (value ?? string.Empty).Trim();
            return string.Equals(value, "Alle", StringComparison.OrdinalIgnoreCase) ? string.Empty : value;
        }

        private static Brush GetReadableTextBrush(Brush? background)
        {
            if (background is SolidColorBrush solid)
            {
                var c = solid.Color;
                var luma = (c.R * 299 + c.G * 587 + c.B * 114) / 1000;
                return luma >= 140
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#102018"))
                    : Brushes.White;
            }

            return Brushes.White;
        }

        private static ComboBoxItem CreateFilterComboItem(string content, Brush foreground, Brush background)
        {
            return new ComboBoxItem
            {
                Content = new TextBlock { Text = content, Foreground = foreground },
                Foreground = foreground,
                Background = background
            };
        }

        private static void ApplyComboItemTextColors(ComboBox comboBox, Brush itemBackground)
        {
            foreach (var item in comboBox.Items)
            {
                if (comboBox.ItemContainerGenerator.ContainerFromItem(item) is not ComboBoxItem container)
                    continue;

                container.Background = itemBackground;
                var readable = GetReadableTextBrush(container.Background);
                container.Foreground = readable;
                container.SetValue(System.Windows.Documents.TextElement.ForegroundProperty, readable);

                if (container.Content is TextBlock tb)
                    tb.Foreground = readable;
            }

            var comboReadable = GetReadableTextBrush(comboBox.Background);
            comboBox.Foreground = comboReadable;
            comboBox.SetValue(System.Windows.Documents.TextElement.ForegroundProperty, comboReadable);
            comboBox.Resources[SystemColors.ControlTextBrushKey] = comboReadable;
            comboBox.Resources[SystemColors.WindowTextBrushKey] = comboReadable;
            comboBox.Resources[SystemColors.GrayTextBrushKey] = comboReadable;
        }

        private void OnFilterComboDropDownOpened(object? sender, EventArgs e)
        {
            if (sender is not ComboBox comboBox)
                return;

            var itemBackground = comboBox.Background is SolidColorBrush solid
                ? solid
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D2D8CF"));

            ApplyComboItemTextColors(comboBox, itemBackground);
        }

        private void PopulateFilterComboBox(ComboBox? comboBox, IEnumerable<string> values, string selectedValue)
        {
            if (comboBox == null)
                return;

            var isLight = ThemeService.CurrentTheme == AppTheme.Light;
            var comboBackground = isLight
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D2D8CF"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#222222"));
            var itemBackground = isLight
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D2D8CF"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111111"));
            var textBrush = GetReadableTextBrush(comboBackground);

            comboBox.Background = comboBackground;
            comboBox.Foreground = textBrush;
            comboBox.SetValue(System.Windows.Documents.TextElement.ForegroundProperty, textBrush);
            comboBox.Resources[SystemColors.ControlTextBrushKey] = textBrush;
            comboBox.Resources[SystemColors.WindowTextBrushKey] = textBrush;
            comboBox.Resources[SystemColors.GrayTextBrushKey] = textBrush;

            comboBox.DropDownOpened -= OnFilterComboDropDownOpened;
            comboBox.DropDownOpened += OnFilterComboDropDownOpened;

            comboBox.Items.Clear();
            comboBox.Items.Add(CreateFilterComboItem("Alle", textBrush, itemBackground));

            foreach (var value in values.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase))
                comboBox.Items.Add(CreateFilterComboItem(value, textBrush, itemBackground));

            var target = string.IsNullOrWhiteSpace(selectedValue) ? "Alle" : selectedValue;
            comboBox.SelectedItem = comboBox.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(i => string.Equals(GetComboBoxItemContentText(i), target, StringComparison.OrdinalIgnoreCase))
                ?? comboBox.Items.OfType<ComboBoxItem>().FirstOrDefault();

            comboBox.Dispatcher.BeginInvoke(new Action(() =>
                ApplyComboItemTextColors(comboBox, itemBackground)), DispatcherPriority.Loaded);
        }

        private void RefreshManualFilterOptions()
        {
            var selectedMaterial = GetComboFilterValue(FilterMaterialBox);
            var selectedStaerke = GetComboFilterValue(FilterStaerkeBox);

            var materialien = _materialienCache
                .Select(m => (m.MaterialArt ?? string.Empty).Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .OrderBy(v => v, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            var staerken = _materialienCache
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

            var baseFiltered = _materialienCache.Where(m =>
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

            RestMaterialien.Clear();
            foreach (var item in filtered)
                RestMaterialien.Add(item);

            SummaryText = $"{filtered.Count} Material(ien)";
        }

        private void OnColumnFilterChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void OnColumnFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
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

            pdfPfad = AuftragArchivService.ResolveAccessiblePdfPath(auftrag.Auftragsnummer, pdfPfad);
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
            var confirmDialog = new BestaetigungsDialog(
                "Reservierung aufheben",
                items.Count == 1
                    ? $"Reservierung für '{items[0].MaterialArt} {items[0].Mass}' aufheben?"
                    : $"Reservierung für {items.Count} markierte Materialien aufheben?",
                confirmText: "Aufheben",
                cancelText: "Abbrechen",
                confirmColorHex: "#1976D2")
            { Owner = this };

            if (confirmDialog.ShowDialog() != true)
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
            var confirmDialog = new BestaetigungsDialog(
                "Produktion abschließen",
                items.Count == 1
                    ? $"Produktion für '{items[0].MaterialArt} {items[0].Mass}' abschließen und gebuchtes Material entfernen?"
                    : $"Produktion für {items.Count} markierte Materialien abschließen und gebuchtes Material entfernen?",
                confirmText: "Abschließen",
                cancelText: "Abbrechen",
                confirmColorHex: "#8E24AA")
            { Owner = this };

            if (confirmDialog.ShowDialog() != true)
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

        private void OnDeleteMaterialClick(object sender, RoutedEventArgs e)
        {
            var items = GetMarkedMaterials();
            if (items.Count == 0)
            {
                MessageBox.Show("Bitte zuerst Material auswählen oder markieren.", "Auftragssteuerung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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
            foreach (var item in items.ToList())
                _alleMaterialien.Remove(item);

            SaveAllMaterials();
            LoadMaterials();
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
            laserWindow.Show();
            Close();
        }

        private void OnOpenLagerProgramClick(object sender, RoutedEventArgs e)
        {
            var lagerWindow = new LagerDemoWindow();
            lagerWindow.Show();
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

            var archivedPdfPfad = string.IsNullOrWhiteSpace(pdfPfad)
                ? string.Empty
                : AuftragArchivService.TryArchivePdfForOrder(auftragNr.Trim(), pdfPfad.Trim(), _ausgewaehlteKalenderWoche, _aktuellesJahr) ?? pdfPfad.Trim();

            var bookedItem = CloneMaterial(item);
            bookedItem.Stueckzahl = menge;
            bookedItem.AuftragNr = auftragNr.Trim();
            bookedItem.PdfPfad = string.IsNullOrWhiteSpace(archivedPdfPfad) ? bookedItem.PdfPfad : archivedPdfPfad;
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

            if (string.Equals(normalized, "0", StringComparison.Ordinal))
                return value > 0 && value < 1;

            if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var target))
                return false;

            return Math.Abs(value - target) < 0.0001;
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
                Id = source.Id,
                Kategorie = source.Kategorie,
                MaterialArt = source.MaterialArt,
                Legierung = source.Legierung,
                Oberflaeche = source.Oberflaeche,
                Guete = source.Guete,
                SuchTrefferArt = source.SuchTrefferArt,
                Form = source.Form,
                Staerke = source.Staerke,
                Mass = source.Mass,
                Durchmesser = source.Durchmesser,
                Laenge = source.Laenge,
                ProfilTyp = source.ProfilTyp,
                ProfilHoehe = source.ProfilHoehe,
                ProfilBreite = source.ProfilBreite,
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
                PdfPfadAngefangeneTafel = source.PdfPfadAngefangeneTafel,
                PreisProKg = source.PreisProKg,
                IsHighlighted = source.IsHighlighted,
                IsSelected = source.IsSelected
            };
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