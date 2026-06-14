using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class RohrZuschnittErgebnisDialog : Window
    {
        private readonly RohrZuschnittErgebnis _ergebnis;
        private static readonly CultureInfo _de = CultureInfo.GetCultureInfo("de-DE");

        public RohrZuschnittErgebnisDialog(RohrZuschnittErgebnis ergebnis, double schnittverlustMm)
        {
            InitializeComponent();
            _ergebnis = ergebnis;
            BefuelleZusammenfassung(schnittverlustMm);
            BaueStangenKarten();
        }

        // ─── Zusammenfassung ─────────────────────────────────────────────────

        private void BefuelleZusammenfassung(double schnittverlustMm)
        {
            var stangenLaenge = _ergebnis.StandardStangenLaengeMm;
            AnzahlStangenBlock.Text  = _ergebnis.Stangen.Count.ToString();
            AnzahlTeileBlock.Text    = _ergebnis.TeilAnzahl.ToString();
            StangenlaengeBlock.Text  = $"{(int)stangenLaenge} mm";
            GesamtVerbrauchBlock.Text= $"{(int)_ergebnis.GesamtVerbrauchMm} mm";
            GesamtRestBlock.Text     = $"{(int)_ergebnis.GesamtRestMm} mm";
            VerschnittBlock.Text     = $"{_ergebnis.VerschnittProzent:N2} %";

            UntertitelBlock.Text =
                $"Stangenlänge {stangenLaenge:N0} mm  ·  " +
                $"Schnittverlust {schnittverlustMm:N1} mm/Schnitt  ·  " +
                $"Berechnet am {DateTime.Now:dd.MM.yyyy HH:mm}";
        }

        // ─── Stangen-Karten ──────────────────────────────────────────────────

        private void BaueStangenKarten()
        {
            StangenPanel.Children.Clear();
            var stangenLaenge = _ergebnis.StandardStangenLaengeMm;

            foreach (var stange in _ergebnis.Stangen)
            {
                var nutzung = stangenLaenge > 0
                    ? Math.Min(1.0, stange.VerbrauchtMm / stangenLaenge)
                    : 0;

                // ── Karten-Container
                var karte = new Border
                {
                    Background       = new SolidColorBrush(Color.FromRgb(0x14, 0x14, 0x14)),
                    BorderBrush      = new SolidColorBrush(Color.FromRgb(0x2C, 0x2C, 0x2C)),
                    BorderThickness  = new Thickness(1),
                    CornerRadius     = new CornerRadius(8),
                    Margin           = new Thickness(0, 0, 0, 14),
                };

                var stack = new StackPanel();
                karte.Child = stack;

                // ── Kopfzeile
                stack.Children.Add(BaueKopf(stange, nutzung));

                // ── Balken-Visualisierung
                stack.Children.Add(BaueBalken(nutzung, stange, stangenLaenge));

                // ── Schnittliste
                stack.Children.Add(BaueSchnittliste(stange));

                StangenPanel.Children.Add(karte);
            }
        }

        private static Border BaueKopf(RohrZuschnittStange stange, double nutzung)
        {
            var kopf = new Border
            {
                Background      = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(0x2C, 0x2C, 0x2C)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding         = new Thickness(16, 10, 16, 10),
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Titel
            var titel = new TextBlock
            {
                Text       = $"📏  Stange #{stange.Nummer}  –  {stange.Teile.Count} Schnitt(e)",
                FontSize   = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xBF, 0xA5)),
            };
            Grid.SetColumn(titel, 0);
            grid.Children.Add(titel);

            // Verbraucht
            var verbraucht = MacheInfoBlock("Verbraucht", $"{(int)stange.VerbrauchtMm} mm", Colors.White);
            Grid.SetColumn(verbraucht, 1);
            grid.Children.Add(verbraucht);

            // Rest
            var rest = MacheInfoBlock("Rest", $"{(int)stange.RestMm} mm", Color.FromRgb(0xFF, 0xA7, 0x26));
            Grid.SetColumn(rest, 3);
            grid.Children.Add(rest);

            // Nutzung
            var nutzungFarbe = nutzung >= 0.9 ? Color.FromRgb(0x00, 0xBF, 0xA5)
                             : nutzung >= 0.7 ? Color.FromRgb(0xFF, 0xA7, 0x26)
                             : Color.FromRgb(0xEF, 0x53, 0x50);
            var nutzungBlock = MacheInfoBlock("Auslastung", $"{nutzung * 100:N1} %", nutzungFarbe);
            Grid.SetColumn(nutzungBlock, 5);
            grid.Children.Add(nutzungBlock);

            kopf.Child = grid;
            return kopf;
        }

        private static StackPanel MacheInfoBlock(string label, string wert, Color farbe)
        {
            return new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock { Text = label, FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(0x77,0x77,0x77)) },
                    new TextBlock { Text = wert,  FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(farbe) }
                }
            };
        }

        private static Border BaueBalken(double nutzung, RohrZuschnittStange stange, double stangenLaenge)
        {
            var container = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x11, 0x11, 0x11)),
                Padding    = new Thickness(16, 8, 16, 8),
            };

            var grid = new Grid { Height = 32 };

            // Hintergrund (Rest)
            var hg = new Border
            {
                Background   = new SolidColorBrush(Color.FromRgb(0x1C, 0x2A, 0x1C)),
                CornerRadius = new CornerRadius(4),
            };
            grid.Children.Add(hg);

            // Verbraucht-Anteil (Grid mit SizeTo Width via Loaded)
            var verbrauchtBalken = new Border
            {
                Background          = new SolidColorBrush(Color.FromRgb(0x00, 0x69, 0x5C)),
                CornerRadius        = new CornerRadius(4),
                HorizontalAlignment = HorizontalAlignment.Left,
            };

            // Einzelne Teile als Segmente darstellen
            var segmentGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Left };

            // Prozentlabel
            var label = new TextBlock
            {
                Text                = $"{nutzung * 100:N1} % genutzt  ·  {(int)stange.VerbrauchtMm} mm von {(int)stangenLaenge} mm",
                Foreground          = Brushes.White,
                FontSize            = 11,
                FontWeight          = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
            };
            grid.Children.Add(label);

            // Balken-Breite beim Laden setzen
            grid.Loaded += (s, e) =>
            {
                var w = grid.ActualWidth * nutzung;
                verbrauchtBalken.Width = Math.Max(0, w);
            };
            grid.SizeChanged += (s, e) =>
            {
                var w = e.NewSize.Width * nutzung;
                verbrauchtBalken.Width = Math.Max(0, w);
            };

            grid.Children.Insert(1, verbrauchtBalken);
            container.Child = grid;
            return container;
        }

        private static Border BaueSchnittliste(RohrZuschnittStange stange)
        {
            var outer = new Border { Padding = new Thickness(12, 6, 12, 12) };
            var list  = new StackPanel();

            // Header
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            void AddHeader(string text, int col, HorizontalAlignment ha = HorizontalAlignment.Left)
            {
                var tb = new TextBlock
                {
                    Text = text, FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
                    HorizontalAlignment = ha, VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetColumn(tb, col);
                headerGrid.Children.Add(tb);
            }
            AddHeader("#",           0, HorizontalAlignment.Center);
            AddHeader("Bezeichnung", 1);
            AddHeader("Länge (mm)",  2);
            AddHeader("Winkel L/R",  3);
            AddHeader("PDF",         4);
            list.Children.Add(headerGrid);

            // Trennlinie
            list.Children.Add(new Border
            {
                BorderBrush     = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x25)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin          = new Thickness(0, 0, 0, 6),
            });

            // Schnitte
            int nr = 1;
            foreach (var teil in stange.Teile)
            {
                list.Children.Add(BaueSchnittZeile(nr++, teil));
            }

            outer.Child = list;
            return outer;
        }

        private static Border BaueSchnittZeile(int nr, RohrZuschnittTeil teil)
        {
            var row = new Border
            {
                Background      = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)),
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(4),
                Margin          = new Thickness(0, 3, 0, 3),
                Padding         = new Thickness(8, 8, 8, 8),
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Nummer-Badge
            var badge = new Border
            {
                Background      = new SolidColorBrush(Color.FromRgb(0x00, 0x69, 0x5C)),
                CornerRadius    = new CornerRadius(4),
                Width           = 28, Height = 28,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text                = nr.ToString(),
                    FontWeight          = FontWeights.Bold,
                    FontSize            = 13,
                    Foreground          = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center,
                }
            };
            Grid.SetColumn(badge, 0);
            grid.Children.Add(badge);

            // Bezeichnung
            var bez = new TextBlock
            {
                Text                = teil.Bezeichnung,
                FontSize            = 13,
                FontWeight          = FontWeights.SemiBold,
                Foreground          = Brushes.White,
                VerticalAlignment   = VerticalAlignment.Center,
                TextTrimming        = TextTrimming.CharacterEllipsis,
            };
            Grid.SetColumn(bez, 1);
            grid.Children.Add(bez);

            // Länge
            var laenge = MacheWertBlock($"{(int)teil.NennLaengeMm} mm", Color.FromRgb(0x00, 0xBF, 0xA5));
            Grid.SetColumn(laenge, 2);
            grid.Children.Add(laenge);

            // Winkel ° immer links (Rohr drehbar), ganzzahlig
            int wl = (int)teil.WinkelLinksGrad, wr = (int)teil.WinkelRechtsGrad;
            if (wr > wl) { var tmp = wl; wl = wr; wr = tmp; }  // grö[char]0x00DFer zuerst
            var winkelText = wl == wr ? $"{wl}°" : $"{wl}° / {wr}°";
            var winkelFarbe = (wl < 90 || wr < 90) ? Color.FromRgb(0xFF, 0xA7, 0x26) : Color.FromRgb(0x77, 0x77, 0x77);
            var winkel = MacheWertBlock(winkelText, winkelFarbe);
            Grid.SetColumn(winkel, 3);
            grid.Children.Add(winkel);

            // PDF
            var pdfText = string.IsNullOrWhiteSpace(teil.PdfPfad)
                ? "–"
                : Path.GetFileName(teil.PdfPfad);
            var pdf = new TextBlock
            {
                Text              = pdfText,
                FontSize          = 12,
                Foreground        = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming      = TextTrimming.CharacterEllipsis,
                ToolTip           = teil.PdfPfad,
            };
            Grid.SetColumn(pdf, 4);
            grid.Children.Add(pdf);

            row.Child = grid;
            return row;
        }

        private static TextBlock MacheWertBlock(string text, Color farbe) =>
            new TextBlock
            {
                Text              = text,
                FontSize          = 13,
                FontWeight        = FontWeights.SemiBold,
                Foreground        = new SolidColorBrush(farbe),
                VerticalAlignment = VerticalAlignment.Center,
            };

        // ─── Export ──────────────────────────────────────────────────────────

        private void OnExportieren(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title            = "Schnittplan exportieren",
                Filter           = "Textdatei (*.txt)|*.txt",
                FileName         = $"Schnittplan_{DateTime.Now:yyyyMMdd_HHmm}.txt",
                DefaultExt       = ".txt",
            };
            if (dlg.ShowDialog() != true) return;

            File.WriteAllText(dlg.FileName, ErstelleTextExport(), Encoding.UTF8);
            MessageBox.Show($"Exportiert: {dlg.FileName}", "Rohrzuschnitt", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string ErstelleTextExport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine("  ROHRZUSCHNITT – SCHNITTPLAN");
            sb.AppendLine($"  Erstellt: {DateTime.Now:dd.MM.yyyy HH:mm}");
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine($"  Stangen:        {_ergebnis.Stangen.Count}");
            sb.AppendLine($"  Stangenlänge:   {(int)_ergebnis.StandardStangenLaengeMm} mm");
            sb.AppendLine($"  Teile gesamt:   {_ergebnis.TeilAnzahl}");
            sb.AppendLine($"  Gesamtrest:     {(int)_ergebnis.GesamtRestMm} mm");
            sb.AppendLine($"  Verschnitt:     {_ergebnis.VerschnittProzent:N2} %");
            sb.AppendLine();

            foreach (var stange in _ergebnis.Stangen)
            {
                sb.AppendLine($"───────────────────────────────────────────────────────");
                sb.AppendLine($"  STANGE #{stange.Nummer}  |  Verbraucht: {(int)stange.VerbrauchtMm} mm  |  Rest: {(int)stange.RestMm} mm");
                sb.AppendLine($"───────────────────────────────────────────────────────");
                int nr = 1;
                foreach (var teil in stange.Teile)
                {
                    int el = (int)teil.WinkelLinksGrad, er = (int)teil.WinkelRechtsGrad;
                    if (el > er) { var tmp = el; el = er; er = tmp; }
                    var wStr = el == er ? $"{el}°" : $"{el}° / {er}°";
                    var pdf = string.IsNullOrWhiteSpace(teil.PdfPfad) ? "" : $"  PDF: {Path.GetFileName(teil.PdfPfad)}";
                    sb.AppendLine($"  [{nr++,2}]  {teil.Bezeichnung,-30}  {(int)teil.NennLaengeMm,5} mm  |  Winkel: {wStr}{pdf}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private void OnSchliessen(object sender, RoutedEventArgs e) => Close();
    }
}
