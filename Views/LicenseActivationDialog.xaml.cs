using System.Windows;
using System.Windows.Controls;

namespace MaterialManager_V01.Views
{
    public partial class LicenseActivationDialog : Window
    {
        public string HardwareId { get; private set; }

        public LicenseActivationDialog()
        {
            InitializeComponent();
            DataContext = this;
            
            HardwareId = Services.LicenseService.GetHardwareId();
        }

        private void OnCopyHardwareId(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(HardwareId);
                MessageBox.Show(
                    "Hardware-ID in Zwischenablage kopiert!\n\n" +
                    "Senden Sie diese ID beim Lizenzkauf mit.",
                    "Kopiert",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show(
                    "Fehler beim Kopieren der Hardware-ID.",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnLicenseKeyChanged(object sender, TextChangedEventArgs e)
        {
            var text = LicenseKeyTextBox.Text.Replace("-", "").ToUpper();
            
            if (text.Length > 0)
            {
                var formatted = "";
                for (int i = 0; i < text.Length; i++)
                {
                    if (i > 0 && i % 4 == 0)
                        formatted += "-";
                    formatted += text[i];
                }
                
                if (formatted != LicenseKeyTextBox.Text)
                {
                    var cursorPos = LicenseKeyTextBox.SelectionStart;
                    LicenseKeyTextBox.Text = formatted;
                    LicenseKeyTextBox.SelectionStart = cursorPos + (formatted.Length > LicenseKeyTextBox.Text.Length ? 1 : 0);
                }
            }

            ActivateButton.IsEnabled = 
                !string.IsNullOrWhiteSpace(LicenseKeyTextBox.Text) &&
                LicenseKeyTextBox.Text.Length >= 19 &&
                !string.IsNullOrWhiteSpace(RegisteredToTextBox.Text);
        }

        private void OnActivate(object sender, RoutedEventArgs e)
        {
            var licenseKey = LicenseKeyTextBox.Text.Trim();
            var registeredTo = RegisteredToTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(licenseKey) || string.IsNullOrWhiteSpace(registeredTo))
            {
                MessageBox.Show(
                    "Bitte füllen Sie alle Felder aus.",
                    "Fehlende Daten",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (Services.LicenseService.ActivateFullLicense(licenseKey, registeredTo))
            {
                MessageBox.Show(
                    "✓ Lizenz erfolgreich aktiviert!\n\n" +
                    $"Registriert auf: {registeredTo}\n\n" +
                    "Die Anwendung wird neu gestartet.",
                    "Aktivierung erfolgreich",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(
                    "❌ Lizenzschlüssel ungültig!\n\n" +
                    "Mögliche Ursachen:\n" +
                    "• Falscher Lizenzschlüssel\n" +
                    "• Hardware-ID stimmt nicht überein\n" +
                    "• Lizenz bereits auf anderem PC aktiviert\n\n" +
                    "Bitte kontaktieren Sie den Support.",
                    "Aktivierung fehlgeschlagen",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
