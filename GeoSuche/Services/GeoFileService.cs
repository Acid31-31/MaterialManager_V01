using System.IO;
using System.Text.RegularExpressions;
using GeoArbeitsvorbereitung.Models;

namespace GeoArbeitsvorbereitung.Services;

public class GeoFileService : IGeoFileService
{
    private static readonly Regex QuantityPattern = new(@"^(\d+)x$", RegexOptions.IgnoreCase);
    private static readonly Regex MaterialPattern = new(@"^(V2a|ST)-\d+(?:[\.,]\d+)?mm$", RegexOptions.IgnoreCase);

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

        var material = source.Material?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(material) || !MaterialPattern.IsMatch(material))
            throw new InvalidOperationException(
                "Ungültiges oder fehlendes Material im Dateinamen. Erwartet z. B. 'V2a-1,5mm' oder 'ST-5,0mm'.");

        var directMatch = Path.Combine(outputRoot, material);
        string targetFolder;

        if (Directory.Exists(directMatch))
        {
            targetFolder = directMatch;
        }
        else
        {
            var recursiveMatch = Directory
                .EnumerateDirectories(outputRoot, "*", SearchOption.AllDirectories)
                .Where(d => string.Equals(Path.GetFileName(d), material, StringComparison.OrdinalIgnoreCase))
                .OrderBy(d => d.Length)
                .ThenBy(d => d, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (recursiveMatch == null)
                throw new DirectoryNotFoundException(
                    $"Kein vorhandener Material-Ordner \"{material}\" unter \"{outputRoot}\" gefunden.");

            targetFolder = recursiveMatch;
        }

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
