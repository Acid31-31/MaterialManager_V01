using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using ClosedXML.Excel;

namespace MaterialManager_V01
{
    // Excel import/export for MaterialItem using ClosedXML (.xlsx)
    // Stufe 2: separate Worksheets "Bleche", "Rohre", "Profile"
    // Abwärtskompatibel: altes Worksheet "Materialien" wird als "Bleche" interpretiert
    public static class ExcelService
    {
        private const int MAX_RETRY_ATTEMPTS = 20;
        private const int RETRY_DELAY_MS = 300;

        // ── Spaltennamen ─────────────────────────────────────────────────────────
        // Bleche (19 Spalten – identisch mit bisherigem Format)
        private static readonly string[] HeaderBleche =
        {
            "MaterialArt","Legierung","Oberflaeche","Guete","Form","Staerke","Mass",
            "Stueckzahl","Restnummer","Datum","Lagerort","AenderungsDatum","AuftragNr",
            "Lieferant","LieferscheinNr","PreisProKg","AngelegtVon","GeaendertVon","PdfPfad","PdfPfadAngefangeneTafel"
        };

        // Rohre (19 Spalten)
        private static readonly string[] HeaderRohre =
        {
            "MaterialArt","Legierung","Oberflaeche","Guete","Durchmesser","Wandstaerke","Laenge",
            "Stueckzahl","Restnummer","Datum","Lagerort","AenderungsDatum","AuftragNr",
            "Lieferant","LieferscheinNr","PreisProKg","AngelegtVon","GeaendertVon","PdfPfad","PdfPfadAngefangeneTafel"
        };

        // Profile (21 Spalten)
        private static readonly string[] HeaderProfile =
        {
            "MaterialArt","Legierung","Oberflaeche","Guete","ProfilTyp","Hoehe","Breite","Wandstaerke","Laenge",
            "Stueckzahl","Restnummer","Datum","Lagerort","AenderungsDatum","AuftragNr",
            "Lieferant","LieferscheinNr","PreisProKg","AngelegtVon","GeaendertVon","PdfPfad","PdfPfadAngefangeneTafel"
        };

        public static void Export(string filePath, IEnumerable<Models.MaterialItem> materialien)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("filePath is required", nameof(filePath));

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            for (int attempt = 0; attempt < MAX_RETRY_ATTEMPTS; attempt++)
            {
                try
                {
                    ExportInternal(filePath, materialien);
                    System.Diagnostics.Debug.WriteLine($"[ExcelService.Export] Erfolgreich gespeichert nach {attempt + 1} Versuch(en)");
                    return;
                }
                catch (IOException ex) when (attempt < MAX_RETRY_ATTEMPTS - 1)
                {
                    System.Diagnostics.Debug.WriteLine($"[ExcelService.Export] Versuch {attempt + 1}/{MAX_RETRY_ATTEMPTS} fehlgeschlagen: {ex.Message}");
                    Thread.Sleep(RETRY_DELAY_MS);
                }
            }

