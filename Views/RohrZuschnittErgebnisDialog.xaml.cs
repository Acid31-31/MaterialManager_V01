using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class RohrZuschnittErgebnisDialog : Window
    {
        private readonly RohrZuschnittErgebnis _ergebnis;
        private string? _pdfVorschauPfad;
        private static readonly CultureInfo _de = CultureInfo.GetCultureInfo("de-DE");

        public RohrZuschnittErgebnisDialog(RohrZuschnittErgebnis ergebnis, double schnittverlustMm)
        {
            InitializeComponent();
            _ergebnis = ergebnis;
            BefuelleZusammenfassung(schnittverlustMm);
            BaueStangenKarten();
            Dispatcher.BeginInvoke(new Action(OeffnePdfVorschau), DispatcherPriority.Background);
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

            // Winkel exakt wie eingegeben – kein Drehen, immer XX° / XX°
            var winkelText = $"{(int)teil.WinkelLinksGrad}° / {(int)teil.WinkelRechtsGrad}°";
            var winkelFarbe = (teil.WinkelLinksGrad < 90 || teil.WinkelRechtsGrad < 90) ? Color.FromRgb(0xFF, 0xA7, 0x26) : Color.FromRgb(0x77, 0x77, 0x77);
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
            OeffnePdfVorschau();
        }

        private void OeffnePdfVorschau()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_pdfVorschauPfad) || !File.Exists(_pdfVorschauPfad))
                {
                    _pdfVorschauPfad = Path.Combine(
                        Path.GetTempPath(),
                        $"Rohrzuschnitt_Schnittplan_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                    SchreibePdfVorschau(_pdfVorschauPfad);
                }

                Process.Start(new ProcessStartInfo(_pdfVorschauPfad) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Die PDF-Vorschau konnte nicht geöffnet werden:\n{ex.Message}",
                    "Rohrzuschnitt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void SchreibePdfVorschau(string pfad)
        {
            var zeilen = new List<string>
            {
                "ROHRZUSCHNITT - SCHNITTPLAN",
                $"Erstellt: {DateTime.Now:dd.MM.yyyy HH:mm}",
                $"Stangen: {_ergebnis.Stangen.Count} | Stangenlaenge: {(int)_ergebnis.StandardStangenLaengeMm} mm | Teile: {_ergebnis.TeilAnzahl} | Gesamtrest: {(int)_ergebnis.GesamtRestMm} mm",
                string.Empty
            };

            foreach (var stange in _ergebnis.Stangen)
            {
                var nutzung = _ergebnis.StandardStangenLaengeMm > 0
                    ? stange.VerbrauchtMm / _ergebnis.StandardStangenLaengeMm * 100
                    : 0;

                zeilen.Add($"STANGE #{stange.Nummer} | Verbraucht: {(int)stange.VerbrauchtMm} mm | Rest: {(int)stange.RestMm} mm | Auslastung: {nutzung:N1} %");
                zeilen.Add("Nr.  Bezeichnung                       Laenge     Winkel L/R     PDF");
                zeilen.Add("--------------------------------------------------------------------------");

                int nr = 1;
                foreach (var teil in stange.Teile)
                {
                    var bezeichnung = KuerzePdfText(teil.Bezeichnung, 32).PadRight(32);
                    var pdfName = string.IsNullOrWhiteSpace(teil.PdfPfad) ? "-" : Path.GetFileName(teil.PdfPfad);
                    var winkel = $"{(int)teil.WinkelLinksGrad}° / {(int)teil.WinkelRechtsGrad}°";
                    zeilen.Add($"{nr,2}.  {bezeichnung}  {(int)teil.NennLaengeMm,5} mm   {winkel,-13} {KuerzePdfText(pdfName, 22)}");
                    nr++;
                }

                zeilen.Add(string.Empty);
            }

            var contentStream = BauePdfContentStream(zeilen);
            var contentBytes = Encoding.GetEncoding(1252).GetBytes(contentStream);
            using var fs = new FileStream(pfad, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(fs, Encoding.ASCII, leaveOpen: true);

            var offsets = new List<long>();
            void Obj(int nr, string text)
            {
                offsets.Add(fs.Position);
                writer.Write($"{nr} 0 obj\n{text}\nendobj\n");
                writer.Flush();
            }

            writer.Write("%PDF-1.4\n% Rohrzuschnitt Vorschau\n");
            writer.Flush();
            Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
            Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
            Obj(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>");
            Obj(4, "<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>");

            offsets.Add(fs.Position);
            writer.Write($"5 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n");
            writer.Flush();
            fs.Write(contentBytes, 0, contentBytes.Length);
            writer.Write("\nendstream\nendobj\n");
            writer.Flush();

            var xrefStart = fs.Position;
            writer.Write($"xref\n0 6\n0000000000 65535 f \n");
            foreach (var offset in offsets)
                writer.Write($"{offset:0000000000} 00000 n \n");
            writer.Write($"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{xrefStart}\n%%EOF");
            writer.Flush();
        }

        private static string BauePdfContentStream(List<string> zeilen)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BT");
            sb.AppendLine("/F1 9 Tf");
            sb.AppendLine("36 806 Td");
            var erste = true;
            foreach (var zeile in zeilen.Take(62))
            {
                if (!erste)
                    sb.AppendLine("0 -12 Td");
                sb.AppendLine($"({EscapePdfText(zeile)}) Tj");
                erste = false;
            }
            sb.AppendLine("ET");
            return sb.ToString();
        }

        private static string EscapePdfText(string text) =>
            (text ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)");

        private static string KuerzePdfText(string text, int maxLaenge)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return text.Length <= maxLaenge ? text : text.Substring(0, Math.Max(0, maxLaenge - 1)) + "…";
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
                    var wStr = $"{(int)teil.WinkelLinksGrad}° / {(int)teil.WinkelRechtsGrad}°";
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
