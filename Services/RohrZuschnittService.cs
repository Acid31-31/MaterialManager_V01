using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MaterialManager_V01.Models;

namespace MaterialManager_V01.Services
{
    public enum RohrSchnittWinkel
    {
        Grad90,
        Grad45
    }

    public sealed class RohrZuschnittEinstellung
    {
        public double StandardStangenLaengeMm { get; set; } = 6000;
        public double SaegeSchnittverlustMm { get; set; } = 3;
        public double GehrungsZuschlagPro45GradEndeMm { get; set; } = 2;
    }

    public sealed class RohrZuschnittEingabePosition
    {
        public string Bezeichnung { get; set; } = string.Empty;
        public double LaengeMm { get; set; }
        public int Menge { get; set; } = 1;
        // Zwei freie Schnittwinkel in Grad (1-90). 90 = gerader Schnitt.
        public double WinkelLinksGrad { get; set; } = 90;
        public double WinkelRechtsGrad { get; set; } = 90;
        // Rückwärtskompatibilität mit BerechneFuerAuftrag
        public RohrSchnittWinkel Winkel
        {
            get => (WinkelLinksGrad < 90 || WinkelRechtsGrad < 90) ? RohrSchnittWinkel.Grad45 : RohrSchnittWinkel.Grad90;
            set
            {
                if (value == RohrSchnittWinkel.Grad45) { WinkelLinksGrad = 45; WinkelRechtsGrad = 45; }
                else { WinkelLinksGrad = 90; WinkelRechtsGrad = 90; }
            }
        }
        public string PdfPfad { get; set; } = string.Empty;

        public string PdfAnzeige => string.IsNullOrWhiteSpace(PdfPfad) ? string.Empty : System.IO.Path.GetFileName(PdfPfad);
        public string WinkelText => $"{WinkelLinksGrad:0.#}° / {WinkelRechtsGrad:0.#}°";
    }

    public sealed class RohrZuschnittTeil
    {
        public string Bezeichnung { get; set; } = string.Empty;
        public double NennLaengeMm { get; set; }
        public double EffektiveLaengeMm { get; set; }
        public double WinkelLinksGrad { get; set; } = 90;
        public double WinkelRechtsGrad { get; set; } = 90;
        public RohrSchnittWinkel Winkel { get; set; }
        public string PdfPfad { get; set; } = string.Empty;

        public string WinkelAnzeige => $"{WinkelLinksGrad:0.#}° / {WinkelRechtsGrad:0.#}°";

        public string Anzeige
        {
            get
            {
                var pdfText = string.IsNullOrWhiteSpace(PdfPfad) ? string.Empty : $" | PDF: {System.IO.Path.GetFileName(PdfPfad)}";
                return $"{Bezeichnung} | {(int)NennLaengeMm} mm | {WinkelAnzeige}{pdfText}";
            }
        }
    }

    public sealed class RohrZuschnittStange
    {
        public int Nummer { get; set; }
        public List<RohrZuschnittTeil> Teile { get; set; } = new();
        public double VerbrauchtMm { get; set; }
        public double RestMm { get; set; }
    }

    public sealed class RohrZuschnittErgebnis
    {
        public RohrSchnittWinkel Winkel { get; set; }
        public double StandardStangenLaengeMm { get; set; }
        public List<RohrZuschnittStange> Stangen { get; set; } = new();
        public int TeilAnzahl { get; set; }
        public double GesamtNennLaengeMm { get; set; }
        public double GesamtVerbrauchMm { get; set; }
        public double GesamtRestMm { get; set; }

        public double VerschnittProzent => GesamtVerbrauchMm <= 0 ? 0 : Math.Round((GesamtRestMm / GesamtVerbrauchMm) * 100.0, 2);
    }

