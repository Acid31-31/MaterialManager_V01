using MaterialManager_V01.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
            var savePath = Services.NetzwerkService.GetSavePath();
            if (System.IO.File.Exists(savePath))
            {
                var alleMaterialien = ExcelService.Import(savePath);
                ReservierteReste.Clear();
                foreach (var item in alleMaterialien.Where(m => !string.IsNullOrEmpty(m.AuftragNr)))
                    ReservierteReste.Add(item);
            }
        }

        private void OnGridMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ReservierteResteGrid.SelectedItem is not MaterialItem item)
                return;

            if (string.IsNullOrWhiteSpace(item.PdfPfad))
            {
                MessageBox.Show("Für dieses reservierte Material ist keine PDF hinterlegt.", "PDF öffnen", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!System.IO.File.Exists(item.PdfPfad))
            {
                MessageBox.Show($"PDF-Datei nicht gefunden:\n{item.PdfPfad}", "PDF öffnen", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = item.PdfPfad,
                    UseShellExecute = true
                });
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"PDF konnte nicht geöffnet werden:\n{ex.Message}", "PDF öffnen", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnSchliessen(object sender, RoutedEventArgs e) => Close();
    }
}
