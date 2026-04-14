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

        // Materialien (27 Spalten – inkl. Gewicht)
        private static readonly string[] HeaderMaterialien =
        {
            "MaterialArt","Legierung","Oberflaeche","Guete","Form","Staerke","Mass",
            "Stueckzahl","GewichtKg","Restnummer","Datum","Lagerort","AenderungsDatum","AuftragNr",
            "Lieferant","LieferscheinNr","PreisProKg","AngelegtVon","GeaendertVon","PdfPfad","PdfPfadAngefangeneTafel",
            "Kategorie","Durchmesser","Laenge","ProfilTyp","ProfilHoehe","ProfilBreite"
        };

        // Bleche/Rohre/Profile Header bleiben für Alt-Import erhalten
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
                WriteMaterialien(wb.Worksheets.Add("Materialien"), materialien ?? Array.Empty<Models.MaterialItem>());
                wb.SaveAs(filePath);
            }
            finally
            {
                wb?.Dispose();
            }

            Thread.Sleep(200);
        }

        private static void WriteMaterialien(IXLWorksheet ws, IEnumerable<Models.MaterialItem> items)
        {
            for (int c = 0; c < HeaderMaterialien.Length; c++)
                ws.Cell(1, c + 1).Value = HeaderMaterialien[c];

            int r = 2;
            foreach (var m in items)
            {
                ws.Cell(r, 1).Value = m.MaterialArt;
                ws.Cell(r, 2).Value = m.Legierung;
                ws.Cell(r, 3).Value = m.Oberflaeche;
                ws.Cell(r, 4).Value = m.Guete;
                ws.Cell(r, 5).Value = m.Form;
                ws.Cell(r, 6).Value = m.Staerke;
                ws.Cell(r, 7).Value = m.Mass;
                ws.Cell(r, 8).Value = m.Stueckzahl;
                ws.Cell(r, 9).Value = Math.Round(m.GewichtKg, 2);
                ws.Cell(r, 10).Value = m.Restnummer;
                ws.Cell(r, 11).Value = m.Datum?.ToString("dd.MM.yyyy HH:mm:ss") ?? "";
                ws.Cell(r, 12).Value = m.Lagerort;
                ws.Cell(r, 13).Value = m.AenderungsDatum?.ToString("dd.MM.yyyy HH:mm:ss") ?? "";
                ws.Cell(r, 14).Value = m.AuftragNr;
                ws.Cell(r, 15).Value = m.Lieferant;
                ws.Cell(r, 16).Value = m.LieferscheinNr;
                ws.Cell(r, 17).Value = (double)m.PreisProKg;
                ws.Cell(r, 18).Value = m.AngelegtVon;
                ws.Cell(r, 19).Value = m.GeaendertVon;
                ws.Cell(r, 20).Value = m.PdfPfad;
                ws.Cell(r, 21).Value = m.PdfPfadAngefangeneTafel;
                ws.Cell(r, 22).Value = m.Kategorie.ToString();
                ws.Cell(r, 23).Value = m.Durchmesser;
                ws.Cell(r, 24).Value = m.Laenge;
                ws.Cell(r, 25).Value = m.ProfilTyp;
                ws.Cell(r, 26).Value = m.ProfilHoehe;
                ws.Cell(r, 27).Value = m.ProfilBreite;
                r++;
            }

            ws.Range(1, 1, Math.Max(1, r - 1), HeaderMaterialien.Length).SetAutoFilter();
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

                        // Neues Standardformat: ein Blatt
                        var singleSheet = wb.Worksheets.FirstOrDefault(w => w.Name.Equals("Materialien", StringComparison.OrdinalIgnoreCase));
                        if (singleSheet != null)
                        {
                            result.AddRange(ReadMaterialien(singleSheet));
                        }
                        else
                        {
                            // Alt-Format + Firmenformat: mehrere Blätter
                            foreach (var ws in wb.Worksheets)
                            {
                                var name = ws.Name;
                                if (name.Equals("Bleche", StringComparison.OrdinalIgnoreCase))
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
                                else
                                {
                                    result.AddRange(ReadHerkoSheet(ws));
                                }
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
                var mass   = NormalizeMassText(ws.Cell(r, 7).GetString());
                var stueck = ParseInt(ws.Cell(r, 8).GetString(), 1);
                var rest   = ws.Cell(r, 9).GetString();
                var datum  = ParseDate(ws.Cell(r, 10).GetString());
                var lager  = MaterialManager_V01.Services.RegalService.DetermineLagerort(matArt, leg, form, sta, mass, null);

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

                if (string.Equals(form, "Rest", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(rest))
                    rest = Models.MaterialDefinitions.NeueRestnummer();

                if (string.IsNullOrWhiteSpace(matArt))
                    matArt = InferMaterialArt(leg);

                if (string.IsNullOrWhiteSpace(form))
                    form = "Rest";

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

        private static IEnumerable<Models.MaterialItem> ReadMaterialien(IXLWorksheet ws)
        {
            var result = new List<Models.MaterialItem>();
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            if (lastRow < 2) return result;

            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var c = 1; c <= lastCol; c++)
            {
                var header = ws.Cell(1, c).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(header) && !map.ContainsKey(header))
                    map[header] = c;
            }

            string GetCell(int row, string colName)
                => map.TryGetValue(colName, out var idx) ? ws.Cell(row, idx).GetString() : string.Empty;

            for (int r = 2; r <= lastRow; r++)
            {
                var matArt = GetCell(r, "MaterialArt");
                var leg = GetCell(r, "Legierung");
                var ober = GetCell(r, "Oberflaeche");
                var guete = GetCell(r, "Guete");
                var form = GetCell(r, "Form");
                var sta = ParseDouble(GetCell(r, "Staerke"));
                var mass = NormalizeMassText(GetCell(r, "Mass"));
                var stueck = ParseInt(GetCell(r, "Stueckzahl"), 1);
                var rest = GetCell(r, "Restnummer");
                var datum = ParseDate(GetCell(r, "Datum"));
                var lager = GetCell(r, "Lagerort");
                var aenderung = ParseDate(GetCell(r, "AenderungsDatum"));
                var auftragNr = GetCell(r, "AuftragNr");
                var lieferant = GetCell(r, "Lieferant");
                var lieferschein = GetCell(r, "LieferscheinNr");
                var preis = ParseDecimal(GetCell(r, "PreisProKg"));
                var angelegtVon = GetCell(r, "AngelegtVon");
                var geaendertVon = GetCell(r, "GeaendertVon");
                var pdfPfad = GetCell(r, "PdfPfad");
                var pdfPfadAngefangeneTafel = GetCell(r, "PdfPfadAngefangeneTafel");

                var durchmesser = ParseDouble(GetCell(r, "Durchmesser"));
                var laenge = ParseDouble(GetCell(r, "Laenge"));
                var profilTyp = GetCell(r, "ProfilTyp");
                var profilHoehe = ParseDouble(GetCell(r, "ProfilHoehe"));
                var profilBreite = ParseDouble(GetCell(r, "ProfilBreite"));
                var katText = GetCell(r, "Kategorie");

                if (string.IsNullOrWhiteSpace(matArt) && string.IsNullOrWhiteSpace(mass) && string.IsNullOrWhiteSpace(rest))
                    continue;

                var kat = Models.MaterialKategorie.Blech;
                if (Enum.TryParse<Models.MaterialKategorie>(katText, true, out var parsedKat))
                    kat = parsedKat;
                else if (durchmesser > 0 || (!string.IsNullOrWhiteSpace(form) && form.Equals("Rohr", StringComparison.OrdinalIgnoreCase)))
                    kat = Models.MaterialKategorie.Rohr;
                else if (!string.IsNullOrWhiteSpace(profilTyp))
                    kat = Models.MaterialKategorie.Profil;

                if (kat == Models.MaterialKategorie.Blech && string.Equals(form, "Rest", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(rest))
                    rest = Models.MaterialDefinitions.NeueRestnummer();

                if (string.IsNullOrWhiteSpace(lager))
                {
                    lager = kat switch
                    {
                        Models.MaterialKategorie.Rohr => "Rohrlager",
                        Models.MaterialKategorie.Profil => "Profillager",
                        _ => MaterialManager_V01.Services.RegalService.DetermineLagerort(matArt, leg, form, sta, mass, null)
                    };
                }

                result.Add(new Models.MaterialItem
                {
                    Kategorie = kat,
                    MaterialArt = matArt,
                    Legierung = leg,
                    Oberflaeche = ober,
                    Guete = guete,
                    Form = form,
                    Staerke = sta,
                    Mass = mass,
                    Stueckzahl = stueck,
                    Restnummer = rest,
                    Datum = datum,
                    Lagerort = lager,
                    AenderungsDatum = aenderung,
                    AuftragNr = auftragNr,
                    Lieferant = lieferant,
                    LieferscheinNr = lieferschein,
                    PreisProKg = preis,
                    AngelegtVon = angelegtVon,
                    GeaendertVon = geaendertVon,
                    PdfPfad = pdfPfad,
                    PdfPfadAngefangeneTafel = pdfPfadAngefangeneTafel,
                    Durchmesser = durchmesser,
                    Laenge = laenge,
                    ProfilTyp = profilTyp,
                    ProfilHoehe = profilHoehe,
                    ProfilBreite = profilBreite
                });
            }

            return result;
        }

        // ── Hilfsmethoden ─────────────────────────────────────────────────────────

        private static double ParseDouble(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;

            var value = s.Trim().Replace(" ", string.Empty).Replace("'", string.Empty);

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

            var text = s.Trim();
            if (int.TryParse(text, out var v)) return v;

            var m = System.Text.RegularExpressions.Regex.Match(text, @"\d+");
            return m.Success && int.TryParse(m.Value, out v) ? v : fallback;
        }

        private static string NormalizeMassText(string? mass)
        {
            if (string.IsNullOrWhiteSpace(mass))
                return string.Empty;

            return mass.Trim()
                .Replace("X", "x")
                .Replace("×", "x")
                .Replace(" mm", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("mm", string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static string InferMaterialArt(string? legierung)
        {
            var leg = (legierung ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(leg)) return string.Empty;
            if (leg.StartsWith("1.")) return "Edelstahl";
            if (leg.StartsWith("s") || leg.Contains("dc")) return "Stahl";
            if (leg.Contains("aw") || leg.Contains("al")) return "Aluminium";
            return string.Empty;
        }

        private static decimal ParseDecimal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0m;

            var value = s.Trim().Replace(" ", string.Empty).Replace("'", string.Empty);

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

        private static bool TryExtractMass(string? mass, out double laenge, out double breite)
        {
            laenge = 0;
            breite = 0;
            if (string.IsNullOrWhiteSpace(mass))
                return false;

            var match = System.Text.RegularExpressions.Regex.Match(mass, @"(?<l>\d+(?:[\.,]\d+)?)\s*[xX×]\s*(?<b>\d+(?:[\.,]\d+)?)");
            if (!match.Success)
                return false;

            laenge = ParseDouble(match.Groups["l"].Value);
            breite = ParseDouble(match.Groups["b"].Value);
            return laenge > 0 && breite > 0;
        }

        private static IEnumerable<Models.MaterialItem> ReadHerkoSheet(IXLWorksheet ws)
        {
            var result = new List<Models.MaterialItem>();
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            if (lastRow < 3)
                return result;

            var h1 = ws.Cell(2, 1).GetString().Trim();
            var h3 = ws.Cell(2, 3).GetString().Trim();
            if (!h1.Equals("Material", StringComparison.OrdinalIgnoreCase)
                || h3.IndexOf("Dicke", StringComparison.OrdinalIgnoreCase) < 0)
                return result;

            var isRohrSheet = ws.Name.IndexOf("Rohr", StringComparison.OrdinalIgnoreCase) >= 0;

            for (int r = 3; r <= lastRow; r++)
            {
                var c1 = ws.Cell(r, 1).GetString();
                var c2 = ws.Cell(r, 2).GetString();
                var c3 = ws.Cell(r, 3).GetString();
                var c4 = ws.Cell(r, 4).GetString();
                var c5 = ws.Cell(r, 5).GetString();
                var c6 = ws.Cell(r, 6).GetString();
                var c7 = ws.Cell(r, 7).GetString();

                if (string.IsNullOrWhiteSpace(c1) && string.IsNullOrWhiteSpace(c3) && string.IsNullOrWhiteSpace(c4))
                    continue;

                var item = new Models.MaterialItem
                {
                    MaterialArt = InferMaterialArt(c1),
                    Legierung = c1,
                    Oberflaeche = c2,
                    Form = "Rest",
                    Restnummer = c6,
                    Lagerort = string.IsNullOrWhiteSpace(c5) ? "EU Palette" : c5,
                    Stueckzahl = ParseInt(c7, 1),
                    Datum = ParseDate(c7),
                    AuftragNr = string.IsNullOrWhiteSpace(c7) ? string.Empty : c7,
                    Mass = NormalizeMassText(c4)
                };

                if (isRohrSheet)
                {
                    item.Kategorie = Models.MaterialKategorie.Profil;
                    item.Form = "Rohr";

                    var profileMatch = System.Text.RegularExpressions.Regex.Match(c3 ?? string.Empty, @"(?<h>\d+(?:[\.,]\d+)?)\s*[xX×]\s*(?<b>\d+(?:[\.,]\d+)?)\s*[xX×]\s*(?<w>\d+(?:[\.,]\d+)?)");
                    if (!profileMatch.Success)
                        continue;

                    item.ProfilHoehe = ParseDouble(profileMatch.Groups["h"].Value);
                    item.ProfilBreite = ParseDouble(profileMatch.Groups["b"].Value);
                    item.Staerke = ParseDouble(profileMatch.Groups["w"].Value);
                    item.Laenge = ParseDouble(c4);

                    if (item.ProfilHoehe <= 0 || item.ProfilBreite <= 0 || item.Staerke <= 0 || item.Laenge <= 0)
                        continue;
                }
                else
                {
                    item.Kategorie = Models.MaterialKategorie.Blech;
                    item.Staerke = ParseDouble(c3);

                    if (item.Staerke <= 0)
                        continue;

                    if (!TryExtractMass(item.Mass, out _, out _))
                        continue;
                }

                if (string.IsNullOrWhiteSpace(item.MaterialArt))
                    item.MaterialArt = "Edelstahl";

                result.Add(item);
            }

            return result;
        }

        public static bool IsMaterialienFormatWithWeight(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return false;

            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var wb = new XLWorkbook(stream);
                var ws = wb.Worksheets.FirstOrDefault(w => w.Name.Equals("Materialien", StringComparison.OrdinalIgnoreCase));
                if (ws == null)
                    return false;

                var headers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
                for (var c = 1; c <= lastCol; c++)
                {
                    var h = ws.Cell(1, c).GetString().Trim();
                    if (!string.IsNullOrWhiteSpace(h))
                        headers.Add(h);
                }

                return headers.Contains("GewichtKg") && headers.Contains("Mass") && headers.Contains("Staerke");
            }
            catch
            {
                return false;
            }
        }
    }
}