            var tempPath = filePath.Replace(".xlsx", $"_TEMP_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
            System.Diagnostics.Debug.WriteLine($"[ExcelService.Export] FALLBACK zu Temp-Datei: {tempPath}");
            ExportInternal(tempPath, materialien);

            System.Windows.MessageBox.Show(
                $"⚠️ Die Hauptdatei ist blockiert (vermutlich in Excel geöffnet).\n\n" +
                $"Daten wurden gespeichert in:\n{Path.GetFileName(tempPath)}\n\n" +
                $"Bitte Excel schließen und Programm erneut speichern.",
                "Speichern - Datei blockiert",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }

        private static void ExportInternal(string filePath, IEnumerable<Models.MaterialItem> materialien)
        {
            XLWorkbook wb = null;
            try
            {
                wb = new XLWorkbook();

                var bleche  = materialien?.Where(m => m.Kategorie == Models.MaterialKategorie.Blech)  ?? Array.Empty<Models.MaterialItem>();
                var rohre   = materialien?.Where(m => m.Kategorie == Models.MaterialKategorie.Rohr)   ?? Array.Empty<Models.MaterialItem>();
                var profile = materialien?.Where(m => m.Kategorie == Models.MaterialKategorie.Profil) ?? Array.Empty<Models.MaterialItem>();

                WriteBlech(wb.Worksheets.Add("Bleche"), bleche);
                WriteRohr (wb.Worksheets.Add("Rohre"),  rohre);
                WriteProfil(wb.Worksheets.Add("Profile"), profile);

                wb.SaveAs(filePath);
            }
            finally
            {
                wb?.Dispose();
            }

            Thread.Sleep(200);
        }

        // ── Schreiben ─────────────────────────────────────────────────────────────

        private static void WriteBlech(IXLWorksheet ws, IEnumerable<Models.MaterialItem> items)
        {
            for (int c = 0; c < HeaderBleche.Length; c++)
                ws.Cell(1, c + 1).Value = HeaderBleche[c];

            int r = 2;
            foreach (var m in items)
            {
                ws.Cell(r, 1).Value  = m.MaterialArt;
                ws.Cell(r, 2).Value  = m.Legierung;
                ws.Cell(r, 3).Value  = m.Oberflaeche;
                ws.Cell(r, 4).Value  = m.Guete;
                ws.Cell(r, 5).Value  = m.Form;
                ws.Cell(r, 6).Value  = m.Staerke;
                ws.Cell(r, 7).Value  = m.Mass;
                ws.Cell(r, 8).Value  = m.Stueckzahl;
                ws.Cell(r, 9).Value  = m.Restnummer;
                ws.Cell(r, 10).Value = m.Datum?.ToString("dd.MM.yyyy") ?? "";
                ws.Cell(r, 11).Value = m.Lagerort;
                ws.Cell(r, 12).Value = m.AenderungsDatum?.ToString("dd.MM.yyyy") ?? "";
                ws.Cell(r, 13).Value = m.AuftragNr;
                ws.Cell(r, 14).Value = m.Lieferant;
                ws.Cell(r, 15).Value = m.LieferscheinNr;
                ws.Cell(r, 16).Value = (double)m.PreisProKg;
                ws.Cell(r, 17).Value = m.AngelegtVon;
                ws.Cell(r, 18).Value = m.GeaendertVon;
                ws.Cell(r, 19).Value = m.PdfPfad;
                ws.Cell(r, 20).Value = m.PdfPfadAngefangeneTafel;
                r++;
            }

            ws.Range(1, 1, Math.Max(1, r - 1), HeaderBleche.Length).SetAutoFilter();
            ws.Columns().AdjustToContents();
        }

        private static void WriteRohr(IXLWorksheet ws, IEnumerable<Models.MaterialItem> items)
        {
            for (int c = 0; c < HeaderRohre.Length; c++)
                ws.Cell(1, c + 1).Value = HeaderRohre[c];

            int r = 2;
            foreach (var m in items)
            {
                ws.Cell(r, 1).Value  = m.MaterialArt;
                ws.Cell(r, 2).Value  = m.Legierung;
                ws.Cell(r, 3).Value  = m.Oberflaeche;
                ws.Cell(r, 4).Value  = m.Guete;
                ws.Cell(r, 5).Value  = m.Durchmesser;
                ws.Cell(r, 6).Value  = m.Staerke;       // Wandstärke
                ws.Cell(r, 7).Value  = m.Laenge;
                ws.Cell(r, 8).Value  = m.Stueckzahl;
                ws.Cell(r, 9).Value  = m.Restnummer;
                ws.Cell(r, 10).Value = m.Datum?.ToString("dd.MM.yyyy") ?? "";
                ws.Cell(r, 11).Value = m.Lagerort;
                ws.Cell(r, 12).Value = m.AenderungsDatum?.ToString("dd.MM.yyyy") ?? "";
                ws.Cell(r, 13).Value = m.AuftragNr;
                ws.Cell(r, 14).Value = m.Lieferant;
                ws.Cell(r, 15).Value = m.LieferscheinNr;
                ws.Cell(r, 16).Value = (double)m.PreisProKg;
                ws.Cell(r, 17).Value = m.AngelegtVon;
                ws.Cell(r, 18).Value = m.GeaendertVon;
                ws.Cell(r, 19).Value = m.PdfPfad;
                ws.Cell(r, 20).Value = m.PdfPfadAngefangeneTafel;
                r++;
            }

            ws.Range(1, 1, Math.Max(1, r - 1), HeaderRohre.Length).SetAutoFilter();
            ws.Columns().AdjustToContents();
        }

        private static void WriteProfil(IXLWorksheet ws, IEnumerable<Models.MaterialItem> items)
        {
            for (int c = 0; c < HeaderProfile.Length; c++)
                ws.Cell(1, c + 1).Value = HeaderProfile[c];

            int r = 2;
            foreach (var m in items)
            {
                ws.Cell(r, 1).Value  = m.MaterialArt;
                ws.Cell(r, 2).Value  = m.Legierung;
                ws.Cell(r, 3).Value  = m.Oberflaeche;
                ws.Cell(r, 4).Value  = m.Guete;
                ws.Cell(r, 5).Value  = m.ProfilTyp;
                ws.Cell(r, 6).Value  = m.ProfilHoehe;
                ws.Cell(r, 7).Value  = m.ProfilBreite;
                ws.Cell(r, 8).Value  = m.Staerke;       // Wandstärke
                ws.Cell(r, 9).Value  = m.Laenge;
                ws.Cell(r, 10).Value = m.Stueckzahl;
                ws.Cell(r, 11).Value = m.Restnummer;
                ws.Cell(r, 12).Value = m.Datum?.ToString("dd.MM.yyyy") ?? "";
                ws.Cell(r, 13).Value = m.Lagerort;
                ws.Cell(r, 14).Value = m.AenderungsDatum?.ToString("dd.MM.yyyy") ?? "";
                ws.Cell(r, 15).Value = m.AuftragNr;
                ws.Cell(r, 16).Value = m.Lieferant;
                ws.Cell(r, 17).Value = m.LieferscheinNr;
                ws.Cell(r, 18).Value = (double)m.PreisProKg;
                ws.Cell(r, 19).Value = m.AngelegtVon;
                ws.Cell(r, 20).Value = m.GeaendertVon;
                ws.Cell(r, 21).Value = m.PdfPfad;
                ws.Cell(r, 22).Value = m.PdfPfadAngefangeneTafel;
                r++;
            }

            ws.Range(1, 1, Math.Max(1, r - 1), HeaderProfile.Length).SetAutoFilter();
            ws.Columns().AdjustToContents();
        }

        // ── Lesen ─────────────────────────────────────────────────────────────────

        public static IEnumerable<Models.MaterialItem> Import(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("filePath is required", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Excel file not found", filePath);

            var result = new List<Models.MaterialItem>();

            for (int attempt = 0; attempt < MAX_RETRY_ATTEMPTS; attempt++)
            {
                XLWorkbook wb = null;
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        wb = new XLWorkbook(stream);

                        // Rückwärtskompatibilität: altes Blatt heißt "Materialien" → als Bleche importieren
                        foreach (var ws in wb.Worksheets)
                        {
                            var name = ws.Name;
                            if (name.Equals("Bleche", StringComparison.OrdinalIgnoreCase) ||
                                name.Equals("Materialien", StringComparison.OrdinalIgnoreCase))
                            {
                                result.AddRange(ReadBleche(ws));
                            }
                            else if (name.Equals("Rohre", StringComparison.OrdinalIgnoreCase))
                            {
                                result.AddRange(ReadRohre(ws));
                            }
                            else if (name.Equals("Profile", StringComparison.OrdinalIgnoreCase))
                            {
                                result.AddRange(ReadProfile(ws));
                            }
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[ExcelService.Import] {result.Count} Materialien geladen nach {attempt + 1} Versuch(en)");
                    return result;
                }
                catch (IOException ex) when (attempt < MAX_RETRY_ATTEMPTS - 1)
                {
                    System.Diagnostics.Debug.WriteLine($"[ExcelService.Import] Versuch {attempt + 1}/{MAX_RETRY_ATTEMPTS} fehlgeschlagen: {ex.Message}");
                    Thread.Sleep(RETRY_DELAY_MS);
                    result.Clear();
                }
                finally
                {
                    wb?.Dispose();
                }
            }

            throw new IOException($"Konnte Datei nach {MAX_RETRY_ATTEMPTS} Versuchen nicht laden: {filePath}");
        }

        private static IEnumerable<Models.MaterialItem> ReadBleche(IXLWorksheet ws)
        {
            var result = new List<Models.MaterialItem>();
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            if (lastRow < 2) return result;
            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

            for (int r = 2; r <= lastRow; r++)
            {
                var matArt = ws.Cell(r, 1).GetString();
                var leg    = ws.Cell(r, 2).GetString();
                var ober   = ws.Cell(r, 3).GetString();
                var guete  = ws.Cell(r, 4).GetString();
                var form   = ws.Cell(r, 5).GetString();
                var sta    = ParseDouble(ws.Cell(r, 6).GetString());
                var mass   = ws.Cell(r, 7).GetString();
                var stueck = ParseInt(ws.Cell(r, 8).GetString(), 1);
                var rest   = ws.Cell(r, 9).GetString();
                var datum  = ParseDate(ws.Cell(r, 10).GetString());
                var lager  = MaterialManager_V01.Services.RegalService.DetermineLagerort(matArt, leg, form, sta, mass, null);

                if (string.Equals(form, "Rest", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(rest))
                    rest = Models.MaterialDefinitions.NeueRestnummer();

                if (string.IsNullOrWhiteSpace(matArt) && string.IsNullOrWhiteSpace(mass))
                    continue;

                var aenderung    = lastCol >= 12 ? ParseDate(ws.Cell(r, 12).GetString()) : null;
                var auftragNr    = lastCol >= 13 ? ws.Cell(r, 13).GetString() : "";
                var lieferant    = lastCol >= 14 ? ws.Cell(r, 14).GetString() : "";
                var lieferschein = lastCol >= 15 ? ws.Cell(r, 15).GetString() : "";
                var preis        = lastCol >= 16 ? ParseDecimal(ws.Cell(r, 16).GetString()) : 0m;
                var angelegtVon  = lastCol >= 17 ? ws.Cell(r, 17).GetString() : "";
                var geaendertVon = lastCol >= 18 ? ws.Cell(r, 18).GetString() : "";
                var pdfPfad      = lastCol >= 19 ? ws.Cell(r, 19).GetString() : "";
                var pdfPfadAngefangeneTafel = lastCol >= 20 ? ws.Cell(r, 20).GetString() : "";

                result.Add(new Models.MaterialItem
                {
                    Kategorie      = Models.MaterialKategorie.Blech,
                    MaterialArt    = matArt,
                    Legierung      = leg,
                    Oberflaeche    = ober,
                    Guete          = guete,
                    Form           = form,
                    Staerke        = sta,
                    Mass           = mass,
                    Stueckzahl     = stueck,
                    Restnummer     = rest,
                    Datum          = datum,
                    Lagerort       = lager,
                    AenderungsDatum = aenderung,
                    AuftragNr      = auftragNr,
                    Lieferant      = lieferant,
                    LieferscheinNr = lieferschein,
                    PreisProKg     = preis,
                    AngelegtVon    = angelegtVon,
                    GeaendertVon   = geaendertVon,
                    PdfPfad        = pdfPfad,
                    PdfPfadAngefangeneTafel = pdfPfadAngefangeneTafel
                });
            }
            return result;
        }

        private static IEnumerable<Models.MaterialItem> ReadRohre(IXLWorksheet ws)
        {
            var result = new List<Models.MaterialItem>();
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            if (lastRow < 2) return result;
            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

            for (int r = 2; r <= lastRow; r++)
            {
                var matArt     = ws.Cell(r, 1).GetString();
                var leg        = ws.Cell(r, 2).GetString();
                var ober       = ws.Cell(r, 3).GetString();
                var guete      = ws.Cell(r, 4).GetString();
                var dm         = ParseDouble(ws.Cell(r, 5).GetString());
                var wand       = ParseDouble(ws.Cell(r, 6).GetString());
                var laenge     = ParseDouble(ws.Cell(r, 7).GetString());
                var stueck     = ParseInt(ws.Cell(r, 8).GetString(), 1);
                var rest       = ws.Cell(r, 9).GetString();
                var datum      = ParseDate(ws.Cell(r, 10).GetString());
                var lager      = ws.Cell(r, 11).GetString();
                var aenderung  = lastCol >= 12 ? ParseDate(ws.Cell(r, 12).GetString()) : null;
                var auftragNr  = lastCol >= 13 ? ws.Cell(r, 13).GetString() : "";
                var lieferant  = lastCol >= 14 ? ws.Cell(r, 14).GetString() : "";
                var lieferschein = lastCol >= 15 ? ws.Cell(r, 15).GetString() : "";
                var preis      = lastCol >= 16 ? ParseDecimal(ws.Cell(r, 16).GetString()) : 0m;
                var angelegtVon  = lastCol >= 17 ? ws.Cell(r, 17).GetString() : "";
                var geaendertVon = lastCol >= 18 ? ws.Cell(r, 18).GetString() : "";
                var pdfPfad    = lastCol >= 19 ? ws.Cell(r, 19).GetString() : "";
                var pdfPfadAngefangeneTafel = lastCol >= 20 ? ws.Cell(r, 20).GetString() : "";

                if (string.IsNullOrWhiteSpace(matArt) && dm == 0) continue;

                result.Add(new Models.MaterialItem
                {
                    Kategorie      = Models.MaterialKategorie.Rohr,
                    MaterialArt    = matArt,
                    Legierung      = leg,
                    Oberflaeche    = ober,
                    Guete          = guete,
                    Durchmesser    = dm,
                    Staerke        = wand,
                    Laenge         = laenge,
                    Stueckzahl     = stueck,
                    Restnummer     = rest,
                    Datum          = datum,
                    Lagerort       = string.IsNullOrWhiteSpace(lager) ? "Rohrlager" : lager,
                    AenderungsDatum = aenderung,
                    AuftragNr      = auftragNr,
                    Lieferant      = lieferant,
                    LieferscheinNr = lieferschein,
                    PreisProKg     = preis,
                    AngelegtVon    = angelegtVon,
                    GeaendertVon   = geaendertVon,
                    PdfPfad        = pdfPfad,
                    PdfPfadAngefangeneTafel = pdfPfadAngefangeneTafel
                });
            }
            return result;
        }

        private static IEnumerable<Models.MaterialItem> ReadProfile(IXLWorksheet ws)
        {
            var result = new List<Models.MaterialItem>();
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            if (lastRow < 2) return result;
            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

            for (int r = 2; r <= lastRow; r++)
            {
                var matArt     = ws.Cell(r, 1).GetString();
                var leg        = ws.Cell(r, 2).GetString();
                var ober       = ws.Cell(r, 3).GetString();
                var guete      = ws.Cell(r, 4).GetString();
                var profilTyp  = ws.Cell(r, 5).GetString();
                var hoehe      = ParseDouble(ws.Cell(r, 6).GetString());
                var breite     = ParseDouble(ws.Cell(r, 7).GetString());
                var wand       = ParseDouble(ws.Cell(r, 8).GetString());
                var laenge     = ParseDouble(ws.Cell(r, 9).GetString());
                var stueck     = ParseInt(ws.Cell(r, 10).GetString(), 1);
                var rest       = ws.Cell(r, 11).GetString();
                var datum      = ParseDate(ws.Cell(r, 12).GetString());
                var lager      = ws.Cell(r, 13).GetString();
                var aenderung  = lastCol >= 14 ? ParseDate(ws.Cell(r, 14).GetString()) : null;
                var auftragNr  = lastCol >= 15 ? ws.Cell(r, 15).GetString() : "";
                var lieferant  = lastCol >= 16 ? ws.Cell(r, 16).GetString() : "";
                var lieferschein = lastCol >= 17 ? ws.Cell(r, 17).GetString() : "";
                var preis      = lastCol >= 18 ? ParseDecimal(ws.Cell(r, 18).GetString()) : 0m;
                var angelegtVon  = lastCol >= 19 ? ws.Cell(r, 19).GetString() : "";
                var geaendertVon = lastCol >= 20 ? ws.Cell(r, 20).GetString() : "";
                var pdfPfad    = lastCol >= 21 ? ws.Cell(r, 21).GetString() : "";
                var pdfPfadAngefangeneTafel = lastCol >= 22 ? ws.Cell(r, 22).GetString() : "";

                if (string.IsNullOrWhiteSpace(matArt) && string.IsNullOrWhiteSpace(profilTyp)) continue;

                result.Add(new Models.MaterialItem
                {
                    Kategorie      = Models.MaterialKategorie.Profil,
                    MaterialArt    = matArt,
                    Legierung      = leg,
                    Oberflaeche    = ober,
                    Guete          = guete,
                    ProfilTyp      = profilTyp,
                    ProfilHoehe    = hoehe,
                    ProfilBreite   = breite,
                    Staerke        = wand,
                    Laenge         = laenge,
                    Stueckzahl     = stueck,
                    Restnummer     = rest,
                    Datum          = datum,
                    Lagerort       = string.IsNullOrWhiteSpace(lager) ? "Profillager" : lager,
                    AenderungsDatum = aenderung,
                    AuftragNr      = auftragNr,
                    Lieferant      = lieferant,
                    LieferscheinNr = lieferschein,
                    PreisProKg     = preis,
                    AngelegtVon    = angelegtVon,
                    GeaendertVon   = geaendertVon,
                    PdfPfad        = pdfPfad,
                    PdfPfadAngefangeneTafel = pdfPfadAngefangeneTafel
                });
            }
            return result;
        }

        // ── Hilfsmethoden ─────────────────────────────────────────────────────────

        private static double ParseDouble(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;

            var value = s.Trim();

            // Wichtig: zuerst deutsches/lokales Format probieren (0,5)
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("de-DE"), out var v)) return v;
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out v)) return v;
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v;

            // Fallback für gemischte Formate
            var normalizedComma = value.Replace(".", "").Replace(',', '.');
            if (double.TryParse(normalizedComma, NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v;

            var normalizedDot = value.Replace(",", "");
            if (double.TryParse(normalizedDot, NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v;

            return 0;
        }

        private static int ParseInt(string s, int fallback = 0)
        {
            if (string.IsNullOrWhiteSpace(s)) return fallback;
            return int.TryParse(s, out var v) ? v : fallback;
        }

        private static decimal ParseDecimal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0m;

            var value = s.Trim();

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("de-DE"), out var v)) return v;
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out v)) return v;
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v;

            var normalizedComma = value.Replace(".", "").Replace(',', '.');
            if (decimal.TryParse(normalizedComma, NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v;

            var normalizedDot = value.Replace(",", "");
            if (decimal.TryParse(normalizedDot, NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v;

            return 0m;
        }

        private static DateTime? ParseDate(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            return DateTime.TryParse(s, out var d) ? d : (DateTime?)null;
        }
    }
}
