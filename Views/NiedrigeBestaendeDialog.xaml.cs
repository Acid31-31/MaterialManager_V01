using MaterialManager_V01.Models;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MaterialManager_V01.Views
{
    public partial class NiedrigeBestaendeDialog : Window
    {
        private ObservableCollection<MaterialItem> _alleMaterialien;
        private ICollectionView _view;
        private readonly Dictionary<MaterialItem, MaterialItem?> _anzeigeZuQuelle = new();

        public MaterialItem? MaterialZumBearbeiten { get; private set; }

        public NiedrigeBestaendeDialog(ObservableCollection<MaterialItem> materialien)
        {
            InitializeComponent();

            _alleMaterialien = materialien;

            var niedrig = ErmittleNiedrigeBestaende(materialien);
            var filtered = new ObservableCollection<MaterialItem>(niedrig);
            BestandGrid.ItemsSource = filtered;

            _view = System.Windows.Data.CollectionViewSource.GetDefaultView(filtered);
            _view.Filter = FilterMaterial;
        }

        private List<MaterialItem> ErmittleNiedrigeBestaende(IEnumerable<MaterialItem> materialien)
        {
            _anzeigeZuQuelle.Clear();

            return materialien
                .Where(m => m.Form == "GF" || m.Form == "MF" || m.Form == "KF")
                .GroupBy(m => new
                {
                    m.Kategorie,
                    m.MaterialArt,
                    m.Legierung,
                    m.Oberflaeche,
                    m.Guete,
                    m.Form,
                    m.Staerke,
                    m.Mass
                })
                .Select(gruppe =>
                {
                    var freieMaterialien = gruppe.Where(m => string.IsNullOrWhiteSpace(m.AuftragNr)).ToList();
                    var freierBestand = freieMaterialien.Sum(m => m.Stueckzahl);
                    var quelle = freieMaterialien.FirstOrDefault();
                    var basis = quelle ?? gruppe.First();

                    var anzeigeItem = new MaterialItem
                    {
                        Kategorie = basis.Kategorie,
                        MaterialArt = basis.MaterialArt,
                        Legierung = basis.Legierung,
                        Oberflaeche = basis.Oberflaeche,
                        Guete = basis.Guete,
                        Form = basis.Form,
                        Staerke = basis.Staerke,
                        Mass = basis.Mass,
                        Stueckzahl = freierBestand,
                        Restnummer = basis.Restnummer,
                        Datum = basis.Datum,
                        AenderungsDatum = basis.AenderungsDatum,
                        Lagerort = freierBestand == 0 ? "Kein freier Bestand" : basis.Lagerort,
                        AngelegtVon = basis.AngelegtVon,
                        GeaendertVon = basis.GeaendertVon,
                        Lieferant = basis.Lieferant,
                        LieferscheinNr = basis.LieferscheinNr,
                        PdfPfad = basis.PdfPfad,
                        PreisProKg = basis.PreisProKg
                    };

                    _anzeigeZuQuelle[anzeigeItem] = quelle;

                    return anzeigeItem;
                })
                .Where(m => m.Stueckzahl <= 3)
                .OrderBy(m => m.Stueckzahl)
                .ThenBy(m => m.MaterialArt)
                .ThenBy(m => m.Legierung)
                .ToList();
        }

        private bool FilterMaterial(object obj)
        {
            if (obj is not MaterialItem item) return false;

            // Material-Filter
            if (MaterialFilterBox.SelectedItem is ComboBoxItem matItem)
            {
                var matText = matItem.Content.ToString();
                if (matText != "Alle Materialien" && !item.MaterialArt.Contains(matText ?? "", StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Form-Filter
            if (FormFilterBox.SelectedItem is ComboBoxItem formItem)
            {
                var formText = formItem.Content.ToString();
                if (formText != "Alle Formen" && !string.Equals(item.Form, formText, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            _view?.Refresh();
        }

        private void OnMaterialDoppelklick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (BestandGrid.SelectedItem is not MaterialItem item)
                return;

            if (!_anzeigeZuQuelle.TryGetValue(item, out var quelle) || quelle == null)
            {
                MessageBox.Show("Für dieses Material ist aktuell kein freier Lagerbestand zum Bearbeiten vorhanden.", "Niedrige Bestände", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MaterialZumBearbeiten = quelle;
            DialogResult = true;
        }

        private void OnExportNachbestellung(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "Excel Datei (*.xlsx)|*.xlsx",
                FileName = $"Nachbestellung_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var items = BestandGrid.ItemsSource as ObservableCollection<MaterialItem>;
                    if (items != null && items.Any())
                    {
                        // Export mit ClosedXML
                        using (var wb = new ClosedXML.Excel.XLWorkbook())
                        {
                            var ws = wb.Worksheets.Add("Nachbestellung");

                            // Header
                            ws.Cell(1, 1).Value = "Material";
                            ws.Cell(1, 2).Value = "Legierung";
                            ws.Cell(1, 3).Value = "Oberfläche";
                            ws.Cell(1, 4).Value = "Güte";
                            ws.Cell(1, 5).Value = "Form";
                            ws.Cell(1, 6).Value = "Stärke (mm)";
                            ws.Cell(1, 7).Value = "Maß";
                            ws.Cell(1, 8).Value = "Aktueller Bestand";
                            ws.Cell(1, 9).Value = "Nachbestellmenge";
                            ws.Cell(1, 10).Value = "Status";

                            // Header-Stil
                            var headerRange = ws.Range(1, 1, 1, 10);
                            headerRange.Style.Font.Bold = true;
                            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

                            int row = 2;
                            foreach (var item in items.OrderBy(m => m.Stueckzahl).ThenBy(m => m.MaterialArt))
                            {
                                ws.Cell(row, 1).Value = item.MaterialArt;
                                ws.Cell(row, 2).Value = item.Legierung;
                                ws.Cell(row, 3).Value = item.Oberflaeche;
                                ws.Cell(row, 4).Value = item.Guete;
                                ws.Cell(row, 5).Value = item.Form;
                                ws.Cell(row, 6).Value = item.Staerke;
                                ws.Cell(row, 7).Value = item.Mass;
                                ws.Cell(row, 8).Value = item.Stueckzahl;
                                ws.Cell(row, 9).Value = ""; // Leer für manuelle Eingabe
                                
                                // Status
                                var status = item.Stueckzahl <= 1 ? "⚠️ KRITISCH" : 
                                             item.Stueckzahl == 2 ? "🟠 NIEDRIG" : 
                                             "🟡 WARNUNG";
                                ws.Cell(row, 10).Value = status;

                                // Farbe je nach Bestand
                                if (item.Stueckzahl <= 1)
                                {
                                    ws.Row(row).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightPink;
                                }
                                else if (item.Stueckzahl == 2)
                                {
                                    ws.Row(row).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightYellow;
                                }

                                row++;
                            }

                            // Auto-fit columns
                            ws.Columns().AdjustToContents();

                            wb.SaveAs(saveDialog.FileName);
                        }

                        MessageBox.Show($"Nachbestell-Liste erfolgreich exportiert:\n{saveDialog.FileName}", 
                            "Export erfolgreich", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Export:\n{ex.Message}", 
                        "Export fehlgeschlagen", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error);
                }
            }
        }

        private void OnSchliessen(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
