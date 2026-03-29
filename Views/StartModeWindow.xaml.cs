using System;
using System.Windows;
using System.Windows.Input;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class StartModeWindow : Window
    {
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
    }
}