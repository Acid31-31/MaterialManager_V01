using System;
using MaterialManager_V01.Models;

namespace MaterialManager_V01.Services
{
    public static class RestMaterialSearchService
    {
        private const double MaxTolerancePercent = 30.0;

        public static bool Matches(MaterialItem materialItem, string materialArt, string legierung, double? staerke, int? laenge, int? breite, double toleranzProzent, string form, bool requireRest)
        {
            if (materialItem == null)
                return false;

            var normalizedTolerancePercent = NormalizeTolerancePercent(toleranzProzent);

            if (requireRest && !string.Equals(materialItem.Form, "Rest", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.IsNullOrEmpty(materialArt) && materialItem.MaterialArt != materialArt)
                return false;

            if (!string.IsNullOrEmpty(legierung) && materialItem.Legierung != legierung)
                return false;

            if (staerke.HasValue)
            {
                var toleranz = normalizedTolerancePercent / 100.0;
                var minStaerke = staerke.Value * (1 - toleranz);
                var maxStaerke = staerke.Value * (1 + toleranz);
                if (materialItem.Staerke < minStaerke || materialItem.Staerke > maxStaerke)
                    return false;
            }

            if (laenge.HasValue && breite.HasValue)
            {
                if (!TryParseMass(materialItem.Mass, out var materialLaenge, out var materialBreite))
                    return false;

                if (!MatchesRequestedDimensions(materialLaenge, materialBreite, laenge.Value, breite.Value, normalizedTolerancePercent))
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
            var maxLaenge = requestedLaenge * (1 + toleranz);
            var minBreite = requestedBreite * (1 - toleranz);
            var maxBreite = requestedBreite * (1 + toleranz);

            return (materialLaenge >= minLaenge && materialLaenge <= maxLaenge && materialBreite >= minBreite && materialBreite <= maxBreite)
                || (materialLaenge >= minBreite && materialLaenge <= maxBreite && materialBreite >= minLaenge && materialBreite <= maxLaenge);
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

        private static double NormalizeTolerancePercent(double toleranzProzent)
        {
            if (double.IsNaN(toleranzProzent) || double.IsInfinity(toleranzProzent))
                return 10.0;

            if (toleranzProzent < 0)
                return 0;

            return Math.Min(toleranzProzent, MaxTolerancePercent);
        }
    }
}