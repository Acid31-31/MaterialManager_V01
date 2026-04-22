using System;
using System.Windows;
using System.Windows.Input;

namespace MaterialManager_V01.Views
{
    public partial class ResteReservierungDialog : Window
    {
        public string AuftragNr { get; private set; } = string.Empty;
        public bool DeleteMaterialFromLager { get; private set; }
        public bool IsNachproduktion { get; private set; }
        /// <summary>0 = alle Stück reservieren; > 0 = nur diese Anzahl</summary>
        public int GewuenschteStueckzahl { get; private set; }

        private readonly int _maxStueckzahl;

        public ResteReservierungDialog(string existingAuftrag, int maxStueckzahl = 0)
        {
            InitializeComponent();
            _maxStueckzahl = maxStueckzahl;

            if (maxStueckzahl > 1)
            {
                StueckzahlPanel.Visibility = Visibility.Visible;
                VerfuegbarText.Text = $"Verfügbar: {maxStueckzahl} Stück";
                StueckzahlBox.Text = maxStueckzahl.ToString();
                StueckzahlBox.TextChanged += OnStueckzahlChanged;
                UpdateRestAnzeige();
            }

            var existing = existingAuftrag ?? string.Empty;
            if (string.Equals(existing.Trim(), "Nachproduktion", StringComparison.OrdinalIgnoreCase))
            {
                NachproduktionCheck.IsChecked = true;
                AuftragBox.Text = string.Empty;
                AuftragBox.IsEnabled = false;
                IsNachproduktion = true;
            }
            else
            {
                AuftragBox.Text = existing;
                AuftragBox.SelectAll();
                AuftragBox.Focus();
            }
        }

        private void OnStueckzahlPreviewInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void OnStueckzahlChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateRestAnzeige();
        }

        private void UpdateRestAnzeige()
        {
            if (RestText == null || _maxStueckzahl <= 1)
                return;

            if (int.TryParse(StueckzahlBox?.Text, out var gewuenscht) && gewuenscht > 0 && gewuenscht < _maxStueckzahl)
            {
                var rest = _maxStueckzahl - gewuenscht;
                RestText.Text = $"→ {rest} Stück\nbleiben im Lager";
                RestText.Foreground = System.Windows.Media.Brushes.Orange;
            }
            else
            {
                RestText.Text = "→ alle Stück\nwerden reserviert";
                RestText.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            DeleteMaterialFromLager = DeleteMaterialCheck.IsChecked == true;
            IsNachproduktion = NachproduktionCheck.IsChecked == true;

            if (DeleteMaterialFromLager)
            {
                AuftragNr = string.Empty;
                GewuenschteStueckzahl = 0;
                DialogResult = true;
                return;
            }

            // Stückzahl auslesen
            if (_maxStueckzahl > 1 && StueckzahlPanel.Visibility == Visibility.Visible)
            {
                if (int.TryParse(StueckzahlBox.Text, out var st) && st > 0 && st < _maxStueckzahl)
                    GewuenschteStueckzahl = st;
                else
                    GewuenschteStueckzahl = 0; // alle
            }

            AuftragNr = IsNachproduktion
                ? "Nachproduktion"
                : (AuftragBox.Text?.Trim() ?? string.Empty);

            DialogResult = true;
        }

        private void OnNachproduktionChecked(object sender, RoutedEventArgs e)
        {
            AuftragBox.Text = string.Empty;
            AuftragBox.IsEnabled = false;
        }

        private void OnNachproduktionUnchecked(object sender, RoutedEventArgs e)
        {
            AuftragBox.IsEnabled = true;
            AuftragBox.Focus();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
