using System.Windows;
using System.Windows.Controls;

namespace MaterialManager_V01.Views
{
    public partial class LicenseActivationDialog : Window
    {
        public string HardwareId { get; private set; }
        private bool _isFormatting;

        public LicenseActivationDialog()
        {
            InitializeComponent();
            DataContext = this;

            HardwareId = Services.LicenseService.GetHardwareId();
            UpdateActivateButtonState();
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
            if (_isFormatting)
            {
                UpdateActivateButtonState();
                return;
            }

            var raw = (LicenseKeyTextBox.Text ?? string.Empty)
                .ToUpperInvariant()
                .Replace("-", "")
                .Trim();

            if (raw.StartsWith("MM"))
                raw = raw.Substring(2);

            if (raw.Length > 16)
                raw = raw.Substring(0, 16);

            var formatted = string.IsNullOrEmpty(raw)
                ? string.Empty
                : $"MM-{raw.Substring(0, System.Math.Min(4, raw.Length))}";

            if (raw.Length > 4)
                formatted += $"-{raw.Substring(4, System.Math.Min(4, raw.Length - 4))}";
            if (raw.Length > 8)
                formatted += $"-{raw.Substring(8, System.Math.Min(4, raw.Length - 8))}";
            if (raw.Length > 12)
                formatted += $"-{raw.Substring(12, System.Math.Min(4, raw.Length - 12))}";

            if (LicenseKeyTextBox.Text != formatted)
            {
                _isFormatting = true;
                LicenseKeyTextBox.Text = formatted;
                LicenseKeyTextBox.SelectionStart = LicenseKeyTextBox.Text.Length;
                _isFormatting = false;
            }

            UpdateActivateButtonState();
        }

        private void OnRegisteredToChanged(object sender, TextChangedEventArgs e)
        {
            UpdateActivateButtonState();
        }

        private void UpdateActivateButtonState()
        {
            ActivateButton.IsEnabled =
                !string.IsNullOrWhiteSpace(LicenseKeyTextBox.Text) &&
                LicenseKeyTextBox.Text.Trim().Length >= 22 &&
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
                // Hauptfenster-Status sofort aktualisieren
                var status = Services.LicenseService.GetStatusMessage();
                if (Application.Current?.MainWindow != null)
                {
                    Application.Current.MainWindow.Title = $"MaterialManager V01 - {status}";
                }

                MessageBox.Show(
                    "✓ Lizenz erfolgreich aktiviert!\n\n" +
                    $"Registriert auf: {registeredTo}\n\n" +
                    "Die Anzeige wurde auf Vollversion aktualisiert.",
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
                    "• Name/Firma stimmt nicht exakt überein\n\n" +
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
