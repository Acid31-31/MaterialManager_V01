using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MaterialManager_V01.Services
{
    public static class GitHubUpdateService
    {
        private const string LatestReleaseApi = "https://api.github.com/repos/Acid31-31/MaterialManager_V01/releases/latest";
        private const string ReleasesApi = "https://api.github.com/repos/Acid31-31/MaterialManager_V01/releases?per_page=25";
        private const string LatestReleasePage = "https://github.com/Acid31-31/MaterialManager_V01/releases/latest";
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
                var releases = await GetPublishedReleasesAsync();
                if (releases.Count == 0)
                {
                    return new UpdateCheckResult
                    {
                        CurrentVersion = current,
                        LatestVersion = current,
                        IsUpdateAvailable = false,
                        ErrorMessage = "Kein GitHub Release vorhanden. Bitte erst ein Release veröffentlichen."
                    };
                }

                var latestRelease = releases[0];
                var missedReleases = releases
                    .Where(r => ParseVersion(r.Tag) > ParseVersion(current))
                    .OrderBy(r => ParseVersion(r.Tag))
                    .ToList();

                var updateAvailable = missedReleases.Count > 0;
                SelectBestAsset(latestRelease, out var selectedUrl, out var selectedName, out var selectedType, out var msiUrl);

                var changelogSource = BuildCumulativeReleaseBody(missedReleases, current, latestRelease.Tag);
                var changelog = await BuildReadableChangelogAsync(changelogSource, current, latestRelease.Tag);

                return new UpdateCheckResult
                {
                    CurrentVersion = current,
                    LatestVersion = latestRelease.Tag,
                    IsUpdateAvailable = updateAvailable,
                    Changelog = changelog,
                    MsiDownloadUrl = msiUrl,
                    DownloadUrl = selectedUrl,
                    AssetName = selectedName,
                    AssetType = selectedType,
                    ReleasePageUrl = latestRelease.HtmlUrl,
                    ErrorMessage = selectedUrl == null ? "Kein UpdateInstaller/MSI/ZIP Asset im Release gefunden." : null,
                    MissingReleaseCount = missedReleases.Count,
                    IsCumulativeUpdate = updateAvailable,
                    IncludedReleaseTags = missedReleases.Select(r => r.Tag).ToList()
                };
            }
            catch (Exception ex)
            {
                try
                {
                    var fallback = await TryCheckForUpdatesViaReleasePageAsync(current);
                    if (fallback != null)
                        return fallback;
                }
                catch
                {
                }

                return new UpdateCheckResult
                {
                    CurrentVersion = current,
                    LatestVersion = current,
                    IsUpdateAvailable = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private static async Task<UpdateCheckResult?> TryCheckForUpdatesViaReleasePageAsync(string current)
        {
            try
            {
                using var response = await Http.GetAsync(LatestReleasePage, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                    return null;

                var finalUri = response.RequestMessage?.RequestUri?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(finalUri) || !finalUri.Contains("/releases/tag/", StringComparison.OrdinalIgnoreCase))
                    return null;

                var tag = finalUri.Split('/').LastOrDefault()?.Trim();
                if (string.IsNullOrWhiteSpace(tag))
                    return null;

                var updateAvailable = ParseVersion(tag) > ParseVersion(current);
                var releasePageUrl = $"https://github.com/Acid31-31/MaterialManager_V01/releases/tag/{tag}";
                var downloadUrl = $"https://github.com/Acid31-31/MaterialManager_V01/releases/download/{tag}/UpdateInstaller.exe";

                return new UpdateCheckResult
                {
                    CurrentVersion = current,
                    LatestVersion = tag,
                    Changelog = updateAvailable
                        ? "Kumulatives Vollupdate: Dieses Update enthält alle fehlenden Änderungen bis zur neuesten Version."
                        : "Die installierte Version ist bereits aktuell.",
                    DownloadUrl = downloadUrl,
                    AssetName = "UpdateInstaller.exe",
                    AssetType = "update-exe",
                    ReleasePageUrl = releasePageUrl,
                    IsUpdateAvailable = updateAvailable,
                    ErrorMessage = null,
                    MissingReleaseCount = updateAvailable ? 1 : 0,
                    IsCumulativeUpdate = updateAvailable,
                    IncludedReleaseTags = updateAvailable ? new List<string> { tag } : new List<string>()
                };
            }
            catch
            {
                return null;
            }
        }

        public static async Task<string> NormalizeChangelogAsync(string rawChangelog, string currentTag, string latestTag)
        {
            return await BuildReadableChangelogAsync(rawChangelog, currentTag, latestTag);
        }

        private static async Task<string> BuildReadableChangelogAsync(string body, string currentTag, string latestTag)
        {
            var cleanBody = string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();

            // 1) Vorrang: Release-Text aus GitHub (wenn vorhanden)
            var parsedBodyLines = ParseBodyToBulletLines(cleanBody);
            if (parsedBodyLines.Count > 0)
            {
                var lines = new List<string> { "Änderungen in dieser Version:" };
                lines.AddRange(parsedBodyLines.Select(x => $"• {x}"));
                return string.Join(Environment.NewLine, lines);
            }

            // 2) Fallback: technische Änderungen zählen, aber keine englischen Commit-Texte anzeigen
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
                    "Änderungen in dieser Version:",
                    "• Interne Verbesserungen und Fehlerbehebungen",
                    "• Stabilitäts- und Leistungsoptimierungen",
                    $"• Enthält {commits.Count} technische Anpassung(en)"
                };

                return string.Join(Environment.NewLine, lines);
            }

            return "Änderungsdetails sind derzeit nicht verfügbar. Bitte versuchen Sie die Update-Prüfung später erneut.";
        }

        private static List<string> ParseBodyToBulletLines(string body)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(body))
                return result;

            var lines = body.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(l => l.Trim())
                            .ToList();

            foreach (var line in lines)
            {
                var cleaned = line.Replace("**", string.Empty).Replace("__", string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(cleaned))
                    continue;

                if (cleaned.StartsWith("Full Changelog", StringComparison.OrdinalIgnoreCase) ||
                    cleaned.Contains("/compare/") ||
                    cleaned.StartsWith("##", StringComparison.OrdinalIgnoreCase) ||
                    cleaned.StartsWith("#", StringComparison.OrdinalIgnoreCase))
                    continue;

                cleaned = cleaned.TrimStart('-', '*', '•').Trim();
                cleaned = NormalizeVisibleUpdateLine(cleaned);
                if (string.IsNullOrWhiteSpace(cleaned))
                    continue;

                if (cleaned.Equals("Änderungen in dieser Version:", StringComparison.OrdinalIgnoreCase) ||
                    cleaned.Equals("Was wurde geändert", StringComparison.OrdinalIgnoreCase) ||
                    cleaned.Equals("Was wurde geändert:", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (LooksLikeEnglishChangelogLine(cleaned))
                    continue;

                if (!result.Contains(cleaned, StringComparer.OrdinalIgnoreCase))
                    result.Add(cleaned);
            }

            return result;
        }

        private static string NormalizeVisibleUpdateLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;

            if (line.Contains("90 Grad immer links", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("90° immer links", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Winkel symmetrisch", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            if (line.Contains("Winkel as-entered", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("as-entered anzeigen", StringComparison.OrdinalIgnoreCase))
            {
                return "Rohrzuschnitt: Winkel links/rechts exakt wie eingegeben anzeigen";
            }

            if (line.Contains("PDF-Druck via FlowDocument", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("PDF-Vorschau statt Druckdialog", StringComparison.OrdinalIgnoreCase))
            {
                return "Rohrzuschnitt: PDF-Vorschau nach der Berechnung anzeigen";
            }

            return line;
        }

        private static bool LooksLikeEnglishChangelogLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var lower = text.Trim().ToLowerInvariant();

            // Typische englische Commit-/Release-Wörter
            var englishMarkers = new[]
            {
                " add ", " added ", " update ", " updated ", " improve ", " improved ",
                " remove ", " removed ", " delete ", " deleted ", " fix ", " fixed ",
                " refactor ", " release ", " merge ", " branch ", " options ", " dialog",
                " remaining ", " controls ", " logic ", " window "
            };

            var padded = $" {lower} ";
            var markerHits = englishMarkers.Count(m => padded.Contains(m, StringComparison.Ordinal));

            // Mindestens zwei Marker oder ein sehr typisches englisches Startmuster
            return markerHits >= 2 ||
                   lower.StartsWith("add ", StringComparison.Ordinal) ||
                   lower.StartsWith("update ", StringComparison.Ordinal) ||
                   lower.StartsWith("fix ", StringComparison.Ordinal) ||
                   lower.StartsWith("remove ", StringComparison.Ordinal) ||
                   lower.StartsWith("delete ", StringComparison.Ordinal) ||
                   lower.StartsWith("refactor ", StringComparison.Ordinal);
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

                await DownloadWithRetryAsync(updateInfo.DownloadUrl, downloadedFile, progress, cancellationToken);
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
                return new PreparedUpdateResult { ErrorMessage = BuildFriendlyUpdateError(ex, updateInfo.ReleasePageUrl) };
            }
        }

        private static async Task DownloadWithRetryAsync(string url, string destinationFile, IProgress<int>? progress, CancellationToken cancellationToken)
        {
            Exception? lastError = null;

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    var total = response.Content.Headers.ContentLength;
                    progress?.Report(0);

                    await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
                    await using var target = File.Create(destinationFile);

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

                    progress?.Report(100);
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    if (attempt < 3)
                        await Task.Delay(800, cancellationToken);
                }
            }

            throw lastError ?? new InvalidOperationException("Download fehlgeschlagen.");
        }

        private static string BuildFriendlyUpdateError(Exception ex, string? releasePageUrl)
        {
            if (ex is HttpRequestException hre && hre.InnerException is SocketException se && se.SocketErrorCode == SocketError.HostNotFound)
            {
                return string.IsNullOrWhiteSpace(releasePageUrl)
                    ? "Der Update-Server konnte nicht aufgelöst werden (DNS/Netzwerk). Bitte Internetverbindung prüfen und später erneut versuchen."
                    : $"Der Update-Server konnte nicht aufgelöst werden (DNS/Netzwerk). Bitte Verbindung prüfen oder über 'Release im Browser' installieren:\n{releasePageUrl}";
            }

            return ex.Message;
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

        private static async Task<List<GitHubReleaseInfo>> GetPublishedReleasesAsync()
        {
            using var response = await Http.GetAsync(ReleasesApi);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return new List<GitHubReleaseInfo>();

                throw new HttpRequestException($"GitHub-API Fehler: {(int)response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return new List<GitHubReleaseInfo>();

            var releases = new List<GitHubReleaseInfo>();
            foreach (var root in doc.RootElement.EnumerateArray())
            {
                if (root.TryGetProperty("draft", out var draftProp) && draftProp.ValueKind == JsonValueKind.True)
                    continue;
                if (root.TryGetProperty("prerelease", out var prereleaseProp) && prereleaseProp.ValueKind == JsonValueKind.True)
                    continue;

                var tag = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? string.Empty : string.Empty;
                if (string.IsNullOrWhiteSpace(tag))
                    continue;

                var body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? string.Empty : string.Empty;
                var htmlUrl = root.TryGetProperty("html_url", out var htmlProp) ? htmlProp.GetString() : null;
                var assets = new List<GitHubReleaseAssetInfo>();

                if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assetsProp.EnumerateArray())
                    {
                        var name = asset.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
                        var url = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
                        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(url))
                            assets.Add(new GitHubReleaseAssetInfo(name, url));
                    }
                }

                releases.Add(new GitHubReleaseInfo(tag, body, htmlUrl, assets));
            }

            return releases
                .OrderByDescending(r => ParseVersion(r.Tag))
                .ToList();
        }

        private static void SelectBestAsset(GitHubReleaseInfo release, out string? selectedUrl, out string? selectedName, out string? selectedType, out string? msiUrl)
        {
            selectedUrl = null;
            selectedName = null;
            selectedType = null;
            msiUrl = null;

            var updateInstaller = release.Assets.FirstOrDefault(a => a.Name.Equals("UpdateInstaller.exe", StringComparison.OrdinalIgnoreCase));
            var msi = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase));
            var zip = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

            if (updateInstaller != null)
            {
                selectedUrl = updateInstaller.Url;
                selectedName = updateInstaller.Name;
                selectedType = "update-exe";
            }
            else if (msi != null)
            {
                selectedUrl = msi.Url;
                selectedName = msi.Name;
                selectedType = "msi";
            }
            else if (zip != null)
            {
                selectedUrl = zip.Url;
                selectedName = zip.Name;
                selectedType = "zip";
            }

            msiUrl = msi?.Url;
        }

        private static string BuildCumulativeReleaseBody(List<GitHubReleaseInfo> missedReleases, string currentTag, string latestTag)
        {
            if (missedReleases.Count == 0)
                return string.Empty;

            var builder = new StringBuilder();
            builder.AppendLine("Kumulatives Vollupdate");
            builder.AppendLine($"Enthält {missedReleases.Count} fehlende Update(s) von {currentTag} bis {latestTag}.");
            builder.AppendLine();

            foreach (var release in missedReleases)
            {
                builder.AppendLine($"Version {release.Tag}");
                if (!string.IsNullOrWhiteSpace(release.Body))
                {
                    builder.AppendLine(release.Body.Trim());
                }
                else
                {
                    builder.AppendLine("- Technische Aktualisierung und Fehlerbehebungen");
                }
                builder.AppendLine();
            }

            return builder.ToString().Trim();
        }
    }

    internal sealed record GitHubReleaseAssetInfo(string Name, string Url);
    internal sealed record GitHubReleaseInfo(string Tag, string Body, string? HtmlUrl, List<GitHubReleaseAssetInfo> Assets);

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
        public int MissingReleaseCount { get; init; }
        public bool IsCumulativeUpdate { get; init; }
        public IReadOnlyList<string> IncludedReleaseTags { get; init; } = Array.Empty<string>();
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
