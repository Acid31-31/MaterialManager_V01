using System.Windows;

namespace MaterialManager_V01.Views
{
    public partial class ResteReservierungDialog : Window
    {
        public string AuftragNr { get; private set; } = string.Empty;

        public ResteReservierungDialog(string existingAuftrag)
        {
            InitializeComponent();
            AuftragBox.Text = existingAuftrag ?? string.Empty;
            AuftragBox.SelectAll();
            AuftragBox.Focus();
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            AuftragNr = AuftragBox.Text?.Trim() ?? string.Empty;
            DialogResult = true;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
