using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ClosedXML.Excel;
using Microsoft.Win32;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class KundenMaterialWindow : Window
    {
        private static readonly string StorePath = Path.Combine(PathService.DataDirectory, "kundenmaterial.json");
        private static readonly string SettingsPath = Path.Combine(PathService.DataDirectory, "kundenmaterial.settings.json");
        private readonly Dictionary<string, string> _customerFolderMap = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _customers = new();
        private readonly ICollectionView _itemsView;

        public ObservableCollection<KundenMaterialItem> Items { get; } = new();

        public KundenMaterialWindow()
        {
            InitializeComponent();
            DataContext = this;
            RefreshLicenseBanner();
            RefreshNetworkStatusBanner();
            _itemsView = CollectionViewSource.GetDefaultView(Items);
            _itemsView.Filter = FilterBySelectedCustomer;
            FitToWorkArea();
            LoadSettings();
            LoadItems();
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
            var status = NetzwerkService.GetNetzwerkStatusText();
            NetworkModeTextBlock.Text = status;
            NetworkExcelTextBlock.Text = NetzwerkService.GetExcelStatusText();

            NetworkModeTextBlock.Foreground = status.Contains("Server verbunden", StringComparison.OrdinalIgnoreCase)
                ? Brushes.LimeGreen
                : status.Contains("nicht erreichbar", StringComparison.OrdinalIgnoreCase)
                    ? Brushes.OrangeRed
                    : Brushes.Gray;
        }

        private bool FilterBySelectedCustomer(object obj)
        {
            if (obj is not KundenMaterialItem item)
                return false;

            var kundeFilter = GetSelectedCustomer();
            if (string.IsNullOrWhiteSpace(kundeFilter))
                return true;

            return (item.Kunde ?? string.Empty)
                .Contains(kundeFilter.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private void RefreshCustomerFilter()
        {
            _itemsView.Refresh();
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

        private void OnOpenMainProgramClick(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
            Close();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            LoadItems();
        }

        private void OnOpenNetworkFolder(object sender, RoutedEventArgs e)
        {
            try
            {
                NetzwerkService.OpenAktivenDatenordnerImExplorer();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Netzwerkordner konnte nicht geöffnet werden:\n{ex.Message}", "Kunden Material", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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
            UpdateCustomerFolderHint();
        }

        private void OnChooseCustomerFolderClick(object sender, RoutedEventArgs e)
        {
            var kunde = GetSelectedCustomer();
            if (string.IsNullOrWhiteSpace(kunde))
            {
                MessageBox.Show("Bitte zuerst einen Kunden eingeben oder auswählen.", "Kunden Material", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new OpenFileDialog
            {
                Title = $"PDF-Datei für Kundenordner von '{kunde}' wählen",
                Filter = "PDF-Dateien (*.pdf)|*.pdf|Alle Dateien (*.*)|*.*",
                CheckFileExists = true
            };

            var currentFolder = GetCustomerSpecificFolder(kunde);
            if (!string.IsNullOrWhiteSpace(currentFolder) && Directory.Exists(currentFolder))
                dlg.InitialDirectory = currentFolder;

            if (dlg.ShowDialog() != true)
                return;

            var folder = Path.GetDirectoryName(dlg.FileName) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(folder))
            {
                _customerFolderMap[kunde] = folder;
                RegisterCustomer(kunde);
                SaveSettings();
                UpdateCustomerFolderHint();
            }
        }

        private void OnSearchPdfClick(object sender, RoutedEventArgs e)
        {
            var zeichnungsnummer = DrawingNumberBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(zeichnungsnummer))
            {
                MessageBox.Show("Bitte zuerst eine Zeichnungsnummer eingeben.", "PDF-Suche", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var path = FindPdfByDrawingNumber(zeichnungsnummer, GetSelectedCustomer());
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

            var kunde = GetSelectedCustomer();
            if (string.IsNullOrWhiteSpace(kunde))
            {
                MessageBox.Show("Bitte einen Kunden angeben.", "Kunden Material", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RegisterCustomer(kunde);
            var pdfPath = FindPdfByDrawingNumber(zeichnungsnummer, kunde);

            if (string.IsNullOrWhiteSpace(pdfPath))
            {
                var confirmWithoutPdf = MessageBox.Show(
                    "Für diese Zeichnungsnummer wurde keine PDF gefunden.\n\nTrotzdem speichern?",
                    "Kunden Material",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmWithoutPdf != MessageBoxResult.Yes)
                    return;
            }
            else if (!IsPdfInFreigabe(pdfPath))
            {
                var confirmOutsideFreigabe = MessageBox.Show(
                    $"Die gefundene PDF liegt nicht in einem 'Freigabe'-Ordner:\n{pdfPath}\n\nTrotzdem speichern?",
                    "Kunden Material",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmOutsideFreigabe != MessageBoxResult.Yes)
                    return;
            }

            Items.Add(new KundenMaterialItem
            {
                Kunde = kunde,
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
            SaveSettings();
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

            var action = MessageBox.Show(
                $"Eintrag für '{item.Kunde}' / '{item.Zeichnungsnummer}' bearbeiten?\n\nJa = Stückzahl ändern\nNein = Löschen\nAbbrechen = nichts",
                "Kunden Material",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (action == MessageBoxResult.Yes)
            {
                var neueStueckzahl = PromptForStueckzahl(item.Stueckzahl);
                if (neueStueckzahl == null)
                    return;

                item.Stueckzahl = neueStueckzahl.Value;
                SaveItems();
                KundenMaterialGrid.Items.Refresh();
                return;
            }

            if (action == MessageBoxResult.No)
            {
                if (MessageBox.Show("Ausgewählten Eintrag wirklich löschen?", "Kunden Material", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    return;

                Items.Remove(item);
                SaveItems();
            }
        }

        private int? PromptForStueckzahl(int aktuelleStueckzahl)
        {
            var input = new TextBox
            {
                Text = aktuelleStueckzahl.ToString(),
                Width = 120,
                Margin = new Thickness(0, 8, 0, 12)
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 90,
                Margin = new Thickness(0, 0, 8, 0),
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#2E7D32"),
                Foreground = System.Windows.Media.Brushes.White
            };

            var cancelButton = new Button
            {
                Content = "Abbrechen",
                Width = 90,
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#555"),
                Foreground = System.Windows.Media.Brushes.White
            };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            buttons.Children.Add(okButton);
            buttons.Children.Add(cancelButton);

            var panel = new StackPanel { Margin = new Thickness(16) };
            panel.Children.Add(new TextBlock
            {
                Text = "Neue Stückzahl eingeben:",
                Foreground = System.Windows.Media.Brushes.White
            });
            panel.Children.Add(input);
            panel.Children.Add(buttons);

            var dialog = new Window
            {
                Title = "Stückzahl ändern",
                Owner = this,
                Content = panel,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.WidthAndHeight,
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#1B1B1B")
            };

            int? result = null;

            okButton.Click += (_, _) =>
            {
                if (!int.TryParse(input.Text?.Trim(), out var parsed) || parsed <= 0)
                {
                    MessageBox.Show("Bitte eine gültige Stückzahl (> 0) eingeben.", "Kunden Material", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                result = parsed;
                dialog.DialogResult = true;
                dialog.Close();
            };

            cancelButton.Click += (_, _) =>
            {
                dialog.DialogResult = false;
                dialog.Close();
            };

            dialog.ShowDialog();
            return result;
        }

        private static bool IsPdfInFreigabe(string? pdfPath)
        {
            if (string.IsNullOrWhiteSpace(pdfPath))
                return false;

            var normalized = pdfPath.Replace('/', '\\');
            return normalized.IndexOf("\\Freigabe\\", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.EndsWith("\\Freigabe", StringComparison.OrdinalIgnoreCase);
        }

        private string? FindPdfByDrawingNumber(string zeichnungsnummer, string kunde)
        {
            var searchFolders = GetSearchFolders(kunde).ToList();
            if (searchFolders.Count == 0)
                return null;

            try
            {
                var normalized = zeichnungsnummer.Trim();
                var files = searchFolders
                    .SelectMany(folder => Directory.EnumerateFiles(folder, "*.pdf", SearchOption.AllDirectories))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
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

        private IEnumerable<string> GetSearchFolders(string kunde)
        {
            var customerFolder = GetCustomerSpecificFolder(kunde);
            if (!string.IsNullOrWhiteSpace(customerFolder))
            {
                var freigabe = Path.Combine(customerFolder, "Freigabe");
                if (Directory.Exists(freigabe))
                    yield return freigabe;

                if (Directory.Exists(customerFolder))
                    yield return customerFolder;
            }

            var root = PdfFolderBox.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(root) && Directory.Exists(root))
            {
                var customerInRoot = string.IsNullOrWhiteSpace(kunde) ? string.Empty : Path.Combine(root, kunde);
                var customerFreigabeInRoot = string.IsNullOrWhiteSpace(customerInRoot) ? string.Empty : Path.Combine(customerInRoot, "Freigabe");
                var rootFreigabe = Path.Combine(root, "Freigabe");

                if (!string.IsNullOrWhiteSpace(customerFreigabeInRoot) && Directory.Exists(customerFreigabeInRoot))
                    yield return customerFreigabeInRoot;

                if (!string.IsNullOrWhiteSpace(customerInRoot) && Directory.Exists(customerInRoot))
                    yield return customerInRoot;

                if (Directory.Exists(rootFreigabe))
                    yield return rootFreigabe;

                yield return root;
            }
        }

        private string GetSelectedCustomer()
        {
            var selected = CustomerBox.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(selected))
                return selected.Trim();

            return (CustomerBox.Text ?? string.Empty).Trim();
        }

        private string? GetCustomerSpecificFolder(string kunde)
        {
            if (string.IsNullOrWhiteSpace(kunde))
                return null;

            if (_customerFolderMap.TryGetValue(kunde, out var mapped) && !string.IsNullOrWhiteSpace(mapped))
                return mapped;

            return null;
        }

        private void RegisterCustomer(string kunde)
        {
            var trimmed = (kunde ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                return;

            if (_customers.Any(c => string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase)))
                return;

            _customers.Add(trimmed);
            CustomerBox.Items.Add(trimmed);
        }

        private void OnCustomerSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateCustomerFolderHint();
            RefreshCustomerFilter();
        }

        private void OnCustomerSelectionLostFocus(object sender, RoutedEventArgs e)
        {
            var kunde = GetSelectedCustomer();
            RegisterCustomer(kunde);
            SaveSettings();
            UpdateCustomerFolderHint();
            RefreshCustomerFilter();
        }

        private void UpdateCustomerFolderHint()
        {
            var kunde = GetSelectedCustomer();
            if (string.IsNullOrWhiteSpace(kunde))
            {
                CustomerFolderHintText.Text = "Suche: Root\\Kunde\\Freigabe";
                return;
            }

            var folder = GetCustomerSpecificFolder(kunde);
            if (string.IsNullOrWhiteSpace(folder))
            {
                var root = PdfFolderBox.Text?.Trim() ?? string.Empty;
                folder = string.IsNullOrWhiteSpace(root) ? string.Empty : Path.Combine(root, kunde, "Freigabe");
            }

            CustomerFolderHintText.Text = string.IsNullOrWhiteSpace(folder)
                ? $"Suche: {kunde}"
                : folder;
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    UpdateCustomerFolderHint();
                    return;
                }

                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<KundenMaterialSettings>(json);
                if (settings == null)
                {
                    UpdateCustomerFolderHint();
                    return;
                }

                PdfFolderBox.Text = settings.PdfFolder ?? string.Empty;
                ImportExcelPathBox.Text = settings.ImportExcelPath ?? string.Empty;
                ImportExcelInfoText.Text = string.IsNullOrWhiteSpace(ImportExcelPathBox.Text)
                    ? "Keine Excel ausgewählt."
                    : Path.GetFileName(ImportExcelPathBox.Text);

                _customerFolderMap.Clear();
                if (settings.CustomerFolders != null)
                {
                    foreach (var kvp in settings.CustomerFolders)
                    {
                        if (!string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                            _customerFolderMap[kvp.Key.Trim()] = kvp.Value.Trim();
                    }
                }

                _customers.Clear();
                CustomerBox.Items.Clear();
                if (settings.Customers != null)
                {
                    foreach (var customer in settings.Customers.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        _customers.Add(customer);
                        CustomerBox.Items.Add(customer);
                    }
                }

                var selected = settings.SelectedCustomer?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(selected))
                {
                    RegisterCustomer(selected);
                    CustomerBox.Text = selected;
                }

                UpdateCustomerFolderHint();
            }
            catch
            {
                UpdateCustomerFolderHint();
            }
        }

        private void SaveSettings()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var settings = new KundenMaterialSettings
                {
                    PdfFolder = PdfFolderBox.Text?.Trim() ?? string.Empty,
                    ImportExcelPath = ImportExcelPathBox.Text?.Trim() ?? string.Empty,
                    SelectedCustomer = GetSelectedCustomer(),
                    Customers = _customers.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c).ToList(),
                    CustomerFolders = _customerFolderMap.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase)
                };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
            }
        }

        private void LoadItems()
        {
            try
            {
                Items.Clear();

                if (!File.Exists(StorePath))
                {
                    RefreshCustomerFilter();
                    return;
                }

                var json = File.ReadAllText(StorePath);
                var parsed = JsonSerializer.Deserialize<KundenMaterialItem[]>(json);
                if (parsed == null)
                {
                    RefreshCustomerFilter();
                    return;
                }

                foreach (var item in parsed)
                    Items.Add(item);

                RefreshCustomerFilter();
            }
            catch
            {
                RefreshCustomerFilter();
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

        private void OnChooseImportExcelPathClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Excel-Datei für Kunden-Material wählen",
                Filter = "Excel-Dateien (*.xlsx)|*.xlsx|Alle Dateien (*.*)|*.*",
                CheckFileExists = true
            };

            var current = ImportExcelPathBox.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(current))
            {
                var dir = Path.GetDirectoryName(current);
                if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                    dlg.InitialDirectory = dir;
            }
            else
            {
                var root = PdfFolderBox.Text?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(root) && Directory.Exists(root))
                    dlg.InitialDirectory = root;
            }

            if (dlg.ShowDialog() != true)
                return;

            ImportExcelPathBox.Text = dlg.FileName;
            ImportExcelInfoText.Text = Path.GetFileName(dlg.FileName);
            SaveSettings();
        }

        private void OnImportExcelClick(object sender, RoutedEventArgs e)
        {
            var path = ImportExcelPathBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                MessageBox.Show("Bitte zuerst unter Datei > Einstellungen eine gültige Excel-Datei auswählen.", "Kunden Material", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ImportKundenMaterialFromExcel(path);
        }

        private void OnOpenSettingsClick(object sender, RoutedEventArgs e)
        {
            var pdfBox = new TextBox
            {
                Text = PdfFolderBox.Text ?? string.Empty,
                Margin = new Thickness(0, 6, 0, 10),
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#1E1E1E"),
                Foreground = System.Windows.Media.Brushes.White,
                BorderBrush = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#444"),
                Padding = new Thickness(8, 5, 8, 5),
                Width = 420
            };

            var excelBox = new TextBox
            {
                Text = ImportExcelPathBox.Text ?? string.Empty,
                Margin = new Thickness(0, 6, 0, 10),
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#1E1E1E"),
                Foreground = System.Windows.Media.Brushes.White,
                BorderBrush = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#444"),
                Padding = new Thickness(8, 5, 8, 5),
                Width = 420
            };

            var choosePdfButton = new Button
            {
                Content = "PDF-Ordner wählen",
                Margin = new Thickness(0, 0, 0, 10),
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#455A64"),
                Foreground = System.Windows.Media.Brushes.White,
                Padding = new Thickness(12, 6, 12, 6),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var chooseExcelButton = new Button
            {
                Content = "Excel-Datei wählen",
                Margin = new Thickness(0, 0, 0, 12),
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#455A64"),
                Foreground = System.Windows.Media.Brushes.White,
                Padding = new Thickness(12, 6, 12, 6),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var saveButton = new Button
            {
                Content = "Speichern",
                Width = 110,
                Margin = new Thickness(0, 0, 8, 0),
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#2E7D32"),
                Foreground = System.Windows.Media.Brushes.White,
                Padding = new Thickness(12, 6, 12, 6)
            };

            var cancelButton = new Button
            {
                Content = "Abbrechen",
                Width = 110,
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#555"),
                Foreground = System.Windows.Media.Brushes.White,
                Padding = new Thickness(12, 6, 12, 6)
            };

            var buttonRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            buttonRow.Children.Add(saveButton);
            buttonRow.Children.Add(cancelButton);

            var panel = new StackPanel { Margin = new Thickness(16) };
            panel.Children.Add(new TextBlock { Text = "PDF-Root-Ordner", Foreground = System.Windows.Media.Brushes.White });
            panel.Children.Add(pdfBox);
            panel.Children.Add(choosePdfButton);
            panel.Children.Add(new TextBlock { Text = "Excel-Datei für Kunden-Import", Foreground = System.Windows.Media.Brushes.White });
            panel.Children.Add(excelBox);
            panel.Children.Add(chooseExcelButton);
            panel.Children.Add(buttonRow);

            var settingsWindow = new Window
            {
                Title = "Einstellungen – Kunden Material",
                Owner = this,
                Content = panel,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.WidthAndHeight,
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#1B1B1B")
            };

            choosePdfButton.Click += (_, _) =>
            {
                var dlg = new OpenFileDialog
                {
                    Title = "PDF-Datei im gewünschten Root-Ordner wählen",
                    Filter = "PDF-Dateien (*.pdf)|*.pdf|Alle Dateien (*.*)|*.*",
                    CheckFileExists = true
                };

                var current = pdfBox.Text?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(current) && Directory.Exists(current))
                    dlg.InitialDirectory = current;

                if (dlg.ShowDialog() != true)
                    return;

                pdfBox.Text = Path.GetDirectoryName(dlg.FileName) ?? string.Empty;
            };

            chooseExcelButton.Click += (_, _) =>
            {
                var dlg = new OpenFileDialog
                {
                    Title = "Excel-Datei wählen",
                    Filter = "Excel-Dateien (*.xlsx)|*.xlsx|Alle Dateien (*.*)|*.*",
                    CheckFileExists = true
                };

                var current = excelBox.Text?.Trim() ?? string.Empty;
                var currentDir = Path.GetDirectoryName(current);
                if (!string.IsNullOrWhiteSpace(currentDir) && Directory.Exists(currentDir))
                    dlg.InitialDirectory = currentDir;

                if (dlg.ShowDialog() != true)
                    return;

                excelBox.Text = dlg.FileName;
            };

            saveButton.Click += (_, _) =>
            {
                PdfFolderBox.Text = pdfBox.Text?.Trim() ?? string.Empty;
                ImportExcelPathBox.Text = excelBox.Text?.Trim() ?? string.Empty;
                ImportExcelInfoText.Text = string.IsNullOrWhiteSpace(ImportExcelPathBox.Text)
                    ? "Keine Excel ausgewählt."
                    : Path.GetFileName(ImportExcelPathBox.Text);

                SaveSettings();
                UpdateCustomerFolderHint();
                settingsWindow.DialogResult = true;
                settingsWindow.Close();
            };

            cancelButton.Click += (_, _) =>
            {
                settingsWindow.DialogResult = false;
                settingsWindow.Close();
            };

            settingsWindow.ShowDialog();
        }

        private void ImportKundenMaterialFromExcel(string filePath)
        {
            try
            {
                using var wb = new XLWorkbook(filePath);
                var ws = wb.Worksheets.FirstOrDefault();
                if (ws == null)
                {
                    MessageBox.Show("Die Excel-Datei enthält kein Blatt.", "Kunden Material", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
                if (lastRow < 2 || lastCol < 2)
                {
                    MessageBox.Show("Die Excel-Datei enthält keine importierbaren Daten.", "Kunden Material", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var headerRow = DetectHeaderRow(ws, lastRow, lastCol);
                var kundeCol = FindColumnIndex(ws, headerRow, lastCol, "kunde", "customer");
                var zeichnungCol = FindColumnIndex(ws, headerRow, lastCol, "zeichnung", "zeichnungsnr", "zeichnungs nr", "drawing");
                var anzahlCol = FindColumnIndex(ws, headerRow, lastCol, "anzahl", "menge", "qty", "quantity", "stück");

                if (kundeCol <= 0 || zeichnungCol <= 0)
                {
                    MessageBox.Show("Erforderliche Spalten nicht gefunden (Kunde / Zeichnungsnummer).", "Kunden Material", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var imported = 0;
                var updated = 0;

                for (var r = headerRow + 1; r <= lastRow; r++)
                {
                    var kunde = (ws.Cell(r, kundeCol).GetFormattedString() ?? string.Empty).Trim();
                    var zeichnung = (ws.Cell(r, zeichnungCol).GetFormattedString() ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(kunde) || string.IsNullOrWhiteSpace(zeichnung))
                        continue;

                    RegisterCustomer(kunde);

                    var stueckzahl = 1;
                    if (anzahlCol > 0)
                    {
                        var qtyText = (ws.Cell(r, anzahlCol).GetFormattedString() ?? string.Empty).Trim();
                        if (int.TryParse(qtyText, out var qtyParsed) && qtyParsed > 0)
                            stueckzahl = qtyParsed;
                    }

                    var existing = Items.FirstOrDefault(i =>
                        string.Equals(i.Kunde, kunde, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(i.Zeichnungsnummer, zeichnung, StringComparison.OrdinalIgnoreCase));

                    var pdfPath = FindPdfByDrawingNumber(zeichnung, kunde) ?? string.Empty;

                    if (existing == null)
                    {
                        Items.Add(new KundenMaterialItem
                        {
                            Kunde = kunde,
                            Zeichnungsnummer = zeichnung,
                            Stueckzahl = stueckzahl,
                            PdfPfad = pdfPath,
                            PdfDateiname = string.IsNullOrWhiteSpace(pdfPath) ? string.Empty : Path.GetFileName(pdfPath),
                            ErstelltAm = DateTime.Now
                        });
                        imported++;
                    }
                    else
                    {
                        existing.Stueckzahl = stueckzahl;
                        existing.PdfPfad = pdfPath;
                        existing.PdfDateiname = string.IsNullOrWhiteSpace(pdfPath) ? string.Empty : Path.GetFileName(pdfPath);
                        updated++;
                    }
                }

                SaveItems();
                SaveSettings();
                UpdateCustomerFolderHint();

                MessageBox.Show($"Import abgeschlossen. Neu: {imported}, aktualisiert: {updated}", "Kunden Material", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel-Import fehlgeschlagen:\n{ex.Message}", "Kunden Material", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static int DetectHeaderRow(IXLWorksheet ws, int lastRow, int lastCol)
        {
            var maxRow = Math.Min(lastRow, 30);
            var bestRow = 1;
            var bestScore = int.MinValue;

            for (var r = 1; r <= maxRow; r++)
            {
                var score = 0;
                for (var c = 1; c <= lastCol; c++)
                {
                    var text = (ws.Cell(r, c).GetFormattedString() ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    score += 1;
                    if (text.Contains("kunde", StringComparison.OrdinalIgnoreCase)
                        || text.Contains("zeichnung", StringComparison.OrdinalIgnoreCase)
                        || text.Contains("anzahl", StringComparison.OrdinalIgnoreCase)
                        || text.Contains("menge", StringComparison.OrdinalIgnoreCase))
                        score += 10;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestRow = r;
                }
            }

            return bestRow;
        }

        private static int FindColumnIndex(IXLWorksheet ws, int headerRow, int lastCol, params string[] keys)
        {
            for (var c = 1; c <= lastCol; c++)
            {
                var text = (ws.Cell(headerRow, c).GetFormattedString() ?? string.Empty).Trim();
                if (keys.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase)))
                    return c;
            }

            return -1;
        }
    }

    public sealed class KundenMaterialItem
    {
        public string Kunde { get; set; } = string.Empty;
        public string Zeichnungsnummer { get; set; } = string.Empty;
        public int Stueckzahl { get; set; }
        public string PdfDateiname { get; set; } = string.Empty;
        public string PdfPfad { get; set; } = string.Empty;
        public DateTime ErstelltAm { get; set; }
    }

    public sealed class KundenMaterialSettings
    {
        public string PdfFolder { get; set; } = string.Empty;
        public string ImportExcelPath { get; set; } = string.Empty;
        public string SelectedCustomer { get; set; } = string.Empty;
        public List<string> Customers { get; set; } = new();
        public Dictionary<string, string> CustomerFolders { get; set; } = new();
    }
}

