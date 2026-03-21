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
    public partial class TafelplanungWindow : Window, INotifyPropertyChanged
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
                    .Where(m => string.Equals(m.Form, "Rest", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Restmaterialien:\n{ex.Message}", "Tafelplanung", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    (m.Lagerort ?? string.Empty).ToLowerInvariant().Contains(query)).ToList();

            RestMaterialien.Clear();
            foreach (var item in filtered)
                RestMaterialien.Add(item);

            var reserviert = filtered.Count(m => !string.IsNullOrWhiteSpace(m.AuftragNr));
            SummaryText = $"{RestMaterialien.Count} Restmaterial(ien), {reserviert} reserviert";
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
            var dlg = new ResteSucheDialog { Owner = this };
            if (dlg.ShowDialog() != true)
                return;

            var gefunden = _alleMaterialien.Where(m =>
            {
                if (!string.Equals(m.Form, "Rest", StringComparison.OrdinalIgnoreCase))
                    return false;

                var match = true;

                if (!string.IsNullOrEmpty(dlg.Material) && m.MaterialArt != dlg.Material)
                    match = false;
                if (!string.IsNullOrEmpty(dlg.Legierung) && m.Legierung != dlg.Legierung)
                    match = false;
                if (dlg.Form != "Alle" && m.Form != dlg.Form)
                    match = false;

                if (dlg.Staerke.HasValue)
                {
                    var toleranz = dlg.ToleranzProzent / 100.0;
                    var min = dlg.Staerke.Value * (1 - toleranz);
                    var max = dlg.Staerke.Value * (1 + toleranz);
                    if (m.Staerke < min || m.Staerke > max)
                        match = false;
                }

                if (dlg.Laenge.HasValue && dlg.Breite.HasValue)
                {
                    var parts = m.Mass?.Split('x', '×');
                    if (parts?.Length == 2 &&
                        int.TryParse(parts[0].Trim(), out var l) &&
                        int.TryParse(parts[1].Trim(), out var b))
                    {
                        var toleranz = dlg.ToleranzProzent / 100.0;
                        var minL = dlg.Laenge.Value * (1 - toleranz);
                        var maxL = dlg.Laenge.Value * (1 + toleranz);
                        var minB = dlg.Breite.Value * (1 - toleranz);
                        var maxB = dlg.Breite.Value * (1 + toleranz);
                        if (l < minL || l > maxL || b < minB || b > maxB)
                            match = false;
                    }
                }

                return match;
            }).ToList();

            if (!gefunden.Any())
            {
                MessageBox.Show("Keine passenden Restmaterialien gefunden.", "Tafelplanung", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void OnReleaseReservationClick(object sender, RoutedEventArgs e)
        {
            var items = GetMarkedMaterials()
                .Where(m => !string.IsNullOrWhiteSpace(m.AuftragNr))
                .ToList();

            if (items.Count == 0)
            {
                MessageBox.Show("Bitte ein reserviertes Restmaterial auswählen oder markieren.", "Tafelplanung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                items.Count == 1
                    ? $"Reservierung für '{items[0].MaterialArt} {items[0].Mass}' aufheben?"
                    : $"Reservierung für {items.Count} markierte Restmaterialien aufheben?",
                "Tafelplanung",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            foreach (var item in items)
            {
                item.AuftragNr = string.Empty;
                item.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
                item.AenderungsDatum = DateTime.Now;
                item.IsSelected = false;
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