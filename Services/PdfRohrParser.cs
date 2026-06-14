using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace MaterialManager_V01.Services
{
    /// <summary>
    /// Ergebnis des PDF-Auslesens für eine Rohr-Zeichnung.
    /// </summary>
    public sealed class PdfRohrParseErgebnis
    {
        public bool Erfolgreich { get; set; }
        public string Fehlermeldung { get; set; } = string.Empty;
        public double? LaengeMm { get; set; }
        public int? Menge { get; set; }
        public string Bezeichnung { get; set; } = string.Empty;
        public string RohText { get; set; } = string.Empty;
    }

    /// <summary>
    /// Liest Länge (mm) und Menge aus einer PDF-Fertigungszeichnung für Rohre.
    /// Unterstützt deutsche Fertigungsbeschriftungen.
    /// </summary>
    public static class PdfRohrParser
    {
        private static readonly CultureInfo _de = CultureInfo.GetCultureInfo("de-DE");
        private static readonly CultureInfo _inv = CultureInfo.InvariantCulture;

        // Längen-Muster (mm) – z.B. "L = 1250", "Länge: 1250", "1250 mm", "L=1250mm"
        private static readonly Regex[] _laengenMuster = new[]
        {
            new Regex(@"L\s*[=:]\s*(?<v>[\d][0-9\s.,]*)\s*mm",           RegexOptions.IgnoreCase),
            new Regex(@"(?:Länge|Laenge|Length)\s*[=:]\s*(?<v>[\d][0-9\s.,]*)\s*mm", RegexOptions.IgnoreCase),
            new Regex(@"(?<v>[\d]{3,5})\s*mm",                            RegexOptions.None),
            new Regex(@"(?:Zuschnitt|Schnittlänge|Abschnitt)\s*[=:]?\s*(?<v>[\d][0-9\s.,]*)\s*mm", RegexOptions.IgnoreCase),
        };

        // Mengen-Muster – z.B. "Menge: 5", "Stück: 3", "Stk. 4", "Anzahl 2", "x 6"
        private static readonly Regex[] _mengenMuster = new[]
        {
            new Regex(@"(?:Menge|Stück|Stueck|Stk\.?|Anzahl|Qty|Quantity)\s*[=:.]?\s*(?<v>\d+)", RegexOptions.IgnoreCase),
            new Regex(@"\bx\s*(?<v>\d+)\b",                               RegexOptions.IgnoreCase),
            new Regex(@"(?<v>\d+)\s*[xX×]\s*\d{3,}",                     RegexOptions.None),
            new Regex(@"(?:Pos\.|Position)\s*\d+.*?(?<v>\d+)\s*(?:St[kü]|Stück|x)", RegexOptions.IgnoreCase),
        };

        public static PdfRohrParseErgebnis LesePdf(string pdfPfad)
        {
            var ergebnis = new PdfRohrParseErgebnis();

            if (string.IsNullOrWhiteSpace(pdfPfad) || !File.Exists(pdfPfad))
            {
                ergebnis.Fehlermeldung = "PDF-Datei nicht gefunden.";
                return ergebnis;
            }

            try
            {
                var sb = new StringBuilder();
                using var doc = PdfDocument.Open(pdfPfad);
                foreach (var page in doc.GetPages())
                {
                    foreach (var word in page.GetWords())
                        sb.Append(word.Text).Append(' ');
                    sb.AppendLine();
                }

                var text = sb.ToString();
                ergebnis.RohText = text;

                // Bezeichnung aus Dateiname
                ergebnis.Bezeichnung = Path.GetFileNameWithoutExtension(pdfPfad);

                // Länge auslesen
                ergebnis.LaengeMm = ExtrahiereLaenge(text);

                // Menge auslesen
                ergebnis.Menge = ExtrahiereMenge(text);

                ergebnis.Erfolgreich = ergebnis.LaengeMm.HasValue || ergebnis.Menge.HasValue;
                if (!ergebnis.Erfolgreich)
                    ergebnis.Fehlermeldung = "Keine Länge oder Menge im PDF-Text gefunden.";
            }
            catch (Exception ex)
            {
                ergebnis.Fehlermeldung = $"PDF konnte nicht gelesen werden: {ex.Message}";
            }

            return ergebnis;
        }

        private static double? ExtrahiereLaenge(string text)
        {
            var kandidaten = new List<double>();

            foreach (var muster in _laengenMuster)
            {
                foreach (Match m in muster.Matches(text))
                {
                    var raw = m.Groups["v"].Value.Trim().Replace(" ", "");
                    if (TryParseDouble(raw, out var val) && val >= 10 && val <= 99999)
                        kandidaten.Add(val);
                }
            }

            if (kandidaten.Count == 0)
                return null;

            // Häufigsten Wert bevorzugen; bei Gleichstand den kleinsten (realistischste Länge)
            return kandidaten
                .GroupBy(v => v)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key)
                .First().Key;
        }

        private static int? ExtrahiereMenge(string text)
        {
            foreach (var muster in _mengenMuster)
            {
                var m = muster.Match(text);
                if (m.Success && int.TryParse(m.Groups["v"].Value.Trim(), out var val) && val >= 1 && val <= 9999)
                    return val;
            }
            return null;
        }

        private static bool TryParseDouble(string raw, out double value)
        {
            var s = raw.Replace(" ", "");
            if (double.TryParse(s, NumberStyles.Any, _de, out value))
                return true;
            s = s.Replace(',', '.');
            return double.TryParse(s, NumberStyles.Any, _inv, out value);
        }
    }
}
