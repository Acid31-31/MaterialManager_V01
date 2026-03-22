using MaterialManager_V01.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MaterialManager_V01.Services
{
    public static class BuchungsService
    {
        private static readonly string LogFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MaterialManager_V01", "buchungen.json");

        private static List<BuchungsEintrag> _buchungen = new();

        static BuchungsService()
        {
            LoadBuchungen();
        }

        /// <summary>
        /// Bucht einen Wareneingang
        /// </summary>
        public static void BucheEingang(MaterialItem material, string lieferant = "", string lieferscheinNr = "")
        {
            var eintrag = new BuchungsEintrag
            {
                Typ = "Eingang",
                Zeitpunkt = DateTime.Now,
                MaterialArt = material.MaterialArt,
                Legierung = material.Legierung,
                Staerke = material.Staerke,
                Mass = material.Mass,
                Stueckzahl = material.Stueckzahl,
                GewichtKg = material.GewichtKg,
                Restnummer = material.Restnummer,
                Lieferant = lieferant,
                LieferscheinNr = lieferscheinNr,
                Lagerort = material.Lagerort
            };

            _buchungen.Add(eintrag);
            SaveBuchungen();
        }

        /// <summary>
        /// Bucht einen Verbrauch/Ausgang
        /// </summary>
        public static void BucheAusgang(MaterialItem material, string auftragNr = "", string mitarbeiter = "")
        {
            var eintrag = new BuchungsEintrag
            {
                Typ = "Ausgang",
                Zeitpunkt = DateTime.Now,
                MaterialArt = material.MaterialArt,
                Legierung = material.Legierung,
                Staerke = material.Staerke,
                Mass = material.Mass,
                Stueckzahl = material.Stueckzahl,
                GewichtKg = material.GewichtKg,
                Restnummer = material.Restnummer,
                AuftragNr = auftragNr,
                Mitarbeiter = mitarbeiter,
                Lagerort = material.Lagerort
            };

            _buchungen.Add(eintrag);
            SaveBuchungen();
        }

        /// <summary>
        /// Gibt die letzten N Buchungen zurück
        /// </summary>
        public static List<BuchungsEintrag> GetLetzteBuchungen(int anzahl = 50)
        {
            return _buchungen.OrderByDescending(b => b.Zeitpunkt).Take(anzahl).ToList();
        }

        /// <summary>
        /// Gibt Buchungen eines bestimmten Zeitraums zurück
        /// </summary>
        public static List<BuchungsEintrag> GetBuchungenImZeitraum(DateTime von, DateTime bis)
        {
            return _buchungen
                .Where(b => b.Zeitpunkt >= von && b.Zeitpunkt <= bis)
                .OrderByDescending(b => b.Zeitpunkt)
                .ToList();
        }

        /// <summary>
        /// Statistik: Verbrauch pro Monat
        /// </summary>
        public static Dictionary<string, double> GetMonatlicherVerbrauch(int monate = 6)
        {
            var result = new Dictionary<string, double>();
            var startDate = DateTime.Now.AddMonths(-monate);

            var monatsGruppen = _buchungen
                .Where(b => b.Typ == "Ausgang" && b.Zeitpunkt >= startDate)
                .GroupBy(b => b.Zeitpunkt.ToString("yyyy-MM"))
                .OrderBy(g => g.Key);

            foreach (var gruppe in monatsGruppen)
            {
                result[gruppe.Key] = gruppe.Sum(b => b.GewichtKg);
            }

            return result;
        }

        /// <summary>
        /// Top-Materialien nach Verbrauch
        /// </summary>
        public static List<(string Material, double KgVerbraucht, int Anzahl)> GetTopMaterialien(int top = 10)
        {
            return _buchungen
                .Where(b => b.Typ == "Ausgang")
                .GroupBy(b => $"{b.MaterialArt} {b.Legierung} {b.Staerke:0.0}mm")
                .Select(g => (Material: g.Key, KgVerbraucht: g.Sum(b => b.GewichtKg), Anzahl: g.Count()))
                .OrderByDescending(x => x.KgVerbraucht)
                .Take(top)
                .ToList();
        }

        private static void LoadBuchungen()
        {
            try
            {
                if (File.Exists(LogFile))
                {
                    var json = File.ReadAllText(LogFile);
                    _buchungen = JsonSerializer.Deserialize<List<BuchungsEintrag>>(json) ?? new();
                }
            }
            catch { _buchungen = new(); }
        }

        private static void SaveBuchungen()
        {
            try
            {
                var dir = Path.GetDirectoryName(LogFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(_buchungen, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(LogFile, json);
            }
            catch { }
        }
    }

    public class BuchungsEintrag
    {
        public string Typ { get; set; } = "";          // "Eingang" oder "Ausgang"
        public DateTime Zeitpunkt { get; set; }
        public string MaterialArt { get; set; } = "";
        public string Legierung { get; set; } = "";
        public double Staerke { get; set; }
        public string Mass { get; set; } = "";
        public int Stueckzahl { get; set; }
        public double GewichtKg { get; set; }
        public string Restnummer { get; set; } = "";
        public string Lieferant { get; set; } = "";
        public string LieferscheinNr { get; set; } = "";
        public string AuftragNr { get; set; } = "";
        public string Mitarbeiter { get; set; } = "";
        public string Lagerort { get; set; } = "";
    }
}
