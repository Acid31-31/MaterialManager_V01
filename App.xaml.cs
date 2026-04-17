using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using MaterialManager_V01.Services;
using MaterialManager_V01.Views;

namespace MaterialManager_V01
{
    public partial class App : Application
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        private const int DwmaUseImmersiveDarkMode = 20;
        private const int DwmaUseImmersiveDarkModeLegacy = 19;

        private static Mutex? _singleInstanceMutex;
        private static bool _ownsSingleInstanceMutex;

        private readonly DispatcherTimer _globalUpdateTimer = new();
        private bool _globalUpdateCheckRunning;
        private string? _lastGlobalPromptedVersion;

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!EnsureSingleInstance())
            {
                Current.Shutdown();
                return;
            }

            base.OnStartup(e);
            ThemeService.Initialize();

            EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
                new RoutedEventHandler((sender, _) =>
                {
                    if (sender is Window window)
                    {
                        ThemeService.ApplyThemeToWindow(window);
                        ApplyImmersiveTitleBar(window);
                    }
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

                StartGlobalUpdateChecks();

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

        private void StartGlobalUpdateChecks()
        {
            _ = Dispatcher.InvokeAsync(async () =>
            {
                await System.Threading.Tasks.Task.Delay(1200);
                await CheckForUpdatesAndPromptAsync("startup");
            });

            _globalUpdateTimer.Stop();
            _globalUpdateTimer.Interval = TimeSpan.FromMinutes(3);
            _globalUpdateTimer.Tick -= OnGlobalUpdateTimerTick;
            _globalUpdateTimer.Tick += OnGlobalUpdateTimerTick;
            _globalUpdateTimer.Start();
        }

        private async void OnGlobalUpdateTimerTick(object? sender, EventArgs e)
        {
            await CheckForUpdatesAndPromptAsync("timer");
        }

        private async System.Threading.Tasks.Task CheckForUpdatesAndPromptAsync(string source)
        {
            if (_globalUpdateCheckRunning)
                return;

            _globalUpdateCheckRunning = true;
            try
            {
                var result = await GitHubUpdateService.CheckForUpdatesAsync();
                if (!result.IsUpdateAvailable || string.IsNullOrWhiteSpace(result.DownloadUrl))
                    return;

                if (!string.IsNullOrWhiteSpace(_lastGlobalPromptedVersion) &&
                    string.Equals(_lastGlobalPromptedVersion, result.LatestVersion, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var owner = Current.Windows
                    .OfType<Window>()
                    .FirstOrDefault(w => w.IsVisible && w.IsActive)
                    ?? Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible)
                    ?? MainWindow;

                var dlg = new UpdateDialog(result) { Owner = owner };
                dlg.ShowDialog();
                _lastGlobalPromptedVersion = result.LatestVersion;
            }
            catch
            {
            }
            finally
            {
                _globalUpdateCheckRunning = false;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _globalUpdateTimer.Stop();
            _globalUpdateTimer.Tick -= OnGlobalUpdateTimerTick;

            try
            {
                if (_ownsSingleInstanceMutex)
                    _singleInstanceMutex?.ReleaseMutex();
            }
            catch
            {
            }
            finally
            {
                _singleInstanceMutex?.Dispose();
                _singleInstanceMutex = null;
                _ownsSingleInstanceMutex = false;
            }

            base.OnExit(e);
        }

        private static bool EnsureSingleInstance()
        {
            try
            {
                _singleInstanceMutex = new Mutex(true, @"Local\MaterialManager_V01_SingleInstance", out var createdNew);
                if (createdNew)
                {
                    _ownsSingleInstanceMutex = true;
                    return true;
                }

                return false;
            }
            catch (AbandonedMutexException)
            {
                _ownsSingleInstanceMutex = true;
                return true;
            }
            catch
            {
                return true;
            }
        }

        private static void ApplyImmersiveTitleBar(Window window)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero)
                    return;

                var useDark = ThemeService.CurrentTheme == AppTheme.Dark ? 1 : 0;
                var result = DwmSetWindowAttribute(hwnd, DwmaUseImmersiveDarkMode, ref useDark, sizeof(int));
                if (result != 0)
                    DwmSetWindowAttribute(hwnd, DwmaUseImmersiveDarkModeLegacy, ref useDark, sizeof(int));
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
                    File.AppendAllText(logPath, "Keine Netzwerk-Konfiguration vorhanden. Starte mit lokalem Datenspeicher.\n");
                    return true;
                }

                var health = Services.NetzwerkService.CheckStartupHealth();
                if (health.IsHealthy)
                    return true;

                File.AppendAllText(logPath, $"Netzwerk-Healthcheck fehlgeschlagen (1. Versuch): {health.Message}\n");

                MessageBox.Show(
                    "Serververbindung wird geprüft. Bitte kurz warten ...",
                    "Netzwerkprüfung",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Zweite Prüfphase nach kurzer Wartezeit (z. B. nach Update/Anmeldung kann Netzwerk verzögert bereit sein)
                const int extraRetries = 3;
                for (var i = 0; i < extraRetries; i++)
                {
                    Thread.Sleep(800);
                    health = Services.NetzwerkService.CheckStartupHealth();
                    if (health.IsHealthy)
                    {
                        File.AppendAllText(logPath, $"Netzwerk nach Verzögerung erreichbar (Retry {i + 1}/{extraRetries}).\n");
                        return true;
                    }
                }

                File.AppendAllText(logPath, $"Netzwerk-Healthcheck endgültig fehlgeschlagen: {health.Message}\n");

                MessageBox.Show(
                    "Netzwerkpfad ist aktuell nicht erreichbar.\n" +
                    "Die Daten werden lokal weitergeführt und sind auf anderen PCs erst wieder sichtbar, wenn das Netzwerk wieder funktioniert.\n\n" +
                    health.Message,
                    "Netzwerkhinweis",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

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
