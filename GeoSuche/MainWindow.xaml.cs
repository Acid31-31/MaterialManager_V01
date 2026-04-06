using System.Windows;
using GeoArbeitsvorbereitung.Services;
using GeoArbeitsvorbereitung.ViewModels;
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
