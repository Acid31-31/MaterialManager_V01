using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MaterialManager_V01.Services
{
    public static class GitHubUpdateService
    {
        private const string LatestReleaseApi = "https://api.github.com/repos/Acid31-31/MaterialManager_V01/releases/latest";
        private static readonly HttpClient Http = CreateClient();

        private static readonly string UpdateSettingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MaterialManager_V01",
            "update_settings.json");

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

                string? msiUrl = null;
                string? msiName = null;

                if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assetsProp.EnumerateArray())
                    {
                        var name = asset.TryGetProperty("name", out var n) ? (n.GetString() ?? string.Empty) : string.Empty;
                        var url = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;

                        if (name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(url))
                        {
                            msiUrl = url;
                            msiName = name;
                            break;
                        }
                    }
                }

                var updateAvailable = ParseVersion(tag) > ParseVersion(current);

                return new UpdateCheckResult
                {
                    CurrentVersion = current,
                    LatestVersion = tag,
                    Changelog = string.IsNullOrWhiteSpace(body) ? "Kein Changelog verfügbar." : body,
                    MsiDownloadUrl = msiUrl,
                    MsiAssetName = msiName,
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

        public static bool ShouldRunAutoCheckToday()
        {
            try
            {
                if (!File.Exists(UpdateSettingsFile))
                    return true;

                var json = File.ReadAllText(UpdateSettingsFile);
                var settings = JsonSerializer.Deserialize<UpdateSettings>(json);
                if (settings == null)
                    return true;

                return settings.LastCheckUtc.Date < DateTime.UtcNow.Date;
            }
            catch
            {
                return true;
            }
        }

        public static void MarkAutoCheckedNow()
        {
            try
            {
                var dir = Path.GetDirectoryName(UpdateSettingsFile);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var settings = new UpdateSettings { LastCheckUtc = DateTime.UtcNow };
                File.WriteAllText(UpdateSettingsFile, JsonSerializer.Serialize(settings));
            }
            catch { }
        }

        public static async Task<PreparedUpdateResult> DownloadMsiAsync(
            UpdateCheckResult updateInfo,
            IProgress<int>? progress,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(updateInfo.MsiDownloadUrl))
                return new PreparedUpdateResult { ErrorMessage = "Kein MSI-Asset in der Release gefunden." };

            try
            {
                var versionFolder = (updateInfo.LatestVersion ?? "v0.0.0").Replace("/", "_").Replace("\\", "_");
                var targetDir = Path.Combine(Path.GetTempPath(), "MaterialManager_Update", versionFolder);
                Directory.CreateDirectory(targetDir);

                var fileName = string.IsNullOrWhiteSpace(updateInfo.MsiAssetName)
                    ? $"MaterialManager_{versionFolder}.msi"
                    : updateInfo.MsiAssetName;

                var msiPath = Path.Combine(targetDir, fileName);
                var logPath = Path.Combine(targetDir, "msi_update.log");

                using var response = await Http.GetAsync(updateInfo.MsiDownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                var total = response.Content.Headers.ContentLength;
                await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var target = File.Create(msiPath);

                var buffer = new byte[81920];
                long readTotal = 0;
                int read;
                while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    readTotal += read;

                    if (total.HasValue && total.Value > 0)
                    {
                        var pct = (int)Math.Round((readTotal * 100.0) / total.Value);
                        progress?.Report(Math.Clamp(pct, 0, 100));
                    }
                }

                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] MSI heruntergeladen: {msiPath}{Environment.NewLine}");

                return new PreparedUpdateResult
                {
                    MsiPath = msiPath,
                    LogPath = logPath
                };
            }
            catch (OperationCanceledException)
            {
                return new PreparedUpdateResult { ErrorMessage = "Download abgebrochen." };
            }
            catch (Exception ex)
            {
                return new PreparedUpdateResult { ErrorMessage = ex.Message };
            }
        }

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MaterialManager_V01-MSI-Updater/1.0");
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
        public string? MsiDownloadUrl { get; init; }
        public string? MsiAssetName { get; init; }
        public string? ReleasePageUrl { get; init; }
        public string? ErrorMessage { get; init; }
    }

    public sealed class PreparedUpdateResult
    {
        public string? MsiPath { get; init; }
        public string? LogPath { get; init; }
        public string? ErrorMessage { get; init; }
    }

    internal sealed class UpdateSettings
    {
        public DateTime LastCheckUtc { get; set; } = DateTime.MinValue;
    }
}
