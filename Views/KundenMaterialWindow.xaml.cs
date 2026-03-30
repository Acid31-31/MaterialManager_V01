using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class KundenMaterialWindow : Window
    {
        private static readonly string StorePath = Path.Combine(PathService.DataDirectory, "kundenmaterial.json");
        private static readonly string SettingsPath = Path.Combine(PathService.DataDirectory, "kundenmaterial.settings.json");

        public ObservableCollection<KundenMaterialItem> Items { get; } = new();

        public KundenMaterialWindow()
        {
            InitializeComponent();
            DataContext = this;
            FitToWorkArea();
            LoadSettings();
            LoadItems();
        }

        private void FitToWorkArea()
        {
            var wa = SystemParameters.WorkArea;
            Left = wa.Left;
            Top = wa.Top;
            Width = wa.Width;
            Height = wa.Height;
            MaxWidth = wa.Width;
            MaxHeight = wa.Height;
        }

        private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void OnMinimizeWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnMaximizeRestoreWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void OnCloseWindow(object sender, RoutedEventArgs e)
        {
            var startWindow = new StartModeWindow();
            Application.Current.MainWindow = startWindow;
            startWindow.Show();
            Close();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            LoadItems();
        }

        private async void OnCheckForUpdates(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var result = await GitHubUpdateService.CheckForUpdatesAsync();

                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    MessageBox.Show($"Update-Prüfung fehlgeschlagen:\n{result.ErrorMessage}", "Update", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!result.IsUpdateAvailable || string.IsNullOrWhiteSpace(result.DownloadUrl))
                {
                    MessageBox.Show($"Sie haben die neueste Version ({result.CurrentVersion}).", "Update", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dlg = new UpdateDialog(result) { Owner = this };
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler bei Update-Prüfung:\n{ex.Message}", "Update", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void OnProgrammHilfe(object sender, RoutedEventArgs e)
        {
            var dlg = new ProgrammHilfeDialog { Owner = this };
            dlg.ShowDialog();
        }

        private void OnChoosePdfFolderClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "PDF-Datei aus gewünschtem Ordner wählen",
                Filter = "PDF-Dateien (*.pdf)|*.pdf|Alle Dateien (*.*)|*.*",
                CheckFileExists = true
            };

            if (!string.IsNullOrWhiteSpace(PdfFolderBox.Text) && Directory.Exists(PdfFolderBox.Text))
                dlg.InitialDirectory = PdfFolderBox.Text;

            if (dlg.ShowDialog() != true)
                return;

            var folder = Path.GetDirectoryName(dlg.FileName) ?? string.Empty;
            PdfFolderBox.Text = folder;
            SaveSettings();
        }

        private void OnSearchPdfClick(object sender, RoutedEventArgs e)
        {
            var zeichnungsnummer = DrawingNumberBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(zeichnungsnummer))
            {
                MessageBox.Show("Bitte zuerst eine Zeichnungsnummer eingeben.", "PDF-Suche", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var path = FindPdfByDrawingNumber(zeichnungsnummer);
            if (string.IsNullOrWhiteSpace(path))
            {
                FoundPdfText.Text = "Keine passende PDF gefunden.";
                return;
            }

            FoundPdfText.Text = Path.GetFileName(path);
            var preview = new PdfPreviewDialog(path) { Owner = this };
            preview.ShowDialog();
        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            var zeichnungsnummer = DrawingNumberBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(zeichnungsnummer))
            {
                MessageBox.Show("Bitte eine Zeichnungsnummer eingeben.", "Kunden Material", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(QuantityBox.Text?.Trim(), out var stueckzahl) || stueckzahl <= 0)
            {
                MessageBox.Show("Bitte eine gültige Stückzahl (> 0) eingeben.", "Kunden Material", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var pdfPath = FindPdfByDrawingNumber(zeichnungsnummer);

            Items.Add(new KundenMaterialItem
            {
                Zeichnungsnummer = zeichnungsnummer,
                Stueckzahl = stueckzahl,
                PdfPfad = pdfPath ?? string.Empty,
                PdfDateiname = string.IsNullOrWhiteSpace(pdfPath) ? string.Empty : Path.GetFileName(pdfPath),
                ErstelltAm = DateTime.Now
            });

            FoundPdfText.Text = string.IsNullOrWhiteSpace(pdfPath)
                ? "Keine passende PDF gefunden."
                : Path.GetFileName(pdfPath);

            SaveItems();
            DrawingNumberBox.Clear();
            QuantityBox.Text = "1";
            DrawingNumberBox.Focus();
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (KundenMaterialGrid.SelectedItem is not KundenMaterialItem item)
            {
                MessageBox.Show("Bitte zuerst einen Eintrag auswählen.", "Kunden Material", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Ausgewählten Eintrag löschen?", "Kunden Material", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            Items.Remove(item);
            SaveItems();
        }

        private void OnGridMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (KundenMaterialGrid.SelectedItem is not KundenMaterialItem item)
                return;

            if (string.IsNullOrWhiteSpace(item.PdfPfad) || !File.Exists(item.PdfPfad))
            {
                MessageBox.Show("Für diesen Eintrag ist keine gültige PDF-Datei hinterlegt.", "PDF-Vorschau", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var preview = new PdfPreviewDialog(item.PdfPfad) { Owner = this };
            preview.ShowDialog();
        }

        private string? FindPdfByDrawingNumber(string zeichnungsnummer)
        {
            var folder = PdfFolderBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                return null;

            try
            {
                var normalized = zeichnungsnummer.Trim();
                var files = Directory.EnumerateFiles(folder, "*.pdf", SearchOption.AllDirectories)
                    .Select(p => new { Path = p, Name = Path.GetFileNameWithoutExtension(p) ?? string.Empty })
                    .Where(x => x.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => string.Equals(x.Name, normalized, StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(x => x.Name.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
                    .ThenBy(x => x.Name.Length)
                    .ToList();

                return files.FirstOrDefault()?.Path;
            }
            catch
            {
                return null;
            }
        }

        private void LoadItems()
        {
            try
            {
                Items.Clear();

                if (!File.Exists(StorePath))
                    return;

                var json = File.ReadAllText(StorePath);
                var parsed = JsonSerializer.Deserialize<KundenMaterialItem[]>(json);
                if (parsed == null)
                    return;

                foreach (var item in parsed)
                    Items.Add(item);
            }
            catch
            {
            }
        }

        private void SaveItems()
        {
            try
            {
                var dir = Path.GetDirectoryName(StorePath);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(Items, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StorePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Speichern fehlgeschlagen:\n{ex.Message}", "Kunden Material", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return;

                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<KundenMaterialSettings>(json);
                if (settings == null)
                    return;

                PdfFolderBox.Text = settings.PdfFolder ?? string.Empty;
            }
            catch
            {
            }
        }

        private void SaveSettings()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var settings = new KundenMaterialSettings { PdfFolder = PdfFolderBox.Text?.Trim() ?? string.Empty };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
            }
        }
    }

    public sealed class KundenMaterialItem
    {
        public string Zeichnungsnummer { get; set; } = string.Empty;
        public int Stueckzahl { get; set; }
        public string PdfDateiname { get; set; } = string.Empty;
        public string PdfPfad { get; set; } = string.Empty;
        public DateTime ErstelltAm { get; set; }
    }

    public sealed class KundenMaterialSettings
    {
        public string PdfFolder { get; set; } = string.Empty;
    }
}
