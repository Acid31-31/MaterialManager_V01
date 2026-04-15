using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using MaterialManager_V01.Models;

namespace MaterialManager_V01.Services
{
    public static class AuftragArchivService
    {
        private const string ArchiveMetaFileName = "archiv_meta.json";

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

                var kwOrdner = GetKwFolderPath(jahr, kalenderWoche);
                Directory.CreateDirectory(kwOrdner);

                var materialien = MaterialDataService.LoadAllMaterials()
                    .Where(m => string.Equals(m.AuftragNr, auftrag.Auftragsnummer, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var pdfQuellen = CollectPdfPaths(auftrag, materialien);
                var kopiertePdf = 0;
                foreach (var quelle in pdfQuellen)
                {
                    var archivPfad = TryArchivePdfForOrder(auftrag.Auftragsnummer, quelle, kalenderWoche, jahr);
                    if (!string.IsNullOrWhiteSpace(archivPfad))
                        kopiertePdf++;
                }

                UpsertArchiveMetadata(new ArchivAuftragMeta
                {
                    Jahr = jahr,
                    Kalenderwoche = kalenderWoche,
                    Auftragsnummer = auftrag.Auftragsnummer,
                    ArchiviertAm = DateTime.Now,
                    MaterialPositionen = auftrag.MaterialPositionen > 0 ? auftrag.MaterialPositionen : materialien.Count,
                    GesamtStueckzahl = auftrag.GesamtStueckzahl > 0 ? auftrag.GesamtStueckzahl : materialien.Sum(m => m.Stueckzahl),
                    GesamtGewichtKg = auftrag.GesamtGewichtKg > 0 ? auftrag.GesamtGewichtKg : Math.Round(materialien.Sum(m => m.GewichtKg), 2),
                    AngelegtVon = auftrag.AngelegtVon ?? string.Empty,
                    GeaendertVon = auftrag.GeaendertVon ?? string.Empty,
                    ProduktionStartDatum = auftrag.ProduktionStartDatum,
                    ProduktionEndDatum = auftrag.ProduktionEndDatum
                });

                AuditLogService.LogAction(
                    OperatorIdentityService.CurrentOperatorName,
                    "ARCHIVE",
                    "Auftrag",
                    auftrag.Auftragsnummer,
                    oldValue: auftrag.Status.ToString(),
                    newValue: $"Archiviert in KW {kalenderWoche:D2}/{jahr}, PDFs: {kopiertePdf}",
                    reason: "Auftrag abgeschlossen und archiviert");

                return (true, $"Archivierung abgeschlossen: {kopiertePdf} PDF-Datei(en) im KW-Ordner bereitgestellt.");
            }
            catch (Exception ex)
            {
                return (false, $"Archivierung fehlgeschlagen: {ex.Message}");
            }
        }

        public static string ResolveAccessiblePdfPath(string? auftragsnummer, string? currentPdfPath)
        {
            if (!string.IsNullOrWhiteSpace(currentPdfPath) && File.Exists(currentPdfPath))
                return currentPdfPath;

            return TryFindPdfForAuftrag(auftragsnummer) ?? (currentPdfPath ?? string.Empty);
        }

