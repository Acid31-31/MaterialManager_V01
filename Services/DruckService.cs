using MaterialManager_V01.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace MaterialManager_V01.Services
{
    public static class DruckService
    {
        /// <summary>
        /// Druckt die Lagerliste (nach Regal sortiert)
        /// </summary>
        public static void DruckeLagerliste(IEnumerable<MaterialItem> materialien)
        {
            var doc = new FlowDocument
            {
                PageWidth = 1122, // A4 Querformat
                PageHeight = 794,
                PagePadding = new Thickness(40),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10
            };

            // Titel
            var titel = new Paragraph(new Run("MaterialManager R03 - Lagerliste"))
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center
            };
            titel.Inlines.Add(new LineBreak());
            titel.Inlines.Add(new Run($"Stand: {DateTime.Now:dd.MM.yyyy HH:mm}") { FontSize = 11, FontWeight = FontWeights.Normal });
            doc.Blocks.Add(titel);

            // Nach Regal gruppieren
            var gruppen = materialien
                .OrderBy(m => m.Lagerort)
                .ThenBy(m => m.MaterialArt)
                .ThenBy(m => m.Legierung)
                .GroupBy(m => m.Lagerort);

            foreach (var gruppe in gruppen)
            {
                var regalTitel = new Paragraph(new Run($"📦 {gruppe.Key}"))
                {
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 12, 0, 4),
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(0, 0, 0, 1)
                };
                doc.Blocks.Add(regalTitel);

                var table = ErstelleTabelle(gruppe.ToList(), false);
                doc.Blocks.Add(table);

                var gewicht = gruppe.Sum(m => m.GewichtKg);
                var summary = new Paragraph(new Run($"Σ {gruppe.Count()} Positionen | {gewicht:N2} kg"))
                {
                    FontSize = 9,
                    FontStyle = FontStyles.Italic,
                    TextAlignment = TextAlignment.Right
                };
                doc.Blocks.Add(summary);
            }

            // Gesamt
            var gesamt = new Paragraph(new Run($"GESAMT: {materialien.Count()} Positionen | {materialien.Sum(m => m.GewichtKg):N2} kg"))
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 16, 0, 0)
            };
            doc.Blocks.Add(gesamt);

            ZeigeDruckVorschau(doc, "Lagerliste");
        }

        /// <summary>
        /// Druckt die Restmaterial-Liste
        /// </summary>
        public static void DruckeResteliste(IEnumerable<MaterialItem> materialien)
        {
            var reste = materialien.Where(m => m.Form == "Rest").ToList();

            var doc = new FlowDocument
            {
                PageWidth = 1122,
                PageHeight = 794,
                PagePadding = new Thickness(40),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10
            };

            var titel = new Paragraph(new Run("MaterialManager R03 - Restmaterial-Liste"))
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center
            };
            titel.Inlines.Add(new LineBreak());
            titel.Inlines.Add(new Run($"Stand: {DateTime.Now:dd.MM.yyyy HH:mm} | {reste.Count} Reste | {reste.Sum(m => m.GewichtKg):N2} kg") { FontSize = 11, FontWeight = FontWeights.Normal });
            doc.Blocks.Add(titel);

            var table = ErstelleTabelle(reste, true);
            doc.Blocks.Add(table);

            ZeigeDruckVorschau(doc, "Restmaterial-Liste");
        }

        private static Table ErstelleTabelle(List<MaterialItem> items, bool mitRestnummer)
        {
            var table = new Table { CellSpacing = 0, FontSize = 9 };

            var cols = new[] { 80, 100, 100, 60, 60, 80, 50, 70, 80 };
            if (mitRestnummer) cols = new[] { 70, 90, 90, 55, 55, 75, 45, 65, 80, 80 };

            foreach (var w in cols)
                table.Columns.Add(new TableColumn { Width = new GridLength(w) });

            var headerGroup = new TableRowGroup();
            var headerRow = new TableRow { Background = Brushes.LightGray };

            headerRow.Cells.Add(NeueZelle("Material", true));
            headerRow.Cells.Add(NeueZelle("Legierung", true));
            headerRow.Cells.Add(NeueZelle("Oberfläche", true));
            headerRow.Cells.Add(NeueZelle("Form", true));
            headerRow.Cells.Add(NeueZelle("Stärke", true));
            headerRow.Cells.Add(NeueZelle("Maß", true));
            headerRow.Cells.Add(NeueZelle("Stk", true));
            headerRow.Cells.Add(NeueZelle("kg", true));
            headerRow.Cells.Add(NeueZelle("Lagerort", true));
            if (mitRestnummer) headerRow.Cells.Add(NeueZelle("Rest-Nr", true));

            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);

            var bodyGroup = new TableRowGroup();
            bool alternate = false;

            foreach (var m in items)
            {
                var row = new TableRow();
                if (alternate) row.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));

                row.Cells.Add(NeueZelle(m.MaterialArt));
                row.Cells.Add(NeueZelle(m.Legierung));
                row.Cells.Add(NeueZelle(m.Oberflaeche));
                row.Cells.Add(NeueZelle(m.Form));
                row.Cells.Add(NeueZelle($"{m.Staerke:0.0}"));
                row.Cells.Add(NeueZelle(m.Mass));
                row.Cells.Add(NeueZelle(m.Stueckzahl.ToString()));
                row.Cells.Add(NeueZelle($"{m.GewichtKg:N2}"));
                row.Cells.Add(NeueZelle(m.Lagerort));
                if (mitRestnummer) row.Cells.Add(NeueZelle(m.Restnummer));

                bodyGroup.Rows.Add(row);
                alternate = !alternate;
            }

            table.RowGroups.Add(bodyGroup);
            return table;
        }

        private static TableCell NeueZelle(string text, bool header = false)
        {
            var para = new Paragraph(new Run(text))
            {
                Margin = new Thickness(2),
                FontWeight = header ? FontWeights.Bold : FontWeights.Normal,
                FontSize = header ? 9 : 8.5
            };
            return new TableCell(para)
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0, 0, 0, 0.5),
                Padding = new Thickness(2)
            };
        }

        private static void ZeigeDruckVorschau(FlowDocument doc, string titel)
        {
            var window = new Window
            {
                Title = $"Druckvorschau - {titel}",
                Width = 1000,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8) };
            var druckenBtn = new Button
            {
                Content = "🖨️ Drucken",
                Padding = new Thickness(16, 8, 16, 8),
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var pdfBtn = new Button
            {
                Content = "📄 Als PDF",
                Padding = new Thickness(16, 8, 16, 8),
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(8, 0, 0, 0)
            };

            druckenBtn.Click += (s, e) =>
            {
                var pd = new PrintDialog();
                if (pd.ShowDialog() == true)
                {
                    doc.PageWidth = pd.PrintableAreaWidth;
                    doc.PageHeight = pd.PrintableAreaHeight;
                    var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
                    pd.PrintDocument(paginator, titel);
                    MessageBox.Show("Druckauftrag gesendet!", "Drucken", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };

            pdfBtn.Click += (s, e) =>
            {
                try
                {
                    var server = new LocalPrintServer();
                    var pdfQueue = server.GetPrintQueues().FirstOrDefault(q => q.Name.Equals("Microsoft Print to PDF", StringComparison.OrdinalIgnoreCase));
                    if (pdfQueue == null)
                    {
                        MessageBox.Show("PDF-Drucker nicht gefunden (Microsoft Print to PDF).", "PDF", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var pd = new PrintDialog { PrintQueue = pdfQueue };
                    doc.PageWidth = pd.PrintableAreaWidth;
                    doc.PageHeight = pd.PrintableAreaHeight;
                    var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
                    pd.PrintDocument(paginator, titel);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"PDF-Export fehlgeschlagen:\n{ex.Message}", "PDF", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            toolbar.Children.Add(druckenBtn);
            toolbar.Children.Add(pdfBtn);

            Grid.SetRow(toolbar, 0);
            grid.Children.Add(toolbar);

            var viewer = new FlowDocumentScrollViewer
            {
                Document = doc,
                Zoom = 90
            };
            Grid.SetRow(viewer, 1);
            grid.Children.Add(viewer);

            window.Content = grid;
            window.ShowDialog();
        }
    }
}
