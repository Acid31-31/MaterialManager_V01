using System;
using System.IO;
using System.Text.Json;

namespace MaterialManager_V01.Services
{
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

        public static bool IsNetzwerkModus => TryGetValidatedNetworkPath(out _);

        public static string NetzwerkPfad => _config.NetzwerkPfad;

        public static string GetSavePath()
        {
            if (TryGetValidatedNetworkPath(out var networkPath))
                return Path.Combine(networkPath, "materialbestand.xlsx");

            var dataDir = GetLocalDataDirectory();
            var targetPath = Path.Combine(dataDir, "materialbestand.xlsx");

            try
            {
                if (!File.Exists(targetPath))
                {
                    var legacyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Excell", "materialbestand.xlsx");
                    if (File.Exists(legacyPath))
                        File.Copy(legacyPath, targetPath, overwrite: false);
                }
            }
            catch { }

            return targetPath;
        }

        public static string GetLockFile()
        {
            if (!TryGetValidatedNetworkPath(out var networkPath))
                return "";

            return Path.Combine(networkPath, ".lock");
        }

        public static bool IstPfadErreichbar()
        {
            if (!TryGetValidatedNetworkPath(out var networkPath))
                return true;

            try
            {
                return Directory.Exists(networkPath);
            }
            catch
            {
                return false;
            }
        }

        public static bool AcquireLock(string benutzer)
        {
            if (!TryGetValidatedNetworkPath(out _))
                return true;

            var lockFile = GetLockFile();
            try
            {
                if (File.Exists(lockFile))
                {
                    var lockInfo = File.ReadAllText(lockFile);
                    var parts = lockInfo.Split('|');
                    if (parts.Length >= 2)
                    {
                        if (DateTime.TryParse(parts[1], out var lockTime) && (DateTime.Now - lockTime).TotalMinutes > 5)
                        {
                            File.Delete(lockFile);
                        }
                        else if (parts[0] != benutzer)
                        {
                            return false;
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

        public static void ReleaseLock()
        {
            if (!TryGetValidatedNetworkPath(out _))
                return;

            var lockFile = GetLockFile();
            try
            {
                if (File.Exists(lockFile))
                    File.Delete(lockFile);
            }
            catch { }
        }

        public static string GetLockOwner()
        {
            if (!TryGetValidatedNetworkPath(out _))
                return "";

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

        public static void SetNetzwerkModus(bool aktiviert, string pfad)
        {
            _config.Aktiviert = aktiviert;
            _config.NetzwerkPfad = NormalizePath(pfad);
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

        private static string GetLocalDataDirectory()
        {
            var dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MaterialManager_V01",
                "Data");

            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);

            return dataDir;
        }

        private static bool TryGetValidatedNetworkPath(out string networkPath)
        {
            networkPath = string.Empty;

            if (!_config.Aktiviert)
                return false;

            var normalized = NormalizePath(_config.NetzwerkPfad);
            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            try
            {
                if (!Path.IsPathRooted(normalized))
                    return false;

                networkPath = Path.GetFullPath(normalized);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizePath(string? path)
        {
            var candidate = (path ?? string.Empty).Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(candidate))
                return string.Empty;

            if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri) && uri.IsFile)
                return uri.LocalPath;

            return candidate;
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
