using MaterialManager_V01.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MaterialManager_V01.Services
{
    public static class BestellService
    {
        private static readonly string ConfigFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MaterialManager_V01", "mindestbestaende.json");

        // Schlüssel: "MaterialArt|Legierung|Staerke"
        private static Dictionary<string, int> _mindestBestaende = new();

        static BestellService()
        {
            LoadConfig();
        }

        private static string GetKey(string materialArt, string legierung, double staerke)
        {
            return $"{materialArt}|{legierung}|{staerke:0.0}";
        }

        /// <summary>
        /// Setzt den Mindestbestand für ein Material
        /// </summary>
        public static void SetMindestbestand(string materialArt, string legierung, double staerke, int mindest)
        {
            var key = GetKey(materialArt, legierung, staerke);
            _mindestBestaende[key] = mindest;
            SaveConfig();
        }

        /// <summary>
        /// Gibt den Mindestbestand zurück (Standard: 3 Tafeln für GF/MF/KF)
        /// </summary>
        public static int GetMindestbestand(string materialArt, string legierung, double staerke)
        {
            var key = GetKey(materialArt, legierung, staerke);
            return _mindestBestaende.TryGetValue(key, out var val) ? val : 3;
        }

        /// <summary>
        /// Berechnet Bestellvorschläge basierend auf aktuellem Bestand vs. Mindestbestand
        /// </summary>
        public static List<Bestellvorschlag> BerechneVorschlaege(IEnumerable<MaterialItem> materialien)
        {
            var vorschlaege = new List<Bestellvorschlag>();

            // Nur GF, MF, KF (keine Reste)
            var tafeln = materialien
                .Where(m => m.Form == "GF" || m.Form == "MF" || m.Form == "KF")
                .GroupBy(m => new { m.MaterialArt, m.Legierung, m.Oberflaeche, m.Staerke, m.Form })
                .ToList();

            foreach (var gruppe in tafeln)
            {
                var aktuellerBestand = gruppe.Sum(m => m.Stueckzahl);
                var mindest = GetMindestbestand(gruppe.Key.MaterialArt, gruppe.Key.Legierung, gruppe.Key.Staerke);

                if (aktuellerBestand < mindest)
                {
                    var fehlmenge = mindest - aktuellerBestand;
                    var dringlichkeit = aktuellerBestand == 0 ? "KRITISCH"
                        : aktuellerBestand <= 1 ? "Hoch"
                        : "Normal";

                    vorschlaege.Add(new Bestellvorschlag
                    {
                        MaterialArt = gruppe.Key.MaterialArt,
                        Legierung = gruppe.Key.Legierung,
                        Oberflaeche = gruppe.Key.Oberflaeche,
                        Form = gruppe.Key.Form,
                        Staerke = gruppe.Key.Staerke,
                        AktuellerBestand = aktuellerBestand,
                        Mindestbestand = mindest,
                        Fehlmenge = fehlmenge,
                        Dringlichkeit = dringlichkeit
                    });
                }
            }

            return vorschlaege
                .OrderByDescending(v => v.Dringlichkeit == "KRITISCH")
                .ThenByDescending(v => v.Dringlichkeit == "Hoch")
                .ThenBy(v => v.MaterialArt)
                .ThenBy(v => v.Legierung)
                .ToList();
        }

        private static void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFile))
                {
                    var json = File.ReadAllText(ConfigFile);
                    _mindestBestaende = JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new();
                }
            }
            catch { _mindestBestaende = new(); }
        }

        private static void SaveConfig()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(_mindestBestaende, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFile, json);
            }
            catch { }
        }
    }

    public class Bestellvorschlag
    {
        public string MaterialArt { get; set; } = "";
        public string Legierung { get; set; } = "";
        public string Oberflaeche { get; set; } = "";
        public string Form { get; set; } = "";
        public double Staerke { get; set; }
        public int AktuellerBestand { get; set; }
        public int Mindestbestand { get; set; }
        public int Fehlmenge { get; set; }
        public string Dringlichkeit { get; set; } = "";
    }
}
