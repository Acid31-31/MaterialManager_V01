using System;
using System.Diagnostics;
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
        public static string AuftragsArchivPfad => _config.AuftragsArchivPfad;

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

        public static void OpenAktivenDatenordnerImExplorer()
        {
            var savePath = GetSavePath();
            var directory = Path.GetDirectoryName(savePath);

            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("Der Speicherordner konnte nicht bestimmt werden.");

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (File.Exists(savePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{savePath}\"",
                    UseShellExecute = true
                });
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{directory}\"",
                UseShellExecute = true
            });
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

            return IsDirectoryReachableWithRetry(networkPath, attempts: 3, delayMs: 350);
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
            _config.NetzwerkPfad = NormalizeDirectoryPath(pfad);
            SaveConfig();
        }

        public static string GetAuftragsArchivBasisPfad()
        {
            if (TryGetValidatedAuftragsArchivPfad(out var archivPfad))
                return archivPfad;

            if (TryGetValidatedNetworkPath(out var netzwerkPfad))
                return Path.Combine(netzwerkPfad, "Auftragsarchiv");

            return string.Empty;
        }

        public static void SetAuftragsArchivPfad(string pfad)
        {
            _config.AuftragsArchivPfad = NormalizeDirectoryPath(pfad);
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

        public static bool HasConfiguredNetworkPath()
        {
            var normalized = NormalizeDirectoryPath(_config.NetzwerkPfad);
            return !string.IsNullOrWhiteSpace(normalized) && Path.IsPathRooted(normalized);
        }

        public static bool HasConfiguredArchivePath()
        {
            var normalized = NormalizeDirectoryPath(_config.AuftragsArchivPfad);
            return !string.IsNullOrWhiteSpace(normalized) && Path.IsPathRooted(normalized);
        }

        public static void ConfigureNetworkMode(bool aktiviert, string netzwerkPfad, string auftragsArchivPfad, string? benutzer = null)
        {
            _config.Aktiviert = aktiviert;
            _config.NetzwerkPfad = NormalizeDirectoryPath(netzwerkPfad);
            _config.AuftragsArchivPfad = NormalizeDirectoryPath(auftragsArchivPfad);
            if (!string.IsNullOrWhiteSpace(benutzer))
                _config.BenutzerName = benutzer.Trim();
            SaveConfig();
        }

        public static NetzwerkHealthStatus CheckStartupHealth()
        {
            if (!IsNetzwerkModus)
            {
                return new NetzwerkHealthStatus
                {
                    IsHealthy = true,
                    Message = "Netzwerkmodus ist nicht aktiv."
                };
            }

            if (!TryGetValidatedNetworkPath(out var netzPfad))
            {
                return new NetzwerkHealthStatus
                {
                    IsHealthy = false,
                    Message = "Netzwerkpfad ist nicht gültig konfiguriert."
                };
            }

            if (!IsDirectoryReachableWithRetry(netzPfad, attempts: 6, delayMs: 500))
            {
                return new NetzwerkHealthStatus
                {
                    IsHealthy = false,
                    Message = $"Netzwerkpfad nicht erreichbar:\n{netzPfad}"
                };
            }

            var savePath = GetSavePath();
            var saveDir = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrWhiteSpace(saveDir))
            {
                try
                {
                    if (!Directory.Exists(saveDir))
                        Directory.CreateDirectory(saveDir);
                }
                catch (Exception ex)
                {
                    return new NetzwerkHealthStatus
                    {
                        IsHealthy = false,
                        Message = $"Speicherordner konnte nicht vorbereitet werden:\n{ex.Message}"
                    };
                }
            }

            var archiv = GetAuftragsArchivBasisPfad();
            if (!string.IsNullOrWhiteSpace(archiv))
            {
                try
                {
                    if (!Directory.Exists(archiv))
                        Directory.CreateDirectory(archiv);
                }
                catch (Exception ex)
                {
                    return new NetzwerkHealthStatus
                    {
                        IsHealthy = false,
                        Message = $"Auftragsarchiv kann nicht vorbereitet werden:\n{ex.Message}"
                    };
                }
            }

            return new NetzwerkHealthStatus
            {
                IsHealthy = true,
                Message = "Netzwerkverbindung ist verfügbar."
            };
        }

        public static string GetNetzwerkStatusText()
        {
            if (!HasConfiguredNetworkPath())
                return "Modus: Lokal (nicht konfiguriert)";

            if (!IsNetzwerkModus)
                return "Modus: Lokal (Netzwerk aus)";

            var configuredPath = (NetzwerkPfad ?? string.Empty).Trim().Trim('"');
            var isUncPath = configuredPath.StartsWith(@"\\");
            var isNetworkDrive = false;

            try
            {
                if (!isUncPath && Path.IsPathRooted(configuredPath))
                {
                    var root = Path.GetPathRoot(configuredPath);
                    if (!string.IsNullOrWhiteSpace(root))
                    {
                        var drive = new DriveInfo(root);
                        isNetworkDrive = drive.DriveType == DriveType.Network;
                    }
                }
            }
            catch
            {
                isNetworkDrive = false;
            }

            var isNetworkLocation = isUncPath || isNetworkDrive;

            if (IstPfadErreichbar())
                return isNetworkLocation ? "Modus: Server verbunden" : "Modus: Lokal (kein Server)";

            return "Modus: Server nicht erreichbar";
        }

        public static string GetExcelStatusText()
        {
            try
            {
                return $"Excel: {GetSavePath()}";
            }
            catch
            {
                return "Excel: Pfad nicht verfügbar";
            }
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

            var normalized = NormalizeDirectoryPath(_config.NetzwerkPfad);
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

        private static bool TryGetValidatedAuftragsArchivPfad(out string archivePath)
        {
            archivePath = string.Empty;

            var normalized = NormalizeDirectoryPath(_config.AuftragsArchivPfad);
            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            try
            {
                if (!Path.IsPathRooted(normalized))
                    return false;

                archivePath = Path.GetFullPath(normalized);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizeDirectoryPath(string? path)
        {
            var candidate = NormalizePath(path);
            if (string.IsNullOrWhiteSpace(candidate))
                return string.Empty;

            try
            {
                var xlsxIndex = candidate.IndexOf(".xlsx", StringComparison.OrdinalIgnoreCase);
                if (xlsxIndex >= 0)
                {
                    var filePart = candidate.Substring(0, xlsxIndex + 5);
                    var remainder = candidate.Substring(xlsxIndex + 5).TrimStart('\\', '/');
                    var fileDirectory = Path.GetDirectoryName(filePart) ?? string.Empty;
                    candidate = string.IsNullOrWhiteSpace(remainder)
                        ? fileDirectory
                        : Path.Combine(fileDirectory, remainder);
                }

                var fullPath = Path.GetFullPath(candidate);

                if (File.Exists(fullPath))
                    return Path.GetDirectoryName(fullPath) ?? string.Empty;

                if (fullPath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    return Path.GetDirectoryName(fullPath) ?? string.Empty;

                return fullPath;
            }
            catch
            {
                return candidate;
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

                    var normalizedNetwork = NormalizeDirectoryPath(_config.NetzwerkPfad);
                    var normalizedArchive = NormalizeDirectoryPath(_config.AuftragsArchivPfad);
                    var changed = !string.Equals(_config.NetzwerkPfad, normalizedNetwork, StringComparison.Ordinal)
                        || !string.Equals(_config.AuftragsArchivPfad, normalizedArchive, StringComparison.Ordinal);

                    _config.NetzwerkPfad = normalizedNetwork;
                    _config.AuftragsArchivPfad = normalizedArchive;

                    if (changed)
                        SaveConfig();
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

        private static bool IsDirectoryReachableWithRetry(string path, int attempts, int delayMs)
        {
            for (var i = 0; i < attempts; i++)
            {
                try
                {
                    if (Directory.Exists(path))
                        return true;
                }
                catch
                {
                }

                if (i < attempts - 1)
                    System.Threading.Thread.Sleep(delayMs);
            }

            return false;
        }
    }

    public sealed class NetzwerkHealthStatus
    {
        public bool IsHealthy { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class NetzwerkConfig
    {
        public bool Aktiviert { get; set; }
        public string NetzwerkPfad { get; set; } = "";
        public string BenutzerName { get; set; } = "";
        public string AuftragsArchivPfad { get; set; } = "";
    }
}
