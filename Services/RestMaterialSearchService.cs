using System;
using System.Collections.Generic;
using System.Linq;
using MaterialManager_V01.Models;

namespace MaterialManager_V01.Services
{
    public static class RestMaterialSearchService
    {
        private const double MaxTolerancePercent = 30.0;

        public static bool Matches(MaterialItem materialItem, string materialArt, string legierung, double? staerke, int? laenge, int? breite, double toleranzProzent, string form, bool requireRest)
        {
            return SearchBestMatches(new[] { materialItem }, materialArt, legierung, staerke, laenge, breite, toleranzProzent, form, requireRest).Any();
        }

        public static List<MaterialItem> SearchBestMatches(IEnumerable<MaterialItem> materialien, string materialArt, string legierung, double? staerke, int? laenge, int? breite, double toleranzProzent, string form, bool requireRest)
        {
            if (materialien == null)
                return new List<MaterialItem>();

            var kandidaten = materialien
                .Where(m => MatchesBaseCriteria(m, materialArt, legierung, staerke))
                .ToList();

            if (!laenge.HasValue || !breite.HasValue)
                return ApplyTrefferArt(OrderWithoutDimensionFallback(kandidaten, form, requireRest), requireRest ? "Restmaterial" : "Material");

            if (string.Equals(form, "GF", StringComparison.OrdinalIgnoreCase)
                || string.Equals(form, "MF", StringComparison.OrdinalIgnoreCase)
                || string.Equals(form, "KF", StringComparison.OrdinalIgnoreCase))
            {
                var formKandidaten = kandidaten.Where(m => string.Equals(m.Form, form, StringComparison.OrdinalIgnoreCase));
                var exakteFormTreffer = GetExactDimensionMatches(formKandidaten, laenge.Value, breite.Value);
                if (exakteFormTreffer.Any())
                    return ApplyTrefferArt(exakteFormTreffer, "Exakte Tafel");

                return ApplyTrefferArt(GetLargerDimensionMatches(formKandidaten, laenge.Value, breite.Value), "Größere Tafel");
            }

            var restKandidaten = kandidaten.Where(IsRest).ToList();
            var exakteReste = GetExactDimensionMatches(restKandidaten, laenge.Value, breite.Value);
            if (exakteReste.Any())
                return ApplyTrefferArt(exakteReste, "Exakter Rest");

            var groessereReste = GetLargerDimensionMatches(restKandidaten, laenge.Value, breite.Value);
            if (groessereReste.Any())
                return ApplyTrefferArt(groessereReste, "Größerer Rest");

            var tafelKandidaten = kandidaten.Where(IsTafel).ToList();
            var exakteTafeln = GetExactDimensionMatches(tafelKandidaten, laenge.Value, breite.Value);
            if (exakteTafeln.Any())
                return ApplyTrefferArt(exakteTafeln, "Exakte Tafel");

            return ApplyTrefferArt(GetLargerDimensionMatches(tafelKandidaten, laenge.Value, breite.Value), "Größere Tafel");
        }

        private static List<MaterialItem> ApplyTrefferArt(List<MaterialItem> materialien, string trefferArt)
        {
            foreach (var material in materialien)
                material.SuchTrefferArt = trefferArt;

            return materialien;
        }

        private static bool MatchesBaseCriteria(MaterialItem materialItem, string materialArt, string legierung, double? staerke)
        {
            if (materialItem == null)
                return false;

            if (!string.IsNullOrEmpty(materialArt) && materialItem.MaterialArt != materialArt)
                return false;

            if (!string.IsNullOrEmpty(legierung) && materialItem.Legierung != legierung)
                return false;

            if (staerke.HasValue && Math.Abs(materialItem.Staerke - staerke.Value) > 0.0001)
                return false;

            return true;
        }

        private static List<MaterialItem> OrderWithoutDimensionFallback(IEnumerable<MaterialItem> kandidaten, string form, bool requireRest)
        {
            IEnumerable<MaterialItem> gefiltert = kandidaten;

            if (string.Equals(form, "GF", StringComparison.OrdinalIgnoreCase)
                || string.Equals(form, "MF", StringComparison.OrdinalIgnoreCase)
                || string.Equals(form, "KF", StringComparison.OrdinalIgnoreCase)
                || string.Equals(form, "Rest", StringComparison.OrdinalIgnoreCase))
            {
                gefiltert = gefiltert.Where(m => string.Equals(m.Form, form, StringComparison.OrdinalIgnoreCase));
            }

            if (requireRest)
            {
                var reste = gefiltert.Where(IsRest).ToList();
                if (reste.Any())
                    return OrderByDimension(reste);
            }

            return OrderByDimension(gefiltert);
        }

