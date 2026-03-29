using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using MaterialManager_V01.Views;

namespace MaterialManager_V01
{
    public partial class App : Application
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        private const int DwmaUseImmersiveDarkMode = 20;
        private const int DwmaUseImmersiveDarkModeLegacy = 19;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
                new RoutedEventHandler((sender, _) =>
                {
                    if (sender is Window window)
                        ApplyDarkTitleBar(window);
                }));

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

        private static void ApplyDarkTitleBar(Window window)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero)
                    return;

                var useDark = 1;
                var result = DwmSetWindowAttribute(hwnd, DwmaUseImmersiveDarkMode, ref useDark, sizeof(int));
                if (result != 0)
                {
                    DwmSetWindowAttribute(hwnd, DwmaUseImmersiveDarkModeLegacy, ref useDark, sizeof(int));
                }
            }
            catch
            {
                // Nicht kritisch: Fenster bleibt funktional.
            }
        }

        private static bool EnsureNetworkStartupReady(string logPath)
        {
            try
            {
                // Keine erzwungene Einrichtung beim Start.
                // Benutzer richtet Netzwerk später manuell über Einstellungen ein.
                if (!Services.NetzwerkService.HasConfiguredNetworkPath())
                {
                    File.AppendAllText(logPath, "Keine Netzwerk-Konfiguration vorhanden. Starte ohne Netzwerkmodus.\n");
                    return true;
                }

                var health = Services.NetzwerkService.CheckStartupHealth();
                if (health.IsHealthy)
                    return true;

                File.AppendAllText(logPath, $"Netzwerk-Healthcheck fehlgeschlagen. Starte lokal: {health.Message}\n");

                // Still auf lokalen Modus zurückfallen, kein Blockieren und kein Zwangs-Dialog.
                Services.NetzwerkService.SetNetzwerkModus(false, Services.NetzwerkService.NetzwerkPfad);
                return true;
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, $"Fehler in EnsureNetworkStartupReady: {ex.Message}\n");
                return true;
            }
        }
    }
}
