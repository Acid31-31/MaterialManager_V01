using MaterialManager_V01.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MaterialManager_V01.Services
{
    public static class VerschnittService
    {
        /// <summary>
        /// Findet das beste Material für gegebene Maße
        /// Prüft Reste zuerst (Reste-Verwertung), dann Tafeln
        /// </summary>
        public static List<VerschnittVorschlag> FindeBestessMaterial(
            IEnumerable<MaterialItem> materialien,
            string materialArt,
            string legierung,
            double staerke,
            int benoetigtLaenge,
            int benoetigtBreite,
            int anzahl = 1)
        {
            var vorschlaege = new List<VerschnittVorschlag>();

            var passend = materialien
                .Where(m => string.Equals(m.MaterialArt, materialArt, StringComparison.OrdinalIgnoreCase))
                .Where(m => string.IsNullOrEmpty(legierung) || string.Equals(m.Legierung, legierung, StringComparison.OrdinalIgnoreCase))
                .Where(m => Math.Abs(m.Staerke - staerke) < 0.01)
                .Where(m => !string.IsNullOrWhiteSpace(m.Mass))
                .ToList();

            foreach (var mat in passend)
            {
                var parts = mat.Mass.ToLower().Replace('×', 'x').Split('x');
                if (parts.Length != 2) continue;
                if (!int.TryParse(parts[0].Trim(), out var matL) || !int.TryParse(parts[1].Trim(), out var matB))
                    continue;

                // Prüfe beide Orientierungen
                var ergebnis = BerechneBesteFuellung(matL, matB, benoetigtLaenge, benoetigtBreite, anzahl);

                if (ergebnis.AnzahlTeile > 0)
                {
                    var restFlaeche = (matL * matB) - (ergebnis.AnzahlTeile * benoetigtLaenge * benoetigtBreite);
                    var verschnittProzent = (double)restFlaeche / (matL * matB) * 100;

                    vorschlaege.Add(new VerschnittVorschlag
                    {
                        Material = mat,
                        AnzahlTeile = ergebnis.AnzahlTeile,
                        VerschnittProzent = verschnittProzent,
                        RestFlaeche = restFlaeche,
                        IstRest = mat.Form == "Rest",
                        Prioritaet = mat.Form == "Rest" ? 1 : 2 // Reste bevorzugen
                    });
                }
            }

            return vorschlaege
                .OrderBy(v => v.Prioritaet)
                .ThenBy(v => v.VerschnittProzent)
                .Take(10)
                .ToList();
        }

        private static (int AnzahlTeile, int Reihen, int Spalten) BerechneBesteFuellung(
            int matL, int matB, int teilL, int teilB, int maxAnzahl)
        {
            // Orientierung 1: Teil normal
            int r1 = matL / teilL;
            int s1 = matB / teilB;
            int a1 = r1 * s1;

            // Orientierung 2: Teil gedreht
            int r2 = matL / teilB;
            int s2 = matB / teilL;
            int a2 = r2 * s2;

            var best = Math.Max(a1, a2);
            if (maxAnzahl > 0)
                best = Math.Min(best, maxAnzahl);

            return best == a1
                ? (Math.Min(a1, maxAnzahl > 0 ? maxAnzahl : a1), r1, s1)
                : (Math.Min(a2, maxAnzahl > 0 ? maxAnzahl : a2), r2, s2);
        }
    }

    public class VerschnittVorschlag
    {
        public MaterialItem Material { get; set; } = new();
        public int AnzahlTeile { get; set; }
        public double VerschnittProzent { get; set; }
        public int RestFlaeche { get; set; }  // mm²
        public bool IstRest { get; set; }
        public int Prioritaet { get; set; }

        public string Beschreibung => IstRest
            ? $"🟢 REST {Material.Restnummer}: {Material.Mass} → {AnzahlTeile} Teile | Verschnitt: {VerschnittProzent:N0}%"
            : $"🔵 TAFEL {Material.Form}: {Material.Mass} → {AnzahlTeile} Teile | Verschnitt: {VerschnittProzent:N0}%";
    }
}
