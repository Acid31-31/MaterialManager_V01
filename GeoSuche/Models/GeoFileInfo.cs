using System.Text.RegularExpressions;

namespace GeoArbeitsvorbereitung.Models;

public class GeoFileInfo
{
    private static readonly Regex MaterialPattern = new(@"^(V2a|ST)-\d+(?:[\.,]\d+)?mm$", RegexOptions.IgnoreCase);

    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public DateTime LastWriteTime { get; set; }
    public string[] Segments { get; set; } = [];

    /// <summary>Segment 2 (index 1): Zeichnungsnummer</summary>
    public string DrawingNumber => Segments.Length > 1 ? Segments[1] : string.Empty;

    /// <summary>
    /// Material token in filename segments, e.g. V2a-1,5mm or ST-5,0mm.
    /// </summary>
    public string Material => Segments.FirstOrDefault(s => MaterialPattern.IsMatch(s)) ?? string.Empty;
}
