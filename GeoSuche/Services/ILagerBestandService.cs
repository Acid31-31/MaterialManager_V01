using GeoArbeitsvorbereitung.Models;

namespace GeoArbeitsvorbereitung.Services;

public interface ILagerBestandService
{
    IReadOnlyList<LagerBestandItem> SucheNachZeichnungsnummer(string excelPfad, string suchbegriff);
}
