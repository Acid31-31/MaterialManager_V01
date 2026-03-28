using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace MaterialManager_V01.Views
{
    public partial class AuftragBuchungDialog : Window
    {
        private readonly int _maxMenge;
        private readonly bool _requirePdf;

        public string AuftragNr { get; private set; } = string.Empty;
        public int Menge { get; private set; }
        public string PdfPfad { get; private set; } = string.Empty;

        public AuftragBuchungDialog(int maxMenge, string existingAuftrag = "", string existingPdfPfad = "", bool requirePdf = false)
        {
            InitializeComponent();
            _maxMenge = maxMenge;
            _requirePdf = requirePdf;
            AuftragTextBox.Text = existingAuftrag ?? string.Empty;
            PdfPfad = existingPdfPfad ?? string.Empty;
            PdfTextBox.Text = PdfPfad;
            MengeTextBox.Text = maxMenge > 1 ? "1" : maxMenge.ToString();
            InfoTextBlock.Text = requirePdf
                ? $"Verfügbar: {maxMenge} Stück\nBitte Auftragsnummer, Menge und PDF-Datei auswählen."
                : $"Verfügbar: {maxMenge} Stück";
            Loaded += (_, _) => AuftragTextBox.Focus();
        }

        private void OnBrowsePdf(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "PDF-Datei für Auftrag auswählen",
                Filter = "PDF-Dateien (*.pdf)|*.pdf|Alle Dateien (*.*)|*.*",
                CheckFileExists = true
            };

            if (!string.IsNullOrWhiteSpace(PdfPfad) && File.Exists(PdfPfad))
                dlg.InitialDirectory = Path.GetDirectoryName(PdfPfad);

            if (dlg.ShowDialog() != true)
                return;

            PdfPfad = dlg.FileName;
            PdfTextBox.Text = PdfPfad;
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

            if (_requirePdf && string.IsNullOrWhiteSpace(PdfPfad))
            {
                MessageBox.Show("Bitte eine PDF-Datei auswählen.", "Auftrag", MessageBoxButton.OK, MessageBoxImage.Information);
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