using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MaterialManager_V01.Models;

namespace MaterialManager_V01.Services
{
    /// <summary>
    /// Verwaltet Regale und ihre Kapazitäten mit spezifischer Lagerlogik
    /// </summary>
    public static class RegalService
    {
        private static readonly string StateFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MaterialManager_V01",
            "regal_capacities.json"
        );

        // Regal-Definitionen mit Kapazitäten (kg)
        private static Dictionary<string, double> Capacities = new()
        {
            { "Regal A", 800 },    // MF
            { "Regal B", 1500 },   // Alu/Edelstahl ≥6mm
            { "Regal C", 1000 },   // GF - S355+ Stähle
            { "Regal D", 1000 },   // GF - S235 und niedrigere Stähle
            { "Regal E", 1000 },   // GF - Sonstige
            { "Regal F", 800 },    // MF
            { "Regal G", 600 },    // KF
            { "Regal H", 600 },    // KF
            { "Regal I", 600 },    // KF
            { "Regal J", 800 },    // MF
            { "EU Palette", 2000 } // Kleine Materialien (Paletten-Regal)
        };

        // Stahl-Klassifikation
        private static readonly HashSet<string> NiedrigeStaehle = new()
        {
            "S235"
        };

        private static readonly HashSet<string> HoheStaehle = new()
        {
            "S355", "S460", "HB400", "HB500"
        };

        static RegalService()
        {
            LoadCapacities();
        }

        /// <summary>
        /// Bestimmt das Regal basierend auf Material-Eigenschaften
        /// PRIORITÄT DER REGELN (von oben nach unten):
        /// 1. MF => Regal A, F oder J (am wenigsten ausgelastet)
        /// 2. KF => Regal G, H oder I (am wenigsten ausgelastet)
        /// 3. Dimensionen ≤1500x1300 oder ≤1300x1500 => EU Palette (Paletten-Regal)
        /// 4. Aluminium oder Edelstahl ab 6mm => Regal B
        /// 5. GF + Stahl S355+ => Regal C
        /// 6. GF + Stahl S235- => Regal D
        /// 7. GF (sonstige) => Regal E
        /// 8. Große Reste (Form="Rest" + große Dimensionen) => Nach Material klassifizieren
        /// 9. Standard (unklassifiziert) => Regal E
        /// </summary>
        public static string DetermineLagerort(string materialArt, string form, double staerke, string mass)
        {
            return DetermineLagerort(materialArt, "", form, staerke, mass, null);
        }

