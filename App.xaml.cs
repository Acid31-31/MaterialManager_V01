using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace MaterialManager_V01
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ✅ LOG-DATEI FÜR DEBUG
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_log.txt");
            
            // ✅ GLOBAL EXCEPTION HANDLER
            Application.Current.DispatcherUnhandledException += (s, ex) =>
            {
                File.AppendAllText(logPath, $"\n!!! UNHANDLED EXCEPTION !!!\n{ex.Exception.Message}\n{ex.Exception.StackTrace}\n");
                MessageBox.Show($"Unbehandelter Fehler:\n{ex.Exception.Message}\n\n{ex.Exception.StackTrace}", 
                    "Kritischer Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                ex.Handled = true; // Verhindere Absturz
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

                if (!Services.LicenseService.IsLicenseValid())
                {
                    File.AppendAllText(logPath, "Lizenz ungültig - zeige Dialog\n");
                    var dlg = new Views.LicenseActivationDialog();
                    if (dlg.ShowDialog() != true)
                    {
                        File.AppendAllText(logPath, "Dialog abgebrochen oder Lizenz abgelaufen\n");
                        MessageBox.Show(
                            Services.LicenseService.GetStatusMessage(),
                            "Demo-Version abgelaufen",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        Current.Shutdown();
                        return;
                    }
                }

                File.AppendAllText(logPath, "Lizenz gültig\n");

                var remainingDays = Services.LicenseService.GetRemainingTrialDays();
                File.AppendAllText(logPath, $"Verbleibende Tage: {remainingDays}\n");
                
                if (remainingDays <= 7)
                {
                    MessageBox.Show(
                        Services.LicenseService.GetStatusMessage(),
                        "Demo-Version",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                
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
