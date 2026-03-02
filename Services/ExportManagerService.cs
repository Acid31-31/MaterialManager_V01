using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using MaterialManager_V01.Models;

namespace MaterialManager_V01.Services
{
    /// <summary>
    /// Service für Export in verschiedene Formate
    /// - CSV (Excel)
    /// - XML (ERP-Systeme)
    /// - JSON (API / Zukunft)
    /// </summary>
    public static class ExportManagerService
    {
        /// <summary>
        /// Exportiert Materialien als CSV (Excel-kompatibel)
        /// </summary>
        public static string ExportToCsv(List<MaterialItem> materialien, string outputPath = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(outputPath))
                    outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                        $"Materialbestand_{DateTime.Now:yyyy-MM-dd_HHmmss}.csv");

                var sb = new StringBuilder();
                
                // CSV Header
                sb.AppendLine("Material;Legierung;Oberfläche;Güte;Form;Stärke (mm);Maß;Stück;Gewicht (kg);Lagerort;Restnummer;Datum;Geändert;Auftrag;Lieferant;Lieferschein");

                // CSV Daten
                foreach (var m in materialien)
                {
                    var row = $"{EscapeCsv(m.MaterialArt)};{EscapeCsv(m.Legierung)};{EscapeCsv(m.Oberflaeche)};{EscapeCsv(m.Guete)};{EscapeCsv(m.Form)};{m.Staerke:F1};{EscapeCsv(m.Mass)};{m.Stueckzahl};{m.GewichtKg:F2};{EscapeCsv(m.Lagerort)};{EscapeCsv(m.Restnummer)};{m.Datum:dd.MM.yyyy};{m.AenderungsDatum:dd.MM.yyyy};{EscapeCsv(m.AuftragNr)};{EscapeCsv(m.Lieferant)};{EscapeCsv(m.LieferscheinNr)}";
                    sb.AppendLine(row);
                }

                File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
                return outputPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim CSV-Export: {ex.Message}");
            }
        }

        /// <summary>
        /// Exportiert Materialien als XML (für ERP-Systeme)
        /// </summary>
        public static string ExportToXml(List<MaterialItem> materialien, string outputPath = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(outputPath))
                    outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                        $"Materialbestand_{DateTime.Now:yyyy-MM-dd_HHmmss}.xml");

                var root = new XElement("Materialbestand",
                    new XAttribute("Datum", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                    new XAttribute("Gesamt", materialien.Count),
                    new XAttribute("GesamtGewicht", $"{materialien.Sum(m => m.GewichtKg):F2}"),
                    materialien.Select(m => new XElement("Material",
                        new XElement("MaterialArt", m.MaterialArt ?? ""),
                        new XElement("Legierung", m.Legierung ?? ""),
                        new XElement("Oberflaeche", m.Oberflaeche ?? ""),
                        new XElement("Guete", m.Guete ?? ""),
                        new XElement("Form", m.Form ?? ""),
                        new XElement("Staerke", m.Staerke.ToString("F1")),
                        new XElement("Mass", m.Mass ?? ""),
                        new XElement("Stueckzahl", m.Stueckzahl),
                        new XElement("GewichtKg", m.GewichtKg.ToString("F2")),
                        new XElement("Lagerort", m.Lagerort ?? ""),
                        new XElement("Restnummer", m.Restnummer ?? ""),
                        new XElement("Datum", m.Datum.ToString()),
                        new XElement("AenderungsDatum", m.AenderungsDatum.ToString()),
                        new XElement("AuftragNr", m.AuftragNr ?? ""),
                        new XElement("Lieferant", m.Lieferant ?? ""),
                        new XElement("LieferscheinNr", m.LieferscheinNr ?? "")
                    ))
                );

                var doc = new XDocument(root);
                doc.Save(outputPath);
                return outputPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim XML-Export: {ex.Message}");
            }
        }

        /// <summary>
        /// Exportiert Materialien als JSON (für APIs / Mobile-Apps)
        /// </summary>
        public static string ExportToJson(List<MaterialItem> materialien, string outputPath = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(outputPath))
                    outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                        $"Materialbestand_{DateTime.Now:yyyy-MM-dd_HHmmss}.json");

                var exportData = new
                {
                    Datum = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Gesamt = materialien.Count,
                    GesamtGewicht = materialien.Sum(m => m.GewichtKg),
                    Materialien = materialien.Select(m => new
                    {
                        MaterialArt = m.MaterialArt,
                        Legierung = m.Legierung,
                        Oberflaeche = m.Oberflaeche,
                        Guete = m.Guete,
                        Form = m.Form,
                        Staerke = m.Staerke,
                        Mass = m.Mass,
                        Stueckzahl = m.Stueckzahl,
                        GewichtKg = m.GewichtKg,
                        Lagerort = m.Lagerort,
                        Restnummer = m.Restnummer,
                        Datum = m.Datum.ToString(),
                        AenderungsDatum = m.AenderungsDatum.ToString(),
                        AuftragNr = m.AuftragNr,
                        Lieferant = m.Lieferant,
                        LieferscheinNr = m.LieferscheinNr
                    }).ToList()
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(exportData, options);
                File.WriteAllText(outputPath, json, Encoding.UTF8);
                return outputPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim JSON-Export: {ex.Message}");
            }
        }

        /// <summary>
        /// Hilfsmethode: Escaped CSV-Werte (für Sonderzeichen)
        /// </summary>
        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(";") || value.Contains("\"") || value.Contains("\n"))
                return $"\"{value.Replace("\"", "\"\"")}\"";

            return value;
        }
    }
}
