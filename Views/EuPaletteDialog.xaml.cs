using MaterialManager_V01.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MaterialManager_V01.Views
{
    public partial class EuPaletteDialog : Window, INotifyPropertyChanged
    {
        private ObservableCollection<MaterialItem> _sourceMaterialien;
        private List<MaterialItem> _paletteMaterialien = new();
        private ObservableCollection<MaterialItem> _filteredItems = new();
        private bool _isUpdatingFilters = false;

        public bool HasChanges { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private string _gesamtStatistik = "";
        public string GesamtStatistik
        {
            get => _gesamtStatistik;
            set { _gesamtStatistik = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GesamtStatistik))); }
        }

        private string _auslastungInfo = "";
        public string AuslastungInfo
        {
            get => _auslastungInfo;
            set { _auslastungInfo = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AuslastungInfo))); }
        }

        private string _filterStatistik = "";
        public string FilterStatistik
        {
            get => _filterStatistik;
            set { _filterStatistik = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilterStatistik))); }
        }

        public EuPaletteDialog(ObservableCollection<MaterialItem> materialien)
        {
            InitializeComponent();
            DataContext = this;

            Width = SystemParameters.PrimaryScreenWidth * 0.9;
            Height = SystemParameters.PrimaryScreenHeight * 0.9;
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;

            _sourceMaterialien = materialien ?? new ObservableCollection<MaterialItem>();

            // Alle EU Palette Materialien sammeln
            _paletteMaterialien = _sourceMaterialien
                .Where(m => m != null && m.Lagerort == "EU Palette")
                .ToList();

            // DataGrid binden
            PaletteGrid.ItemsSource = _filteredItems;

            // Filter-ComboBoxen füllen (BEVOR Events aktiv werden)
            FuelleFilterComboBoxen();

            // Erste Anzeige: alle Materialien
            AktualisiereAnzeige();
        }

        /// <summary>
        /// Füllt alle Filter-ComboBoxen mit Werten aus MaterialDefinitions
        /// </summary>
        private void FuelleFilterComboBoxen()
        {
            foreach (var mat in new[] { "Stahl", "Edelstahl", "Aluminium" })
                MaterialFilterBox.Items.Add(new ComboBoxItem { Content = mat });

            var alleLeg = MaterialDefinitions.Legierungen.Values.SelectMany(x => x).Distinct().OrderBy(x => x);
            foreach (var leg in alleLeg)
                LegierungFilterBox.Items.Add(new ComboBoxItem { Content = leg });

            var alleOberfl = MaterialDefinitions.Oberflaechen.Values.SelectMany(x => x).Distinct().OrderBy(x => x);
            foreach (var oberfl in alleOberfl)
                OberflaecheFilterBox.Items.Add(new ComboBoxItem { Content = oberfl });

            foreach (var guete in MaterialDefinitions.AluminiumGueten)
                GueteFilterBox.Items.Add(new ComboBoxItem { Content = guete });

            var staerken = new[] { 0.3, 0.5, 0.8, 1.0, 1.2, 1.5, 2.0, 2.5, 3.0, 4.0, 5.0, 6.0, 8.0, 10.0, 12.0, 15.0, 20.0, 25.0 };
            foreach (var s in staerken)
                StaerkeFilterBox.Items.Add(new ComboBoxItem { Content = string.Format(CultureInfo.InvariantCulture, "{0:0.0} mm", s) });
        }

        /// <summary>
        /// Liest den ausgewählten Wert einer ComboBox. Gibt "" zurück wenn "Alle" oder nichts ausgewählt.
        /// </summary>
        private string GetComboBoxValue(ComboBox box)
        {
            if (box == null || box.SelectedIndex <= 0)
                return "";
            if (box.SelectedItem is ComboBoxItem item)
                return item.Content?.ToString() ?? "";
            return "";
        }

        /// <summary>
        /// KERN-METHODE: Filtert die Rohdaten und gruppiert sie neu.
        /// Wird bei JEDER Filter-Änderung aufgerufen.
        /// </summary>
        private void AktualisiereAnzeige()
        {
            // 1. Aktuelle Filter-Werte lesen
            var matFilter = GetComboBoxValue(MaterialFilterBox);
            var legFilter = GetComboBoxValue(LegierungFilterBox);
            var oberflFilter = GetComboBoxValue(OberflaecheFilterBox);

            var gueteFilter = "";
            if (GueteFilterBox != null && GueteFilterBox.Visibility == Visibility.Visible)
                gueteFilter = GetComboBoxValue(GueteFilterBox);

            double staerkeFilter = 0;
            var staerkeText = GetComboBoxValue(StaerkeFilterBox);
            if (!string.IsNullOrEmpty(staerkeText))
            {
                var parts = staerkeText.Split(' ');
                if (parts.Length > 0)
                    double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out staerkeFilter);
            }

            // 2. Rohdaten filtern
            IEnumerable<MaterialItem> gefiltert = _paletteMaterialien;

            if (!string.IsNullOrEmpty(matFilter))
                gefiltert = gefiltert.Where(m => string.Equals(m.MaterialArt, matFilter, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(legFilter))
                gefiltert = gefiltert.Where(m => string.Equals(m.Legierung, legFilter, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(oberflFilter))
                gefiltert = gefiltert.Where(m => string.Equals(m.Oberflaeche, oberflFilter, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(gueteFilter))
                gefiltert = gefiltert.Where(m => string.Equals(m.Guete, gueteFilter, StringComparison.OrdinalIgnoreCase));

            if (staerkeFilter > 0)
                gefiltert = gefiltert.Where(m => Math.Abs(m.Staerke - staerkeFilter) < 0.01);

            var gefiltertList = gefiltert.ToList();

            // 3. DataGrid aktualisieren
            _filteredItems.Clear();
            foreach (var item in gefiltertList)
                _filteredItems.Add(item);

            // 4. Statistik aktualisieren
            BerechneStat(gefiltertList);
        }

        /// <summary>
        /// Material-Filter geändert → Dynamische Filter + Güte-Sichtbarkeit anpassen
        /// </summary>
        private void OnMaterialSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingFilters)
                return;

            if (PaletteGrid?.ItemsSource == null || LegierungFilterBox == null ||
                OberflaecheFilterBox == null || GueteFilterBox == null)
                return;

            var selectedMaterial = GetComboBoxValue(MaterialFilterBox);

            // Güte nur bei Aluminium sichtbar
            GueteFilterBox.Visibility = string.IsNullOrEmpty(selectedMaterial) || selectedMaterial == "Aluminium"
                ? Visibility.Visible
                : Visibility.Collapsed;

            // Legierung + Oberfläche passend zum Material aktualisieren
            AktualisiereDynamischeFilter(string.IsNullOrEmpty(selectedMaterial) ? null : selectedMaterial);

            AktualisiereAnzeige();
        }

        /// <summary>
        /// Anderer Filter geändert (Legierung, Oberfläche, Güte, Stärke)
        /// </summary>
        private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingFilters)
                return;

            AktualisiereAnzeige();
        }

        /// <summary>
        /// Aktualisiert Legierung- und Oberfläche-ComboBoxen passend zum gewählten Material
        /// </summary>
        private void AktualisiereDynamischeFilter(string? selectedMaterial)
        {
            if (LegierungFilterBox == null || OberflaecheFilterBox == null)
                return;

            _isUpdatingFilters = true;

            try
            {
                // Aktuelle Auswahl merken
                var currentLeg = GetComboBoxValue(LegierungFilterBox);
                var currentOberfl = GetComboBoxValue(OberflaecheFilterBox);

                // Legierung neu füllen
                LegierungFilterBox.SelectionChanged -= OnFilterChanged;
                LegierungFilterBox.Items.Clear();
                LegierungFilterBox.Items.Add(new ComboBoxItem { Content = "Alle", IsSelected = true });

                IEnumerable<string> legierungen;
                if (string.IsNullOrEmpty(selectedMaterial))
                    legierungen = MaterialDefinitions.Legierungen.Values.SelectMany(x => x).Distinct();
                else if (MaterialDefinitions.Legierungen.TryGetValue(selectedMaterial, out var matLeg))
                    legierungen = matLeg;
                else
                    legierungen = Enumerable.Empty<string>();

                int selIdx = 0, idx = 1;
                foreach (var leg in legierungen.OrderBy(x => x))
                {
                    LegierungFilterBox.Items.Add(new ComboBoxItem { Content = leg });
                    if (leg == currentLeg) selIdx = idx;
                    idx++;
                }
                LegierungFilterBox.SelectedIndex = selIdx;
                LegierungFilterBox.SelectionChanged += OnFilterChanged;

                // Oberfläche neu füllen
                OberflaecheFilterBox.SelectionChanged -= OnFilterChanged;
                OberflaecheFilterBox.Items.Clear();
                OberflaecheFilterBox.Items.Add(new ComboBoxItem { Content = "Alle", IsSelected = true });

                IEnumerable<string> oberflaechen;
                if (string.IsNullOrEmpty(selectedMaterial))
                    oberflaechen = MaterialDefinitions.Oberflaechen.Values.SelectMany(x => x).Distinct();
                else if (MaterialDefinitions.Oberflaechen.TryGetValue(selectedMaterial, out var matOberfl))
                    oberflaechen = matOberfl;
                else
                    oberflaechen = Enumerable.Empty<string>();

                selIdx = 0; idx = 1;
                foreach (var oberfl in oberflaechen.OrderBy(x => x))
                {
                    OberflaecheFilterBox.Items.Add(new ComboBoxItem { Content = oberfl });
                    if (oberfl == currentOberfl) selIdx = idx;
                    idx++;
                }
                OberflaecheFilterBox.SelectedIndex = selIdx;
                OberflaecheFilterBox.SelectionChanged += OnFilterChanged;
            }
            finally
            {
                _isUpdatingFilters = false;
            }
        }

        /// <summary>
        /// Berechnet und zeigt Statistiken an
        /// </summary>
        private void BerechneStat(List<MaterialItem> gefilterteMaterialien)
        {
            // Gesamt (alle EU Palette Materialien)
            var gesamtGewicht = _paletteMaterialien.Sum(m => m.GewichtKg);
            var gesamtAnzahl = _paletteMaterialien.Count;
            var kapazitaet = 2000.0;
            var auslastung = kapazitaet > 0 ? (gesamtGewicht / kapazitaet) * 100 : 0;

            GesamtStatistik = $"📦 EU Palette: {gesamtAnzahl} Reste | Gesamt: {gesamtGewicht:N2} kg";
            AuslastungInfo = $"Auslastung: {auslastung:N0}% von {kapazitaet:N0} kg Kapazität";

            // Filter-Statistik (nur wenn gefiltert)
            if (gefilterteMaterialien.Count < _paletteMaterialien.Count)
            {
                var filterGewicht = gefilterteMaterialien.Sum(g => g.GewichtKg);
                var filterReste = gefilterteMaterialien.Count;
                FilterStatistik = $"→ Gefiltert: {filterReste} Reste | {filterGewicht:N2} kg";
            }
            else
            {
                FilterStatistik = "";
            }
        }

        private void OnSchliessen(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnMaterialDoppelklick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PaletteGrid.SelectedItem is not MaterialItem item)
                return;

            var dlg = new MaterialDialog(_sourceMaterialien) { Owner = this };
            dlg.SetEditMode(item);
            if (dlg.ShowDialog() == true)
            {
                var idx = _sourceMaterialien.IndexOf(item);
                if (idx >= 0)
                    _sourceMaterialien[idx] = dlg.Material;

                _paletteMaterialien = _sourceMaterialien
                    .Where(m => m != null && m.Lagerort == "EU Palette")
                    .ToList();

                HasChanges = true;
                AktualisiereAnzeige();
            }
        }
    }
}
