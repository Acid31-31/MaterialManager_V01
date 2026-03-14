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
        private readonly bool _autoStartInstall;
        private System.Threading.CancellationTokenSource? _cts;

        private string _versionInfo = "Aktuell: v1.0.0 → Neu: v1.0.0";
        public string VersionInfo { get => _versionInfo; set { _versionInfo = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VersionInfo))); } }

        private string _changelog = "Kein Changelog verfügbar.";
        public string Changelog { get => _changelog; set { _changelog = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Changelog))); } }

        private int _downloadProgress;
        public int DownloadProgress { get => _downloadProgress; set { _downloadProgress = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DownloadProgress))); } }

        private string _downloadStatus = "Bereit.";
        public string DownloadStatus { get => _downloadStatus; set { _downloadStatus = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DownloadStatus))); } }

        public event PropertyChangedEventHandler? PropertyChanged;

        public UpdateDialog(UpdateCheckResult updateInfo, bool autoStartInstall = false)
        {
            InitializeComponent();
            DataContext = this;
            _updateInfo = updateInfo;
            _autoStartInstall = autoStartInstall;
            _uiUpdateLogPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MaterialManager_V01",
                "update_ui.log");
            LoadUpdateInfo();

            if (_autoStartInstall)
            {
                Loaded += async (_, __) =>
                {
                    await System.Threading.Tasks.Task.Delay(200);
                    OnInstallieren(null!, new RoutedEventArgs());
                };
            }
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
                if (sender is FrameworkElement fe)
                    fe.IsEnabled = false;

                Mouse.OverrideCursor = Cursors.Wait;
                DownloadProgress = 0;
                DownloadStatus = "Update wird vorbereitet...";

                _cts = new System.Threading.CancellationTokenSource();
                var progress = new Progress<int>(p =>
                {
                    DownloadProgress = p;
                    DownloadStatus = $"Download: {p}%";
                });

                var prepared = await GitHubUpdateService.PrepareUpdateAsync(_updateInfo, progress, _cts.Token);
                if (!string.IsNullOrWhiteSpace(prepared.ErrorMessage))
                {
                    MessageBox.Show($"Update fehlgeschlagen:\n{prepared.ErrorMessage}", "Update", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(prepared.InstallerExecutablePath) || !File.Exists(prepared.InstallerExecutablePath))
                {
                    MessageBox.Show("Kein installierbares Update gefunden.", "Update", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var installerPath = prepared.InstallerExecutablePath;

                var confirm = MessageBox.Show(
                    "Das Update wird jetzt gestartet.\nDie Anwendung wird geschlossen.\n\nWeiter?",
                    "Update",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (confirm != MessageBoxResult.Yes)
                    return;

                Process? startedProcess;

                if (installerPath.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                {
                    var msiLog = Path.Combine(Path.GetTempPath(), "MaterialManager_msi_install.log");
                    var args = $"/i \"{installerPath}\" /passive /norestart /l*v \"{msiLog}\"";

                    startedProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = "msiexec.exe",
                        Arguments = args,
                        UseShellExecute = true
                    });

                    AppendUiLog($"MSI gestartet: {installerPath}");
                    AppendUiLog($"MSI-Log: {msiLog}");
                }
                else
                {
                    startedProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = installerPath,
                        UseShellExecute = true
                    });

                    AppendUiLog($"Installer gestartet: {installerPath}");
                }

                if (!string.IsNullOrWhiteSpace(prepared.LogPath))
                    AppendUiLog($"Prepare-Log: {prepared.LogPath}");

                await System.Threading.Tasks.Task.Delay(1200);

                if (startedProcess == null)
                {
                    AppendUiLog("Installer konnte nicht gestartet werden (Process.Start gab null zurück).");
                    MessageBox.Show(
                        "Update konnte nicht gestartet werden.\n\nBitte über 'Release im Browser' manuell installieren.",
                        "Update",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    DownloadStatus = "Installer konnte nicht gestartet werden.";
                    return;
                }

                if (startedProcess.HasExited)
                {
                    AppendUiLog($"Installer ist sofort beendet (ExitCode={startedProcess.ExitCode}).");
                    MessageBox.Show(
                        "Der Installer wurde gestartet, aber sofort beendet.\n\nBitte über 'Release im Browser' manuell installieren.",
                        "Update",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    DownloadStatus = "Installer wurde beendet.";
                    return;
                }

                DownloadStatus = "Installer läuft. Anwendung wird geschlossen...";
                Application.Current.Shutdown();
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
                _cts?.Dispose();
                _cts = null;
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

        private void OnOpenRelease(object sender, RoutedEventArgs e)
        {
            try
            {
                var url = _updateInfo.ReleasePageUrl;
                if (string.IsNullOrWhiteSpace(url))
                {
                    MessageBox.Show("Keine Release-URL verfügbar.", "Update", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnSpaeter(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            Close();
        }
    }
}
