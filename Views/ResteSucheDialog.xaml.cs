using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace MaterialManager_V01.Views
{
    public partial class ResteSucheDialog : Window, INotifyPropertyChanged
    {
        public string Material { get; private set; } = "";
        public string Legierung { get; private set; } = "";
        public double? Staerke { get; private set; }
        public int? Laenge { get; private set; }
        public int? Breite { get; private set; }
        public double ToleranzProzent { get; private set; } = 10.0;
        public string Form { get; private set; } = "Alle";

        private List<string> _legierungen = new List<string> { "Alle" };
        public List<string> Legierungen
        {
            get => _legierungen;
            set
            {
                _legierungen = value;
                OnPropertyChanged(nameof(Legierungen));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ResteSucheDialog()
        {
            InitializeComponent();
            DataContext = this;

            // Initial mit allen Legierungen starten
            Legierungen = new List<string> 
            { 
                "Alle", "S235", "S355", "S460", "HB400", "HB500", 
                "1.4301", "1.4571", "EN AW-5754", "EN AW-5083" 
            };
        }

        private void MaterialBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (MaterialBox.SelectedItem is System.Windows.Controls.ComboBoxItem item)
            {
                var material = item.Content.ToString() ?? "Alle";
                UpdateLegierungen(material);
            }
        }

        private void UpdateLegierungen(string material)
        {
            var legierungen = new List<string> { "Alle" };

            switch (material)
            {
                case "Stahl":
                    legierungen.AddRange(new[] { "S235", "S355", "S460", "HB400", "HB500" });
                    break;
                case "Edelstahl":
                    legierungen.AddRange(new[] { "1.4301", "1.4571" });
                    break;
                case "Aluminium":
                    legierungen.AddRange(new[] { "EN AW-5754", "EN AW-5083" });
                    break;
                case "Alle":
                    legierungen.AddRange(new[] { "S235", "S355", "S460", "HB400", "HB500", "1.4301", "1.4571", "EN AW-5754", "EN AW-5083" });
                    break;
            }

            Legierungen = legierungen;

            // Setze Auswahl auf "Alle" zurück, wenn LegierungBox initialisiert ist
            if (LegierungBox != null)
            {
                LegierungBox.SelectedIndex = 0;
            }
        }

        private void OnSuchen(object sender, RoutedEventArgs e)
        {
            // Material
            if (MaterialBox.SelectedItem is System.Windows.Controls.ComboBoxItem matItem)
            {
                var matText = matItem.Content.ToString();
                if (matText != "Alle")
                    Material = matText ?? "";
            }

            // Legierung
            if (LegierungBox.SelectedItem is string legText && legText != "Alle")
            {
                Legierung = legText;
            }

            // Stärke
            if (StaerkeBox.SelectedItem is System.Windows.Controls.ComboBoxItem staItem)
            {
                var staText = staItem.Content.ToString();
                if (staText != "Alle" && !string.IsNullOrWhiteSpace(staText))
                {
                    if (double.TryParse(staText.Replace(',', '.'), 
                        System.Globalization.NumberStyles.Any, 
                        System.Globalization.CultureInfo.InvariantCulture, 
                        out var sta))
                    {
                        Staerke = sta;
                    }
                }
            }

            // Maße
            if (!string.IsNullOrWhiteSpace(MassBox.Text))
            {
                var mass = MassBox.Text.ToLower().Replace('×', 'x').Trim();
                var parts = mass.Split('x');
                if (parts.Length == 2)
                {
                    if (int.TryParse(parts[0].Trim(), out var l))
                        Laenge = l;
                    if (int.TryParse(parts[1].Trim(), out var b))
                        Breite = b;
                }
            }

            // Form
            if (FormBox.SelectedItem is System.Windows.Controls.ComboBoxItem formItem)
            {
                Form = formItem.Content.ToString() ?? "Alle";
            }

            // Toleranz
            if (!string.IsNullOrWhiteSpace(ToleranzBox.Text) && double.TryParse(ToleranzBox.Text, out var tol))
            {
                ToleranzProzent = tol;
            }

            DialogResult = true;
            Close();
        }

        private void OnAbbrechen(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
