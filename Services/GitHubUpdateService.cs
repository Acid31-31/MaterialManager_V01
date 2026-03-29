using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MaterialManager_V01.Services
{
    public static class GitHubUpdateService
    {
        private const string LatestReleaseApi = "https://api.github.com/repos/Acid31-31/MaterialManager_V01/releases/latest";
        private const string CompareApiBase = "https://api.github.com/repos/Acid31-31/MaterialManager_V01/compare/";
        private const string CommitsApi = "https://api.github.com/repos/Acid31-31/MaterialManager_V01/commits?per_page=20";
        private static readonly HttpClient Http = CreateClient();

        private static readonly string UpdateSettingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MaterialManager_V01",
            "update_settings.json");

        public static async Task<UpdateCheckResult> CheckForUpdatesAsync()
        {
            var current = GetCurrentVersionTag();

            if (IsDevelopmentPreviewRun())
            {
                return new UpdateCheckResult
                {
                    CurrentVersion = current,
                    LatestVersion = current,
                    IsUpdateAvailable = false
                };
            }

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
                    string? updateInstallerUrl = null; string? updateInstallerName = null;
                    string? zipUrl = null; string? zipName = null;

                    foreach (var asset in assetsProp.EnumerateArray())
                    {
                        var name = asset.TryGetProperty("name", out var n) ? (n.GetString() ?? string.Empty) : string.Empty;
                        var url = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
                        if (string.IsNullOrWhiteSpace(url))
                            continue;

                        if (name.Equals("UpdateInstaller.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            updateInstallerUrl ??= url;
                            updateInstallerName ??= name;
                        }
                        else if (name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                        {
                            msiUrl ??= url;
                        }
                        else if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            zipUrl ??= url;
                            zipName ??= name;
                        }
                    }

                    if (updateInstallerUrl != null)
                    {
                        selectedUrl = updateInstallerUrl;
                        selectedName = updateInstallerName;
                        selectedType = "update-exe";
                    }
                    else if (msiUrl != null)
                    {
                        selectedUrl = msiUrl;
                        selectedName = Path.GetFileName(msiUrl);
                        selectedType = "msi";
                    }
                    else if (zipUrl != null)
                    {
                        selectedUrl = zipUrl;
                        selectedName = zipName;
                        selectedType = "zip";
                    }
                }

                var updateAvailable = ParseVersion(tag) > ParseVersion(current);
                var changelog = await BuildReadableChangelogAsync(body, current, tag);

                var assetError = selectedUrl == null
                    ? "Kein UpdateInstaller/MSI/ZIP Asset im Release gefunden."
                    : null;

                return new UpdateCheckResult
                {
                    CurrentVersion = current,
                    LatestVersion = tag,
                    Changelog = changelog,
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

        private static async Task<string> BuildReadableChangelogAsync(string body, string currentTag, string latestTag)
        {
            var cleanBody = string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();

            var commits = await TryGetCompareCommitsAsync(currentTag, latestTag);
            if (commits.Count == 0)
            {
                var compareFromBody = ParseCompareTagsFromBody(cleanBody);
                if (compareFromBody != null)
                    commits = await TryGetCompareCommitsAsync(compareFromBody.Value.baseTag, compareFromBody.Value.headTag);
            }

            if (commits.Count == 0)
                commits = await TryGetLatestCommitsAsync(12);

            if (commits.Count > 0)
            {
                var lines = new List<string>
                {
                    "Änderungen in dieser Version:"
                };

                foreach (var msg in commits.Take(20))
                    lines.Add($"• {msg}");

                if (commits.Count > 20)
                    lines.Add($"• ... und {commits.Count - 20} weitere Commits");

                lines.Add(string.Empty);
                lines.Add($"Vergleich: https://github.com/Acid31-31/MaterialManager_V01/compare/{currentTag}...{latestTag}");
                return string.Join(Environment.NewLine, lines);
            }

            if (LooksLikeOnlyCompareLink(cleanBody))
            {
                return "Änderungsdetails sind im Release hinterlegt, konnten aber aktuell nicht automatisch geladen werden. " +
                       "Bitte kurz später erneut auf 'Nach Updates suchen' klicken.";
            }

            return string.IsNullOrWhiteSpace(cleanBody) ? "Kein Changelog verfügbar." : cleanBody;
        }

        private static async Task<List<string>> TryGetCompareCommitsAsync(string baseTag, string headTag)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseTag) || string.IsNullOrWhiteSpace(headTag))
                    return new List<string>();

                var url = CompareApiBase + $"{Uri.EscapeDataString(baseTag)}...{Uri.EscapeDataString(headTag)}";
                using var response = await Http.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return new List<string>();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("commits", out var commitsElement) || commitsElement.ValueKind != JsonValueKind.Array)
                    return new List<string>();

                var result = new List<string>();
                foreach (var c in commitsElement.EnumerateArray())
                {
                    if (!c.TryGetProperty("commit", out var commitObj))
                        continue;
                    if (!commitObj.TryGetProperty("message", out var messageObj))
                        continue;

                    var raw = messageObj.GetString() ?? string.Empty;
                    var firstLine = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)[0].Trim();
                    if (!string.IsNullOrWhiteSpace(firstLine))
                        result.Add(firstLine);
                }

                return result;
            }
            catch
            {
                return new List<string>();
            }
        }

        private static async Task<List<string>> TryGetLatestCommitsAsync(int maxItems)
        {
            try
            {
                using var response = await Http.GetAsync(CommitsApi);
                if (!response.IsSuccessStatusCode)
                    return new List<string>();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return new List<string>();

                var result = new List<string>();
                foreach (var c in doc.RootElement.EnumerateArray())
                {
                    if (!c.TryGetProperty("commit", out var commitObj))
                        continue;
                    if (!commitObj.TryGetProperty("message", out var messageObj))
                        continue;

                    var raw = messageObj.GetString() ?? string.Empty;
                    var firstLine = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)[0].Trim();
                    if (!string.IsNullOrWhiteSpace(firstLine))
                        result.Add(firstLine);

                    if (result.Count >= maxItems)
                        break;
                }

                return result;
            }
            catch
            {
                return new List<string>();
            }
        }

        private static bool LooksLikeOnlyCompareLink(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return false;

            var compact = body.Replace("**", string.Empty).Trim();
            return compact.StartsWith("Full Changelog", StringComparison.OrdinalIgnoreCase)
                   && compact.Contains("/compare/");
        }

        private static (string baseTag, string headTag)? ParseCompareTagsFromBody(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return null;

            var match = Regex.Match(body, "compare/(?<base>[^/\\s]+)\\.\\.\\.(?<head>[^\\s)]+)", RegexOptions.IgnoreCase);
            if (!match.Success)
                return null;

            var baseTag = match.Groups["base"].Value.Trim();
            var headTag = match.Groups["head"].Value.Trim();
            if (string.IsNullOrWhiteSpace(baseTag) || string.IsNullOrWhiteSpace(headTag))
                return null;

            return (baseTag, headTag);
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

                await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
                await using (var target = File.Create(downloadedFile))
                {
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

        private static bool IsDevelopmentPreviewRun()
        {
            if (Debugger.IsAttached)
                return true;

            try
            {
                var baseDir = AppContext.BaseDirectory;
                if (baseDir.IndexOf("\\bin\\Debug\\", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    baseDir.IndexOf("\\bin\\Release\\", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                var dir = new DirectoryInfo(baseDir);
                for (var i = 0; i < 6 && dir != null; i++)
                {
                    if (Directory.Exists(Path.Combine(dir.FullName, ".git")) ||
                        File.Exists(Path.Combine(dir.FullName, "MaterialManager_V01.csproj")))
                    {
                        return true;
                    }

                    dir = dir.Parent;
                }
            }
            catch
            {
            }

            return false;
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

            return version.Revision > 0
                ? $"v{version.Major}.{version.Minor}.{version.Build}.{version.Revision}"
                : $"v{version.Major}.{version.Minor}.{version.Build}";
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
