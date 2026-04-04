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

        public static string FindKantzeichnungPdf(string originalPdfPfad)
        {
            if (string.IsNullOrWhiteSpace(originalPdfPfad))
                return string.Empty;

            var drawNo = ExtractDrawingNumber(Path.GetFileNameWithoutExtension(originalPdfPfad));
            if (string.IsNullOrWhiteSpace(drawNo))
                return string.Empty;

            var drawNoNorm = NormalizeToken(drawNo);
            if (string.IsNullOrWhiteSpace(drawNoNorm))
                return string.Empty;

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

                    if (!nameNorm.Contains(drawNoNorm, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var score = 100;
                    if (nameNorm.Contains("KANT", StringComparison.OrdinalIgnoreCase) ||
                        nameNorm.Contains("BIEG", StringComparison.OrdinalIgnoreCase))
                        score += 50;

                    if (nameNorm.Equals(drawNoNorm, StringComparison.OrdinalIgnoreCase))
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

        private static string ExtractDrawingNumber(string fileNameWithoutExtension)
        {
            if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
                return string.Empty;

            var cleaned = fileNameWithoutExtension.Trim();
            var match = Regex.Match(cleaned, @"[A-Za-z0-9][A-Za-z0-9\-_/\.]{3,}");
            return match.Success ? match.Value : cleaned;
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
    }
}
