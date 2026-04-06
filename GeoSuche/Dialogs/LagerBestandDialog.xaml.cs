using System.Windows;
using GeoArbeitsvorbereitung.Models;

namespace GeoArbeitsvorbereitung.Dialogs;

public partial class LagerBestandDialog : Window
{
    public LagerBestandDialog(string suchbegriff, string excelPfad, IReadOnlyList<LagerBestandItem> items)
    {
        InitializeComponent();

        HeaderText.Text = $"Lagerbestand für: {suchbegriff}";
        ExcelText.Text = $"Quelle: {excelPfad}";
        ResultGrid.ItemsSource = items;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