        public static string? TryFindPdfForAuftrag(string? auftragsnummer)
        {
            var tokens = BuildPdfSearchTokens(auftragsnummer);
            if (tokens.Count == 0)
                return null;

            var basisPfad = ResolveArchiveBasePath();
            if (string.IsNullOrWhiteSpace(basisPfad) || !Directory.Exists(basisPfad))
                return null;

            try
            {
                var searchRoot = Path.Combine(basisPfad, DateTime.Now.Year.ToString());
                if (!Directory.Exists(searchRoot))
                    searchRoot = basisPfad;

                return Directory
                    .EnumerateFiles(searchRoot, "*.pdf", SearchOption.AllDirectories)
                    .Select(path => new
                    {
                        Path = path,
                        Name = Path.GetFileNameWithoutExtension(path) ?? string.Empty
                    })
                    .Select(x => new
                    {
                        x.Path,
                        x.Name,
                        Score = GetPdfMatchScore(x.Name, tokens)
                    })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .ThenBy(x => x.Name.Length)
                    .Select(x => x.Path)
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        public static string? TryArchivePdfForOrder(string? auftragsnummer, string? sourcePdfPath, int kalenderWoche, int jahr)
        {
            if (string.IsNullOrWhiteSpace(auftragsnummer) || string.IsNullOrWhiteSpace(sourcePdfPath) || !File.Exists(sourcePdfPath))
                return null;

            var kwOrdner = GetKwFolderPath(jahr, kalenderWoche);
            Directory.CreateDirectory(kwOrdner);

            var sourceFullPath = Path.GetFullPath(sourcePdfPath);
            var sourceDir = Path.GetDirectoryName(sourceFullPath) ?? string.Empty;
            if (string.Equals(sourceDir.TrimEnd(Path.DirectorySeparatorChar), kwOrdner.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                return sourceFullPath;

            var archivedFileName = BuildArchivedPdfFileName(auftragsnummer, sourceFullPath);
            var targetPath = GetUniqueTargetPath(Path.Combine(kwOrdner, archivedFileName));
            File.Copy(sourceFullPath, targetPath, overwrite: false);
            return targetPath;
        }

        public static List<ArchivAuftragEintrag> GetArchivedOrdersForYear(int jahr)
        {
            var basisPfad = ResolveArchiveBasePath();
            if (string.IsNullOrWhiteSpace(basisPfad))
                return new List<ArchivAuftragEintrag>();

            var yearFolder = Path.Combine(basisPfad, jahr.ToString());
            if (!Directory.Exists(yearFolder))
                return new List<ArchivAuftragEintrag>();

            var result = new List<ArchivAuftragEintrag>();
            foreach (var kwFolder in Directory.EnumerateDirectories(yearFolder, "KW_*", SearchOption.TopDirectoryOnly))
            {
                var kwText = Path.GetFileName(kwFolder);
                var kwNumber = 0;
                if (kwText?.StartsWith("KW_", StringComparison.OrdinalIgnoreCase) == true)
                    int.TryParse(kwText.Substring(3), out kwNumber);

                result.AddRange(GetArchivedOrdersForWeek(jahr, kwNumber));
            }

            return result
                .OrderByDescending(x => x.ArchiviertAm)
                .ToList();
        }

        public static List<ArchivAuftragEintrag> GetArchivedOrdersForWeek(int jahr, int kalenderWoche)
        {
            var basisPfad = ResolveArchiveBasePath();
            if (string.IsNullOrWhiteSpace(basisPfad))
                return new List<ArchivAuftragEintrag>();

            var kwFolder = GetKwFolderPath(jahr, kalenderWoche);
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
                DateTime? produktionStart = null;
                DateTime? produktionEnde = null;
                try
                {
                    if (File.Exists(auftragJson))
                    {
                        using var doc = JsonDocument.Parse(File.ReadAllText(auftragJson));
                        if (doc.RootElement.TryGetProperty("Auftrag", out var orderElement))
                        {
                            if (orderElement.TryGetProperty("Auftragsnummer", out var numberElement))
                                orderNumber = numberElement.GetString() ?? orderNumber;

                            if (orderElement.TryGetProperty("ProduktionStartDatum", out var startElement)
                                && startElement.ValueKind != JsonValueKind.Null
                                && startElement.TryGetDateTime(out var startDt))
                                produktionStart = startDt;

                            if (orderElement.TryGetProperty("ProduktionEndDatum", out var endElement)
                                && endElement.ValueKind != JsonValueKind.Null
                                && endElement.TryGetDateTime(out var endDt))
                                produktionEnde = endDt;
                        }
                    }
                }
                catch
                {
                }

                var meta = TryGetArchiveMetadata(jahr, kalenderWoche, orderNumber);

                result.Add(new ArchivAuftragEintrag
                {
                    Jahr = jahr,
                    Kalenderwoche = kalenderWoche,
                    Auftragsnummer = orderNumber,
                    OrdnerPfad = auftragFolder,
                    AuftragJsonPfad = auftragJson,
                    ErstePdfPfad = pdfFiles.FirstOrDefault() ?? string.Empty,
                    PdfAnzahl = pdfFiles.Count,
                    ArchiviertAm = meta?.ArchiviertAm ?? Directory.GetLastWriteTime(auftragFolder),
                    ProduktionStartDatum = meta?.ProduktionStartDatum ?? produktionStart,
                    ProduktionEndDatum = meta?.ProduktionEndDatum ?? produktionEnde,
                    MaterialPositionen = meta?.MaterialPositionen ?? 0,
                    GesamtStueckzahl = meta?.GesamtStueckzahl ?? 0,
                    GesamtGewichtKg = meta?.GesamtGewichtKg ?? 0,
                    AngelegtVon = meta?.AngelegtVon ?? string.Empty,
                    GeaendertVon = meta?.GeaendertVon ?? string.Empty
                });
            }

            var flatPdfGroups = Directory.EnumerateFiles(kwFolder, "*.pdf", SearchOption.TopDirectoryOnly)
                .GroupBy(path => ExtractOrderNumberFromArchivedPdf(Path.GetFileNameWithoutExtension(path) ?? string.Empty), StringComparer.OrdinalIgnoreCase)
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .ToList();

            foreach (var group in flatPdfGroups)
            {
                var files = group.ToList();
                var existing = result.FirstOrDefault(x => string.Equals(x.Auftragsnummer, group.Key, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.ErstePdfPfad = string.IsNullOrWhiteSpace(existing.ErstePdfPfad) ? files[0] : existing.ErstePdfPfad;
                    existing.PdfAnzahl += files.Count;
                    existing.ArchiviertAm = files.Max(File.GetLastWriteTime);
                    continue;
                }

                var meta = TryGetArchiveMetadata(jahr, kalenderWoche, group.Key);
                result.Add(new ArchivAuftragEintrag
                {
                    Jahr = jahr,
                    Kalenderwoche = kalenderWoche,
                    Auftragsnummer = group.Key,
                    OrdnerPfad = kwFolder,
                    AuftragJsonPfad = string.Empty,
                    ErstePdfPfad = files.FirstOrDefault() ?? string.Empty,
                    PdfAnzahl = files.Count,
                    ArchiviertAm = meta?.ArchiviertAm ?? files.Max(File.GetLastWriteTime),
                    ProduktionStartDatum = meta?.ProduktionStartDatum,
                    ProduktionEndDatum = meta?.ProduktionEndDatum,
                    MaterialPositionen = meta?.MaterialPositionen ?? 0,
                    GesamtStueckzahl = meta?.GesamtStueckzahl ?? 0,
                    GesamtGewichtKg = meta?.GesamtGewichtKg ?? 0,
                    AngelegtVon = meta?.AngelegtVon ?? string.Empty,
                    GeaendertVon = meta?.GeaendertVon ?? string.Empty
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

        private static string GetKwFolderPath(int jahr, int kalenderWoche)
        {
            return Path.Combine(ResolveArchiveBasePath(), jahr.ToString(), $"KW_{kalenderWoche:D2}");
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

        private static string BuildArchivedPdfFileName(string auftragsnummer, string sourcePdfPath)
        {
            var prefix = SanitizePathSegment((auftragsnummer ?? string.Empty).Trim());
            var originalName = SanitizePathSegment(Path.GetFileName(sourcePdfPath));
            return originalName.StartsWith(prefix + "__", StringComparison.OrdinalIgnoreCase)
                ? originalName
                : $"{prefix}__{originalName}";
        }

        private static string ExtractOrderNumberFromArchivedPdf(string fileNameWithoutExtension)
        {
            var markerIndex = fileNameWithoutExtension.IndexOf("__", StringComparison.OrdinalIgnoreCase);
            return markerIndex > 0 ? fileNameWithoutExtension[..markerIndex] : string.Empty;
        }

        private static List<string> BuildPdfSearchTokens(string? auftragsnummer)
        {
            var raw = (auftragsnummer ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return new List<string>();

            var withoutLst = Regex.Replace(raw, @"(?:[\s\-_]+)?LST$", string.Empty, RegexOptions.IgnoreCase).Trim().Trim('-', '_');
            var parts = Regex.Split(withoutLst, @"[\s\-_]+")
                .Where(p => !string.IsNullOrWhiteSpace(p) && !string.Equals(p, "LST", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var tokens = new List<string> { raw, withoutLst };
            if (parts.Count > 0)
                tokens.Add(parts[^1]);
            if (parts.Count > 1)
                tokens.Add(string.Join(string.Empty, parts.Skip(1)));

            return tokens
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static int GetPdfMatchScore(string fileNameWithoutExtension, List<string> tokens)
        {
            var normalizedFileName = NormalizeSearchText(fileNameWithoutExtension);
            var bestScore = 0;

            foreach (var token in tokens)
            {
                var normalizedToken = NormalizeSearchText(token);
                if (string.IsNullOrWhiteSpace(normalizedToken))
                    continue;

                if (string.Equals(normalizedFileName, normalizedToken, StringComparison.OrdinalIgnoreCase))
                    bestScore = Math.Max(bestScore, 500);
                else if (normalizedFileName.StartsWith(normalizedToken, StringComparison.OrdinalIgnoreCase))
                    bestScore = Math.Max(bestScore, 400);
                else if (normalizedFileName.Contains(normalizedToken, StringComparison.OrdinalIgnoreCase))
                    bestScore = Math.Max(bestScore, 300);
            }

            return bestScore;
        }

        private static string NormalizeSearchText(string value)
        {
            return new string((value ?? string.Empty)
                .Where(char.IsLetterOrDigit)
                .ToArray())
                .ToUpperInvariant();
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
                }
            }
        }

        private static string SanitizePathSegment(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "Auftrag" : cleaned;
        }

        private static string GetArchiveMetaFilePath()
        {
            return Path.Combine(ResolveArchiveBasePath(), ArchiveMetaFileName);
        }

        private static List<ArchivAuftragMeta> LoadArchiveMetadata()
        {
            try
            {
                var path = GetArchiveMetaFilePath();
                if (!File.Exists(path))
                    return new List<ArchivAuftragMeta>();

                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<ArchivAuftragMeta>>(json) ?? new List<ArchivAuftragMeta>();
            }
            catch
            {
                return new List<ArchivAuftragMeta>();
            }
        }

        private static void SaveArchiveMetadata(List<ArchivAuftragMeta> list)
        {
            try
            {
                var path = GetArchiveMetaFilePath();
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch
            {
            }
        }

        private static void UpsertArchiveMetadata(ArchivAuftragMeta meta)
        {
            var list = LoadArchiveMetadata();
            var idx = list.FindIndex(x =>
                x.Jahr == meta.Jahr
                && x.Kalenderwoche == meta.Kalenderwoche
                && string.Equals(x.Auftragsnummer, meta.Auftragsnummer, StringComparison.OrdinalIgnoreCase));

            if (idx >= 0)
                list[idx] = meta;
            else
                list.Add(meta);

            SaveArchiveMetadata(list);
        }

        private static ArchivAuftragMeta? TryGetArchiveMetadata(int jahr, int kalenderwoche, string? auftragsnummer)
        {
            if (string.IsNullOrWhiteSpace(auftragsnummer))
                return null;

            var list = LoadArchiveMetadata();
            return list
                .Where(x => x.Jahr == jahr && x.Kalenderwoche == kalenderwoche)
                .OrderByDescending(x => x.ArchiviertAm)
                .FirstOrDefault(x => string.Equals(x.Auftragsnummer, auftragsnummer, StringComparison.OrdinalIgnoreCase));
        }
    }

    public sealed class ArchivAuftragEintrag
    {
        public int Jahr { get; set; }
        public int Kalenderwoche { get; set; }
        public string Auftragsnummer { get; set; } = string.Empty;
        public string OrdnerPfad { get; set; } = string.Empty;
        public string AuftragJsonPfad { get; set; } = string.Empty;
        public string ErstePdfPfad { get; set; } = string.Empty;
        public int PdfAnzahl { get; set; }
        public DateTime ArchiviertAm { get; set; }
        public DateTime? ProduktionStartDatum { get; set; }
        public DateTime? ProduktionEndDatum { get; set; }
        public int MaterialPositionen { get; set; }
        public int GesamtStueckzahl { get; set; }
        public double GesamtGewichtKg { get; set; }
        public string AngelegtVon { get; set; } = string.Empty;
        public string GeaendertVon { get; set; } = string.Empty;
        public string ProduktionStartText => ProduktionStartDatum?.ToString("dd.MM.yyyy HH:mm") ?? "–";
        public string ProduktionEndText => ProduktionEndDatum?.ToString("dd.MM.yyyy HH:mm") ?? "–";
        public string ProduktionsDauer
        {
            get
            {
                if (ProduktionStartDatum == null || ProduktionEndDatum == null)
                    return "–";

                var diff = ProduktionEndDatum.Value - ProduktionStartDatum.Value;
                if (diff.TotalHours >= 1)
                    return $"{(int)diff.TotalHours}h {diff.Minutes}min";
                return $"{Math.Max(0, diff.Minutes)}min";
            }
        }
    }

    public sealed class ArchivAuftragMeta
    {
        public int Jahr { get; set; }
        public int Kalenderwoche { get; set; }
        public string Auftragsnummer { get; set; } = string.Empty;
        public DateTime ArchiviertAm { get; set; }
        public int MaterialPositionen { get; set; }
        public int GesamtStueckzahl { get; set; }
        public double GesamtGewichtKg { get; set; }
        public string AngelegtVon { get; set; } = string.Empty;
        public string GeaendertVon { get; set; } = string.Empty;
        public DateTime? ProduktionStartDatum { get; set; }
        public DateTime? ProduktionEndDatum { get; set; }
    }
}
