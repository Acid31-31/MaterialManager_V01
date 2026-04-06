using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;

namespace GeoArbeitsvorbereitung.Dialogs;

public partial class SearchFoldersDialog : Window
{
    public ObservableCollection<FolderEntry> FolderEntries { get; } = [];

    public List<string> FolderPaths => FolderEntries.Select(f => f.Path?.Trim() ?? string.Empty).ToList();
    public List<string> SelectedFolders => FolderEntries
        .Where(f => f.IsSelected && !string.IsNullOrWhiteSpace(f.Path))
        .Select(f => f.Path.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    public string? SelectedFolder { get; private set; }

    public SearchFoldersDialog(
        IEnumerable<string>? configuredFolders,
        string? currentSelectedFolder,
        string dialogTitle = "Suchordner-Auswahl",
        string headerText = "Suchordner auswählen (mindestens 10 Plätze)",
        string slotPrefix = "Ordner")
    {
        InitializeComponent();

        Title = dialogTitle;
        HeaderTextBlock.Text = headerText;

        var normalized = NormalizeToTen(configuredFolders);

        if (!string.IsNullOrWhiteSpace(currentSelectedFolder)
            && !normalized.Any(p => string.Equals(p, currentSelectedFolder, StringComparison.OrdinalIgnoreCase)))
        {
            var firstEmpty = normalized.FindIndex(string.IsNullOrWhiteSpace);
            if (firstEmpty >= 0)
                normalized[firstEmpty] = currentSelectedFolder.Trim();
        }

        for (int i = 0; i < normalized.Count; i++)
        {
            var path = normalized[i];
            var hasPath = !string.IsNullOrWhiteSpace(path);
            FolderEntries.Add(new FolderEntry
            {
                IndexLabel = $"{slotPrefix} {i + 1}",
                Path = path,
                IsSelected = hasPath
            });
        }

        DataContext = this;
    }

    private static List<string> NormalizeToTen(IEnumerable<string>? items)
    {
        var result = (items ?? Enumerable.Empty<string>())
            .Take(10)
            .Select(s => s?.Trim() ?? string.Empty)
            .ToList();

        while (result.Count < 10)
            result.Add(string.Empty);

        return result;
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: FolderEntry entry })
            return;

        var dialog = new OpenFolderDialog
        {
            Title = $"{entry.IndexLabel} auswählen",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            entry.Path = dialog.FolderName;
            entry.IsSelected = true;
        }
    }

    private void SelectAllWithPath_Click(object sender, RoutedEventArgs e)
    {
        foreach (var entry in FolderEntries)
            entry.IsSelected = !string.IsNullOrWhiteSpace(entry.Path?.Trim());
    }

    private void ClearSelection_Click(object sender, RoutedEventArgs e)
    {
        foreach (var entry in FolderEntries)
            entry.IsSelected = false;
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        var selected = SelectedFolders;
        if (selected.Count == 0)
        {
            MessageBox.Show(
                "Bitte mindestens einen Ordner auswählen und einen gültigen Pfad eintragen.",
                "Hinweis",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var missing = selected.FirstOrDefault(p => !Directory.Exists(p));
        if (!string.IsNullOrWhiteSpace(missing))
        {
            MessageBox.Show(
                $"Ein ausgewählter Ordner existiert nicht:\n{missing}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        SelectedFolder = selected.First();
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    public class FolderEntry : INotifyPropertyChanged
    {
        private string _path = string.Empty;
        private bool _isSelected;

        public string IndexLabel { get; set; } = string.Empty;

        public string Path
        {
            get => _path;
            set
            {
                _path = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
