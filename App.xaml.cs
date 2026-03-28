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

                if (!EnsureNetworkStartupReady(logPath))
                {
                    Current.Shutdown();
                    return;
                }

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

        private static bool EnsureNetworkStartupReady(string logPath)
        {
            try
            {
                if (!Services.NetzwerkService.HasConfiguredNetworkPath())
                {
                    File.AppendAllText(logPath, "Keine Netzwerk-Konfiguration gefunden. Starte Erstkonfiguration.\n");
                    var setup = new NetzwerkSetupDialog();
                    setup.ShowDialog();
                }

                var health = Services.NetzwerkService.CheckStartupHealth();
                if (health.IsHealthy)
                    return true;

                File.AppendAllText(logPath, $"Netzwerk-Healthcheck fehlgeschlagen: {health.Message}\n");

                var result = MessageBox.Show(
                    $"{health.Message}\n\nJa = Netzwerk-Einrichtung öffnen\nNein = lokal ohne Netzwerk starten\nAbbrechen = Programm beenden",
                    "Netzwerkprüfung",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var setup = new NetzwerkSetupDialog();
                    setup.ShowDialog();
                    health = Services.NetzwerkService.CheckStartupHealth();
                    if (health.IsHealthy)
                        return true;

                    MessageBox.Show(health.Message, "Netzwerkprüfung", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (result == MessageBoxResult.No)
                {
                    Services.NetzwerkService.SetNetzwerkModus(false, Services.NetzwerkService.NetzwerkPfad);
                    File.AppendAllText(logPath, "Netzwerkmodus temporär deaktiviert. Start lokal.\n");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, $"Fehler in EnsureNetworkStartupReady: {ex.Message}\n");
                return true;
            }
        }
    }
}
