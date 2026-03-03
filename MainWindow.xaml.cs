using MaterialManager_V01.Models;
using MaterialManager_V01.Views;
using MaterialManager_V01.Services;
using Microsoft.Win32;
using Microsoft.VisualBasic;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MaterialManager_V01
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<MaterialItem> Materialien { get; set; } = new();
        private DateTime _lastSaveUtc;

        private string _gesamtGewichtText = "0,00 kg";
        public string GesamtGewichtText { get => _gesamtGewichtText; set { _gesamtGewichtText = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GesamtGewichtText))); } }

        private double _durchschnittAuslastung = 0;
        public double DurchschnittAuslastung { get => _durchschnittAuslastung; set { _durchschnittAuslastung = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DurchschnittAuslastung))); } }

        private string _auslastungText = "0%";
        public string AuslastungText { get => _auslastungText; set { _auslastungText = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AuslastungText))); } }

        private string _niedrigeBestaendeText = "Alle OK ✓";
        public string NiedrigeBestaendeText { get => _niedrigeBestaendeText; set { _niedrigeBestaendeText = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NiedrigeBestaendeText))); } }

        private string _niedrigeBestaendeFarbe = "#4CAF50";
        public string NiedrigeBestaendeFarbe { get => _niedrigeBestaendeFarbe; set { _niedrigeBestaendeFarbe = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NiedrigeBestaendeFarbe))); } }

        private double _euPalettePct = 0;
        public double EuPalettePct { get => _euPalettePct; set { _euPalettePct = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EuPalettePct))); } }

        private string _euPaletteDisplayText = "0 kg / 2.000 kg";
        public string EuPaletteDisplayText { get => _euPaletteDisplayText; set { _euPaletteDisplayText = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EuPaletteDisplayText))); } }

        private int _reservierteResteCount = 0;
        public int ReservierteResteCount { get => _reservierteResteCount; set { _reservierteResteCount = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReservierteResteCount))); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            // ✅ ROBUSTES LOGGING: Erstelle nur wenn nicht existiert, dann StreamWriter verwenden
            StreamWriter logWriter = null;
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"startup_{Environment.UserName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                logWriter = new StreamWriter(logPath, append: true) { AutoFlush = true };
                
                void Log(string message)
                {
                    try
                    {
                        logWriter?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
                        System.Diagnostics.Debug.WriteLine(message);
                    }
                    catch { }
                }
                
                Log("MainWindow() Constructor gestartet");
                
                InitializeComponent();
                Log("InitializeComponent() OK");
                
                DataContext = this;
                Log("DataContext gesetzt");

                var status = Services.LicenseService.GetStatusMessage();
                Title = $"MaterialManager V01 - {status}";
                Log($"Titel gesetzt: {Title}");

                Materialien.CollectionChanged += (_, __) => UpdateStats();
                this.KeyDown += MainWindow_KeyDown;
                Log("Event-Handler registriert");

                LoadAutosave();
                Log("LoadAutosave() OK");
                
                UpdateStats();
                Log("UpdateStats() OK");

                Application.Current.Exit += (_, __) => SaveNow();
                Services.BackupService.StartAutoBackup(() => Services.BackupService.CreateBackup(Materialien.ToList()));
                Log("BackupService gestartet");

                ReservierteResteCount = 0;

                var savePath = Services.NetzwerkService.GetSavePath();
                Log($"SavePath: {savePath}");
                
                InitializeAutoSync(savePath);
                Log("AutoSync initialisiert");

                Services.OnlineUserService.RegisterCurrentUser();
                Log("User registriert");
                
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(5);
                timer.Tick += (_, __) => UpdateOnlineStatus();
                timer.Start();
                Log("Timer gestartet");
                
                this.MouseDown += (s, e) =>
                {
                    var popup = this.FindName("OnlineUsersPopup") as System.Windows.Controls.Primitives.Popup;
                    var border = this.FindName("OnlineUsersBorder") as Border;
                    
                    if (popup != null && popup.IsOpen && e.Source != border)
                    {
                        popup.IsOpen = false;
                    }
                };
                
                Log("MainWindow() Constructor erfolgreich beendet");
                
                this.Loaded += (s, e) =>
                {
                    Log("MainWindow.Loaded Event ausgelöst - Fenster ist sichtbar!");
                    _ = CheckForUpdatesOnStartupAsync();
                };
                
                // ✅ Cleanup beim Beenden
                this.Closed += (s, e) =>
                {
                    Log("MainWindow wird geschlossen");
                    logWriter?.Close();
                    logWriter?.Dispose();
                };
            }
            catch (Exception ex)
            {
                try
                {
                    logWriter?.WriteLine($"FEHLER in MainWindow(): {ex.Message}\n{ex.StackTrace}");
                }
                catch { }
                
                logWriter?.Close();
                logWriter?.Dispose();
                
                MessageBox.Show($"Fehler beim Laden: {ex.Message}\n\n{ex.StackTrace}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        // ✅ UPDATE ONLINE-STATUS alle 5 Sekunden
        private void UpdateOnlineStatus()
        {
            try
            {
                var onlineUsers = Services.OnlineUserService.GetOnlineUsers();
                var display = this.FindName("OnlineUsersDisplay") as TextBlock;
                if (display != null)
                {
                    display.Text = string.Join("\n", onlineUsers.Take(2).Select(u => $"👤 {u}"));
                }
            }
            catch { }
        }

        // ✅ CLICK auf Online-Users: Popup anzeigen
        private void OnOnlineUsersClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var popup = this.FindName("OnlineUsersPopup") as System.Windows.Controls.Primitives.Popup;
                var listBox = this.FindName("OnlineUsersList") as ListBox;
                
                if (popup != null && listBox != null)
                {
                    var onlineUsers = Services.OnlineUserService.GetOnlineUsers();
                    listBox.ItemsSource = onlineUsers;
                    popup.IsOpen = true;
                }
                e.Handled = true;
            }
            catch { }
        }

        private void InitializeAutoSync(string savePath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[InitializeAutoSync] START - Pfad: {savePath}");
                
                Services.FileWatcherService.StartWatching(savePath);
                System.Diagnostics.Debug.WriteLine($"[InitializeAutoSync] FileWatcher gestartet");
                
                Services.FileWatcherService.OnFileChanged += (path) => 
                {
                    System.Diagnostics.Debug.WriteLine($"[FileWatcherService] Änderung erkannt: {path}");
                    ReloadMaterialienAsync();
                };
                System.Diagnostics.Debug.WriteLine($"[InitializeAutoSync] FileWatcher Event registriert");
                
                Services.AutoSyncManager.StartAutoSync(savePath);
                System.Diagnostics.Debug.WriteLine($"[InitializeAutoSync] AutoSyncManager gestartet");
                
                Services.AutoSyncManager.OnAutoSyncTriggered += () => 
                {
                    System.Diagnostics.Debug.WriteLine($"[AutoSyncManager] Sync triggert - Reload wird aufgerufen");
                    ReloadMaterialienAsync();
                };
                System.Diagnostics.Debug.WriteLine($"[InitializeAutoSync] AutoSync Event registriert");
                System.Diagnostics.Debug.WriteLine($"[InitializeAutoSync] KOMPLETT FERTIG!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InitializeAutoSync] FEHLER: {ex.Message}");
            }
        }

        private async void ReloadMaterialienAsync()
        {
            try
            {
                var savePath = Services.NetzwerkService.GetSavePath();
                System.Diagnostics.Debug.WriteLine($"[ReloadMaterialienAsync] START - Pfad: {savePath}");
                
                if (!File.Exists(savePath)) 
                {
                    System.Diagnostics.Debug.WriteLine($"[ReloadMaterialienAsync] FEHLER - Datei nicht gefunden!");
                    return;
                }

                // ✅ VERBESSERT: Kürzere Window für lokale Saves (500ms für sicheres Debouncing)
                bool isLocalSave = (DateTime.UtcNow - _lastSaveUtc).TotalMilliseconds < 500;
                System.Diagnostics.Debug.WriteLine($"[ReloadMaterialienAsync] isLocalSave: {isLocalSave}, Zeit: {(DateTime.UtcNow - _lastSaveUtc).TotalMilliseconds}ms");
                
                if (isLocalSave)
                {
                    System.Diagnostics.Debug.WriteLine($"[ReloadMaterialienAsync] SKIP - Das war unser lokales Speichern");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[ReloadMaterialienAsync] LADEN - Externe Änderung erkannt!");
                
                // ✅ Versuche bis zu 30x zu laden
                for (var attempt = 0; attempt < 30; attempt++)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[ReloadMaterialienAsync] Versuch {attempt + 1}/30");
                        var externalItems = ExcelService.Import(savePath);
                        
                        System.Diagnostics.Debug.WriteLine($"[ReloadMaterialienAsync] Import erfolgreich! {externalItems?.Count()} Materialien");
                        
                        if (externalItems?.Any() == true)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                System.Diagnostics.Debug.WriteLine($"[ReloadMaterialienAsync] INTELLIGENTER MERGE startet...");
                                
                                // ✅ INTELLIGENT MERGE: Aktualisiere/Füge hinzu statt alles zu ersetzen
                                var currentRestNummern = Materialien.Where(m => !string.IsNullOrEmpty(m.Restnummer))
                                    .Select(m => m.Restnummer)
                                    .ToHashSet();
                                
                                var externalRestNummern = externalItems.Where(m => !string.IsNullOrEmpty(m.Restnummer))
                                    .Select(m => m.Restnummer)
                                    .ToHashSet();

                                // 1. Entferne gelöschte Items (existieren nicht mehr in Excel)
                                var toRemove = Materialien.Where(m => 
                                    !string.IsNullOrEmpty(m.Restnummer) && 
                                    !externalRestNummern.Contains(m.Restnummer)
                                ).ToList();
                                
                                foreach (var item in toRemove)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[MERGE] Entferne gelöschtes: {item.Restnummer}");
                                    Materialien.Remove(item);
                                }

                                // 2. Aktualisiere existierende Items
                                foreach (var externalItem in externalItems)
                                {
                                    if (!string.IsNullOrEmpty(externalItem.Restnummer))
                                    {
                                        var existing = Materialien.FirstOrDefault(m => m.Restnummer == externalItem.Restnummer);
                                        if (existing != null)
                                        {
                                            var idx = Materialien.IndexOf(existing);
                                            if (idx >= 0)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"[MERGE] Aktualisiere: {externalItem.Restnummer}");
                                                Materialien[idx] = externalItem;
                                            }
                                        }
                                        else
                                        {
                                            // 3. Füge neue Items hinzu
                                            System.Diagnostics.Debug.WriteLine($"[MERGE] Neu hinzufügen: {externalItem.Restnummer}");
                                            Materialien.Add(externalItem);
                                        }
                                    }
                                }
                                
                                UpdateStats();
                                
                                // ✅ VISUELLES FEEDBACK
                                Title = $"MaterialManager V01 - 🔄 Synchronisiert {DateTime.Now:HH:mm:ss}";
                                System.Diagnostics.Debug.WriteLine($"[ReloadMaterialienAsync] MERGE FERTIG!");
                            });
                            
                            Services.ReloadService.RegisterLoad(savePath);
                            System.Diagnostics.Debug.WriteLine($"[ReloadMaterialienAsync] KOMPLETT FERTIG!");
                        }
                        return;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ReloadMaterialienAsync] Fehler Versuch {attempt + 1}: {ex.Message}");
                        await System.Threading.Tasks.Task.Delay(200);
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[ReloadMaterialienAsync] FEHLER - Alle 30 Versuche fehlgeschlagen!");
            }
            catch (Exception ex) 
            { 
                System.Diagnostics.Debug.WriteLine($"[ReloadMaterialienAsync] KRITISCHER FEHLER: {ex.Message}");
            }
        }

        private void OnMaterialNeu(object sender, RoutedEventArgs e)
        {
            var dlg = new MaterialDialog(Materialien) { Owner = this };
            if (dlg.ShowDialog() == true) 
            { 
                // ✅ SOFORT zur UI hinzufügen (keine Verzögerung!)
                Materialien.Add(dlg.Material);
                UpdateStats();
                
                // ✅ DANN asynchron speichern (blockiert UI nicht)
                System.Threading.Tasks.Task.Run(() => SaveNow());
            }
        }

        private void OnMaterialBearbeiten(object sender, MouseButtonEventArgs e)
        {
            if (MaterialGrid.SelectedItem is MaterialItem item)
            {
                var dlg = new MaterialDialog(Materialien) { Owner = this };
                dlg.SetEditMode(item);
                if (dlg.ShowDialog() == true) 
                { 
                    var idx = Materialien.IndexOf(item);
                    if (idx >= 0 && idx < Materialien.Count)
                    {
                        // ✅ SOFORT in UI aktualisieren
                        Materialien[idx] = dlg.Material;
                        UpdateStats();
                        
                        // ✅ DANN asynchron speichern
                        System.Threading.Tasks.Task.Run(() => SaveNow());
                    }
                }
            }
        }

        private void OnMaterialLoeschen(object sender, RoutedEventArgs e)
        {
            var selected = Materialien.Where(m => m.IsSelected).ToList();
            if (!selected.Any()) { MessageBox.Show("Bitte Material auswählen."); return; }
            if (MessageBox.Show($"{selected.Count} Material(ien) löschen?", "Bestätigung", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Services.UndoService.Push($"{selected.Count} Material(ien) gelöscht", selected);
                
                // ✅ SOFORT aus UI entfernen
                foreach (var item in selected) 
                { 
                    Services.BuchungsService.BucheAusgang(item); 
                    Materialien.Remove(item); 
                }
                UpdateStats();
                
                // ✅ DANN asynchron speichern
                System.Threading.Tasks.Task.Run(() => SaveNow());
            }
        }

        private void OnReservierteReste(object sender, RoutedEventArgs e)
        {
            var dlg = new ReservierteResteDialog { Owner = this };
            dlg.ShowDialog();
            ReservierteResteCount = Materialien.Count(m => !string.IsNullOrEmpty(m.AuftragNr));
        }

        private async void OnCheckForUpdates(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var result = await Services.GitHubUpdateService.CheckForUpdatesAsync();

                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    MessageBox.Show($"Update-Prüfung fehlgeschlagen:\n{result.ErrorMessage}\n\nAktuell: {result.CurrentVersion}",
                        "Update-Prüfung", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!result.IsUpdateAvailable)
                {
                    MessageBox.Show($"Sie haben die neueste Version.\n\nAktuell: {result.CurrentVersion}",
                        "Update-Prüfung", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (string.IsNullOrWhiteSpace(result.MsiDownloadUrl))
                {
                    MessageBox.Show(
                        $"Neue Version {result.LatestVersion} gefunden, aber kein MSI-Asset in der Release.",
                        "Update-Prüfung",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var dlg = new UpdateDialog(result) { Owner = this };
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler bei Update-Prüfung:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void OnSelectAllChecked(object sender, RoutedEventArgs e) { foreach (var m in Materialien) m.IsSelected = true; }
        private void OnSelectAllUnchecked(object sender, RoutedEventArgs e) { foreach (var m in Materialien) m.IsSelected = false; }
        private void CheckBox_Click(object sender, RoutedEventArgs e) { if (sender is CheckBox cb && cb.DataContext is MaterialItem item) item.IsSelected = cb.IsChecked ?? false; }

        private void OnFormFilterChanged(object sender, SelectionChangedEventArgs e) { }
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchBox == null || MaterialGrid == null)
                return;

            var query = SearchBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                MaterialGrid.ItemsSource = Materialien;
                return;
            }

            var q = query.ToLowerInvariant();
            var filtered = Materialien.Where(m =>
                (m.MaterialArt ?? string.Empty).ToLowerInvariant().Contains(q) ||
                (m.Legierung ?? string.Empty).ToLowerInvariant().Contains(q) ||
                (m.Oberflaeche ?? string.Empty).ToLowerInvariant().Contains(q) ||
                (m.Guete ?? string.Empty).ToLowerInvariant().Contains(q) ||
                (m.Form ?? string.Empty).ToLowerInvariant().Contains(q) ||
                m.Staerke.ToString("0.0").ToLowerInvariant().Contains(q) ||
                (m.Mass ?? string.Empty).ToLowerInvariant().Contains(q) ||
                m.Stueckzahl.ToString().Contains(q) ||
                (m.Lagerort ?? string.Empty).ToLowerInvariant().Contains(q) ||
                (m.Restnummer ?? string.Empty).ToLowerInvariant().Contains(q) ||
                (m.AuftragNr ?? string.Empty).ToLowerInvariant().Contains(q) ||
                (m.Lieferant ?? string.Empty).ToLowerInvariant().Contains(q) ||
                (m.LieferscheinNr ?? string.Empty).ToLowerInvariant().Contains(q) ||
                (m.AngelegtVon ?? string.Empty).ToLowerInvariant().Contains(q) ||
                (m.GeaendertVon ?? string.Empty).ToLowerInvariant().Contains(q)
            ).ToList();

            MaterialGrid.ItemsSource = filtered;
        }
        private void OnWeightModeChanged(object sender, SelectionChangedEventArgs e) { }

        private void OnTafelVerbrauchen(object sender, RoutedEventArgs e)
        {
            if (MaterialGrid.SelectedItem is MaterialItem item)
            {
                var dlg = new TafelVerbrauchDialog(item, Materialien) { Owner = this };
                if (dlg.ShowDialog() == true) 
                { 
                    // ✅ SOFORT UI aktualisieren
                    if (item.Stueckzahl <= 1) 
                    { 
                        Services.BuchungsService.BucheAusgang(item); 
                        Materialien.Remove(item); 
                    } 
                    else 
                    {
                        item.Stueckzahl--;
                    }
                    UpdateStats();
                    
                    // ✅ Asynchron speichern
                    System.Threading.Tasks.Task.Run(() => SaveNow());
                }
            }
        }

        private void OnNiedrigeBestaendeClick(object sender, RoutedEventArgs e)
        {
            var dlg = new NiedrigeBestaendeDialog(Materialien) { Owner = this };
            if (dlg.ShowDialog() == true && dlg.MaterialZumBearbeiten != null)
            {
                var editDlg = new MaterialDialog(Materialien) { Owner = this };
                editDlg.SetEditMode(dlg.MaterialZumBearbeiten);
                if (editDlg.ShowDialog() == true)
                {
                    var idx = Materialien.IndexOf(dlg.MaterialZumBearbeiten);
                    if (idx >= 0)
                    {
                        // ✅ SOFORT aktualisieren
                        Materialien[idx] = editDlg.Material;
                        UpdateStats();
                        
                        // ✅ Asynchron speichern
                        System.Threading.Tasks.Task.Run(() => SaveNow());
                    }
                }
            }
        }

        private void OnEuPaletteClick(object sender, RoutedEventArgs e)
        {
            var dlg = new EuPaletteDialog(Materialien) { Owner = this };
            dlg.ShowDialog();
            if (dlg.HasChanges)
            {
                // ✅ SOFORT Stats aktualisieren
                UpdateStats();
                
                // ✅ Asynchron speichern
                System.Threading.Tasks.Task.Run(() => SaveNow());
            }
        }
        private void OnResteSuchen(object sender, RoutedEventArgs e) 
        { 
            var dlg = new ResteSucheDialog { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                // Suche nach passenden Materialien
                var gefunden = Materialien.Where(m =>
                {
                    bool match = true;

                    // Material prüfen
                    if (!string.IsNullOrEmpty(dlg.Material) && m.MaterialArt != dlg.Material)
                        match = false;

                    // Legierung prüfen
                    if (!string.IsNullOrEmpty(dlg.Legierung) && m.Legierung != dlg.Legierung)
                        match = false;

                    // Stärke mit Toleranz prüfen
                    if (dlg.Staerke.HasValue)
                    {
                        var toleranz = dlg.ToleranzProzent / 100.0;
                        var min = dlg.Staerke.Value * (1 - toleranz);
                        var max = dlg.Staerke.Value * (1 + toleranz);
                        if (m.Staerke < min || m.Staerke > max)
                            match = false;
                    }

                    // Maße mit Toleranz prüfen
                    if (dlg.Laenge.HasValue && dlg.Breite.HasValue)
                    {
                        var parts = m.Mass?.Split('x', '×');
                        if (parts?.Length == 2)
                        {
                            if (int.TryParse(parts[0].Trim(), out var l) && int.TryParse(parts[1].Trim(), out var b))
                            {
                                var toleranz = dlg.ToleranzProzent / 100.0;
                                var minL = dlg.Laenge.Value * (1 - toleranz);
                                var maxL = dlg.Laenge.Value * (1 + toleranz);
                                var minB = dlg.Breite.Value * (1 - toleranz);
                                var maxB = dlg.Breite.Value * (1 + toleranz);

                                if (l < minL || l > maxL || b < minB || b > maxB)
                                    match = false;
                            }
                        }
                    }

                    // Form prüfen
                    if (dlg.Form != "Alle" && m.Form != dlg.Form)
                        match = false;

                    return match;
                }).ToList();

                // Markiere gefundene Materialien GRÜN
                foreach (var m in Materialien)
                    m.IsHighlighted = gefunden.Contains(m);

                if (!gefunden.Any())
                {
                    MessageBox.Show("❌ Keine passenden Materialien gefunden.",
                        "Reste-Suche Ergebnis", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show($"✅ {gefunden.Count} passende(s) Material(ien) gefunden!  \n\nDie Materialien sind GRÜN markiert.",
                    "Reste-Suche Ergebnis", MessageBoxButton.OK, MessageBoxImage.Information);

                var auswahlDlg = new ResteAuswahlDialog(gefunden) { Owner = this };
                if (auswahlDlg.ShowDialog() != true || auswahlDlg.SelectedMaterial == null)
                    return;

                var reservierungDlg = new ResteReservierungDialog(auswahlDlg.SelectedMaterial.AuftragNr) { Owner = this };
                if (reservierungDlg.ShowDialog() == true)
                {
                    var auftragNr = reservierungDlg.AuftragNr;
                    if (!string.IsNullOrWhiteSpace(auftragNr))
                    {
                        var user = Environment.UserName;
                        
                        // ✅ SOFORT in UI aktualisieren
                        auswahlDlg.SelectedMaterial.AuftragNr = auftragNr.Trim();
                        auswahlDlg.SelectedMaterial.GeaendertVon = user;
                        auswahlDlg.SelectedMaterial.AenderungsDatum = DateTime.Now;
                        UpdateStats();
                        
                        // ✅ Asynchron speichern
                        System.Threading.Tasks.Task.Run(() => SaveNow());
                    }
                }
            }
        }
        private void OnInventur(object sender, RoutedEventArgs e) { new InventurDialog(Materialien) { Owner = this }.ShowDialog(); }
        private void OnStatistik(object sender, RoutedEventArgs e) { new StatistikDialog(Materialien) { Owner = this }.ShowDialog(); }

        private void OnExport(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx" };
            if (dlg.ShowDialog() == true) { try { ExcelService.Export(dlg.FileName, Materialien); MessageBox.Show("✅ Export erfolgreich"); } catch (Exception ex) { MessageBox.Show($"Fehler: {ex.Message}"); } }
        }

        private void OnImport(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Excel (*.xlsx)|*.xlsx" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var items = ExcelService.Import(dlg.FileName);
                    if (items?.Any() != true) { MessageBox.Show("Keine Materialien gefunden"); return; }
                    if (MessageBox.Show($"{items.Count()} Materialien. Ersetzen?", "Import", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        // ✅ SOFORT UI aktualisieren
                        Materialien.Clear();
                        foreach (var item in items) Materialien.Add(item);
                        UpdateStats();
                        
                        // ✅ Asynchron speichern
                        System.Threading.Tasks.Task.Run(() => SaveNow());
                        
                        MessageBox.Show("✅ Import erfolgreich");
                    }
                }
                catch (Exception ex) { MessageBox.Show($"Fehler: {ex.Message}"); }
            }
        }

        private void OnUndo(object sender, RoutedEventArgs e) 
        { 
            if (Services.UndoService.CanUndo) 
            {
                var geloeschteItems = Services.UndoService.Undo();
                if (geloeschteItems != null)
                {
                    // ✅ SOFORT in UI wiederherstellen
                    foreach (var item in geloeschteItems)
                    {
                        Materialien.Add(item);
                    }
                    UpdateStats();
                    
                    // ✅ Asynchron speichern
                    System.Threading.Tasks.Task.Run(() => SaveNow());
                    
                    MessageBox.Show($"{geloeschteItems.Count} Material(ien) wiederhergestellt!", "Rückgängig", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Keine Aktion zum Rückgängig machen verfügbar.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        private void OnRedo(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Redo-Funktion ist nicht verfügbar.\n\nVerwendet Ctrl+Z oder Alt+Pfeil Links zum Rückgängigmachen.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnUeber(object sender, RoutedEventArgs e) { MessageBox.Show("MaterialManager V01 v1.0\n.NET 8.0 | WPF\n\nMit Reservierungs-Funktion"); }
        private void OnBeenden(object sender, RoutedEventArgs e) { Application.Current.Shutdown(); }
        
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // ✅ DELETE - Material löschen
            if (e.Key == Key.Delete)
            {
                OnMaterialLoeschen(null, null);
                e.Handled = true;
                return;
            }

            // ✅ CTRL+Z - Undo (Zurück)
            if (e.Key == Key.Z && (e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0)
            {
                OnUndo(null, null);
                e.Handled = true;
                return;
            }

            // ✅ CTRL+Y - Redo (Vorwärts)
            if (e.Key == Key.Y && (e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0)
            {
                OnRedo(null, null);
                e.Handled = true;
                return;
            }

            // ✅ ALT+LINKS - Browser-ähnliche Zurück-Navigation
            if (e.Key == Key.Left && (e.KeyboardDevice.Modifiers & ModifierKeys.Alt) != 0)
            {
                OnUndo(null, null);
                e.Handled = true;
                return;
            }

            // ✅ ALT+RECHTS - Browser-ähnliche Vorwärts-Navigation
            if (e.Key == Key.Right && (e.KeyboardDevice.Modifiers & ModifierKeys.Alt) != 0)
            {
                OnRedo(null, null);
                e.Handled = true;
                return;
            }
        }

        private void UpdateStats()
        {
            var total = Materialien.Sum(m => m.GewichtKg);
            GesamtGewichtText = $"{total:F2} kg";
            DurchschnittAuslastung = Materialien.Any() ? 50 : 0;
            AuslastungText = "Ø 50% (Regale A-J)";
            
            var restGewicht = Materialien.Where(m => m.Form == "Rest").Sum(m => m.GewichtKg);
            EuPalettePct = restGewicht / 2000.0 * 100.0;
            EuPaletteDisplayText = $"{restGewicht:F2} / 2.000 kg ({EuPalettePct:F0}%)";
            
            var niedrig = Materialien.Count(m => (m.Form == "GF" || m.Form == "MF" || m.Form == "KF") && m.Stueckzahl <= 3);
            NiedrigeBestaendeText = niedrig > 0 ? $"{niedrig} Materialien" : "Alle OK ✓";
            NiedrigeBestaendeFarbe = niedrig > 0 ? "#FF9800" : "#4CAF50";
            
            ReservierteResteCount = Materialien.Count(m => !string.IsNullOrEmpty(m.AuftragNr));
        }

        private readonly object _saveLock = new object();
        
        private void SaveNow()
        {
            // ✅ Thread-Safe: Nur ein Speichervorgang gleichzeitig
            lock (_saveLock)
            {
                try
                {
                    var savePath = Services.NetzwerkService.GetSavePath();
                    
                    var dir = Path.GetDirectoryName(savePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                        System.Diagnostics.Debug.WriteLine($"[SaveNow] Verzeichnis erstellt: {dir}");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[SaveNow] Speichere zu: {savePath}");
                    
                    // ✅ Erstelle Snapshot der aktuellen Daten (Thread-Safe)
                    var snapshot = Materialien.ToList();
                    ExcelService.Export(savePath, snapshot);
                    
                    _lastSaveUtc = DateTime.UtcNow;
                    System.Diagnostics.Debug.WriteLine($"[SaveNow] FERTIG gespeichert um {DateTime.Now:HH:mm:ss}!");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SaveNow] FEHLER: {ex.Message}");
                }
            }
        }

        private void LoadAutosave()
        {
            try
            {
                var savePath = Services.NetzwerkService.GetSavePath();
                System.Diagnostics.Debug.WriteLine($"[LoadAutosave] Versuche zu laden: {savePath}");
                
                // Wenn Datei nicht existiert → Nicht laden, einfach leer starten
                if (!File.Exists(savePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadAutosave] Datei existiert NICHT - starte mit leerer Liste");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[LoadAutosave] Datei existiert, lade sie...");
                var items = ExcelService.Import(savePath);
                Materialien.Clear();
                foreach (var item in items ?? Enumerable.Empty<MaterialItem>())
                    Materialien.Add(item);
                
                // ✅ Registriere dass wir gerade geladen haben
                Services.ReloadService.RegisterLoad(savePath);
                
                System.Diagnostics.Debug.WriteLine($"[LoadAutosave] FERTIG! {Materialien.Count} Materialien geladen");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadAutosave] FEHLER: {ex.Message}");
            }
        }

        private void OnRegalauslastungClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount < 2)
                return;

            var dlg = new RegalauslastungDialog(Materialien) { Owner = this };
            dlg.ShowDialog();
            e.Handled = true;
        }

        private void OnNetzwerkEinstellungen(object sender, RoutedEventArgs e)
        {
            var dlg = new NetzwerkEinstellungenDialog { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                var savePath = Services.NetzwerkService.GetSavePath();
                Services.FileWatcherService.StartWatching(savePath);
            }
        }

        private void OnLizenzAktivieren(object sender, RoutedEventArgs e)
        {
            var dlg = new Views.LicenseActivationDialog { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                var status = Services.LicenseService.GetStatusMessage();
                Title = $"MaterialManager V01 - {status}";
            }
        }

        // ✅ PRINT & EXPORT FUNKTIONEN
        private void OnPrintMaterialList(object sender, RoutedEventArgs e)
        {
            if (!Materialien.Any())
            {
                MessageBox.Show("Keine Materialien zum Drucken vorhanden.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Services.PrintService.PrintMaterialList(Materialien);
        }

        private void OnExportExcel(object sender, RoutedEventArgs e)
        {
            if (!Materialien.Any())
            {
                MessageBox.Show("Keine Materialien zum Exportieren vorhanden.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "Excel-Datei (*.xlsx)|*.xlsx",
                FileName = $"Materialliste_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (saveDialog.ShowDialog() == true)
            {
                Services.PrintService.ExportToExcel(saveDialog.FileName, Materialien);
            }
        }

        private void OnExportCSV(object sender, RoutedEventArgs e)
        {
            if (!Materialien.Any())
            {
                MessageBox.Show("Keine Materialien zum Exportieren vorhanden.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "CSV-Datei (*.csv)|*.csv",
                FileName = $"Materialliste_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                Services.PrintService.ExportToCSV(saveDialog.FileName, Materialien);
            }
        }

        // ✅ AUTO-UPDATE PRÜFUNG BEIM START
        private async System.Threading.Tasks.Task CheckForUpdatesOnStartupAsync()
        {
            try
            {
                if (!Services.GitHubUpdateService.ShouldRunAutoCheckToday())
                    return;

                Services.GitHubUpdateService.MarkAutoCheckedNow();

                var result = await Services.GitHubUpdateService.CheckForUpdatesAsync();
                if (!result.IsUpdateAvailable || string.IsNullOrWhiteSpace(result.MsiDownloadUrl))
                    return;

                Dispatcher.Invoke(() =>
                {
                    var dlg = new UpdateDialog(result) { Owner = this };
                    dlg.ShowDialog();
                });
            }
            catch
            {
                // Start darf durch Update-Check nie blockiert werden
            }
        }

    }
}
