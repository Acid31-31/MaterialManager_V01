using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using GeoArbeitsvorbereitung.Models;

namespace GeoArbeitsvorbereitung.Services;

public class GeoFileService : IGeoFileService
{
    private static readonly Regex QuantityPattern = new(@"^(\d+)x$", RegexOptions.IgnoreCase);
    private static readonly Regex MaterialPattern = new(@"^(V2a|ST)-\d+(?:[\.,]\d+)?mm$", RegexOptions.IgnoreCase);
    private static readonly Regex MaterialPartsPattern = new(@"^(?<mat>V2a|ST)-(?<thick>\d+(?:[\.,]\d+)?)mm$", RegexOptions.IgnoreCase);

    public IReadOnlyList<GeoFileInfo> FindAll(string searchRoot, string searchTerm)
    {
        if (!Directory.Exists(searchRoot))
            throw new DirectoryNotFoundException($"Suchordner nicht gefunden: {searchRoot}");

        var term = searchTerm.Trim();
        var results = new List<GeoFileInfo>();

        foreach (var filePath in Directory.EnumerateFiles(searchRoot, "*.geo", SearchOption.AllDirectories))
        {
            GeoFileInfo info;
            try
            {
                info = ParseFile(filePath);
            }
            catch (UnauthorizedAccessException) { continue; }
            catch (IOException) { continue; }

            if (!MatchesSearchTerm(info, term))
                continue;

            results.Add(info);
        }

        return results
            .OrderByDescending(f => f.LastWriteTime)
            .ThenBy(f => f.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public GeoFileInfo? FindNewest(string searchRoot, string searchTerm)
    {
        return FindAll(searchRoot, searchTerm).FirstOrDefault();
    }

    public string BuildNewFileName(GeoFileInfo source, int newQuantity)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(source.FileName);
        var segments = nameWithoutExt.Split('_');

        // Replace the last segment that matches the Nx quantity pattern
        for (int i = segments.Length - 1; i >= 0; i--)
        {
            if (QuantityPattern.IsMatch(segments[i]))
            {
                segments[i] = $"{newQuantity}x";
                break;
            }
        }

        return string.Join("_", segments) + ".geo";
    }

    public string BuildTargetPath(string outputRoot, GeoFileInfo source, string newFileName)
    {
        if (!Directory.Exists(outputRoot))
            throw new DirectoryNotFoundException($"Speicherordner nicht gefunden: {outputRoot}");

        var materialToken = source.Material?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(materialToken) || !MaterialPattern.IsMatch(materialToken))
            throw new InvalidOperationException(
                "Ungültiges oder fehlendes Material im Dateinamen. Erwartet z. B. 'V2a-1,5mm' oder 'ST-5,0mm'.");

        var targetFolder = ResolveAutonomousMaterialFolder(outputRoot, materialToken)
            ?? ResolveLegacyMaterialFolder(outputRoot, materialToken);

        if (string.IsNullOrWhiteSpace(targetFolder))
            throw new DirectoryNotFoundException(
                $"Kein passender Material-/Stärken-Ordner für '{materialToken}' unter '{outputRoot}' gefunden.");

        return Path.Combine(targetFolder, newFileName);
    }

    public void CopyFile(string sourcePath, string targetPath)
    {
        var dir = Path.GetDirectoryName(targetPath)
                  ?? throw new InvalidOperationException($"Ungültiger Zielpfad: {targetPath}");

        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"Zielordner existiert nicht: {dir}");

        File.Copy(sourcePath, targetPath, overwrite: false);
    }

    private static string? ResolveLegacyMaterialFolder(string outputRoot, string materialToken)
    {
        var directMatch = Path.Combine(outputRoot, materialToken);
        if (Directory.Exists(directMatch))
            return directMatch;

        return Directory
            .EnumerateDirectories(outputRoot, "*", SearchOption.AllDirectories)
            .Where(d => string.Equals(Path.GetFileName(d), materialToken, StringComparison.OrdinalIgnoreCase))
            .OrderBy(d => d.Length)
            .ThenBy(d => d, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static string? ResolveAutonomousMaterialFolder(string outputRoot, string materialToken)
    {
        var match = MaterialPartsPattern.Match(materialToken);
        if (!match.Success)
            return null;

        var materialMain = match.Groups["mat"].Value.Trim();
        var thicknessRaw = match.Groups["thick"].Value.Trim();
        var thicknessNames = BuildThicknessNameCandidates(thicknessRaw);

        var allDirs = Directory.EnumerateDirectories(outputRoot, "*", SearchOption.AllDirectories).ToList();
        var mainMaterialFolders = allDirs
            .Where(d => string.Equals(Path.GetFileName(d), materialMain, StringComparison.OrdinalIgnoreCase)
                        || Path.GetFileName(d).StartsWith(materialMain + "-", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var thicknessFolders = allDirs
            .Where(d => thicknessNames.Any(t => string.Equals(Path.GetFileName(d), t, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var withinMaterial = thicknessFolders
            .Where(t => mainMaterialFolders.Any(m => IsSubPathOf(t, m)))
            .OrderBy(t => t.Length)
            .ThenBy(t => t, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(withinMaterial))
            return withinMaterial;

        return thicknessFolders
            .OrderBy(t => t.Length)
            .ThenBy(t => t, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static bool IsSubPathOf(string path, string parent)
    {
        var p = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var root = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        return p.StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    private static HashSet<string> BuildThicknessNameCandidates(string raw)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = raw.Replace(',', '.');

        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var mmValue))
        {
            var sInvariant = mmValue.ToString("0.###", CultureInfo.InvariantCulture);
            var sGerman = mmValue.ToString("0.###", CultureInfo.GetCultureInfo("de-DE"));
            candidates.Add($"{sInvariant}mm");
            candidates.Add($"{sGerman}mm");

            var compact = sInvariant.Replace(".", string.Empty);
            if (!string.IsNullOrWhiteSpace(compact))
                candidates.Add($"{compact}mm");

            if (decimal.Truncate(mmValue) == mmValue)
            {
                var intValue = ((int)mmValue).ToString(CultureInfo.InvariantCulture);
                candidates.Add($"{intValue}mm");
            }
        }

        candidates.Add($"{raw}mm");
        candidates.Add($"{raw.Replace('.', ',')}mm");
        candidates.Add($"{raw.Replace(',', '.')}mm");

        return candidates;
    }

    private static bool MatchesSearchTerm(GeoFileInfo info, string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return true;

        if (info.Segments.Any(s => string.Equals(s, term, StringComparison.OrdinalIgnoreCase)))
            return true;

        return info.FileName.Contains(term, StringComparison.OrdinalIgnoreCase)
            || info.FullPath.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private static GeoFileInfo ParseFile(string fullPath)
    {
        var fileName = Path.GetFileName(fullPath);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var segments = nameWithoutExt.Split('_');

        var fi = new FileInfo(fullPath);
        return new GeoFileInfo
        {
            FileName = fileName,
            FullPath = fullPath,
            LastWriteTime = fi.LastWriteTime,
            Segments = segments,
        };
    }
}
