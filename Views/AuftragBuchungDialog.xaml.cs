using System.Windows;

namespace MaterialManager_V01.Views
{
    public partial class AuftragBuchungDialog : Window
    {
        private readonly int _maxMenge;

        public string AuftragNr { get; private set; } = string.Empty;
        public int Menge { get; private set; }

        public AuftragBuchungDialog(int maxMenge, string existingAuftrag = "")
        {
            InitializeComponent();
            _maxMenge = maxMenge;
            AuftragTextBox.Text = existingAuftrag ?? string.Empty;
            MengeTextBox.Text = maxMenge > 1 ? "1" : maxMenge.ToString();
            InfoTextBlock.Text = $"Verfügbar: {maxMenge} Stück";
            Loaded += (_, _) => AuftragTextBox.Focus();
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            AuftragNr = AuftragTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(AuftragNr))
            {
                MessageBox.Show("Bitte eine Auftragsnummer eingeben.", "Auftrag", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(MengeTextBox.Text?.Trim(), out var menge) || menge <= 0 || menge > _maxMenge)
            {
                MessageBox.Show($"Bitte eine Menge zwischen 1 und {_maxMenge} eingeben.", "Auftrag", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Menge = menge;
            DialogResult = true;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}