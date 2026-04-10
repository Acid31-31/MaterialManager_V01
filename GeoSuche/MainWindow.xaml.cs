using System.Windows;
using System.Windows.Media;
using GeoArbeitsvorbereitung.Services;
using GeoArbeitsvorbereitung.ViewModels;
using MaterialManager_V01.Services;
using MaterialManager_V01.Views;

namespace GeoArbeitsvorbereitung;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var settingsService = new SettingsService();
        var geoFileService = new GeoFileService();
        var dialogService = new WpfDialogService();
        DataContext = new MainViewModel(settingsService, geoFileService, dialogService);

        RefreshLicenseBanner();
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

    private void OnOpenStartProgrammClick(object sender, RoutedEventArgs e)
    {
        var window = new StartModeWindow();
        Application.Current.MainWindow = window;
        window.Show();
        Close();
    }

    private void OnOpenHauptProgrammClick(object sender, RoutedEventArgs e)
    {
        var window = new MaterialManager_V01.MainWindow();
        Application.Current.MainWindow = window;
        window.Show();
        Close();
    }
}
