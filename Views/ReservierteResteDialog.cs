using MaterialManager_V01.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

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

        private void OnSchliessen(object sender, RoutedEventArgs e) => Close();
    }
}
