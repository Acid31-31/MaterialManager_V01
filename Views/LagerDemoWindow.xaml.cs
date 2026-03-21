using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MaterialManager_V01.Models;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class LagerDemoWindow : Window, INotifyPropertyChanged
    {
        private readonly User _selectedUser;
        private List<MaterialItem> _alleMaterialien = new();

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

        public LagerDemoWindow(User selectedUser)
        {
            InitializeComponent();
            _selectedUser = selectedUser;
            DataContext = this;
            HeaderText = $"Angemeldet als {_selectedUser.DisplayName} – Lager-Sicht";
            Loaded += (_, _) => LoadMaterials();
        }

        private void LoadMaterials()
        {
            try
            {
                var savePath = NetzwerkService.GetSavePath();
                _alleMaterialien = System.IO.File.Exists(savePath)
                    ? ExcelService.Import(savePath)?.ToList() ?? new List<MaterialItem>()
                    : new List<MaterialItem>();

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Materialien:\n{ex.Message}", "Lager-Demo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            var query = SearchBox?.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            var selectedForm = (FormFilterBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Alle";

            var filtered = _alleMaterialien.Where(m =>
            {
                var formMatch = selectedForm == "Alle" || string.Equals(m.Form, selectedForm, StringComparison.OrdinalIgnoreCase);
                if (!formMatch)
                    return false;

                if (string.IsNullOrWhiteSpace(query))
                    return true;

                return (m.MaterialArt ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Legierung ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Oberflaeche ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Mass ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Restnummer ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.AuftragNr ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Lagerort ?? string.Empty).ToLowerInvariant().Contains(query);
            }).ToList();

            GefilterteMaterialien.Clear();
            foreach (var item in filtered)
                GefilterteMaterialien.Add(item);

            var reserviert = filtered.Count(m => !string.IsNullOrWhiteSpace(m.AuftragNr));
            SummaryText = $"{filtered.Count} Material(ien), {reserviert} reserviert";
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

        private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
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

        private void OnNewMaterialClick(object sender, RoutedEventArgs e)
        {
            var dlg = new MaterialDialog(_alleMaterialien) { Owner = this };
            if (dlg.ShowDialog() != true)
                return;

            _alleMaterialien.Add(dlg.Material);
            SaveAllMaterials();
            LoadMaterials();
        }

        private void OnEditMaterialClick(object sender, RoutedEventArgs e)
        {
            if (MaterialGrid.SelectedItem is not MaterialItem item)
                return;

            var dlg = new MaterialDialog(_alleMaterialien) { Owner = this };
            dlg.SetEditMode(item);
            if (dlg.ShowDialog() != true)
                return;

            var index = _alleMaterialien.IndexOf(item);
            if (index < 0)
                return;

            _alleMaterialien[index] = dlg.Material;
            SaveAllMaterials();
            LoadMaterials();
        }

        private void OnDeleteMaterialClick(object sender, RoutedEventArgs e)
        {
            if (MaterialGrid.SelectedItem is not MaterialItem item)
                return;

            var confirm = MessageBox.Show(
                $"Material '{item.MaterialArt} {item.Mass}' wirklich löschen?",
                "Lager-Demo",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

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
    }
}