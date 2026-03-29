using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class KundenMaterialWindow : Window
    {
        private static readonly string StorePath = Path.Combine(PathService.DataDirectory, "kundenmaterial.json");

        public ObservableCollection<KundenMaterialItem> Items { get; } = new();

        public KundenMaterialWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadItems();
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

            Items.Add(new KundenMaterialItem
            {
                Zeichnungsnummer = zeichnungsnummer,
                Stueckzahl = stueckzahl,
                ErstelltAm = DateTime.Now
            });

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
    }

    public sealed class KundenMaterialItem
    {
        public string Zeichnungsnummer { get; set; } = string.Empty;
        public int Stueckzahl { get; set; }
        public DateTime ErstelltAm { get; set; }
    }
}
