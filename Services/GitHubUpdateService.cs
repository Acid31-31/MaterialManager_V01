using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
                    var msg = $"GitHub-API Fehler: {(int)response.StatusCode}";
                    if (response.StatusCode == HttpStatusCode.NotFound)
                        msg = "Kein GitHub Release vorhanden. Bitte erst ein Release veröffentlichen.";

                    return new UpdateCheckResult
                    {
                        CurrentVersion = current,
                        LatestVersion = current,
                        IsUpdateAvailable = false,
                        ErrorMessage = msg
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

                string? selectedUrl = null;
                string? selectedName = null;
                string? selectedType = null;
                string? msiUrl = null;

                if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
                {
                    string? exeUrl = null; string? exeName = null;
                    string? zipUrl = null; string? zipName = null;

                    foreach (var asset in assetsProp.EnumerateArray())
                    {
                        var name = asset.TryGetProperty("name", out var n) ? (n.GetString() ?? string.Empty) : string.Empty;
                        var url = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
                        if (string.IsNullOrWhiteSpace(url))
                            continue;

                        if (name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                        {
                            msiUrl ??= url;
                            selectedUrl ??= url;
                            selectedName ??= name;
                            selectedType ??= "msi";
                        }
                        else if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            exeUrl ??= url;
                            exeName ??= name;
                        }
                        else if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            zipUrl ??= url;
                            zipName ??= name;
                        }
                    }

                    if (selectedUrl == null && exeUrl != null)
                    {
                        selectedUrl = exeUrl;
                        selectedName = exeName;
                        selectedType = "exe";
                    }
                    if (selectedUrl == null && zipUrl != null)
                    {
                        selectedUrl = zipUrl;
                        selectedName = zipName;
                        selectedType = "zip";
                    }
                }

                var updateAvailable = ParseVersion(tag) > ParseVersion(current);

                var assetError = selectedUrl == null
                    ? "Kein MSI/EXE/ZIP Asset im Release gefunden."
                    : null;

                return new UpdateCheckResult
                {
                    CurrentVersion = current,
                    LatestVersion = tag,
                    Changelog = string.IsNullOrWhiteSpace(body) ? "Kein Changelog verfügbar." : body,
                    MsiDownloadUrl = msiUrl,
                    DownloadUrl = selectedUrl,
                    AssetName = selectedName,
                    AssetType = selectedType,
                    ReleasePageUrl = htmlUrl,
                    IsUpdateAvailable = updateAvailable,
                    ErrorMessage = assetError
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

        public static async Task<PreparedUpdateResult> PrepareUpdateAsync(
            UpdateCheckResult updateInfo,
            IProgress<int>? progress,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(updateInfo.DownloadUrl))
                return new PreparedUpdateResult { ErrorMessage = "Kein geeignetes Release-Asset gefunden." };

            try
            {
                var versionFolder = (updateInfo.LatestVersion ?? "v0.0.0").Replace("/", "_").Replace("\\", "_");
                var targetDir = Path.Combine(Path.GetTempPath(), "MaterialManager_Update", versionFolder);
                Directory.CreateDirectory(targetDir);

                var fileName = string.IsNullOrWhiteSpace(updateInfo.AssetName)
                    ? $"MaterialManager_{versionFolder}.{updateInfo.AssetType ?? "bin"}"
                    : updateInfo.AssetName;

                var downloadedFile = Path.Combine(targetDir, fileName);
                var logPath = Path.Combine(targetDir, "prepare_update.log");

                using var response = await Http.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                var total = response.Content.Headers.ContentLength;
                await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var target = File.Create(downloadedFile);

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

                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Download: {downloadedFile}{Environment.NewLine}");

                if (downloadedFile.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                {
                    return new PreparedUpdateResult
                    {
                        InstallerExecutablePath = downloadedFile,
                        RunExecutableDirectly = true,
                        LogPath = logPath
                    };
                }

                if (downloadedFile.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    return new PreparedUpdateResult
                    {
                        InstallerExecutablePath = downloadedFile,
                        RunExecutableDirectly = true,
                        LogPath = logPath
                    };
                }

                if (downloadedFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    var extractDir = Path.Combine(targetDir, "extracted");
                    ZipFile.ExtractToDirectory(downloadedFile, extractDir, true);
                    File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ZIP entpackt: {extractDir}{Environment.NewLine}");

                    return new PreparedUpdateResult
                    {
                        ExtractedFolderPath = extractDir,
                        RunExecutableDirectly = false,
                        LogPath = logPath
                    };
                }

                return new PreparedUpdateResult
                {
                    ErrorMessage = "Unbekanntes Asset-Format.",
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

            var token = Environment.GetEnvironmentVariable("MATERIALMANAGER_GITHUB_TOKEN");
            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

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
        public string? DownloadUrl { get; init; }
        public string? AssetName { get; init; }
        public string? AssetType { get; init; }
        public string? ReleasePageUrl { get; init; }
        public string? ErrorMessage { get; init; }
    }

    public sealed class PreparedUpdateResult
    {
        public string? InstallerExecutablePath { get; init; }
        public string? ExtractedFolderPath { get; init; }
        public bool RunExecutableDirectly { get; init; }
        public string? LogPath { get; init; }
        public string? ErrorMessage { get; init; }
    }

    internal sealed class UpdateSettings
    {
        public DateTime LastCheckUtc { get; set; } = DateTime.MinValue;
    }
}
