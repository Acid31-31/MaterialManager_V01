using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MaterialManager_V01.Services
{
    public static class KantzeichnungPdfService
    {
        private static readonly string SettingsPath = Path.Combine(PathService.DataDirectory, "kundenmaterial.settings.json");
        private static readonly string KundenMaterialStorePath = Path.Combine(PathService.DataDirectory, "kundenmaterial.json");

        public static string FindKantzeichnungPdf(string originalPdfPfad)
        {
            if (string.IsNullOrWhiteSpace(originalPdfPfad))
                return string.Empty;

            var searchTokens = ExtractSearchTokens(Path.GetFileNameWithoutExtension(originalPdfPfad));
            if (searchTokens.Count == 0)
                return string.Empty;

            var fromStore = FindInKundenMaterialStore(searchTokens);
            if (!string.IsNullOrWhiteSpace(fromStore))
                return fromStore;

            var folders = LoadCustomerPdfFolders();
            if (folders.Count == 0)
                return string.Empty;

            var candidates = new List<(string Path, int Score)>();
            foreach (var folder in folders)
            {
                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(folder, "*.pdf", SearchOption.AllDirectories);
                }
                catch
                {
                    continue;
                }

                foreach (var file in files)
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    var nameNorm = NormalizeToken(name);
                    if (string.IsNullOrWhiteSpace(nameNorm))
                        continue;

                    var matchingToken = searchTokens
                        .Where(t => nameNorm.Contains(t, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(t => t.Length)
                        .FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(matchingToken))
                        continue;

                    var score = 100 + matchingToken.Length;
                    if (nameNorm.Contains("KANT", StringComparison.OrdinalIgnoreCase) ||
                        nameNorm.Contains("BIEG", StringComparison.OrdinalIgnoreCase) ||
                        nameNorm.Contains("ABKANT", StringComparison.OrdinalIgnoreCase))
                        score += 50;

                    if (nameNorm.Equals(matchingToken, StringComparison.OrdinalIgnoreCase))
                        score += 25;

                    candidates.Add((file, score));
                }
            }

            return candidates
                .OrderByDescending(c => c.Score)
                .ThenBy(c => c.Path.Length)
                .Select(c => c.Path)
                .FirstOrDefault() ?? string.Empty;
        }

        private static string FindInKundenMaterialStore(List<string> searchTokens)
        {
            try
            {
                if (!File.Exists(KundenMaterialStorePath))
                    return string.Empty;

                var json = File.ReadAllText(KundenMaterialStorePath);
                var items = JsonSerializer.Deserialize<List<KundenMaterialItemDto>>(json);
                if (items == null || items.Count == 0)
                    return string.Empty;

                var candidate = items
                    .Where(i => !string.IsNullOrWhiteSpace(i.PdfPfad) && File.Exists(i.PdfPfad))
                    .Select(i => new
                    {
                        Item = i,
                        ZeichnungNorm = NormalizeToken(i.Zeichnungsnummer),
                        PdfNameNorm = NormalizeToken(Path.GetFileNameWithoutExtension(i.PdfPfad ?? string.Empty))
                    })
                    .Select(x => new
                    {
                        x.Item,
                        Score = searchTokens
                            .Where(t => (!string.IsNullOrWhiteSpace(x.ZeichnungNorm) && x.ZeichnungNorm.Contains(t, StringComparison.OrdinalIgnoreCase))
                                     || (!string.IsNullOrWhiteSpace(x.PdfNameNorm) && x.PdfNameNorm.Contains(t, StringComparison.OrdinalIgnoreCase)))
                            .OrderByDescending(t => t.Length)
                            .Select(t => t.Length)
                            .FirstOrDefault()
                    })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .Select(x => x.Item.PdfPfad)
                    .FirstOrDefault();

                return candidate ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static List<string> ExtractSearchTokens(string fileNameWithoutExtension)
        {
            if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
                return new List<string>();

            var parts = Regex.Split(fileNameWithoutExtension, @"[^A-Za-z0-9]+")
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(NormalizeToken)
                .Where(p => p.Length >= 4)
                .ToList();

            var withDigits = parts.Where(p => p.Any(char.IsDigit)).OrderByDescending(p => p.Length).ToList();
            if (withDigits.Count > 0)
                return withDigits.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            return parts.OrderByDescending(p => p.Length).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string NormalizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return new string(value
                .ToUpperInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
        }

        private static List<string> LoadCustomerPdfFolders()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new List<string>();

                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<KundenMaterialSettingsDto>(json);
                if (settings == null)
                    return new List<string>();

                var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (!string.IsNullOrWhiteSpace(settings.PdfFolder) && Directory.Exists(settings.PdfFolder))
                    folders.Add(settings.PdfFolder.Trim());

                if (settings.CustomerFolders != null)
                {
                    foreach (var kv in settings.CustomerFolders)
                    {
                        if (!string.IsNullOrWhiteSpace(kv.Value) && Directory.Exists(kv.Value))
                            folders.Add(kv.Value.Trim());
                    }
                }

                return folders.ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        private sealed class KundenMaterialSettingsDto
        {
            public string PdfFolder { get; set; } = string.Empty;
            public Dictionary<string, string> CustomerFolders { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class KundenMaterialItemDto
        {
            public string Zeichnungsnummer { get; set; } = string.Empty;
            public string PdfPfad { get; set; } = string.Empty;
        }
    }
}
