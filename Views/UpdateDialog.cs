using System.ComponentModel;
using System.Windows;

namespace MaterialManager_V01.Views
{
    public partial class UpdateDialog : Window, INotifyPropertyChanged
    {
        private string _versionInfo = "Aktuell: v1.0.0 → Neu: v2.0.0";
        public string VersionInfo { get => _versionInfo; set { _versionInfo = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VersionInfo))); } }

        private string _changelog = "• Neue Funktionen\n• Verbesserungen\n• Bugfixes";
        public string Changelog { get => _changelog; set { _changelog = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Changelog))); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public UpdateDialog()
        {
            InitializeComponent();
            DataContext = this;
            LoadUpdateInfo();
        }

        private void LoadUpdateInfo()
        {
            try
            {
                VersionInfo = "Aktuell: v1.0.0 → Neu: v2.0.0";
                Changelog = "• Material-Reservierung\n• Auto-Sync zwischen PCs\n• Verbesserte Performance\n• Bugfixes";
            }
            catch { }
        }

        private void OnInstallieren(object sender, RoutedEventArgs e)
        {
            try
            {
                var updatePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updates", "v2.0.0", "INSTALL_UPDATE.bat");
                if (System.IO.File.Exists(updatePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = updatePath,
                        UseShellExecute = true
                    });
                    Application.Current.Shutdown();
                }
                else
                {
                    MessageBox.Show("Update-Datei nicht gefunden!", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Fehler beim Installieren:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnSpaeter(object sender, RoutedEventArgs e) => Close();
    }
}
