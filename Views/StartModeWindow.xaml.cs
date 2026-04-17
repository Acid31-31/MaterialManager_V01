using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class StartModeWindow : Window
    {
        private bool _syncingThemeUi;

        public StartModeWindow()
        {
            InitializeComponent();
            RefreshLicenseTitle();
            RefreshLicenseBanner();
            RefreshNetworkStatusBanner();
            FitToWorkArea();
            SyncThemeUi();
        }

        private void SyncThemeUi()
        {
            _syncingThemeUi = true;
            try
            {
                var isLight = ThemeService.CurrentTheme == AppTheme.Light;
                var sliderValue = isLight ? 1 : 0;
                var modeText = isLight ? "Hell Pro" : "Dunkel";

                if (ThemeSlider != null)
                    ThemeSlider.Value = sliderValue;
                if (ThemeModeTextBlock != null)
                    ThemeModeTextBlock.Text = modeText;

                if (ThemeSliderMain != null)
                    ThemeSliderMain.Value = sliderValue;
                if (ThemeModeTextBlockMain != null)
                    ThemeModeTextBlockMain.Text = modeText;
            }
            finally
            {
                _syncingThemeUi = false;
            }
        }

        private void ApplyThemeFromSliderValue(double value)
        {
            var theme = value >= 0.5 ? AppTheme.Light : AppTheme.Dark;
            ThemeService.SetTheme(theme);
            SyncThemeUi();
        }

        private void RefreshLicenseBanner()
        {
            if (LicenseService.IsFullLicenseActive())
            {
                LicenseBannerTextBlock.Text = "Vollversion aktiv";
                LicenseBannerTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                return;
            }

            var remainingDays = LicenseService.GetRemainingTrialDays();
            var expiration = LicenseService.GetExpirationDate();
            var expiryText = expiration.HasValue ? $" (bis {expiration.Value:dd.MM.yyyy})" : string.Empty;

            LicenseBannerTextBlock.Text = $"Pilotbetrieb: {remainingDays} Tage{expiryText}";
            LicenseBannerTextBlock.Foreground = remainingDays <= 7
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC107"));
        }

        private void RefreshLicenseTitle()
        {
            var mode = LicenseService.GetLicenseModeText();
            var device = LicenseService.GetDeviceUsageText();
            Title = string.IsNullOrWhiteSpace(device)
                ? $"MaterialManager V01 - {mode}"
                : $"MaterialManager V01 - {mode} | {device}";
        }

        private void RefreshNetworkStatusBanner()
        {
            var status = NetzwerkService.GetNetzwerkStatusText();
            NetworkModeTextBlock.Text = status;
            NetworkExcelTextBlock.Text = NetzwerkService.GetExcelStatusText();

            NetworkModeTextBlock.Foreground = status.Contains("Server verbunden", StringComparison.OrdinalIgnoreCase)
                ? Brushes.LimeGreen
                : status.Contains("nicht erreichbar", StringComparison.OrdinalIgnoreCase)
                    ? Brushes.OrangeRed
                    : Brushes.DeepSkyBlue;
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
                MessageBox.Show($"Netzwerkordner könnte nicht geöffnet werden:\n{ex.Message}", "Start", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            MessageBox.Show("Kantbank ist vorerst deaktiviert.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnGeoSucheClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PromptForOperatorName())
                    return;

                var geoWindow = new GeoArbeitsvorbereitung.MainWindow();
                Application.Current.MainWindow = geoWindow;
                geoWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Geo Suche konnte nicht geöffnet werden:\n{ex.Message}", "Start", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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

        private void OnThemeSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_syncingThemeUi || !IsLoaded)
                return;

            ApplyThemeFromSliderValue(ThemeSlider.Value);
        }

        private void OnThemeSliderChangedMain(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_syncingThemeUi || !IsLoaded)
                return;

            ApplyThemeFromSliderValue(ThemeSliderMain.Value);
        }
    }
}