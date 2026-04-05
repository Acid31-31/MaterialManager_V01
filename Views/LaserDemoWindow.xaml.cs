using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ClosedXML.Excel;
using Microsoft.Win32;
using MaterialManager_V01.Models;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class LaserDemoWindow : Window, INotifyPropertyChanged
    {
        protected virtual string Arbeitsbereich => AuftragArbeitsplatzService.Laser;
        protected virtual bool ShowReservedMaterialArea => true;
        protected virtual bool ShowExcelOrderButton => false;

        private static readonly string KantbankExcelSettingsPath = Path.Combine(PathService.DataDirectory, "kantbank_excel.settings.json");

        private List<MaterialItem> _alleMaterialien = new();
        private List<MaterialItem> _restMaterialienCache = new();
        private List<Auftrag> _auftraegeCache = new();
        private readonly int _aktuellesJahr = DateTime.Now.Year;
        private int _ausgewaehlteKalenderWoche = ISOWeek.GetWeekOfYear(DateTime.Now);
        private DataTable? _kantbankExcelTable;

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
            UpdateAuftragsKwText();
            FitToWorkArea();
            ConfigureArbeitsbereichLayout();
            Loaded += (_, _) => LoadMaterials();
            PreviewKeyDown += OnWindowPreviewKeyDown;
        }

        private void ConfigureArbeitsbereichLayout()
        {
            if (ShowExcelOrderButton)
            {
                if (OpenExcelButton != null) OpenExcelButton.Visibility = Visibility.Visible;
                if (SelectExcelButton != null) SelectExcelButton.Visibility = Visibility.Visible;
                if (SaveExcelButton != null) SaveExcelButton.Visibility = Visibility.Visible;
                if (ExcelPathLabel != null) ExcelPathLabel.Visibility = Visibility.Visible;
                if (ExcelPathBox != null) ExcelPathBox.Visibility = Visibility.Visible;
                if (CustomerFilterLabel != null) CustomerFilterLabel.Visibility = Visibility.Visible;
                if (KantbankCustomerFilterBox != null) KantbankCustomerFilterBox.Visibility = Visibility.Visible;
                if (DateFilterLabel != null) DateFilterLabel.Visibility = Visibility.Visible;
                if (KantbankDateFilterPicker != null) KantbankDateFilterPicker.Visibility = Visibility.Visible;
            }

            if (!ShowReservedMaterialArea)
            {
                if (EditRestButton != null) EditRestButton.Visibility = Visibility.Collapsed;
                if (DeleteRestButton != null) DeleteRestButton.Visibility = Visibility.Collapsed;
                if (OpenRestPdfButton != null) OpenRestPdfButton.Visibility = Visibility.Collapsed;
                if (MaterialActionHint != null) MaterialActionHint.Visibility = Visibility.Collapsed;

                if (MaterialFilterBorder != null)
                    MaterialFilterBorder.Visibility = Visibility.Collapsed;

                if (RestMaterialGrid != null)
                    RestMaterialGrid.Visibility = Visibility.Collapsed;

                if (AuftraegeGrid != null)
                    AuftraegeGrid.Visibility = Visibility.Collapsed;

                if (KantbankExcelGrid != null)
                    KantbankExcelGrid.Visibility = Visibility.Visible;

                if (AuftraegeGridRowDefinition != null)
                    AuftraegeGridRowDefinition.Height = new GridLength(1, GridUnitType.Star);

                var path = LoadSavedKantbankExcelPath();
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    LoadKantbankExcel(path);
            }
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
                _restMaterialienCache = _alleMaterialien
                    .Where(m => !string.IsNullOrWhiteSpace(m.AuftragNr))
                    .Where(m => MatchesArbeitsbereich(m.AuftragNr))
                    .ToList();

                LoadAuftraege();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Auftragsmaterialien:\n{ex.Message}", "Laser", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                MessageBox.Show("Es gibt keine Aktion zum Vorwärtssetzen.", "Laser", MessageBoxButton.OK, MessageBoxImage.Information);
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
            if (ShowExcelOrderButton)
            {
                var path = ExcelPathBox?.Text?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    LoadKantbankExcel(path);
                else
                    LoadMaterials();
                return;
            }

            LoadMaterials();
        }

        private void OnSelectExcelPathClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Kantbank-Excel auswählen",
                Filter = "Excel-Dateien (*.xlsx)|*.xlsx|Alle Dateien (*.*)|*.*",
                CheckFileExists = true
            };

            var netDir = Path.GetDirectoryName(NetzwerkService.GetSavePath());
            if (!string.IsNullOrWhiteSpace(netDir) && Directory.Exists(netDir))
                dlg.InitialDirectory = netDir;

            if (dlg.ShowDialog() != true)
                return;

            SaveKantbankExcelPath(dlg.FileName);
            LoadKantbankExcel(dlg.FileName);
        }

        private void OnSaveExcelClick(object sender, RoutedEventArgs e)
        {
            SaveKantbankExcel();
        }

        private void OnKantbankFilterChanged(object sender, EventArgs e)
        {
            ApplyKantbankExcelFilter();
        }

        private void LoadKantbankExcel(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return;

                using var wb = new XLWorkbook(path);
                var ws = SelectBestWorksheetForKantbank(wb);
                if (ws == null)
                    return;

                var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
                if (lastRow < 1 || lastCol < 1)
                    return;

                var headerRow = DetectHeaderRow(ws, lastRow, lastCol);
                var table = new DataTable();

                for (var c = 1; c <= lastCol; c++)
                {
                    var name = NormalizeHeaderName(GetCellDisplayValue(ws.Cell(headerRow, c)));
                    if (string.IsNullOrWhiteSpace(name))
                        name = $"Spalte{c}";

                    var original = name;
                    var idx = 2;
                    while (table.Columns.Contains(name))
                    {
                        name = $"{original}_{idx}";
                        idx++;
                    }

                    table.Columns.Add(name, typeof(string));
                }

                for (var r = headerRow + 1; r <= lastRow; r++)
                {
                    var row = table.NewRow();
                    var hasValue = false;

                    for (var c = 1; c <= lastCol; c++)
                    {
                        var value = GetCellDisplayValue(ws.Cell(r, c)).Trim();
                        if (!string.IsNullOrWhiteSpace(value))
                            hasValue = true;
                        row[c - 1] = value;
                    }

                    if (hasValue)
                        table.Rows.Add(row);
                }

                _kantbankExcelTable = table;
                KantbankExcelGrid.ItemsSource = table.DefaultView;
                ExcelPathBox.Text = path;
                ApplyKantbankExcelFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel konnte nicht geladen werden:\n{ex.Message}", "Kantbank", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static IXLWorksheet? SelectBestWorksheetForKantbank(XLWorkbook wb)
        {
            IXLWorksheet? best = null;
            var bestScore = int.MinValue;

            foreach (var ws in wb.Worksheets)
            {
                var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
                if (lastRow == 0 || lastCol == 0)
                    continue;

                var headerRow = DetectHeaderRow(ws, lastRow, lastCol);
                var headerHits = 0;
                for (var c = 1; c <= lastCol; c++)
                {
                    var text = NormalizeHeaderName(GetCellDisplayValue(ws.Cell(headerRow, c)));
                    if (IsKantbankHeaderKeyword(text))
                        headerHits++;
                }

                var probeRows = Math.Min(lastRow, headerRow + 30);
                var dataCells = 0;
                for (var r = headerRow + 1; r <= probeRows; r++)
                {
                    for (var c = 1; c <= lastCol; c++)
                    {
                        if (!string.IsNullOrWhiteSpace(GetCellDisplayValue(ws.Cell(r, c))))
                            dataCells++;
                    }
                }

                var score = (headerHits * 1000) + dataCells + lastRow;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = ws;
                }
            }

            return best ?? wb.Worksheets.FirstOrDefault();
        }

        private static int DetectHeaderRow(IXLWorksheet ws, int lastRow, int lastCol)
        {
            var maxProbeRows = Math.Min(lastRow, 25);
            var bestRow = 1;
            var bestScore = int.MinValue;

            for (var r = 1; r <= maxProbeRows; r++)
            {
                var rowScore = 0;
                var nonEmpty = 0;
                for (var c = 1; c <= lastCol; c++)
                {
                    var text = NormalizeHeaderName(GetCellDisplayValue(ws.Cell(r, c)));
                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    nonEmpty++;
                    rowScore += 1;
                    if (IsKantbankHeaderKeyword(text))
                        rowScore += 8;
                }

                rowScore += nonEmpty;
                if (rowScore > bestScore)
                {
                    bestScore = rowScore;
                    bestRow = r;
                }
            }

            return bestRow;
        }

        private static string GetCellDisplayValue(IXLCell cell)
        {
            var text = cell.GetFormattedString();
            if (!string.IsNullOrWhiteSpace(text))
                return text;

            var merged = cell.MergedRange();
            if (merged != null)
                return merged.FirstCell().GetFormattedString();

            return string.Empty;
        }

        private static bool IsKantbankHeaderKeyword(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return value.Contains("kunde", StringComparison.OrdinalIgnoreCase)
                   || value.Contains("datum", StringComparison.OrdinalIgnoreCase)
                   || value.Contains("date", StringComparison.OrdinalIgnoreCase)
                   || value.Contains("termin", StringComparison.OrdinalIgnoreCase)
                   || value.Contains("zeichnung", StringComparison.OrdinalIgnoreCase)
                   || value.Contains("zeichnungsnr", StringComparison.OrdinalIgnoreCase)
                   || value.Contains("revision", StringComparison.OrdinalIgnoreCase)
                   || value.Contains("pos", StringComparison.OrdinalIgnoreCase)
                   || value.Contains("anzahl", StringComparison.OrdinalIgnoreCase)
                   || value.Contains("status", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeHeaderName(string value)
        {
            return (value ?? string.Empty)
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
        }

        private void ApplyKantbankExcelFilter()
        {
            if (_kantbankExcelTable == null)
                return;

            var view = _kantbankExcelTable.DefaultView;
            var clauses = new List<string>();

            var customer = KantbankCustomerFilterBox?.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(customer))
            {
                var customerCols = _kantbankExcelTable.Columns.Cast<DataColumn>()
                    .Select(c => c.ColumnName)
                    .Where(n => n.Contains("kunde", StringComparison.OrdinalIgnoreCase) || n.Contains("customer", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (customerCols.Count > 0)
                {
                    var escaped = customer.Replace("'", "''");
                    var customerExpr = string.Join(" OR ", customerCols.Select(c => $"[{c}] LIKE '%{escaped}%'"));
                    clauses.Add($"({customerExpr})");
                }
            }

            if (KantbankDateFilterPicker?.SelectedDate is DateTime date)
            {
                var dateCols = _kantbankExcelTable.Columns.Cast<DataColumn>()
                    .Select(c => c.ColumnName)
                    .Where(n => n.Contains("datum", StringComparison.OrdinalIgnoreCase)
                             || n.Contains("date", StringComparison.OrdinalIgnoreCase)
                             || n.Contains("termin", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (dateCols.Count > 0)
                {
                    var d1 = date.ToString("dd.MM.yyyy");
                    var d2 = date.ToString("d.M.yyyy");
                    var d3 = date.ToString("yyyy-MM-dd");
                    var d4 = date.ToString("yyyy-M-d");

                    var dateExpr = string.Join(" OR ", dateCols.Select(c =>
                        $"([{c}] LIKE '%{d1}%' OR [{c}] LIKE '%{d2}%' OR [{c}] LIKE '%{d3}%' OR [{c}] LIKE '%{d4}%')"));
                    clauses.Add($"({dateExpr})");
                }
            }

            view.RowFilter = clauses.Count == 0 ? string.Empty : string.Join(" AND ", clauses);
        }

        private void SaveKantbankExcel()
        {
            if (_kantbankExcelTable == null)
                return;

            var path = ExcelPathBox?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show("Bitte zuerst eine Excel-Datei auswählen.", "Kantbank", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Aufträge");

                for (var c = 0; c < _kantbankExcelTable.Columns.Count; c++)
                    ws.Cell(1, c + 1).Value = _kantbankExcelTable.Columns[c].ColumnName;

                for (var r = 0; r < _kantbankExcelTable.Rows.Count; r++)
                {
                    for (var c = 0; c < _kantbankExcelTable.Columns.Count; c++)
                        ws.Cell(r + 2, c + 1).Value = _kantbankExcelTable.Rows[r][c]?.ToString() ?? string.Empty;
                }

                ws.Columns().AdjustToContents();
                wb.SaveAs(path);

                MessageBox.Show("Excel wurde gespeichert.", "Kantbank", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel konnte nicht gespeichert werden:\n{ex.Message}", "Kantbank", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static string LoadSavedKantbankExcelPath()
        {
            try
            {
                if (!File.Exists(KantbankExcelSettingsPath))
                    return string.Empty;

                var json = File.ReadAllText(KantbankExcelSettingsPath, Encoding.UTF8);
                var dto = JsonSerializer.Deserialize<KantbankExcelSettingsDto>(json);
                return dto?.ExcelPath?.Trim() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void SaveKantbankExcelPath(string path)
        {
            try
            {
                var dir = Path.GetDirectoryName(KantbankExcelSettingsPath);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var dto = new KantbankExcelSettingsDto { ExcelPath = path?.Trim() ?? string.Empty };
                var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(KantbankExcelSettingsPath, json, Encoding.UTF8);
            }
            catch
            {
            }
        }

        private sealed class KantbankExcelSettingsDto
        {
            public string ExcelPath { get; set; } = string.Empty;
        }

        private void OnExcelPathTextChanged(object sender, TextChangedEventArgs e)
        {
            var path = ExcelPathBox?.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                LoadKantbankExcel(path);
        }

        private void OnOpenExcelClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var excelPath = ExcelPathBox?.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(excelPath) || !System.IO.File.Exists(excelPath))
                {
                    MessageBox.Show("Bitte zuerst eine gültige Kantbank-Excel auswählen.", "Kantbank", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = excelPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel-Datei konnte nicht geöffnet werden:\n{ex.Message}", "Kantbank", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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

            var dlg = new MaterialDialog(_alleMaterialien) { Owner = this };
            dlg.SetEditMode(item);
            if (dlg.ShowDialog() != true)
                return;

            var index = _alleMaterialien.IndexOf(item);
            if (index < 0)
                return;

            PushUndoSnapshot("Reserviertes Material bearbeiten");
            dlg.Material.AuftragNr = string.Empty;
            dlg.Material.Lagerort = RegalService.DetermineLagerort(
                dlg.Material.MaterialArt,
                dlg.Material.Legierung,
                dlg.Material.Form,
                dlg.Material.Staerke,
                dlg.Material.Mass,
                _alleMaterialien.Where(m => !ReferenceEquals(m, item)).ToList());
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

            var confirm = MessageBox.Show(
                items.Count == 1
                    ? $"Reserviertes Material '{items[0].MaterialArt} {items[0].Mass}' wirklich löschen?"
                    : $"{items.Count} reservierte Materialien wirklich löschen?",
                "Laser",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
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

            OpenPdfPreview(pdfPfad, "Diesem Auftrag ist keine Original-PDF zugeordnet.");
            e.Handled = true;
        }

        private void OnAuftragKantPdfButtonClick(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not Auftrag auftrag)
                return;

            var pdfPfad = auftrag.PdfPfadKantzeichnung;
            OpenPdfPreview(pdfPfad, "Für diesen Auftrag wurde keine Kant-PDF im Kundenordner gefunden.");
            e.Handled = true;
        }

        private void OpenPdfPreviewForAuftrag(Auftrag auftrag)
        {
            var pdfPfad = !string.IsNullOrWhiteSpace(auftrag.PdfPfadAngefangeneTafel)
                ? auftrag.PdfPfadAngefangeneTafel
                : auftrag.PdfPfad;

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
    }
}