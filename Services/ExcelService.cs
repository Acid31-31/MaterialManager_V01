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
    public static class ExcelService
    {
        private const int MAX_RETRY_ATTEMPTS = 20;
        private const int RETRY_DELAY_MS = 300;
        
        public static void Export(string filePath, IEnumerable<Models.MaterialItem> materialien)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("filePath is required", nameof(filePath));

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // ✅ STRATEGIE: Versuche mit Retry, dann Temp-Datei als Fallback
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

            // ✅ FALLBACK: Speichere in TEMP-Datei (mit Zeitstempel)
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
                var ws = wb.Worksheets.Add("Materialien");

                // Header
                ws.Cell(1, 1).Value = "MaterialArt";
                ws.Cell(1, 2).Value = "Legierung";
                ws.Cell(1, 3).Value = "Oberflaeche";
                ws.Cell(1, 4).Value = "Guete";
                ws.Cell(1, 5).Value = "Form";
                ws.Cell(1, 6).Value = "Staerke";
                ws.Cell(1, 7).Value = "Mass";
                ws.Cell(1, 8).Value = "Stueckzahl";
                ws.Cell(1, 9).Value = "Restnummer";
                ws.Cell(1, 10).Value = "Datum";
                ws.Cell(1, 11).Value = "Lagerort";
                ws.Cell(1, 12).Value = "AenderungsDatum";
                ws.Cell(1, 13).Value = "AuftragNr";
                ws.Cell(1, 14).Value = "Lieferant";
                ws.Cell(1, 15).Value = "LieferscheinNr";
                ws.Cell(1, 16).Value = "PreisProKg";
                ws.Cell(1, 17).Value = "AngelegtVon";
                ws.Cell(1, 18).Value = "GeaendertVon";

                int r = 2;
                foreach (var m in materialien ?? Array.Empty<Models.MaterialItem>())
                {
                    ws.Cell(r, 1).Value = m.MaterialArt;
                    ws.Cell(r, 2).Value = m.Legierung;
                    ws.Cell(r, 3).Value = m.Oberflaeche;
                    ws.Cell(r, 4).Value = m.Guete;
                    ws.Cell(r, 5).Value = m.Form;
                    ws.Cell(r, 6).Value = m.Staerke;
                    ws.Cell(r, 7).Value = m.Mass;
                    ws.Cell(r, 8).Value = m.Stueckzahl;
                    ws.Cell(r, 9).Value = m.Restnummer;
                    ws.Cell(r, 10).Value = m.Datum?.ToString("dd.MM.yyyy") ?? "";
                    ws.Cell(r, 11).Value = m.Lagerort;
                    ws.Cell(r, 12).Value = m.AenderungsDatum?.ToString("dd.MM.yyyy") ?? "";
                    ws.Cell(r, 13).Value = m.AuftragNr;
                    ws.Cell(r, 14).Value = m.Lieferant;
                    ws.Cell(r, 15).Value = m.LieferscheinNr;
                    ws.Cell(r, 16).Value = (double)m.PreisProKg;
                    ws.Cell(r, 17).Value = m.AngelegtVon;
                    ws.Cell(r, 18).Value = m.GeaendertVon;
                    r++;
                }

                // Auto-fit columns and enable autofilter on header
                ws.Range(1, 1, Math.Max(1, r - 1), 18).SetAutoFilter();
                ws.Columns().AdjustToContents();

                wb.SaveAs(filePath);
            }
            finally
            {
                // ✅ WICHTIG: Stelle sicher dass Workbook IMMER geschlossen wird!
                if (wb != null)
                {
                    wb.Dispose();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            
            // ✅ Warte damit Windows Datei-Lock GARANTIERT freigibt
            System.Threading.Thread.Sleep(200);
        }

        public static IEnumerable<Models.MaterialItem> Import(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("filePath is required", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Excel file not found", filePath);

            var result = new List<Models.MaterialItem>();
            
            // ✅ RETRY-MECHANISMUS beim Laden (falls Excel gerade speichert)
            for (int attempt = 0; attempt < MAX_RETRY_ATTEMPTS; attempt++)
            {
                XLWorkbook wb = null;
                try
                {
                    // ✅ Öffne mit FileShare.ReadWrite (erlaubt gleichzeitigen Zugriff)
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        wb = new XLWorkbook(stream);
                        var ws = wb.Worksheets.First();

                        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                        if (lastRow < 2)
                            return result;

                        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

                        for (int r = 2; r <= lastRow; r++)
                        {
                            var matArt = ws.Cell(r, 1).GetString();
                            var leg = ws.Cell(r, 2).GetString();
                            var ober = ws.Cell(r, 3).GetString();
                            var guete = ws.Cell(r, 4).GetString();
                            var form = ws.Cell(r, 5).GetString();

                            double sta = 0;
                            var staCell = ws.Cell(r, 6).GetString();
                            if (!string.IsNullOrWhiteSpace(staCell))
                            {
                                if (!double.TryParse(staCell, NumberStyles.Any, CultureInfo.InvariantCulture, out sta))
                                    double.TryParse(staCell, NumberStyles.Any, CultureInfo.CurrentCulture, out sta);
                            }

                            var mass = ws.Cell(r, 7).GetString();

                            int stueckzahl = 1;
                            var stueckzahlCell = ws.Cell(r, 8).GetString();
                            if (!string.IsNullOrWhiteSpace(stueckzahlCell))
                            {
                                if (!int.TryParse(stueckzahlCell, out stueckzahl))
                                    stueckzahl = 1;
                            }

                            var rest = ws.Cell(r, 9).GetString();

                            DateTime? datum = null;
                            var datumCell = ws.Cell(r, 10).GetString();
                            if (!string.IsNullOrWhiteSpace(datumCell))
                            {
                                if (DateTime.TryParse(datumCell, out var parsedDatum))
                                    datum = parsedDatum;
                            }

                            var lager = MaterialManager_V01.Services.RegalService.DetermineLagerort(matArt, leg, form, sta, mass, null);

                            if (string.Equals(form, "Rest", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(rest))
                                rest = MaterialManager_V01.Models.MaterialDefinitions.NeueRestnummer();

                            if (string.IsNullOrWhiteSpace(matArt) && string.IsNullOrWhiteSpace(mass))
                                continue;

                            DateTime? aenderungsDatum = null;
                            var aenderungCell = lastCol >= 12 ? ws.Cell(r, 12).GetString() : "";
                            if (!string.IsNullOrWhiteSpace(aenderungCell))
                            {
                                if (DateTime.TryParse(aenderungCell, out var parsedAenderung))
                                    aenderungsDatum = parsedAenderung;
                            }

                            var auftragNr = lastCol >= 13 ? ws.Cell(r, 13).GetString() : "";
                            var lieferant = lastCol >= 14 ? ws.Cell(r, 14).GetString() : "";
                            var lieferscheinNr = lastCol >= 15 ? ws.Cell(r, 15).GetString() : "";

                            decimal preisProKg = 0m;
                            var preisCell = lastCol >= 16 ? ws.Cell(r, 16).GetString() : "";
                            if (!string.IsNullOrWhiteSpace(preisCell))
                            {
                                if (!decimal.TryParse(preisCell, NumberStyles.Any, CultureInfo.InvariantCulture, out preisProKg))
                                    decimal.TryParse(preisCell, NumberStyles.Any, CultureInfo.CurrentCulture, out preisProKg);
                            }

                            var angelegtVon = lastCol >= 17 ? ws.Cell(r, 17).GetString() : "";
                            var geaendertVon = lastCol >= 18 ? ws.Cell(r, 18).GetString() : "";

                            result.Add(new Models.MaterialItem
                            {
                                MaterialArt = matArt,
                                Legierung = leg,
                                Oberflaeche = ober,
                                Guete = guete,
                                Form = form,
                                Staerke = sta,
                                Mass = mass,
                                Stueckzahl = stueckzahl,
                                Restnummer = rest,
                                Datum = datum,
                                Lagerort = lager,
                                AenderungsDatum = aenderungsDatum,
                                AuftragNr = auftragNr,
                                Lieferant = lieferant,
                                LieferscheinNr = lieferscheinNr,
                                PreisProKg = preisProKg,
                                AngelegtVon = angelegtVon,
                                GeaendertVon = geaendertVon
                            });
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[ExcelService.Import] Erfolgreich geladen nach {attempt + 1} Versuch(en): {result.Count} Materialien");
                    return result;
                }
                catch (IOException ex) when (attempt < MAX_RETRY_ATTEMPTS - 1)
                {
                    System.Diagnostics.Debug.WriteLine($"[ExcelService.Import] Versuch {attempt + 1}/{MAX_RETRY_ATTEMPTS} fehlgeschlagen: {ex.Message}");
                    Thread.Sleep(RETRY_DELAY_MS);
                }
                finally
                {
                    if (wb != null)
                    {
                        wb.Dispose();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }
            }

            throw new IOException($"Konnte Datei nach {MAX_RETRY_ATTEMPTS} Versuchen nicht laden: {filePath}");
        }
    }
}
