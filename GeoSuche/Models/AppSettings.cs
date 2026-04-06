namespace GeoArbeitsvorbereitung.Models;

public class AppSettings
{
    public string SearchRoot { get; set; } = string.Empty;
    public string OutputRoot { get; set; } = string.Empty;
    public List<string> SearchFolderOptions { get; set; } = new();
    public List<string> OutputFolderOptions { get; set; } = new();
    public string LagerBestandExcelPath { get; set; } = string.Empty;
}
