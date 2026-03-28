using MaterialManager_V01.Models;
using MaterialManager_V01.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MaterialManager_V01.Views
{
    public partial class ReservierteResteDialog : Window
    {
        public ObservableCollection<MaterialItem> ReservierteReste { get; set; } = new();

        public ReservierteResteDialog()
        {
            InitializeComponent();
            DataContext = this;
            Width = SystemParameters.PrimaryScreenWidth * 0.9;
            Height = SystemParameters.PrimaryScreenHeight * 0.9;
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
            LoadReservierteReste();
        }

        private void LoadReservierteReste()
        {
            var alleMaterialien = MaterialDataService.LoadAllMaterials();
            ReservierteReste.Clear();
            foreach (var item in alleMaterialien.Where(m => !string.IsNullOrEmpty(m.AuftragNr)))
                ReservierteReste.Add(item);
        }

        private void OnGridMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ReservierteResteGrid.SelectedItem is not MaterialItem item)
                return;

            OpenPdf(item);
        }

        private void OnPdfClick(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not MaterialItem item)
                return;

            OpenPdf(item);
            e.Handled = true;
        }

        private void OnPdfButtonClick(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not MaterialItem item)
                return;

            OpenPdf(item);
            e.Handled = true;
        }

        private void OpenPdf(MaterialItem item)
        {
            var pdfPfad = !string.IsNullOrWhiteSpace(item.PdfPfadAngefangeneTafel)
                ? item.PdfPfadAngefangeneTafel
                : item.PdfPfad;

            if (string.IsNullOrWhiteSpace(pdfPfad))
            {
                MessageBox.Show("Für dieses reservierte Material ist keine PDF hinterlegt.", "PDF-Vorschau", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!System.IO.File.Exists(pdfPfad))
            {
                MessageBox.Show($"PDF-Datei nicht gefunden:\n{pdfPfad}", "PDF-Vorschau", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dlg = new PdfPreviewDialog(pdfPfad) { Owner = this };
                dlg.ShowDialog();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"PDF-Vorschau konnte nicht geöffnet werden:\n{ex.Message}", "PDF-Vorschau", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnSchliessen(object sender, RoutedEventArgs e) => Close();
    }
}
