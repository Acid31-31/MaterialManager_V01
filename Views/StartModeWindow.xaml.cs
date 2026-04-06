using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class StartModeWindow : Window
    {
        private static readonly string GeoSucheSettingsPath = Path.Combine(PathService.DataDirectory, "geosuche.settings.json");
        private static readonly string[] GeoSucheKnownPaths =
        {
            @"C:\Users\hoelz.WIN-G2OC48399EJ\source\repos\Acid31-31\Arbeitsvorbereitung\GeoArbeitsvorbereitung\bin\Release\net8.0-windows\win-x64\GeoArbeitsvorbereitung.exe",
            @"C:\Users\hoelz.WIN-G2OC48399EJ\source\repos\Acid31-31\Arbeitsvorbereitung\GeoArbeitsvorbereitung\bin\Debug\net8.0-windows\GeoArbeitsvorbereitung.exe"
        };

        public StartModeWindow()
        {
            InitializeComponent();
            RefreshLicenseTitle();
            FitToWorkArea();
        }

        private void RefreshLicenseTitle()
        {
            var mode = LicenseService.GetLicenseModeText();
            var device = LicenseService.GetDeviceUsageText();
            Title = string.IsNullOrWhiteSpace(device)
                ? $"MaterialManager V01 - {mode}"
                : $"MaterialManager V01 - {mode} | {device}";
        }

        private void FitToWorkArea()
        {
            var wa = SystemParameters.WorkArea;
            Left = wa.Left;
            Top = wa.Top;
            Width = wa.Width;
            Height = wa.Height;
            MaxWidth = wa.Width;
            MaxHeight = wa.Height;
        }

        private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private async void OnCheckForUpdates(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var result = await GitHubUpdateService.CheckForUpdatesAsync();

                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    MessageBox.Show($"Update-Prüfung fehlgeschlagen:\n{result.ErrorMessage}", "Update", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!result.IsUpdateAvailable || string.IsNullOrWhiteSpace(result.DownloadUrl))
                {
                    MessageBox.Show($"Sie haben die neueste Version ({result.CurrentVersion}).", "Update", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dlg = new UpdateDialog(result) { Owner = this };
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler bei Update-Prüfung:\n{ex.Message}", "Update", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void OnProgrammHilfe(object sender, RoutedEventArgs e)
        {
            var dlg = new ProgrammHilfeDialog { Owner = this };
            dlg.ShowDialog();
        }

        private void OnOpenNetworkFolder(object sender, RoutedEventArgs e)
        {
            try
            {
                NetzwerkService.OpenAktivenDatenordnerImExplorer();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Netzwerkordner konnte nicht geöffnet werden:\n{ex.Message}", "Start", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnCleanupUpdateProcessesClick(object sender, RoutedEventArgs e)
        {
            var killed = 0;
            var failed = 0;

            foreach (var processName in new[] { "UpdateInstaller", "updater" })
            {
                Process[] processes;
                try
                {
                    processes = Process.GetProcessesByName(processName);
                }
                catch
                {
                    continue;
                }

                foreach (var process in processes)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(true);
                            process.WaitForExit(3000);
                            killed++;
                        }
                    }
                    catch
                    {
                        failed++;
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }

            if (killed == 0 && failed == 0)
            {
                MessageBox.Show("Es waren keine hängenden Update-Prozesse aktiv.", "Update-Prozesse", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (failed > 0)
            {
                MessageBox.Show($"{killed} Update-Prozesse beendet, {failed} konnten nicht beendet werden.\n\nBei Bedarf die App einmal als Administrator starten und erneut ausführen.", "Update-Prozesse", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"{killed} hängende Update-Prozesse wurden beendet.", "Update-Prozesse", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnSelectOperatorClick(object sender, RoutedEventArgs e)
        {
            _ = PromptForOperatorName();
        }

        private void OnMinimizeWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnMaximizeRestoreWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void OnCloseWindow(object sender, RoutedEventArgs e)
        {
            OnCloseClick(sender, e);
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private bool PromptForOperatorName()
        {
            var dlg = new LaserUserSelectionWindow { Owner = this };
            return dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.SelectedName);
        }

        private void OnStandardClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PromptForOperatorName())
                    return;

                var window = new MainWindow();
                Application.Current.MainWindow = window;
                window.Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hauptprogramm konnte nicht geöffnet werden:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnLagerClick(object sender, RoutedEventArgs e)
        {
            if (!PromptForOperatorName())
                return;

            var window = new LagerDemoWindow();
            Application.Current.MainWindow = window;
            window.Show();
            Close();
        }

        private void OnTafelplanungClick(object sender, RoutedEventArgs e)
        {
            if (!PromptForOperatorName())
                return;

            var window = new TafelplanungWindow();
            Application.Current.MainWindow = window;
            window.Show();
            Close();
        }

        private void OnLaserClick(object sender, RoutedEventArgs e)
        {
            if (!PromptForOperatorName())
                return;

            var window = new LaserDemoWindow();
            Application.Current.MainWindow = window;
            window.Show();
            Close();
        }

        private void OnKantbankClick(object sender, RoutedEventArgs e)
        {
            if (!PromptForOperatorName())
                return;

            var window = new KantbankDemoWindow();
            Application.Current.MainWindow = window;
            window.Show();
            Close();
        }

        private void OnGeoSucheClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var exePath = ResolveGeoSuchePath();
                if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                {
                    var dlg = new OpenFileDialog
                    {
                        Title = "Geo Suche EXE auswählen",
                        Filter = "Anwendung (*.exe)|*.exe|Alle Dateien (*.*)|*.*",
                        CheckFileExists = true,
                        InitialDirectory = @"C:\Users\hoelz.WIN-G2OC48399EJ\source\repos\Acid31-31\Arbeitsvorbereitung"
                    };

                    if (dlg.ShowDialog() != true)
                        return;

                    exePath = dlg.FileName;
                    SaveGeoSuchePath(exePath);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Geo Suche konnte nicht gestartet werden:\n{ex.Message}", "Start", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static string ResolveGeoSuchePath()
        {
            var saved = LoadGeoSuchePath();
            if (!string.IsNullOrWhiteSpace(saved) && File.Exists(saved))
                return saved;

            foreach (var p in GeoSucheKnownPaths)
            {
                if (File.Exists(p))
                {
                    SaveGeoSuchePath(p);
                    return p;
                }
            }

            return string.Empty;
        }

        private static string LoadGeoSuchePath()
        {
            try
            {
                if (!File.Exists(GeoSucheSettingsPath))
                    return string.Empty;

                var json = File.ReadAllText(GeoSucheSettingsPath);
                var dto = JsonSerializer.Deserialize<GeoSucheSettingsDto>(json);
                return dto?.ExePath?.Trim() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void SaveGeoSuchePath(string exePath)
        {
            try
            {
                var dir = Path.GetDirectoryName(GeoSucheSettingsPath);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var dto = new GeoSucheSettingsDto { ExePath = exePath?.Trim() ?? string.Empty };
                var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(GeoSucheSettingsPath, json);
            }
            catch
            {
            }
        }

        private sealed class GeoSucheSettingsDto
        {
            public string ExePath { get; set; } = string.Empty;
        }

        private void OnKundenMaterialClick(object sender, RoutedEventArgs e)
        {
            if (!PromptForOperatorName())
                return;

            var window = new KundenMaterialWindow();
            Application.Current.MainWindow = window;
            window.Show();
            Close();
        }
    }
}