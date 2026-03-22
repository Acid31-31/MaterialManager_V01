using MaterialManager_V01.Models;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MaterialManager_V01.Services
{
    public static class QrCodeService
    {
        /// <summary>
        /// Erstellt einen einfachen QR-Code als Bitmap (ohne externe Bibliothek).
        /// Erzeugt einen Text-Barcode mit den Materialinformationen.
        /// </summary>
        public static void ZeigeEtikett(MaterialItem material)
        {
            var window = new Window
            {
                Title = $"Material-Etikett - {material.Restnummer}",
                Width = 420,
                Height = 480,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = Brushes.White
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Titel
            var titel = new TextBlock
            {
                Text = "MATERIAL-ETIKETT",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(titel, 0);
            grid.Children.Add(titel);

            // Etikett-Inhalt
            var border = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(2),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 4, 0, 4)
            };

            var stack = new StackPanel();

            // Restnummer groß
            if (!string.IsNullOrWhiteSpace(material.Restnummer))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = material.Restnummer,
                    FontSize = 28,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontFamily = new FontFamily("Consolas"),
                    Margin = new Thickness(0, 0, 0, 8)
                });
            }

            // Barcode-ähnliche Darstellung (Striche basierend auf Restnummer)
            var barcodeCanvas = ErstelleBarcode(material.Restnummer ?? material.MaterialArt, 360, 40);
            stack.Children.Add(barcodeCanvas);

            // Details
            AddEtikettZeile(stack, "Material:", $"{material.MaterialArt} {material.Legierung}");
            AddEtikettZeile(stack, "Oberfläche:", material.Oberflaeche);
            if (!string.IsNullOrWhiteSpace(material.Guete))
                AddEtikettZeile(stack, "Güte:", material.Guete);
            AddEtikettZeile(stack, "Stärke:", $"{material.Staerke:0.0} mm");
            AddEtikettZeile(stack, "Maß:", material.Mass);
            AddEtikettZeile(stack, "Gewicht:", $"{material.GewichtKg:N2} kg");
            AddEtikettZeile(stack, "Lagerort:", material.Lagerort);
            
            // ✅ Lieferant und Liifierscheinnummer IMMER anzeigen (auch wenn leer)
            AddEtikettZeile(stack, "Lieferant:", material.Lieferant ?? "-");
            AddEtikettZeile(stack, "Lieferschein:", material.LieferscheinNr ?? "-");
            
            if (material.Datum.HasValue)
                AddEtikettZeile(stack, "Datum:", material.Datum.Value.ToString("dd.MM.yyyy"));

            border.Child = stack;
            Grid.SetRow(border, 1);
            grid.Children.Add(border);

            // Drucken-Button
            var druckenBtn = new Button
            {
                Content = "🖨️ Etikett drucken",
                Padding = new Thickness(16, 8, 16, 8),
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            druckenBtn.Click += (s, e) =>
            {
                var pd = new System.Windows.Controls.PrintDialog();
                if (pd.ShowDialog() == true)
                {
                    pd.PrintVisual(border, $"Etikett {material.Restnummer}");
                    MessageBox.Show("Etikett gesendet!", "Drucken", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };

            Grid.SetRow(druckenBtn, 2);
            grid.Children.Add(druckenBtn);

            window.Content = grid;
            window.ShowDialog();
        }

        private static void AddEtikettZeile(StackPanel stack, string label, string value)
        {
            var panel = new DockPanel { Margin = new Thickness(0, 1, 0, 1) };
            panel.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Width = 80
            });
            panel.Children.Add(new TextBlock
            {
                Text = value ?? "",
                FontSize = 10
            });
            stack.Children.Add(panel);
        }

        /// <summary>
        /// Erzeugt eine einfache Barcode-ähnliche Darstellung (Code 39 vereinfacht)
        /// </summary>
        private static Canvas ErstelleBarcode(string text, double width, double height)
        {
            var canvas = new Canvas
            {
                Width = width,
                Height = height,
                Margin = new Thickness(0, 4, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = Brushes.White
            };

            if (string.IsNullOrEmpty(text)) return canvas;

            // Einfache Barcode-Generierung basierend auf Zeichenwerten
            double x = 0;
            double barWidth = width / (text.Length * 12 + 2);

            foreach (char c in text)
            {
                var pattern = GetBarcodePattern(c);
                foreach (var narrow in pattern)
                {
                    var w = narrow ? barWidth : barWidth * 2.5;
                    var rect = new System.Windows.Shapes.Rectangle
                    {
                        Width = w,
                        Height = height,
                        Fill = Brushes.Black
                    };
                    Canvas.SetLeft(rect, x);
                    canvas.Children.Add(rect);
                    x += w;

                    // Lücke
                    x += barWidth * 0.5;
                }
                x += barWidth; // Zeichenabstand
            }

            return canvas;
        }

        private static bool[] GetBarcodePattern(char c)
        {
            // Vereinfachtes Muster basierend auf Zeichenwert
            var val = (int)c;
            return new[]
            {
                (val & 1) != 0,
                (val & 2) != 0,
                (val & 4) != 0,
                (val & 8) != 0,
                true,
                (val & 16) != 0,
                (val & 32) != 0,
                (val & 64) != 0,
                true
            };
        }
    }
}
