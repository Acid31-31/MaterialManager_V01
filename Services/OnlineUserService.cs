using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MaterialManager_V01.Services
{
    public static class OnlineUserService
    {
        private const int ONLINE_THRESHOLD_MINUTES = 5;
        private static readonly Dictionary<string, DateTime> _userActivity = new();

        public static void RegisterCurrentUser()
        {
            UpdateUserActivity(OperatorIdentityService.CurrentOperatorName);
        }

        public static List<string> GetOnlineUsers()
        {
            var now = DateTime.Now;
            var onlineThreshold = now.AddMinutes(-ONLINE_THRESHOLD_MINUTES);
            var result = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in _userActivity.Where(kvp => kvp.Value >= onlineThreshold))
                result[kvp.Key] = kvp.Value;

            try
            {
                var presenceDir = GetPresenceDirectory();
                if (Directory.Exists(presenceDir))
                {
                    foreach (var file in Directory.GetFiles(presenceDir, "*.online"))
                    {
                        DateTime lastSeen;
                        try
                        {
                            lastSeen = File.GetLastWriteTime(file);
                        }
                        catch
                        {
                            continue;
                        }

                        if (lastSeen < onlineThreshold)
                            continue;

                        string username;
                        try
                        {
                            username = (File.ReadAllText(file) ?? string.Empty).Trim();
                        }
                        catch
                        {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(username))
                            continue;

                        if (!result.TryGetValue(username, out var existing) || lastSeen > existing)
                            result[username] = lastSeen;
                    }
                }
            }
            catch
            {
            }

            return result
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        public static string GetOnlineStatusText()
        {
            var onlineUsers = GetOnlineUsers();
            return onlineUsers.Count switch
            {
                0 => "Niemand Online",
                1 => $"1 Benutzer Online: {onlineUsers[0]}",
                _ => $"{onlineUsers.Count} Benutzer Online"
            };
        }

        public static void UpdateUserActivity(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return;

            var now = DateTime.Now;
            _userActivity[username] = now;

            try
            {
                var presenceDir = GetPresenceDirectory();
                if (!Directory.Exists(presenceDir))
                    Directory.CreateDirectory(presenceDir);

                var safeFileName = MakeSafeFileName(username);
                var presenceFile = Path.Combine(presenceDir, safeFileName + ".online");
                File.WriteAllText(presenceFile, username.Trim());
            }
            catch
            {
            }
        }

        private static string GetPresenceDirectory()
        {
            try
            {
                var savePath = NetzwerkService.GetSavePath();
                var baseDir = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrWhiteSpace(baseDir))
                    return Path.Combine(baseDir, ".online-users");
            }
            catch
            {
            }

            return Path.Combine(PathService.DataDirectory, ".online-users");
        }

        private static string MakeSafeFileName(string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var cleaned = new string((value ?? string.Empty).Trim().Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "unknown" : cleaned;
        }
    }
}
