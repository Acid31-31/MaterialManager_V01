using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MaterialManager_V01.Services
{
    public static class UpdateService
    {
        private const string GITHUB_API_URL = "https://api.github.com/repos/Acid31-31/MaterialManager_V01/releases/latest";
        private const string CURRENT_VERSION = "1.0.0";

        public class GitHubRelease
        {
            public string tag_name { get; set; } = "";
            public string name { get; set; } = "";
            public string body { get; set; } = "";
            public string html_url { get; set; } = "";
            public bool prerelease { get; set; }
            public DateTime published_at { get; set; }
        }

        public static async Task<(bool UpdateAvailable, GitHubRelease? Release)> CheckForUpdatesAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "MaterialManager-R03");
                client.Timeout = TimeSpan.FromSeconds(10);

                var response = await client.GetAsync(GITHUB_API_URL);
                if (!response.IsSuccessStatusCode)
                    return (false, null);

                var json = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (release == null)
                    return (false, null);

                // Version vergleichen (tag_name ist z.B. "v1.0.1" oder "1.0.1")
                var latestVersion = release.tag_name.TrimStart('v');
                var isNewer = CompareVersions(latestVersion, CURRENT_VERSION) > 0;

                return (isNewer, release);
            }
            catch (Exception)
            {
                // Bei Fehler (kein Internet, etc.) einfach false zurückgeben
                return (false, null);
            }
        }

        private static int CompareVersions(string version1, string version2)
        {
            var v1Parts = version1.Split('.');
            var v2Parts = version2.Split('.');

            for (int i = 0; i < Math.Max(v1Parts.Length, v2Parts.Length); i++)
            {
                var v1Part = i < v1Parts.Length && int.TryParse(v1Parts[i], out var v1) ? v1 : 0;
                var v2Part = i < v2Parts.Length && int.TryParse(v2Parts[i], out var v2) ? v2 : 0;

                if (v1Part > v2Part) return 1;
                if (v1Part < v2Part) return -1;
            }

            return 0;
        }

        public static string GetCurrentVersion() => CURRENT_VERSION;
    }
}
