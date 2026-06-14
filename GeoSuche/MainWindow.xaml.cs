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
        RefreshNetworkStatusBanner();
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

    private void RefreshNetworkStatusBanner()
    {
        var status = LicenseService.IsFullLicenseActive() ? NetzwerkService.GetNetzwerkStatusText() : NetzwerkService.GetNetzwerkStatusText();
        NetworkModeTextBlock.Text = status;
        NetworkExcelTextBlock.Text = NetzwerkService.GetExcelStatusText();

        NetworkModeTextBlock.Foreground = status.Contains("Server verbunden", System.StringComparison.OrdinalIgnoreCase)
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"))
            : status.Contains("nicht erreichbar", System.StringComparison.OrdinalIgnoreCase)
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5722"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#03A9F4"));
    }

    private void OnOpenStartProgrammClick(object sender, RoutedEventArgs e)
    {
        WindowNavigationService.NavigateToStart(this);
    }

    private void OnOpenHauptProgrammClick(object sender, RoutedEventArgs e)
    {
        WindowNavigationService.NavigateToMain(this);
    }
}
