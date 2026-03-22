using MaterialManager_V01.Models;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;

namespace MaterialManager_V01.Services
{
    /// <summary>
    /// Service für Druck und Export von Materiallisten
    /// </summary>
    public static class PrintService
    {
        /// <summary>
        /// Exportiert Materialliste als CSV
        /// </summary>
        public static bool ExportToCSV(string filePath, IEnumerable<MaterialItem> materialien)
        {
            try
            {
                using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                {
                    // Header
                    writer.WriteLine("Material,Legierung,Oberfläche,Güte,Form,Stärke,Maß,Stück,Gewicht (kg),Lagerort,Restnummer,Datum,Angelegt von");

                    // Daten
                    foreach (var item in materialien)
                    {
                        var line = $"\"{item.MaterialArt}\",\"{item.Legierung}\",\"{item.Oberflaeche}\",\"{item.Guete}\",\"{item.Form}\",\"{item.Staerke:0.0}\",\"{item.Mass}\",\"{item.Stueckzahl}\",\"{item.GewichtKg:0.00}\",\"{item.Lagerort}\",\"{item.Restnummer}\",\"{item.Datum:dd.MM.yyyy}\",\"{item.AngelegtVon}\"";
                        writer.WriteLine(line);
                    }
                }

                MessageBox.Show($"✅ CSV erfolgreich exportiert!\n\n{filePath}", "Export erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Fehler beim Export:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Generiert QR-Code für ein Material
        /// </summary>
        public static Bitmap GenerateQRCode(string content, int size = 200)
        {
            try
            {
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
                    QRCode qrCode = new QRCode(qrCodeData);
                    return qrCode.GetGraphic(size / 29); // 29 Module für 200px
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Druckt Materialliste
        /// </summary>
        public static void PrintMaterialList(IEnumerable<MaterialItem> materialien)
        {
            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();
                
                if (printDialog.ShowDialog() == true)
                {
                    var document = CreatePrintDocument(materialien);
                    var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
                    printDialog.PrintDocument(paginator, "Materialliste");
                    MessageBox.Show("✅ Druck gestartet!", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Druckfehler:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Erstellt Druck-Dokument
        /// </summary>
        private static FlowDocument CreatePrintDocument(IEnumerable<MaterialItem> materialien)
        {
            var doc = new FlowDocument();
            doc.PageHeight = 1056;
            doc.PageWidth = 816;
            doc.Foreground = System.Windows.Media.Brushes.Black;

            // Titel
            var title = new Paragraph(new Run("📋 Materialliste"))
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                TextAlignment = System.Windows.TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            doc.Blocks.Add(title);

            // Datum
            var dateTime = new Paragraph(new Run($"Stand: {DateTime.Now:dd.MM.yyyy HH:mm}"))
            {
                TextAlignment = System.Windows.TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            doc.Blocks.Add(dateTime);

            // Tabelle
            var table = new Table { BorderThickness = new Thickness(1), BorderBrush = System.Windows.Media.Brushes.Black };
            table.Columns.Add(new TableColumn { Width = new System.Windows.GridLength(100) });
            table.Columns.Add(new TableColumn { Width = new System.Windows.GridLength(100) });
            table.Columns.Add(new TableColumn { Width = new System.Windows.GridLength(100) });
            table.Columns.Add(new TableColumn { Width = new System.Windows.GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new System.Windows.GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new System.Windows.GridLength(100) });

            // Header Row
            var headerRow = new TableRow();
            AddTableCell(headerRow, "Material", true);
            AddTableCell(headerRow, "Legierung", true);
            AddTableCell(headerRow, "Maß", true);
            AddTableCell(headerRow, "Stück", true);
            AddTableCell(headerRow, "Gewicht", true);
            AddTableCell(headerRow, "Lagerort", true);
            table.RowGroups[0].Rows.Add(headerRow);

            // Daten-Rows
            foreach (var item in materialien.Take(100)) // Max 100 Items pro Seite
            {
                var row = new TableRow();
                AddTableCell(row, item.MaterialArt);
                AddTableCell(row, item.Legierung);
                AddTableCell(row, item.Mass);
                AddTableCell(row, item.Stueckzahl.ToString());
                AddTableCell(row, $"{item.GewichtKg:0.00} kg");
                AddTableCell(row, item.Lagerort);
                table.RowGroups[0].Rows.Add(row);
            }

            doc.Blocks.Add(table);
            return doc;
        }

        /// <summary>
        /// Hilfsfunktion für Tabellenzellen
        /// </summary>
        private static void AddTableCell(TableRow row, string text, bool isHeader = false)
        {
            var cell = new TableCell(new Paragraph(new Run(text)))
            {
                Padding = new Thickness(5),
                BorderThickness = new Thickness(1),
                BorderBrush = System.Windows.Media.Brushes.Black
            };

            if (isHeader)
            {
                cell.Background = System.Windows.Media.Brushes.LightGray;
                ((Paragraph)cell.Blocks.FirstBlock).FontWeight = FontWeights.Bold;
            }

            row.Cells.Add(cell);
        }

        /// <summary>
        /// Erstellt Excel-Datei mit Formatierung
        /// </summary>
        public static bool ExportToExcel(string filePath, IEnumerable<MaterialItem> materialien)
        {
            try
            {
                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Materialliste");

                    // Header
                    ws.Cell(1, 1).Value = "Material";
                    ws.Cell(1, 2).Value = "Legierung";
                    ws.Cell(1, 3).Value = "Oberfläche";
                    ws.Cell(1, 4).Value = "Güte";
                    ws.Cell(1, 5).Value = "Form";
                    ws.Cell(1, 6).Value = "Stärke (mm)";
                    ws.Cell(1, 7).Value = "Maß";
                    ws.Cell(1, 8).Value = "Stück";
                    ws.Cell(1, 9).Value = "Gewicht (kg)";
                    ws.Cell(1, 10).Value = "Lagerort";
                    ws.Cell(1, 11).Value = "Restnummer";
                    ws.Cell(1, 12).Value = "Datum";
                    ws.Cell(1, 13).Value = "Angelegt von";

                    // Format Header
                    var headerRow = ws.Range(1, 1, 1, 13);
                    headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(76, 175, 80);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                    headerRow.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                    // Daten
                    int row = 2;
                    foreach (var item in materialien)
                    {
                        ws.Cell(row, 1).Value = item.MaterialArt;
                        ws.Cell(row, 2).Value = item.Legierung;
                        ws.Cell(row, 3).Value = item.Oberflaeche;
                        ws.Cell(row, 4).Value = item.Guete;
                        ws.Cell(row, 5).Value = item.Form;
                        ws.Cell(row, 6).Value = item.Staerke;
                        ws.Cell(row, 7).Value = item.Mass;
                        ws.Cell(row, 8).Value = item.Stueckzahl;
                        ws.Cell(row, 9).Value = item.GewichtKg;
                        ws.Cell(row, 10).Value = item.Lagerort;
                        ws.Cell(row, 11).Value = item.Restnummer;
                        ws.Cell(row, 12).Value = item.Datum.HasValue ? item.Datum.Value.ToShortDateString() : "";
                        ws.Cell(row, 13).Value = item.AngelegtVon;
                        row++;
                    }

                    // Auto-Breite
                    ws.Columns().AdjustToContents();

                    workbook.SaveAs(filePath);
                    MessageBox.Show($"✅ Excel erfolgreich exportiert!\n\n{filePath}", "Export erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Fehler beim Excel-Export:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}