        public static string DetermineLagerort(string materialArt, string legierung, string form, double staerke, string mass, IEnumerable<MaterialItem>? currentInventory)
        {
            // Regel 1: MF => Regal A, F oder J
            // MF hat IMMER Vorrang, auch wenn Dimensionen klein sind
            if (string.Equals(form, "MF", StringComparison.OrdinalIgnoreCase))
            {
                return FindBestMFRegal(currentInventory);
            }

            // Regel 2: KF => Regal G, H oder I
            // KF hat IMMER Vorrang, auch wenn Dimensionen klein sind
            if (string.Equals(form, "KF", StringComparison.OrdinalIgnoreCase))
            {
                return FindBestKFRegal(currentInventory);
            }

            // Regel 3: Kleine Dimensionen (≤1500x1300 oder ≤1300x1500) => EU Palette
            // Gilt für alle Materialien (auch Reste)
            if (!string.IsNullOrWhiteSpace(mass))
            {
                try
                {
                    var s = mass.ToLower().Replace('×', 'x');
                    var parts = s.Split('x');
                    if (parts.Length >= 2)
                    {
                        if (int.TryParse(parts[0].Trim(), out var laenge) && 
                            int.TryParse(parts[1].Trim(), out var breite))
                        {
                            // Kleine Materialien: 1500x1300 oder 1300x1500 => EU Palette (Paletten-Regal)
                            if ((laenge <= 1500 && breite <= 1300) || (laenge <= 1300 && breite <= 1500))
                            {
                                return "EU Palette";  // Kleine Materialien auf Paletten-Regal
                            }
                        }
                    }
                }
                catch { }
            }

            // Regel 4: Aluminium oder Edelstahl ab 6mm => Regal B
            if ((string.Equals(materialArt, "Aluminium", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(materialArt, "Edelstahl", StringComparison.OrdinalIgnoreCase)) &&
                staerke >= 6.0)
            {
                return "Regal B";
            }

            // Regel 5-7: GF mit Stahl-Klassifikation
            if (string.Equals(form, "GF", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(materialArt, "Stahl", StringComparison.OrdinalIgnoreCase))
                {
                    // Hohe Stähle (S355, S460, HB400, HB500) => Regal C
                    if (HoheStaehle.Contains(legierung))
                    {
                        return "Regal C";
                    }
                    // Niedrige Stähle (S235) => Regal D
                    else if (NiedrigeStaehle.Contains(legierung))
                    {
                        return "Regal D";
                    }
                }

                // Sonstige GF (Alu/Edelstahl GF oder unbekannte Stähle) => Regal E
                return "Regal E";
            }

            // Regel 8: Große Reste (Form="Rest" mit großen Dimensionen)
            // Werden nach Material klassifiziert
            if (string.Equals(form, "Rest", StringComparison.OrdinalIgnoreCase))
            {
                // Große Reste: Nach Material klassifizieren
                if (string.Equals(materialArt, "Stahl", StringComparison.OrdinalIgnoreCase))
                {
                    // Stahl-Reste nach Festigkeit
                    if (HoheStaehle.Contains(legierung))
                    {
                        return "Regal C";  // Hochfeste Stahl-Reste
                    }
                    else if (NiedrigeStaehle.Contains(legierung))
                    {
                        return "Regal D";  // Baustahl-Reste
                    }
                    else
                    {
                        return "Regal E";  // Sonstige Stahl-Reste
                    }
                }
                else if (string.Equals(materialArt, "Aluminium", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(materialArt, "Edelstahl", StringComparison.OrdinalIgnoreCase))
                {
                    // Alu/Edelstahl-Reste ab 6mm
                    if (staerke >= 6.0)
                    {
                        return "Regal B";
                    }
                    else
                    {
                        return "Regal E";  // Dünne Alu/Edelstahl-Reste
                    }
                }
            }

            // Regel 8: Standard (keine Form, keine Dimensionen oder nicht klassifizierbar)
            // Alle unklassifizierten Materialien => Regal E
            return "Regal E";
        }

        /// <summary>
        /// Findet das am wenigsten ausgelastete MF-Regal (A, F, J)
        /// </summary>
        private static string FindBestMFRegal(IEnumerable<MaterialItem>? currentInventory)
        {
            if (currentInventory == null)
                return "Regal A"; // Default

            var mfRegale = new[] { "Regal A", "Regal F", "Regal J" };
            var loads = new Dictionary<string, double>();

            foreach (var regal in mfRegale)
            {
                var load = CalculateCurrentLoad(currentInventory, regal);
                var capacity = GetCapacity(regal);
                loads[regal] = capacity > 0 ? (load / capacity) : 999; // Prozentuale Auslastung
            }

            return loads.OrderBy(x => x.Value).First().Key;
        }

        /// <summary>
        /// Findet das am wenigsten ausgelastete KF-Regal (G, H, I)
        /// </summary>
        private static string FindBestKFRegal(IEnumerable<MaterialItem>? currentInventory)
        {
            if (currentInventory == null)
                return "Regal G"; // Default

            var kfRegale = new[] { "Regal G", "Regal H", "Regal I" };
            var loads = new Dictionary<string, double>();

            foreach (var regal in kfRegale)
            {
                var load = CalculateCurrentLoad(currentInventory, regal);
                var capacity = GetCapacity(regal);
                loads[regal] = capacity > 0 ? (load / capacity) : 999;
            }

            return loads.OrderBy(x => x.Value).First().Key;
        }

        public static double GetCapacity(string lagerort)
        {
            if (string.IsNullOrWhiteSpace(lagerort)) return 500;
            if (Capacities.TryGetValue(lagerort, out var c)) return c;
            return 500;
        }

        public static void SetCapacity(string lagerort, double kg)
        {
            if (Capacities.ContainsKey(lagerort))
            {
                Capacities[lagerort] = kg;
                SaveCapacities();
            }
        }

        public static double CalculateCurrentLoad(IEnumerable<MaterialItem> items, string lagerort)
        {
            if (items == null) return 0;
            return items.Where(i => string.Equals(i.Lagerort, lagerort, StringComparison.OrdinalIgnoreCase))
                       .Sum(i => i.GewichtKg);
        }

        public static double ComputeUtilizationPercent(double loadKg, double capacityKg)
        {
            if (capacityKg <= 0) return 0;
            var pct = (loadKg / capacityKg) * 100.0;
            if (pct < 0) pct = 0;
            if (pct > 100) pct = 100;
            return pct;
        }

        /// <summary>
        /// Gibt alle verfügbaren Lagerorte zurück
        /// </summary>
        public static IEnumerable<string> GetAllLagerorte()
        {
            return Capacities.Keys.OrderBy(k => k);
        }

        /// <summary>
        /// Gruppiert Regale für die Anzeige
        /// </summary>
        public static Dictionary<string, List<string>> GetRegalGroups()
        {
            return new Dictionary<string, List<string>>
            {
                { "MF-Regale (A, F, J)", new List<string> { "Regal A", "Regal F", "Regal J" } },
                { "KF-Regale (G, H, I)", new List<string> { "Regal G", "Regal H", "Regal I" } },
                { "GF-Regale", new List<string> { "Regal B", "Regal C", "Regal D", "Regal E" } },
                { "Sonstige", new List<string> { "EU Palette" } }
            };
        }

        /// <summary>
        /// Gibt Beschreibung für ein Regal zurück
        /// </summary>
        public static string GetRegalDescription(string lagerort)
        {
            return lagerort switch
            {
                "Regal A" => "MF (Mittelformat)",
                "Regal B" => "Alu/Edelstahl ≥6mm",
                "Regal C" => "GF - S355+ Stähle",
                "Regal D" => "GF - S235 Stähle",
                "Regal E" => "GF - Sonstige",
                "Regal F" => "MF (Mittelformat)",
                "Regal G" => "KF (Kleinformat)",
                "Regal H" => "KF (Kleinformat)",
                "Regal I" => "KF (Kleinformat)",
                "Regal J" => "MF (Mittelformat)",
                "EU Palette" => "Reste und Kleine",
                _ => lagerort
            };
        }

        private static void LoadCapacities()
        {
            try
            {
                if (File.Exists(StateFile))
                {
                    var json = File.ReadAllText(StateFile);
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, double>>(json);
                    if (loaded != null)
                    {
                        foreach (var kvp in loaded)
                        {
                            if (Capacities.ContainsKey(kvp.Key))
                                Capacities[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch { }
        }

        private static void SaveCapacities()
        {
            try
            {
                var dir = Path.GetDirectoryName(StateFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir!);

                var json = JsonSerializer.Serialize(Capacities, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StateFile, json);
            }
            catch { }
        }
    }
}
