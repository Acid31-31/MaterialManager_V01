using System.Windows;

namespace MaterialManager_V01.Views
{
    public partial class ProduktionsBegruendungDialog : Window
    {
        public string Kommentar { get; private set; } = string.Empty;

        public ProduktionsBegruendungDialog()
        {
            InitializeComponent();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            Kommentar = (KommentarTextBox.Text ?? string.Empty).Trim();
            DialogResult = true;
            Close();
        }

        private void OnSkipClick(object sender, RoutedEventArgs e)
        {
            Kommentar = string.Empty;
            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
