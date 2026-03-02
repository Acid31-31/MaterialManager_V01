using System;
using System.Collections.Generic;
using System.Linq;

namespace MaterialManager_V01.Services
{
    public static class OnlineUserService
    {
        private const int ONLINE_THRESHOLD_MINUTES = 5;
        private static readonly Dictionary<string, DateTime> _userActivity = new();

        public static void RegisterCurrentUser()
        {
            _userActivity.Clear();
            UpdateUserActivity(Environment.UserName);
        }

        public static List<string> GetOnlineUsers()
        {
            var now = DateTime.Now;
            var onlineThreshold = now.AddMinutes(-ONLINE_THRESHOLD_MINUTES);
            
            return _userActivity
                .Where(kvp => kvp.Value >= onlineThreshold)
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
            if (!string.IsNullOrEmpty(username))
            {
                _userActivity[username] = DateTime.Now;
            }
        }
    }
}
