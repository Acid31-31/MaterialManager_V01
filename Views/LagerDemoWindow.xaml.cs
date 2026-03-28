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
    public partial class LagerDemoWindow : Window, INotifyPropertyChanged
    {
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

        public LagerDemoWindow()
        {
            InitializeComponent();
            DataContext = this;
            HeaderText = $"Angemeldet als {OperatorIdentityService.CurrentOperatorName} – Lager";
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

        private void LoadMaterials()
        {
            try
            {
                _alleMaterialien = MaterialDataService.LoadAllMaterials();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Materialien:\n{ex.Message}", "Lager", MessageBoxButton.OK, MessageBoxImage.Error);
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

            var filtered = _alleMaterialien.Where(m =>
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
                       (m.Oberflaeche ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Form ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       m.Kategorie.ToString().ToLowerInvariant().Contains(query) ||
                       (m.Mass ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.ProfilTyp ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       m.Laenge.ToString().ToLowerInvariant().Contains(query) ||
                       (m.Restnummer ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.AuftragNr ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Lagerort ?? string.Empty).ToLowerInvariant().Contains(query);
            }).ToList();

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

        private void OnGridMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MaterialGrid.SelectedItem is not MaterialItem item)
                return;

            // Prüfe ob PDF vorhanden ist und zeige PDF-Dialog
            if (!string.IsNullOrWhiteSpace(item.PdfPfad) && System.IO.File.Exists(item.PdfPfad))
            {
                var dlg = new PdfPreviewDialog(item.PdfPfad) { Owner = this };
                dlg.ShowDialog();
                return;
            }

            // Sonst öffne Bearbeitungs-Dialog
            OnEditMaterialClick(sender, null);
        }

        private void OnGridPreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var dep = e.OriginalSource as System.Windows.DependencyObject;
            if (dep == null) return;

            var cell = FindVisualParent<System.Windows.Controls.DataGridCell>(dep);
            if (cell == null) return;

            if (MaterialGrid.Columns.Count == 0) return;

            // Prüfe ob auf PDF-Spalte geklickt wurde (letzte Spalte mit PDF-Dateien)
            var columnIndex = cell.Column.DisplayIndex;
            var isPdfColumn = columnIndex == MaterialGrid.Columns.Count - 1;

            if (!isPdfColumn) return;

            if (MaterialGrid.SelectedItem is not MaterialItem item) return;

            // Öffne PDF-Dialog bei Doppelklick auf PDF-Spalte
            // Double-Click wird von OnGridMouseDoubleClick gehandhabt
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
        
        private void OnDeleteMaterialClick(object sender, RoutedEventArgs e)
        {
            var items = GetMarkedMaterials();
            if (items.Count == 0)
                return;

            var confirm = MessageBox.Show(
                items.Count == 1
                    ? $"Material '{items[0].MaterialArt} {items[0].Mass}' wirklich löschen?"
                    : $"{items.Count} markierte Materialien wirklich löschen?",
                "Lager",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            PushUndoSnapshot(items.Count == 1 ? "Material löschen" : "Materialien löschen");
            foreach (var item in items)
            {
                _alleMaterialien.Remove(item);
            }

            SaveAllMaterials();
            LoadMaterials();
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
    }
}