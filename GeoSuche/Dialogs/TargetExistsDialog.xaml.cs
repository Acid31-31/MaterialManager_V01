using System.Windows;

namespace GeoArbeitsvorbereitung.Dialogs;

public partial class TargetExistsDialog : Window
{
    public string ResultFileName { get; private set; } = string.Empty;

    public TargetExistsDialog(string targetFolder, string suggestedFileName)
    {
        InitializeComponent();

        FolderText.Text = targetFolder;
        ExistingFileText.Text = suggestedFileName;
        FileNameBox.Text = suggestedFileName;
        FileNameBox.Focus();

        // Pre-select name without extension for easy editing
        var dotIndex = suggestedFileName.LastIndexOf('.');
        FileNameBox.SelectionStart = 0;
        FileNameBox.SelectionLength = dotIndex > 0 ? dotIndex : suggestedFileName.Length;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var name = FileNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Dateiname darf nicht leer sein.", "Fehler",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Enforce .geo
        if (!name.EndsWith(".geo", StringComparison.OrdinalIgnoreCase))
            name += ".geo";

        ResultFileName = name;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
