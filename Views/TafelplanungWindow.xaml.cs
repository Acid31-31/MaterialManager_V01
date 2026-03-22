using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MaterialManager_V01.Models;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class TafelplanungWindow : Window, INotifyPropertyChanged
    {
        private List<MaterialItem> _alleMaterialien = new();
        private List<MaterialItem> _materialienCache = new();

        public ObservableCollection<MaterialItem> RestMaterialien { get; } = new();

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

        public TafelplanungWindow()
        {
            InitializeComponent();
            DataContext = this;
            HeaderText = $"Angemeldet als {OperatorIdentityService.CurrentOperatorName}";
            Loaded += (_, _) => LoadMaterials();
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
                var savePath = NetzwerkService.GetSavePath();
                var items = await Task.Run(() =>
                {
                    if (!System.IO.File.Exists(savePath))
                        return new List<MaterialItem>();
                    return ExcelService.Import(savePath)?.ToList() ?? new List<MaterialItem>();
                });

                _alleMaterialien = items;
                _materialienCache = _alleMaterialien.ToList();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Materialien:\n{ex.Message}", "Tafelplanung", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            var query = SearchBox?.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            var selectedForm = (FormFilterBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Alle";

            var filtered = _materialienCache.Where(m =>
            {
                var formMatch = selectedForm == "Alle" || string.Equals(m.Form, selectedForm, StringComparison.OrdinalIgnoreCase);
                if (!formMatch)
                    return false;

                if (string.IsNullOrWhiteSpace(query))
                    return true;

                return (m.MaterialArt ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Legierung ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Form ?? string.Empty).ToLowerInvariant().Contains(query) ||
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
            var savePath = NetzwerkService.GetSavePath();
            ExcelService.Export(savePath, _alleMaterialien);
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

        private void OnReservedRestsClick(object sender, RoutedEventArgs e)
        {
            var dlg = new ReservierteResteDialog { Owner = this };
            dlg.ShowDialog();
            LoadMaterials();
        }

        private void OnBookForOrderClick(object sender, RoutedEventArgs e)
        {
            var items = GetMarkedMaterials().Where(m => string.IsNullOrWhiteSpace(m.AuftragNr)).ToList();
            if (items.Count == 0)
            {
                MessageBox.Show("Bitte zuerst ein verfügbares Material auswählen oder markieren.", "Tafelplanung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (items.Count == 1)
            {
                var item = items[0];
                var dlg = new AuftragBuchungDialog(item.Stueckzahl) { Owner = this };
                if (dlg.ShowDialog() != true)
                    return;

                BookMaterialForOrder(item, dlg.AuftragNr, dlg.Menge);
            }
            else
            {
                var dlg = new ResteReservierungDialog(string.Empty) { Owner = this };
                if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.AuftragNr))
                    return;

                foreach (var item in items.ToList())
                {
                    BookMaterialForOrder(item, dlg.AuftragNr.Trim(), item.Stueckzahl);
                }
            }

            SaveAllMaterials();
            LoadMaterials();
        }

        private void BookMaterialForOrder(MaterialItem item, string auftragNr, int menge)
        {
            if (string.IsNullOrWhiteSpace(auftragNr) || menge <= 0 || menge > item.Stueckzahl)
                return;

            var bookedItem = CloneMaterial(item);
            bookedItem.Stueckzahl = menge;
            bookedItem.AuftragNr = auftragNr.Trim();
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

        private void OnReleaseReservationClick(object sender, RoutedEventArgs e)
        {
            var items = GetMarkedMaterials().Where(m => !string.IsNullOrWhiteSpace(m.AuftragNr)).ToList();
            if (items.Count == 0)
            {
                MessageBox.Show("Bitte gebuchte Materialien auswählen oder markieren.", "Tafelplanung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                items.Count == 1
                    ? $"Reservierung für '{items[0].MaterialArt} {items[0].Mass}' aufheben?"
                    : $"Reservierung für {items.Count} markierte Materialien aufheben?",
                "Tafelplanung",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            foreach (var item in items.ToList())
            {
                var existing = FindAvailableMaterial(item);
                if (existing != null)
                {
                    existing.Stueckzahl += item.Stueckzahl;
                    existing.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
                    existing.AenderungsDatum = DateTime.Now;
                }
                else
                {
                    var restored = CloneMaterial(item);
                    restored.AuftragNr = string.Empty;
                    restored.Lagerort = RegalService.DetermineLagerort(restored.MaterialArt, restored.Legierung, restored.Form, restored.Staerke, restored.Mass, _alleMaterialien);
                    restored.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
                    restored.AenderungsDatum = DateTime.Now;
                    restored.IsSelected = false;
                    _alleMaterialien.Add(restored);
                }

                _alleMaterialien.Remove(item);
            }

            SaveAllMaterials();
            LoadMaterials();
        }

        private void OnCompleteProductionClick(object sender, RoutedEventArgs e)
        {
            var items = GetMarkedMaterials().Where(m => !string.IsNullOrWhiteSpace(m.AuftragNr)).ToList();
            if (items.Count == 0)
            {
                MessageBox.Show("Bitte gebuchte Materialien auswählen oder markieren.", "Tafelplanung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                items.Count == 1
                    ? $"Produktion für '{items[0].MaterialArt} {items[0].Mass}' abschließen und gebuchte Menge entfernen?"
                    : $"Produktion für {items.Count} markierte Materialien abschließen und gebuchte Mengen entfernen?",
                "Tafelplanung",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            foreach (var item in items.ToList())
            {
                BuchungsService.BucheAusgang(item, item.AuftragNr, OperatorIdentityService.CurrentOperatorName);
                _alleMaterialien.Remove(item);
            }

            SaveAllMaterials();
            LoadMaterials();
        }

        private MaterialItem? FindAvailableMaterial(MaterialItem bookedItem)
        {
            return _alleMaterialien.FirstOrDefault(m =>
                string.IsNullOrWhiteSpace(m.AuftragNr) &&
                m.MaterialArt == bookedItem.MaterialArt &&
                m.Legierung == bookedItem.Legierung &&
                m.Oberflaeche == bookedItem.Oberflaeche &&
                m.Guete == bookedItem.Guete &&
                m.Form == bookedItem.Form &&
                Math.Abs(m.Staerke - bookedItem.Staerke) < 0.0001 &&
                m.Mass == bookedItem.Mass);
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
                PdfPfad = source.PdfPfad
            };
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            var startWindow = new StartModeWindow();
            Application.Current.MainWindow = startWindow;
            startWindow.Show();
            Close();
        }

        private void OnGridPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dep = e.OriginalSource as DependencyObject;
            if (dep == null) return;

            var cell = FindVisualParent<DataGridCell>(dep);
            if (cell == null) return;

            if (RestMaterialGrid.Columns.Count == 0 || cell.Column != RestMaterialGrid.Columns[0]) return;

            if (cell.DataContext is not MaterialItem item) return;

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

        private void OnAttachPdfClick(object sender, RoutedEventArgs e)
        {
            var item = GetPrimarySelectedMaterial();
            if (item == null)
            {
                MessageBox.Show("Bitte zuerst ein Material auswählen.", "PDF anhängen", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "PDF-Datei für Auftrag auswählen",
                Filter = "PDF-Dateien (*.pdf)|*.pdf|Alle Dateien (*.*)|*.*",
                CheckFileExists = true
            };

            if (!string.IsNullOrWhiteSpace(item.PdfPfad) && System.IO.File.Exists(item.PdfPfad))
                dlg.InitialDirectory = System.IO.Path.GetDirectoryName(item.PdfPfad);

            if (dlg.ShowDialog() != true)
                return;

            item.PdfPfad = dlg.FileName;
            item.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
            item.AenderungsDatum = DateTime.Now;
            SaveAllMaterials();
            LoadMaterials();
        }
    }
}