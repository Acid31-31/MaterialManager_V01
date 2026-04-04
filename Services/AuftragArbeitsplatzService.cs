using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MaterialManager_V01.Services
{
    public static class AuftragArbeitsplatzService
    {
        public const string Laser = "Laser";
        public const string Kantbank = "Kantbank";
        public const string Beides = "Beides";

        private static readonly string StorePath = Path.Combine(PathService.DataDirectory, "auftrag-arbeitsplatz.json");

        public static string GetArbeitsplatz(string? auftragsnummer)
        {
            if (string.IsNullOrWhiteSpace(auftragsnummer))
                return Beides;

            var key = auftragsnummer.Trim();
            var map = LoadMap();
            return map.TryGetValue(key, out var value) ? Normalize(value) : Beides;
        }

        public static void SetArbeitsplatz(string? auftragsnummer, string? arbeitsplatz)
        {
            if (string.IsNullOrWhiteSpace(auftragsnummer))
                return;

            var key = auftragsnummer.Trim();
            var map = LoadMap();
            map[key] = Normalize(arbeitsplatz);
            SaveMap(map);
        }

        public static void SetDefaultArbeitsplatzIfMissing(string? auftragsnummer, string? arbeitsplatz)
        {
            if (string.IsNullOrWhiteSpace(auftragsnummer))
                return;

            var key = auftragsnummer.Trim();
            var map = LoadMap();
            if (!map.ContainsKey(key))
            {
                map[key] = Normalize(arbeitsplatz);
                SaveMap(map);
            }
        }

        public static bool IsMatchForBereich(string arbeitsplatz, string bereich)
        {
            var normalizedWorkplace = Normalize(arbeitsplatz);
            var normalizedBereich = NormalizeBereich(bereich);

            if (normalizedWorkplace == Beides)
                return true;

            return string.Equals(normalizedWorkplace, normalizedBereich, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeBereich(string? bereich)
        {
            if (string.Equals(bereich, Kantbank, StringComparison.OrdinalIgnoreCase))
                return Kantbank;
            return Laser;
        }

        private static string Normalize(string? arbeitsplatz)
        {
            if (string.Equals(arbeitsplatz, Laser, StringComparison.OrdinalIgnoreCase))
                return Laser;
            if (string.Equals(arbeitsplatz, Kantbank, StringComparison.OrdinalIgnoreCase))
                return Kantbank;
            return Beides;
        }

        private static Dictionary<string, string> LoadMap()
        {
            try
            {
                if (!File.Exists(StorePath))
                    return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                var json = File.ReadAllText(StorePath);
                var map = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                return map != null
                    ? new Dictionary<string, string>(map, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static void SaveMap(Dictionary<string, string> map)
        {
            try
            {
                var dir = Path.GetDirectoryName(StorePath);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StorePath, json);
            }
            catch
            {
            }
        }
    }
}
