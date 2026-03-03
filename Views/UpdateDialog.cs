using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class UpdateDialog : Window, INotifyPropertyChanged
    {
        private readonly UpdateCheckResult _updateInfo;
        private readonly string _uiUpdateLogPath;

        private string _versionInfo = "Aktuell: v1.0.0 → Neu: v1.0.0";
        public string VersionInfo { get => _versionInfo; set { _versionInfo = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VersionInfo))); } }

        private string _changelog = "Kein Changelog verfügbar.";
        public string Changelog { get => _changelog; set { _changelog = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Changelog))); } }

        public event PropertyChangedEventHandler? PropertyChanged;

        public UpdateDialog(UpdateCheckResult updateInfo)
        {
            InitializeComponent();
            DataContext = this;
            _updateInfo = updateInfo;
            _uiUpdateLogPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MaterialManager_V01",
                "update_ui.log");
            LoadUpdateInfo();
        }

        private void LoadUpdateInfo()
        {
            VersionInfo = $"Aktuell: {_updateInfo.CurrentVersion} → Neu: {_updateInfo.LatestVersion}";
            Changelog = string.IsNullOrWhiteSpace(_updateInfo.Changelog)
                ? "Kein Changelog verfügbar."
                : _updateInfo.Changelog;
        }

        private async void OnInstallieren(object sender, RoutedEventArgs e)
        {
            try
            {
                AppendUiLog("Update-Installation gestartet.");

                if (sender is FrameworkElement fe)
                    fe.IsEnabled = false;

                Mouse.OverrideCursor = Cursors.Wait;

                var prepared = await GitHubUpdateService.PrepareUpdateAsync(_updateInfo, AppDomain.CurrentDomain.BaseDirectory);
                if (!string.IsNullOrWhiteSpace(prepared.LogPath))
                    AppendUiLog($"Service-Log: {prepared.LogPath}");

                if (!string.IsNullOrWhiteSpace(prepared.ErrorMessage))
                {
                    AppendUiLog($"FEHLER: {prepared.ErrorMessage}");
                    MessageBox.Show($"Update konnte nicht vorbereitet werden:\n{prepared.ErrorMessage}", "Update", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (prepared.RunExecutableDirectly && !string.IsNullOrWhiteSpace(prepared.InstallerExecutablePath))
                {
                    AppendUiLog($"Starte Installer-EXE: {prepared.InstallerExecutablePath}");
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = prepared.InstallerExecutablePath,
                        UseShellExecute = true
                    });

                    Application.Current.Shutdown();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(prepared.UpdateScriptPath))
                {
                    AppendUiLog($"Starte Update-Skript: {prepared.UpdateScriptPath}");
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c \"{prepared.UpdateScriptPath}\"",
                        UseShellExecute = true,
                        CreateNoWindow = false
                    });

                    Application.Current.Shutdown();
                    return;
                }

                AppendUiLog("Kein installierbares Update gefunden.");
                MessageBox.Show("Kein installierbares Update gefunden.", "Update", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                AppendUiLog($"Exception: {ex.Message}");
                MessageBox.Show($"Fehler beim Installieren:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
                if (sender is FrameworkElement fe)
                    fe.IsEnabled = true;
            }
        }

        private void AppendUiLog(string message)
        {
            try
            {
                var dir = Path.GetDirectoryName(_uiUpdateLogPath);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.AppendAllText(_uiUpdateLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch { }
        }

        private void OnSpaeter(object sender, RoutedEventArgs e) => Close();
    }
}
