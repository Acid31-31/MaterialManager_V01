using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MaterialManager_V01.Services
{
    public static class GitHubUpdateService
    {
        private const string LatestReleaseApi = "https://api.github.com/repos/Acid31-31/MaterialManager_V01/releases/latest";
        private static readonly HttpClient Http = CreateClient();

        public static async Task<UpdateCheckResult> CheckForUpdatesAsync()
        {
            var current = GetCurrentVersionTag();

            try
            {
                using var response = await Http.GetAsync(LatestReleaseApi);
                if (!response.IsSuccessStatusCode)
                {
                    return new UpdateCheckResult
                    {
                        CurrentVersion = current,
                        LatestVersion = current,
                        IsUpdateAvailable = false,
                        ErrorMessage = $"GitHub-API Fehler: {(int)response.StatusCode}"
                    };
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var tag = root.TryGetProperty("tag_name", out var tagProp)
                    ? (tagProp.GetString() ?? current)
                    : current;

                var body = root.TryGetProperty("body", out var bodyProp)
                    ? (bodyProp.GetString() ?? string.Empty)
                    : string.Empty;

                var htmlUrl = root.TryGetProperty("html_url", out var htmlProp)
                    ? htmlProp.GetString()
                    : null;

                string? downloadUrl = null;
                if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assetsProp.EnumerateArray())
                    {
                        var name = asset.TryGetProperty("name", out var n) ? (n.GetString() ?? string.Empty) : string.Empty;
                        var url = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
                        if (string.IsNullOrWhiteSpace(url))
                            continue;

                        if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                            name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = url;
                            break;
                        }

                        if (downloadUrl == null)
                            downloadUrl = url;
                    }
                }

                var updateAvailable = ParseVersion(tag) > ParseVersion(current);

                return new UpdateCheckResult
                {
                    CurrentVersion = current,
                    LatestVersion = tag,
                    Changelog = string.IsNullOrWhiteSpace(body) ? "Kein Changelog verfügbar." : body,
                    DownloadUrl = downloadUrl,
                    ReleasePageUrl = htmlUrl,
                    IsUpdateAvailable = updateAvailable
                };
            }
            catch (Exception ex)
            {
                return new UpdateCheckResult
                {
                    CurrentVersion = current,
                    LatestVersion = current,
                    IsUpdateAvailable = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public static async Task<PreparedUpdateResult> PrepareUpdateAsync(UpdateCheckResult updateInfo, string appBaseDirectory)
        {
            if (string.IsNullOrWhiteSpace(updateInfo.DownloadUrl))
            {
                return new PreparedUpdateResult { ErrorMessage = "Keine Download-URL verfügbar." };
            }

            try
            {
                var root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MaterialManager_V01",
                    "updates",
                    DateTime.Now.ToString("yyyyMMdd_HHmmss"));

                Directory.CreateDirectory(root);
                var serviceLog = Path.Combine(root, "prepare_update.log");
                AppendUpdateLog(serviceLog, $"Start PrepareUpdateAsync | DownloadUrl={updateInfo.DownloadUrl}");

                var fileName = Path.GetFileName(new Uri(updateInfo.DownloadUrl).LocalPath);
                if (string.IsNullOrWhiteSpace(fileName))
                    fileName = "update_package.zip";

                var downloadedFile = Path.Combine(root, fileName);
                AppendUpdateLog(serviceLog, $"Downloading: {downloadedFile}");

                using (var stream = await Http.GetStreamAsync(updateInfo.DownloadUrl))
                using (var file = File.Create(downloadedFile))
                {
                    await stream.CopyToAsync(file);
                }

                AppendUpdateLog(serviceLog, "Download abgeschlossen.");

                if (downloadedFile.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    AppendUpdateLog(serviceLog, "EXE-Update erkannt.");
                    return new PreparedUpdateResult
                    {
                        InstallerExecutablePath = downloadedFile,
                        RunExecutableDirectly = true,
                        LogPath = serviceLog
                    };
                }

                if (!downloadedFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    AppendUpdateLog(serviceLog, "Unbekanntes Update-Format.");
                    return new PreparedUpdateResult { ErrorMessage = "Unbekanntes Update-Format.", LogPath = serviceLog };
                }

                var extractDir = Path.Combine(root, "extracted");
                AppendUpdateLog(serviceLog, $"Extract ZIP -> {extractDir}");
                ZipFile.ExtractToDirectory(downloadedFile, extractDir, true);

                var sourceExe = FindFileRecursive(extractDir, "MaterialManager_V01.exe");
                if (sourceExe == null)
                {
                    AppendUpdateLog(serviceLog, "MaterialManager_V01.exe nicht gefunden.");
                    return new PreparedUpdateResult { ErrorMessage = "MaterialManager_V01.exe im Update-Paket nicht gefunden.", LogPath = serviceLog };
                }

                var sourceDir = Path.GetDirectoryName(sourceExe)!;
                var targetDir = appBaseDirectory.TrimEnd('\\');
                var scriptPath = Path.Combine(root, "apply_update.bat");

                AppendUpdateLog(serviceLog, $"SourceDir={sourceDir}");
                AppendUpdateLog(serviceLog, $"TargetDir={targetDir}");
                File.WriteAllText(scriptPath, BuildUpdateScript(sourceDir, targetDir, serviceLog), Encoding.UTF8);
                AppendUpdateLog(serviceLog, $"Update-Skript erstellt: {scriptPath}");

                return new PreparedUpdateResult
                {
                    UpdateScriptPath = scriptPath,
                    RunExecutableDirectly = false,
                    LogPath = serviceLog
                };
            }
            catch (Exception ex)
            {
                return new PreparedUpdateResult { ErrorMessage = ex.Message };
            }
        }

        private static string BuildUpdateScript(string sourceDir, string targetDir, string serviceLogPath)
        {
            var s = sourceDir.Replace("\"", "\"\"");
            var t = targetDir.Replace("\"", "\"\"");
            var l = serviceLogPath.Replace("\"", "\"\"");

            return "@echo off\r\n" +
                   "setlocal\r\n" +
                   $"set \"LOG={l}\"\r\n" +
                   "echo [%date% %time%] apply_update gestartet>>\"%LOG%\"\r\n" +
                   "if /I \"%~1\" NEQ \"elevated\" (\r\n" +
                   "  echo [%date% %time%] UAC-Elevation angefordert>>\"%LOG%\"\r\n" +
                   "  powershell -NoProfile -ExecutionPolicy Bypass -Command \"Start-Process -FilePath '%~f0' -ArgumentList 'elevated' -Verb RunAs\"\r\n" +
                   "  exit /b\r\n" +
                   ")\r\n" +
                   "timeout /t 2 /nobreak >nul\r\n" +
                   "taskkill /IM MaterialManager_V01.exe /F >nul 2>&1\r\n" +
                   "echo [%date% %time%] Dateien werden kopiert...>>\"%LOG%\"\r\n" +
                   $"xcopy \"{s}\\*\" \"{t}\\\" /E /I /Y >nul\r\n" +
                   "if errorlevel 1 (\r\n" +
                   "  echo [%date% %time%] FEHLER beim Kopieren>>\"%LOG%\"\r\n" +
                   "  echo Update fehlgeschlagen.\r\n" +
                   "  pause\r\n" +
                   "  exit /b 1\r\n" +
                   ")\r\n" +
                   "echo [%date% %time%] Kopieren erfolgreich. Starte App neu...>>\"%LOG%\"\r\n" +
                   $"start \"\" \"{t}\\MaterialManager_V01.exe\"\r\n" +
                   "echo [%date% %time%] Update abgeschlossen>>\"%LOG%\"\r\n" +
                   "exit /b 0\r\n";
        }

        private static void AppendUpdateLog(string path, string message)
        {
            try
            {
                File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch { }
        }

        private static string? FindFileRecursive(string root, string fileName)
        {
            foreach (var file in Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories))
                return file;
            return null;
        }

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MaterialManager_V01-Updater/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }

        private static string GetCurrentVersionTag()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version == null)
                return "v1.0.0";

            return $"v{version.Major}.{version.Minor}.{version.Build}";
        }

        private static Version ParseVersion(string tag)
        {
            var cleaned = (tag ?? string.Empty).Trim().TrimStart('v', 'V');
            return Version.TryParse(cleaned, out var v) ? v : new Version(0, 0, 0);
        }
    }

    public sealed class UpdateCheckResult
    {
        public string CurrentVersion { get; init; } = "v1.0.0";
        public string LatestVersion { get; init; } = "v1.0.0";
        public bool IsUpdateAvailable { get; init; }
        public string Changelog { get; init; } = "";
        public string? DownloadUrl { get; init; }
        public string? ReleasePageUrl { get; init; }
        public string? ErrorMessage { get; init; }
    }

    public sealed class PreparedUpdateResult
    {
        public string? UpdateScriptPath { get; init; }
        public string? InstallerExecutablePath { get; init; }
        public bool RunExecutableDirectly { get; init; }
        public string? ErrorMessage { get; init; }
        public string? LogPath { get; init; }
    }
}
