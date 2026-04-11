using System.Windows;

namespace MaterialManager_V01.Views
{
    public partial class ResteReservierungDialog : Window
    {
        public string AuftragNr { get; private set; } = string.Empty;
        public bool DeleteMaterialFromLager { get; private set; }
        public bool IsNachproduktion { get; private set; }

        public ResteReservierungDialog(string existingAuftrag)
        {
            InitializeComponent();

            var existing = existingAuftrag ?? string.Empty;
            if (string.Equals(existing.Trim(), "Nachproduktion", System.StringComparison.OrdinalIgnoreCase))
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

        private void OnOk(object sender, RoutedEventArgs e)
        {
            DeleteMaterialFromLager = DeleteMaterialCheck.IsChecked == true;
            IsNachproduktion = NachproduktionCheck.IsChecked == true;

            if (DeleteMaterialFromLager)
            {
                AuftragNr = string.Empty;
                DialogResult = true;
                return;
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
