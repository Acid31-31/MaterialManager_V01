using System;
using System.IO;
using System.Text.Json;

namespace MaterialManager_V01.Services
{
    /// <summary>
    /// Netzwerk-Service: Ermöglicht Speichern/Laden auf einem gemeinsamen Netzwerkpfad.
    /// Büro + Werkstatt können gleichzeitig arbeiten (File-Locking).
    /// </summary>
    public static class NetzwerkService
    {
        private static readonly string ConfigFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MaterialManager_V01", "netzwerk_config.json");

        private static NetzwerkConfig _config = new();

        static NetzwerkService()
        {
            LoadConfig();
        }

        public static bool IsNetzwerkModus => _config.Aktiviert && !string.IsNullOrWhiteSpace(_config.NetzwerkPfad);

        public static string NetzwerkPfad => _config.NetzwerkPfad;

        public static string GetSavePath()
        {
            if (IsNetzwerkModus)
                return Path.Combine(_config.NetzwerkPfad, "materialbestand.xlsx");

            // ✅ EINFACH: Speichere im Ordner "Excell" im Projekt-Root!
            var projectDir = AppDomain.CurrentDomain.BaseDirectory;
            var excellDir = Path.Combine(projectDir, "Excell");
            
            // Erstelle Ordner wenn nicht vorhanden
            if (!Directory.Exists(excellDir))
            {
                Directory.CreateDirectory(excellDir);
            }
            
            return Path.Combine(excellDir, "materialbestand.xlsx");
        }

        public static string GetLockFile()
        {
            if (!IsNetzwerkModus) return "";
            return Path.Combine(_config.NetzwerkPfad, ".lock");
        }

        /// <summary>
        /// Prüft ob der Netzwerkpfad erreichbar ist
        /// </summary>
        public static bool IstPfadErreichbar()
        {
            if (!IsNetzwerkModus) return true;
            try
            {
                return Directory.Exists(_config.NetzwerkPfad);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Sperrt die Datei für exklusiven Zugriff
        /// </summary>
        public static bool AcquireLock(string benutzer)
        {
            if (!IsNetzwerkModus) return true;

            var lockFile = GetLockFile();
            try
            {
                if (File.Exists(lockFile))
                {
                    var lockInfo = File.ReadAllText(lockFile);
                    var parts = lockInfo.Split('|');
                    if (parts.Length >= 2)
                    {
                        // Lock ist älter als 5 Minuten → ungültig
                        if (DateTime.TryParse(parts[1], out var lockTime) && (DateTime.Now - lockTime).TotalMinutes > 5)
                        {
                            File.Delete(lockFile);
                        }
                        else if (parts[0] != benutzer)
                        {
                            return false; // Anderer Benutzer hat Lock
                        }
                    }
                }

                File.WriteAllText(lockFile, $"{benutzer}|{DateTime.Now:O}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gibt den Lock frei
        /// </summary>
        public static void ReleaseLock()
        {
            if (!IsNetzwerkModus) return;

            var lockFile = GetLockFile();
            try
            {
                if (File.Exists(lockFile))
                    File.Delete(lockFile);
            }
            catch { }
        }

        /// <summary>
        /// Prüft wer gerade die Datei gesperrt hat
        /// </summary>
        public static string GetLockOwner()
        {
            if (!IsNetzwerkModus) return "";

            var lockFile = GetLockFile();
            try
            {
                if (File.Exists(lockFile))
                {
                    var info = File.ReadAllText(lockFile);
                    var parts = info.Split('|');
                    return parts.Length > 0 ? parts[0] : "";
                }
            }
            catch { }
            return "";
        }

        /// <summary>
        /// Letztes Änderungsdatum der Netzwerk-Datei
        /// </summary>
        public static DateTime? GetLetztesUpdate()
        {
            var path = GetSavePath();
            try
            {
                if (File.Exists(path))
                    return File.GetLastWriteTime(path);
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Aktiviert/Deaktiviert Netzwerk-Modus
        /// </summary>
        public static void SetNetzwerkModus(bool aktiviert, string pfad)
        {
            _config.Aktiviert = aktiviert;
            _config.NetzwerkPfad = pfad;
            SaveConfig();
        }

        public static string GetBenutzerName()
        {
            return string.IsNullOrWhiteSpace(_config.BenutzerName)
                ? Environment.UserName
                : _config.BenutzerName;
        }

        public static void SetBenutzerName(string name)
        {
            _config.BenutzerName = name;
            SaveConfig();
        }

        private static void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFile))
                {
                    var json = File.ReadAllText(ConfigFile);
                    _config = JsonSerializer.Deserialize<NetzwerkConfig>(json) ?? new();
                }
            }
            catch
            {
                _config = new();
            }
        }

        private static void SaveConfig()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(ConfigFile, JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }
    }

    public class NetzwerkConfig
    {
        public bool Aktiviert { get; set; }
        public string NetzwerkPfad { get; set; } = "";
        public string BenutzerName { get; set; } = "";
    }
}
