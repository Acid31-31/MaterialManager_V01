using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using MaterialManager_V01.Views;

namespace MaterialManager_V01
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var logPath = Services.PathService.LogPath;

            Application.Current.DispatcherUnhandledException += (s, ex) =>
            {
                File.AppendAllText(logPath, $"\n!!! UNHANDLED EXCEPTION !!!\n{ex.Exception.Message}\n{ex.Exception.StackTrace}\n");
                MessageBox.Show($"Unbehandelter Fehler:\n{ex.Exception.Message}\n\n{ex.Exception.StackTrace}",
                    "Kritischer Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                ex.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                var exception = ex.ExceptionObject as Exception;
                File.AppendAllText(logPath, $"\n!!! APPDOMAIN EXCEPTION !!!\n{exception?.Message}\n{exception?.StackTrace}\n");
            };

            try
            {
                File.AppendAllText(logPath, $"\n\n=== START {DateTime.Now} ===\n");
                File.AppendAllText(logPath, "App.OnStartup() gestartet\n");

                Services.DatabaseBootstrapService.Initialize();
                File.AppendAllText(logPath, $"Datenbank initialisiert: {Services.PathService.DatabasePath}\n");

                if (!Services.LicenseService.IsLicenseValid())
                {
                    File.AppendAllText(logPath, "Lizenz ungültig\n");

                    if (Services.LicenseService.IsFullLicenseActive())
                    {
                        MessageBox.Show(
                            Services.LicenseService.GetStatusMessage(),
                            "Lizenzprüfung",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        Current.Shutdown();
                        return;
                    }

                    File.AppendAllText(logPath, "Zeige Lizenzdialog\n");
                    var dlg = new LicenseActivationDialog();
                    if (dlg.ShowDialog() != true)
                    {
                        MessageBox.Show(
                            Services.LicenseService.GetStatusMessage(),
                            "Testversion",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        Current.Shutdown();
                        return;
                    }
                }

                File.AppendAllText(logPath, "Lizenz gültig\n");

                var remainingDays = Services.LicenseService.GetRemainingTrialDays();
                File.AppendAllText(logPath, $"Verbleibende Tage: {remainingDays}\n");

                if (!Services.LicenseService.IsFullLicenseActive() && remainingDays <= 7)
                {
                    MessageBox.Show(
                        Services.LicenseService.GetStatusMessage(),
                        "Testversion",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                var startWindow = new StartModeWindow();
                MainWindow = startWindow;
                startWindow.Show();

                File.AppendAllText(logPath, "App.OnStartup() erfolgreich beendet\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, $"FEHLER: {ex.Message}\n{ex.StackTrace}\n");
                MessageBox.Show(
                    $"Fehler beim Start:\n{ex.Message}\n\nStackTrace:\n{ex.StackTrace}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Current.Shutdown();
            }
        }
    }
}
