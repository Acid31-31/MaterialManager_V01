using System;
using MaterialManager_V01.Models;

namespace MaterialManager_V01.Services
{
    public static class RestMaterialSearchService
    {
        public static bool Matches(MaterialItem materialItem, string materialArt, string legierung, double? staerke, int? laenge, int? breite, double toleranzProzent, string form, bool requireRest)
        {
            if (materialItem == null)
                return false;

            if (requireRest && !string.Equals(materialItem.Form, "Rest", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.IsNullOrEmpty(materialArt) && materialItem.MaterialArt != materialArt)
                return false;

            if (!string.IsNullOrEmpty(legierung) && materialItem.Legierung != legierung)
                return false;

            if (staerke.HasValue)
            {
                var toleranz = toleranzProzent / 100.0;
                var minStaerke = staerke.Value * (1 - toleranz);
                if (materialItem.Staerke < minStaerke)
                    return false;
            }

            if (laenge.HasValue && breite.HasValue)
            {
                if (!TryParseMass(materialItem.Mass, out var materialLaenge, out var materialBreite))
                    return false;

                if (!MatchesRequestedDimensions(materialLaenge, materialBreite, laenge.Value, breite.Value, toleranzProzent))
                    return false;
            }

            if (form != "Alle" && materialItem.Form != form)
                return false;

            return true;
        }

        private static bool MatchesRequestedDimensions(int materialLaenge, int materialBreite, int requestedLaenge, int requestedBreite, double toleranzProzent)
        {
            var toleranz = toleranzProzent / 100.0;
            var minLaenge = requestedLaenge * (1 - toleranz);
            var minBreite = requestedBreite * (1 - toleranz);

            return (materialLaenge >= minLaenge && materialBreite >= minBreite)
                || (materialLaenge >= minBreite && materialBreite >= minLaenge);
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