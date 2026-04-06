namespace GeoArbeitsvorbereitung.Models;

public class LagerBestandItem
{
    public string Zeichnungsnummer { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Bestand { get; set; } = string.Empty;
    public string Status { get; set; } = "Nicht vorhanden";
}
