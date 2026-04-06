using GeoArbeitsvorbereitung.Models;

namespace GeoArbeitsvorbereitung.Services;

public interface ISettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
}
