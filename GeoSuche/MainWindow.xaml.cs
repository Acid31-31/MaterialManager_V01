using System.Windows;
using GeoArbeitsvorbereitung.Services;
using GeoArbeitsvorbereitung.ViewModels;

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
}
