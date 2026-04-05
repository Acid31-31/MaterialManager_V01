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

            var searchTokens = ExtractSearchTokens(Path.GetFileNameWithoutExtension(originalPdfPfad));
            if (searchTokens.Count == 0)
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
    }
}
