using GeoArbeitsvorbereitung.Models;

namespace GeoArbeitsvorbereitung.Services;

public interface IGeoFileService
{
    /// <summary>
    /// Searches <paramref name="searchRoot"/> recursively for *.geo files.
    /// If <paramref name="searchTerm"/> is set, file must match it (case-insensitive).
    /// Returns all matches sorted by LastWriteTime (newest first).
    /// </summary>
    IReadOnlyList<GeoFileInfo> FindAll(string searchRoot, string searchTerm);

    /// <summary>
    /// Returns the newest matching GEO file, or null if none found.
    /// </summary>
    GeoFileInfo? FindNewest(string searchRoot, string searchTerm);

    /// <summary>
    /// Returns the new filename with the last quantity segment (e.g. "3x") replaced by "{newQuantity}x".
    /// </summary>
    string BuildNewFileName(GeoFileInfo source, int newQuantity);

    /// <summary>
    /// Returns the full target path inside an existing material folder under outputRoot.
    /// </summary>
    string BuildTargetPath(string outputRoot, GeoFileInfo source, string newFileName);

    /// <summary>
    /// Copies the source file to targetPath.
    /// Never overwrites and never creates directories.
    /// </summary>
    void CopyFile(string sourcePath, string targetPath);
}
