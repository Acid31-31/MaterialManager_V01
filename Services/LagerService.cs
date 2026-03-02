using System.Collections.Generic;
using System.Linq;
using MaterialManager_V01.Models;

namespace MaterialManager_V01.Services
{
    public static class LagerService
    {
        // capacities in kg (mutable, persisted)
        private static Dictionary<string, double> Capacities = new()
        {
            { "Tafelregal", 5000 },
            { "EU Palette", 2000 }
        };

        private static readonly string CapacitiesFile = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "MaterialManager_V01", "capacities.json");

        public static void LoadCapacities()
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(CapacitiesFile);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                if (!System.IO.File.Exists(CapacitiesFile))
                {
                    SaveCapacities();
                    return;
                }

                var json = System.IO.File.ReadAllText(CapacitiesFile);
                var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(json);
                if (dict != null)
                    Capacities = dict;
            }
            catch { }
        }

        public static void SaveCapacities()
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(CapacitiesFile);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                var json = System.Text.Json.JsonSerializer.Serialize(Capacities);
                System.IO.File.WriteAllText(CapacitiesFile, json);
            }
            catch { }
        }

        public static void SetCapacity(string key, double kg)
        {
            Capacities[key] = kg;
        }

        // thresholds (mm)
        private const int ThresholdLength = 1500;
        private const int ThresholdWidth = 1300;

        /// <summary>
        /// Determine target storage location based on mass string "LxB".
        /// Rules:
        /// - If dimensions can be parsed:
        ///   - Both dimensions <= thresholds (1500x1300) => EU Palette
        ///   - Either dimension > thresholds => Tafelregal
        /// - If dimensions cannot be parsed => EU Palette
        /// </summary>
        public static string DetermineLagerort(string form, string mass)
        {
            // Try to parse dimensions from mass
            if (!string.IsNullOrWhiteSpace(mass))
            {
                try
                {
                    var s = mass.ToLower().Replace('×', 'x');
                    var parts = s.Split('x');
                    if (parts.Length >= 2)
                    {
                        if (int.TryParse(parts[0].Trim(), out var laenge) && int.TryParse(parts[1].Trim(), out var breite))
                        {
                            // Check if both dimensions are within thresholds
                            if (laenge <= ThresholdLength && breite <= ThresholdWidth)
                            {
                                return "EU Palette";
                            }
                            else
                            {
                                return "Tafelregal";
                            }
                        }
                    }
                }
                catch { }
            }

            // Default: EU Palette if no dimensions or parsing failed
            return "EU Palette";
        }

        public static double GetCapacity(string lagerort)
        {
            if (string.IsNullOrWhiteSpace(lagerort)) return 2000;
            if (Capacities.TryGetValue(lagerort, out var c)) return c;
            return 2000;
        }

        public static double CalculateCurrentLoad(IEnumerable<MaterialItem> items, string lagerort)
        {
            if (items == null) return 0;
            return items.Where(i => string.Equals(i.Lagerort, lagerort)).Sum(i => i.GewichtKg);
        }

        public static double ComputeUtilizationPercent(double loadKg, double capacityKg)
        {
            if (capacityKg <= 0) return 0;
            var pct = (loadKg / capacityKg) * 100.0;
            if (pct < 0) pct = 0;
            if (pct > 100) pct = 100;
            return pct;
        }
    }
}