        private static List<MaterialItem> GetExactDimensionMatches(IEnumerable<MaterialItem> kandidaten, int requestedLaenge, int requestedBreite)
        {
            var suchMass = NormalizeDimensions(requestedLaenge, requestedBreite);

            return kandidaten
                .Where(m => TryParseMass(m.Mass, out var laenge, out var breite)
                    && NormalizeDimensions(laenge, breite) == suchMass)
                .OrderBy(m => GetFormPriority(m.Form))
                .ThenBy(m => m.MaterialArt)
                .ThenBy(m => m.Legierung)
                .ThenBy(m => m.Staerke)
                .ToList();
        }

        private static List<MaterialItem> GetLargerDimensionMatches(IEnumerable<MaterialItem> kandidaten, int requestedLaenge, int requestedBreite)
        {
            var suchMass = NormalizeDimensions(requestedLaenge, requestedBreite);

            return kandidaten
                .Select(m => new
                {
                    Material = m,
                    Mass = TryParseMass(m.Mass, out var laenge, out var breite)
                        ? NormalizeDimensions(laenge, breite)
                        : (Laenge: 0, Breite: 0)
                })
                .Where(x => x.Mass.Laenge > 0
                    && x.Mass.Laenge >= suchMass.Laenge
                    && x.Mass.Breite >= suchMass.Breite
                    && x.Mass != suchMass)
                .OrderBy(x => x.Mass.Laenge * x.Mass.Breite)
                .ThenBy(x => x.Mass.Laenge - suchMass.Laenge)
                .ThenBy(x => x.Mass.Breite - suchMass.Breite)
                .ThenBy(x => GetFormPriority(x.Material.Form))
                .ThenBy(x => x.Material.MaterialArt)
                .ThenBy(x => x.Material.Legierung)
                .ThenBy(x => x.Material.Staerke)
                .Select(x => x.Material)
                .ToList();
        }

        private static List<MaterialItem> OrderByDimension(IEnumerable<MaterialItem> kandidaten)
        {
            return kandidaten
                .Select(m => new
                {
                    Material = m,
                    Mass = TryParseMass(m.Mass, out var laenge, out var breite)
                        ? NormalizeDimensions(laenge, breite)
                        : (Laenge: int.MaxValue, Breite: int.MaxValue)
                })
                .OrderBy(x => GetFormPriority(x.Material.Form))
                .ThenBy(x => x.Mass.Laenge * x.Mass.Breite)
                .ThenBy(x => x.Mass.Laenge)
                .ThenBy(x => x.Mass.Breite)
                .ThenBy(x => x.Material.MaterialArt)
                .ThenBy(x => x.Material.Legierung)
                .ThenBy(x => x.Material.Staerke)
                .Select(x => x.Material)
                .ToList();
        }

        private static (int Laenge, int Breite) NormalizeDimensions(int laenge, int breite)
        {
            return laenge >= breite ? (laenge, breite) : (breite, laenge);
        }

        private static bool IsRest(MaterialItem materialItem)
        {
            return string.Equals(materialItem.Form, "Rest", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTafel(MaterialItem materialItem)
        {
            return string.Equals(materialItem.Form, "GF", StringComparison.OrdinalIgnoreCase)
                || string.Equals(materialItem.Form, "MF", StringComparison.OrdinalIgnoreCase)
                || string.Equals(materialItem.Form, "KF", StringComparison.OrdinalIgnoreCase);
        }

        private static int GetFormPriority(string? form)
        {
            if (string.Equals(form, "Rest", StringComparison.OrdinalIgnoreCase))
                return 0;

            return 1;
        }

        private static bool TryParseMass(string? mass, out int laenge, out int breite)
        {
            laenge = 0;
            breite = 0;

            var parts = mass?.Split('x', '×');
            if (parts?.Length != 2)
                return false;

            return int.TryParse(parts[0].Trim(), out laenge)
                && int.TryParse(parts[1].Trim(), out breite);
        }
    }
}