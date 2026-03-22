using MaterialManager_V01.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace MaterialManager_V01.Services
{
    /// <summary>
    /// ERP-Schnittstelle für SAP, SAGE und andere Systeme.
    /// Unterstützt: SAP CSV (MM), SAGE CSV, BMEcat XML (Industrie-Standard), DATANORM
    /// </summary>
    public static class ErpExportService
    {
        private static readonly string ExportDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MaterialManager_V01", "ERP_Export");

        /// <summary>
        /// SAP MM (Materialwirtschaft) CSV-Export
        /// Felder: MATNR, MAKTX, MATKL, MEINS, BRGEW, GEWEI, LGORT, CHARG, LFDAT, LIFNR, EBELN
        /// </summary>
        public static string ExportSapCsv(IEnumerable<MaterialItem> materialien)
        {
            EnsureDir();
            var filename = $"SAP_MM_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var path = Path.Combine(ExportDir, filename);

            var sb = new StringBuilder();
            // SAP MM Header
            sb.AppendLine("MATNR;MAKTX;MATKL;MEINS;BRGEW;GEWEI;LGORT;CHARG;LFDAT;LIFNR;EBELN;BSTME;MENGE;WRKST");

            int nr = 1;
            foreach (var m in materialien)
            {
                var matnr = $"MAT-{nr:D6}";
                var maktx = $"{m.MaterialArt} {m.Legierung} {m.Oberflaeche} {m.Staerke:0.0}mm {m.Mass}".Trim();
                var matkl = GetSapMaterialklasse(m);
                var wrkst = GetWerkstoffNummer(m);

                sb.AppendLine(string.Join(";",
                    matnr,                                          // MATNR - Materialnummer
                    maktx,                                          // MAKTX - Kurztext
                    matkl,                                          // MATKL - Materialklasse
                    "KG",                                           // MEINS - Mengeneinheit
                    m.GewichtKg.ToString("F2", CultureInfo.InvariantCulture), // BRGEW - Bruttogewicht
                    "KG",                                           // GEWEI - Gewichtseinheit
                    m.Lagerort,                                     // LGORT - Lagerort
                    m.Restnummer,                                   // CHARG - Charge/Restnummer
                    m.Datum?.ToString("yyyyMMdd") ?? "",             // LFDAT - Lieferdatum
                    m.Lieferant,                                    // LIFNR - Lieferant
                    m.LieferscheinNr,                               // EBELN - Bestellnummer
                    "ST",                                           // BSTME - Bestellmengeneinheit
                    m.Stueckzahl.ToString(),                        // MENGE - Menge
                    wrkst                                           // WRKST - Werkstoffnummer
                ));
                nr++;
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
        }

        /// <summary>
        /// SAGE CSV-Export (kompatibel mit SAGE 50/100/b7)
        /// </summary>
        public static string ExportSageCsv(IEnumerable<MaterialItem> materialien)
        {
            EnsureDir();
            var filename = $"SAGE_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var path = Path.Combine(ExportDir, filename);

            var sb = new StringBuilder();
            sb.AppendLine("Artikelnummer;Bezeichnung;Warengruppe;Einheit;Bestand;Lagerort;Gewicht_kg;Lieferant;Lieferschein;Preis_EUR;Datum;Bemerkung");

            int nr = 1;
            foreach (var m in materialien)
            {
                var artNr = $"ART-{nr:D5}";
                var bez = $"{m.MaterialArt} {m.Legierung} {m.Staerke:0.0}mm {m.Form}";
                var wg = m.MaterialArt;

                sb.AppendLine(string.Join(";",
                    artNr,
                    bez,
                    wg,
                    "Stk",
                    m.Stueckzahl.ToString(),
                    m.Lagerort,
                    m.GewichtKg.ToString("F2", CultureInfo.InvariantCulture),
                    m.Lieferant,
                    m.LieferscheinNr,
                    "",  // Preis (leer, falls nicht gepflegt)
                    m.Datum?.ToString("dd.MM.yyyy") ?? "",
                    string.IsNullOrEmpty(m.Restnummer) ? m.Mass : $"Rest {m.Restnummer} {m.Mass}"
                ));
                nr++;
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
        }

        /// <summary>
        /// BMEcat XML Export (Deutscher Industrie-Standard für elektronischen Produktdatenaustausch)
        /// </summary>
        public static string ExportBmeCatXml(IEnumerable<MaterialItem> materialien)
        {
            EnsureDir();
            var filename = $"BMEcat_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xml";
            var path = Path.Combine(ExportDir, filename);

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("BMECAT",
                    new XAttribute("version", "2005"),
                    new XElement("HEADER",
                        new XElement("GENERATOR_INFO", "MaterialManager R03"),
                        new XElement("CATALOG",
                            new XElement("CATALOG_ID", "MM_V01"),
                            new XElement("CATALOG_VERSION", "1.0"),
                            new XElement("CATALOG_NAME", "Materialbestand"),
                            new XElement("DATETIME",
                                new XAttribute("type", "generation_date"),
                                new XElement("DATE", DateTime.Now.ToString("yyyy-MM-dd"))
                            )
                        )
                    ),
                    new XElement("T_NEW_CATALOG",
                        materialien.Select((m, i) => new XElement("ARTICLE",
                            new XElement("SUPPLIER_AID", $"MAT-{i + 1:D6}"),
                            new XElement("ARTICLE_DETAILS",
                                new XElement("DESCRIPTION_SHORT",
                                    $"{m.MaterialArt} {m.Legierung} {m.Staerke:0.0}mm"),
                                new XElement("DESCRIPTION_LONG",
                                    $"{m.MaterialArt} {m.Legierung} {m.Oberflaeche} {m.Form} {m.Staerke:0.0}mm {m.Mass}"),
                                new XElement("EAN", ""),
                                new XElement("MANUFACTURER_NAME", m.Lieferant),
                                new XElement("DELIVERY_TIME",
                                    m.Datum?.ToString("yyyy-MM-dd") ?? "")
                            ),
                            new XElement("ARTICLE_FEATURES",
                                new XElement("FEATURE",
                                    new XElement("FNAME", "Material"),
                                    new XElement("FVALUE", m.MaterialArt)),
                                new XElement("FEATURE",
                                    new XElement("FNAME", "Legierung"),
                                    new XElement("FVALUE", m.Legierung)),
                                new XElement("FEATURE",
                                    new XElement("FNAME", "Staerke_mm"),
                                    new XElement("FVALUE", m.Staerke.ToString("F1", CultureInfo.InvariantCulture))),
                                new XElement("FEATURE",
                                    new XElement("FNAME", "Mass_mm"),
                                    new XElement("FVALUE", m.Mass)),
                                new XElement("FEATURE",
                                    new XElement("FNAME", "Form"),
                                    new XElement("FVALUE", m.Form)),
                                new XElement("FEATURE",
                                    new XElement("FNAME", "Gewicht_kg"),
                                    new XElement("FVALUE", m.GewichtKg.ToString("F2", CultureInfo.InvariantCulture))),
                                new XElement("FEATURE",
                                    new XElement("FNAME", "Lagerort"),
                                    new XElement("FVALUE", m.Lagerort)),
                                new XElement("FEATURE",
                                    new XElement("FNAME", "Restnummer"),
                                    new XElement("FVALUE", m.Restnummer))
                            )
                        ))
                    )
                )
            );

            doc.Save(path);
            return path;
        }

        /// <summary>
        /// Universeller CSV-Export (für alle ERP-Systeme importierbar)
        /// </summary>
        public static string ExportUniversalCsv(IEnumerable<MaterialItem> materialien)
        {
            EnsureDir();
            var filename = $"Universal_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var path = Path.Combine(ExportDir, filename);

            var sb = new StringBuilder();
            sb.AppendLine("MaterialArt;Legierung;Oberflaeche;Guete;Form;Staerke_mm;Mass_mm;Stueckzahl;Gewicht_kg;Lagerort;Restnummer;Datum;Lieferant;LieferscheinNr;AuftragNr");

            foreach (var m in materialien)
            {
                sb.AppendLine(string.Join(";",
                    m.MaterialArt, m.Legierung, m.Oberflaeche, m.Guete, m.Form,
                    m.Staerke.ToString("F1", CultureInfo.InvariantCulture),
                    m.Mass, m.Stueckzahl,
                    m.GewichtKg.ToString("F2", CultureInfo.InvariantCulture),
                    m.Lagerort, m.Restnummer,
                    m.Datum?.ToString("dd.MM.yyyy") ?? "",
                    m.Lieferant, m.LieferscheinNr, m.AuftragNr
                ));
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
        }

        /// <summary>
        /// SAP-Materialklasse bestimmen
        /// </summary>
        private static string GetSapMaterialklasse(MaterialItem m)
        {
            return m.MaterialArt switch
            {
                "Stahl" => "ROH-STL",
                "Edelstahl" => "ROH-ESL",
                "Aluminium" => "ROH-ALU",
                _ => "ROH-SON"
            };
        }

        /// <summary>
        /// Werkstoffnummer (DIN/EN) bestimmen
        /// </summary>
        private static string GetWerkstoffNummer(MaterialItem m)
        {
            return m.Legierung switch
            {
                "S235" => "1.0037",
                "S355" => "1.0577",
                "DC01" => "1.0330",
                "DC04" => "1.0338",
                "DD11" => "1.0332",
                "1.4301" => "1.4301",
                "1.4404" => "1.4404",
                "1.4571" => "1.4571",
                _ => ""
            };
        }

        public static string GetExportDir() => ExportDir;

        private static void EnsureDir()
        {
            if (!Directory.Exists(ExportDir))
                Directory.CreateDirectory(ExportDir);
        }
    }
}
