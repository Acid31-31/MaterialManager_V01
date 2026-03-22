using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MaterialManager_V01.Models;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class LaserDemoWindow : Window, INotifyPropertyChanged
    {
        private List<MaterialItem> _alleMaterialien = new();
        private List<MaterialItem> _restMaterialienCache = new();

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

        public bool CanManageRestMaterials => false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public LaserDemoWindow()
        {
            InitializeComponent();
            DataContext = this;
            HeaderText = $"Angemeldet als {OperatorIdentityService.CurrentOperatorName} – Produktionssicht";
            Loaded += (_, _) => LoadMaterials();
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
            try
            {
                var savePath = NetzwerkService.GetSavePath();
                var items = System.IO.File.Exists(savePath)
                    ? ExcelService.Import(savePath)?.ToList() ?? new List<MaterialItem>()
                    : new List<MaterialItem>();

                _alleMaterialien = items;
                _restMaterialienCache = _alleMaterialien
                    .Where(m => !string.IsNullOrWhiteSpace(m.AuftragNr))
                    .ToList();

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Auftragsmaterialien:\n{ex.Message}", "Laser", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            var query = SearchBox?.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            var filtered = string.IsNullOrWhiteSpace(query)
                ? _restMaterialienCache
                : _restMaterialienCache.Where(m =>
                    (m.MaterialArt ?? string.Empty).ToLowerInvariant().Contains(query) ||
                    (m.Legierung ?? string.Empty).ToLowerInvariant().Contains(query) ||
                    (m.Mass ?? string.Empty).ToLowerInvariant().Contains(query) ||
                    (m.Restnummer ?? string.Empty).ToLowerInvariant().Contains(query) ||
                    (m.AuftragNr ?? string.Empty).ToLowerInvariant().Contains(query) ||
                    (m.Form ?? string.Empty).ToLowerInvariant().Contains(query) ||
                    (m.Lagerort ?? string.Empty).ToLowerInvariant().Contains(query)).ToList();

            RestMaterialien.Clear();
            foreach (var item in filtered)
                RestMaterialien.Add(item);

            SummaryText = $"{RestMaterialien.Count} gebuchte Material(ien)";
        }

        private void SaveAllMaterials()
        {
            var savePath = NetzwerkService.GetSavePath();
            ExcelService.Export(savePath, _alleMaterialien);
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
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

        private void OnSearchReserveClick(object sender, RoutedEventArgs e)
        {
            if (!CanManageRestMaterials)
                return;

            var dlg = new ResteSucheDialog { Owner = this };
            if (dlg.ShowDialog() != true)
                return;

            var gefunden = _alleMaterialien.Where(m =>
                RestMaterialSearchService.Matches(
                    m,
                    dlg.Material,
                    dlg.Legierung,
                    dlg.Staerke,
                    dlg.Laenge,
                    dlg.Breite,
                    dlg.ToleranzProzent,
                    dlg.Form,
                    requireRest: true)).ToList();

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

            foreach (var item in items)
            {
                BuchungsService.BucheAusgang(item, item.AuftragNr, OperatorIdentityService.CurrentOperatorName);
                _alleMaterialien.Remove(item);
            }

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
    }
}