    public static class RohrZuschnittService
    {
        public static RohrZuschnittErgebnis BerechneFuerAuftrag(
            IEnumerable<MaterialItem> materialien,
            string auftragNr,
            RohrSchnittWinkel winkel,
            RohrZuschnittEinstellung? einstellung = null)
        {
            var cfg = einstellung ?? new RohrZuschnittEinstellung();
            var stock = cfg.StandardStangenLaengeMm <= 0 ? 6000 : cfg.StandardStangenLaengeMm;
            var kerf = Math.Max(0, cfg.SaegeSchnittverlustMm);
            var gehrung = Math.Max(0, cfg.GehrungsZuschlagPro45GradEndeMm);

            var positionen = (materialien ?? Enumerable.Empty<MaterialItem>())
                .Where(m => m.Kategorie == MaterialKategorie.Rohr)
                .Where(m => string.Equals((m.AuftragNr ?? string.Empty).Trim(), (auftragNr ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
                .Where(m => m.Laenge > 0)
                .Where(m => m.Stueckzahl > 0)
                .Select(m => new RohrZuschnittEingabePosition
                {
                    Bezeichnung = BuildPositionsName(m),
                    LaengeMm = m.Laenge,
                    Menge = m.Stueckzahl,
                    Winkel = winkel,
                    PdfPfad = !string.IsNullOrWhiteSpace(m.PdfPfad) ? m.PdfPfad : (string.IsNullOrWhiteSpace(m.PdfPfadAngefangeneTafel) ? string.Empty : m.PdfPfadAngefangeneTafel)
                });

            return BerechneFuerPositionen(positionen, cfg);
        }

        public static RohrZuschnittErgebnis BerechneFuerPositionen(
            IEnumerable<RohrZuschnittEingabePosition> positionen,
            RohrZuschnittEinstellung? einstellung = null)
        {
            var cfg     = einstellung ?? new RohrZuschnittEinstellung();
            var stock   = cfg.StandardStangenLaengeMm <= 0 ? 6000 : cfg.StandardStangenLaengeMm;
            var kerf    = Math.Max(0, cfg.SaegeSchnittverlustMm);
            var gehrung = Math.Max(0, cfg.GehrungsZuschlagPro45GradEndeMm);

            var alleTeile = BuildTeile(positionen, kerf, gehrung);
            if (alleTeile.Count == 0)
                return new RohrZuschnittErgebnis { StandardStangenLaengeMm = stock };

            // ── Phase 1: Optimales Packing – Best-Fit Decreasing ─────────────
            // Winkel spielen beim Packing KEINE Rolle → maximale Materialausnutzung.
            // Größte Teile zuerst, jedes Teil in die Stange mit kleinstem verbleibenden Rest.
            var stangen = new List<RohrZuschnittStange>();

            foreach (var teil in alleTeile.OrderByDescending(t => t.EffektiveLaengeMm))
            {
                RohrZuschnittStange? beste = null;
                double bestDelta = double.MaxValue;

                foreach (var s in stangen)
                {
                    var frei = stock - s.VerbrauchtMm;
                    if (frei + 0.001 < teil.EffektiveLaengeMm) continue;
                    var delta = frei - teil.EffektiveLaengeMm;
                    if (delta < bestDelta) { bestDelta = delta; beste = s; }
                }

                if (beste == null)
                {
                    beste = new RohrZuschnittStange { Nummer = stangen.Count + 1, VerbrauchtMm = 0, RestMm = stock };
                    stangen.Add(beste);
                }

                beste.Teile.Add(teil);
                beste.VerbrauchtMm += teil.EffektiveLaengeMm;
                beste.RestMm = Math.Max(0, stock - beste.VerbrauchtMm);
            }

            // ── Phase 2: Schneidreihenfolge pro Stange optimieren ────────────
            // Winkelgruppen-Blöcke (minimale Säge-Umrüstungen), darin Länge absteigend.
            // Symmetrisch: 90°/45° == 45°/90° (Rohr umdrehen).
            static string WinkelKey(RohrZuschnittTeil t)
            {
                double Rnd(double g) => Math.Round(g * 2) / 2.0;
                var a = Rnd(t.WinkelLinksGrad);
                var b = Rnd(t.WinkelRechtsGrad);
                return a <= b ? $"{a}_{b}" : $"{b}_{a}";
            }

            foreach (var stange in stangen)
            {
                stange.Teile = stange.Teile
                    .GroupBy(WinkelKey)
                    .OrderByDescending(g => g.Key == "90_90" ? int.MaxValue : g.Count())
                    .ThenBy(g => g.Key)
                    .SelectMany(g => g.OrderByDescending(t => t.NennLaengeMm))
                    .ToList();
            }

            return new RohrZuschnittErgebnis
            {
                Winkel                  = positionen?.FirstOrDefault()?.Winkel ?? RohrSchnittWinkel.Grad90,
                StandardStangenLaengeMm = stock,
                TeilAnzahl              = alleTeile.Count,
                GesamtNennLaengeMm      = alleTeile.Sum(t => t.NennLaengeMm),
                Stangen                 = stangen,
                GesamtVerbrauchMm       = stangen.Count * stock,
                GesamtRestMm            = stangen.Sum(s => s.RestMm),
            };
        }

        private static RohrZuschnittErgebnis BerechneAusTeilen(List<RohrZuschnittTeil> teile, double stock, RohrSchnittWinkel winkel)
        {
            var positionen = teile.Select(t => new RohrZuschnittEingabePosition
            {
                Bezeichnung      = t.Bezeichnung,
                LaengeMm         = t.NennLaengeMm,
                Menge            = 1,
                WinkelLinksGrad  = t.WinkelLinksGrad,
                WinkelRechtsGrad = t.WinkelRechtsGrad,
                PdfPfad          = t.PdfPfad,
            });
            return BerechneFuerPositionen(positionen, new RohrZuschnittEinstellung
            {
                StandardStangenLaengeMm         = stock,
                SaegeSchnittverlustMm           = 0,
                GehrungsZuschlagPro45GradEndeMm = 0,
            });
        }

        private static List<RohrZuschnittTeil> BuildTeile(
            IEnumerable<RohrZuschnittEingabePosition> positionen,
            double saegeSchnittverlust,
            double gehrungsZuschlagPro45GradEnde)
        {
            var teile = new List<RohrZuschnittTeil>();

            foreach (var pos in positionen ?? Enumerable.Empty<RohrZuschnittEingabePosition>())
            {
                if (pos == null || pos.LaengeMm <= 0 || pos.Menge <= 0)
                    continue;

                // Gehrungszuschlag proportional: bei 45° voller Zuschlag, bei 90° kein Zuschlag
                double ZuschlagFuerSeite(double grad)
                {
                    var g = Math.Max(1, Math.Min(90, grad));
                    return g >= 90 ? 0.0 : (90.0 - g) / 45.0 * gehrungsZuschlagPro45GradEnde;
                }

                var zuschlagLinks  = ZuschlagFuerSeite(pos.WinkelLinksGrad);
                var zuschlagRechts = ZuschlagFuerSeite(pos.WinkelRechtsGrad);
                var effektiveLaenge = pos.LaengeMm + saegeSchnittverlust + zuschlagLinks + zuschlagRechts;

                for (var i = 0; i < pos.Menge; i++)
                {
                    teile.Add(new RohrZuschnittTeil
                    {
                        Bezeichnung       = string.IsNullOrWhiteSpace(pos.Bezeichnung) ? "Rohr" : pos.Bezeichnung.Trim(),
                        NennLaengeMm      = Math.Round(pos.LaengeMm, 2),
                        EffektiveLaengeMm = Math.Round(effektiveLaenge, 2),
                        WinkelLinksGrad   = pos.WinkelLinksGrad,
                        WinkelRechtsGrad  = pos.WinkelRechtsGrad,
                        Winkel            = pos.Winkel,
                        PdfPfad           = pos.PdfPfad ?? string.Empty
                    });
                }
            }

            return teile;
        }

        private static string BuildPositionsName(MaterialItem pos)
        {
            var art = string.IsNullOrWhiteSpace(pos.MaterialArt) ? "Rohr" : pos.MaterialArt.Trim();
            var form = string.IsNullOrWhiteSpace(pos.Form) ? string.Empty : $" {pos.Form.Trim()}";
            var dm = pos.Durchmesser > 0 ? $" Ø{pos.Durchmesser:0.##}" : string.Empty;
            return $"{art}{form}{dm}".Trim();
        }
    }
}
