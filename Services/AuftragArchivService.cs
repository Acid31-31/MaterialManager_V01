using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MaterialManager_V01.Models;

namespace MaterialManager_V01.Services
{
    public static class AuftragArchivService
    {
        public static (bool Success, string Message) ArchiveCompletedOrder(Auftrag auftrag, int kalenderWoche, int jahr)
        {
            if (auftrag == null || string.IsNullOrWhiteSpace(auftrag.Auftragsnummer))
                return (false, "Ungültiger Auftrag.");

            var basisPfad = ResolveArchiveBasePath();
            if (string.IsNullOrWhiteSpace(basisPfad))
                return (false, "Kein Auftrags-Archivpfad verfügbar.");

            try
            {
                if (!Directory.Exists(basisPfad))
                    Directory.CreateDirectory(basisPfad);

                CleanupArchivesOlderThan12Months(basisPfad);

                var safeAuftrag = SanitizePathSegment(auftrag.Auftragsnummer);
                var zielOrdner = Path.Combine(basisPfad, jahr.ToString(), $"KW_{kalenderWoche:D2}", safeAuftrag);
                var pdfOrdner = Path.Combine(zielOrdner, "PDF");
                Directory.CreateDirectory(zielOrdner);
                Directory.CreateDirectory(pdfOrdner);

                var materialien = MaterialDataService.LoadAllMaterials()
                    .Where(m => string.Equals(m.AuftragNr, auftrag.Auftragsnummer, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var archivDatensatz = new
                {
                    Auftrag = new
                    {
                        auftrag.Id,
                        auftrag.Auftragsnummer,
                        Status = auftrag.Status.ToString(),
                        auftrag.ErstelltAm,
                        auftrag.GeaendertAm,
                        auftrag.AngelegtVon,
                        auftrag.GeaendertVon,
                        auftrag.MaterialPositionen,
                        auftrag.GesamtStueckzahl,
                        auftrag.GesamtGewichtKg,
                        auftrag.ProduktionStartDatum,
                        auftrag.ProduktionEndDatum,
                        ProduktionsDauer = auftrag.ProduktionsDauer
                    },
                    ArchivInfo = new
                    {
                        ArchiviertAm = DateTime.Now,
                        ArchiviertVon = OperatorIdentityService.CurrentOperatorName,
                        Jahr = jahr,
                        KalenderWoche = kalenderWoche
                    },
                    Materialien = materialien.Select(m => new
                    {
                        m.MaterialArt,
                        m.Legierung,
                        m.Form,
                        m.Staerke,
                        m.Mass,
                        m.Stueckzahl,
                        m.Lagerort,
                        m.Restnummer,
                        m.PdfPfad,
                        m.PdfPfadAngefangeneTafel
                    })
                };

                var jsonPath = Path.Combine(zielOrdner, "auftrag.json");
                File.WriteAllText(jsonPath, JsonSerializer.Serialize(archivDatensatz, new JsonSerializerOptions { WriteIndented = true }));

                var pdfQuellen = CollectPdfPaths(auftrag, materialien);
                var kopiertePdf = 0;
                foreach (var quelle in pdfQuellen)
                {
                    if (!File.Exists(quelle))
                        continue;

                    var dateiName = Path.GetFileName(quelle);
                    var zielDatei = GetUniqueTargetPath(Path.Combine(pdfOrdner, dateiName));
                    File.Copy(quelle, zielDatei, overwrite: false);
                    kopiertePdf++;
                }

                AuditLogService.LogAction(
                    OperatorIdentityService.CurrentOperatorName,
                    "ARCHIVE",
                    "Auftrag",
                    auftrag.Auftragsnummer,
                    oldValue: auftrag.Status.ToString(),
                    newValue: $"Archiviert in KW {kalenderWoche:D2}/{jahr}, PDFs: {kopiertePdf}",
                    reason: "Auftrag abgeschlossen und archiviert");

                return (true, $"Archivierung abgeschlossen: {kopiertePdf} PDF-Datei(en) kopiert.");
            }
            catch (Exception ex)
            {
                return (false, $"Archivierung fehlgeschlagen: {ex.Message}");
            }
        }

        public static List<ArchivAuftragEintrag> GetArchivedOrdersForWeek(int jahr, int kalenderWoche)
        {
            var basisPfad = ResolveArchiveBasePath();
            if (string.IsNullOrWhiteSpace(basisPfad))
                return new List<ArchivAuftragEintrag>();

            var kwFolder = Path.Combine(basisPfad, jahr.ToString(), $"KW_{kalenderWoche:D2}");
            if (!Directory.Exists(kwFolder))
                return new List<ArchivAuftragEintrag>();

            var result = new List<ArchivAuftragEintrag>();
            foreach (var auftragFolder in Directory.EnumerateDirectories(kwFolder))
            {
                var auftragJson = Path.Combine(auftragFolder, "auftrag.json");
                var pdfFolder = Path.Combine(auftragFolder, "PDF");
                var pdfFiles = Directory.Exists(pdfFolder)
                    ? Directory.EnumerateFiles(pdfFolder, "*.pdf", SearchOption.TopDirectoryOnly).ToList()
                    : new List<string>();

                var orderNumber = Path.GetFileName(auftragFolder);
                try
                {
                    if (File.Exists(auftragJson))
                    {
                        using var doc = JsonDocument.Parse(File.ReadAllText(auftragJson));
                        if (doc.RootElement.TryGetProperty("Auftrag", out var orderElement)
                            && orderElement.TryGetProperty("Auftragsnummer", out var numberElement))
                        {
                            orderNumber = numberElement.GetString() ?? orderNumber;
                        }
                    }
                }
                catch
                {
                }

                result.Add(new ArchivAuftragEintrag
                {
                    Auftragsnummer = orderNumber,
                    OrdnerPfad = auftragFolder,
                    AuftragJsonPfad = auftragJson,
                    ErstePdfPfad = pdfFiles.FirstOrDefault() ?? string.Empty,
                    PdfAnzahl = pdfFiles.Count,
                    ArchiviertAm = Directory.GetLastWriteTime(auftragFolder)
                });
            }

            return result
                .OrderByDescending(x => x.ArchiviertAm)
                .ToList();
        }

        private static string ResolveArchiveBasePath()
        {
            var basisPfad = NetzwerkService.GetAuftragsArchivBasisPfad();
            if (!string.IsNullOrWhiteSpace(basisPfad))
                return basisPfad;

            return Path.Combine(PathService.DataDirectory, "Auftragsarchiv");
        }

        private static List<string> CollectPdfPaths(Auftrag auftrag, List<MaterialItem> materialien)
        {
            var pfade = new List<string>();

            if (!string.IsNullOrWhiteSpace(auftrag.PdfPfad))
                pfade.Add(auftrag.PdfPfad);
            if (!string.IsNullOrWhiteSpace(auftrag.PdfPfadAngefangeneTafel))
                pfade.Add(auftrag.PdfPfadAngefangeneTafel);

            foreach (var item in materialien)
            {
                if (!string.IsNullOrWhiteSpace(item.PdfPfad))
                    pfade.Add(item.PdfPfad);
                if (!string.IsNullOrWhiteSpace(item.PdfPfadAngefangeneTafel))
                    pfade.Add(item.PdfPfadAngefangeneTafel);
            }

            return pfade
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string GetUniqueTargetPath(string targetPath)
        {
            if (!File.Exists(targetPath))
                return targetPath;

            var directory = Path.GetDirectoryName(targetPath) ?? string.Empty;
            var name = Path.GetFileNameWithoutExtension(targetPath);
            var ext = Path.GetExtension(targetPath);
            var index = 1;

            string candidate;
            do
            {
                candidate = Path.Combine(directory, $"{name}_{index}{ext}");
                index++;
            } while (File.Exists(candidate));

            return candidate;
        }

        private static void CleanupArchivesOlderThan12Months(string basisPfad)
        {
            var cutoff = DateTime.Now.AddMonths(-12);

            foreach (var dir in Directory.EnumerateDirectories(basisPfad, "*", SearchOption.AllDirectories))
            {
                try
                {
                    var info = new DirectoryInfo(dir);
                    if (info.LastWriteTime < cutoff && info.Name.StartsWith("KW_", StringComparison.OrdinalIgnoreCase))
                    {
                        info.Delete(recursive: true);
                    }
                }
                catch
                {
                    // bewusst ignorieren, damit Archivierung nicht abbricht
                }
            }
        }

        private static string SanitizePathSegment(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "Auftrag" : cleaned;
        }
    }

    public sealed class ArchivAuftragEintrag
    {
        public string Auftragsnummer { get; set; } = string.Empty;
        public string OrdnerPfad { get; set; } = string.Empty;
        public string AuftragJsonPfad { get; set; } = string.Empty;
        public string ErstePdfPfad { get; set; } = string.Empty;
        public int PdfAnzahl { get; set; }
        public DateTime ArchiviertAm { get; set; }
    }
}
