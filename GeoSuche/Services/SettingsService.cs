using System.IO;
using System.Text.Json;
using GeoArbeitsvorbereitung.Models;

namespace GeoArbeitsvorbereitung.Services;

public class SettingsService : ISettingsService
{
    private static readonly string SettingsPath =
        Path.Combine(AppContext.BaseDirectory, "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (JsonException) { /* return defaults on parse error */ }
        catch (IOException) { /* return defaults if file is inaccessible */ }
        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}
