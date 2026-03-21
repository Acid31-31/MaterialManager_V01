using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MaterialManager_V01.Services
{
    public static class OperatorIdentityService
    {
        private static readonly string IdentityFilePath = Path.Combine(PathService.DataDirectory, "operator_identity.json");
        private static OperatorIdentityStore _store = new();

        static OperatorIdentityService()
        {
            Load();
        }

        public static string CurrentOperatorName =>
            string.IsNullOrWhiteSpace(_store.CurrentOperatorName)
                ? Environment.UserName
                : _store.CurrentOperatorName;

        public static IReadOnlyList<string> RecentOperatorNames => _store.RecentOperatorNames;

        public static void SetCurrentOperatorName(string name)
        {
            var normalized = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return;

            _store.CurrentOperatorName = normalized;
            _store.RecentOperatorNames = _store.RecentOperatorNames
                .Where(n => !string.Equals(n, normalized, StringComparison.OrdinalIgnoreCase))
                .Prepend(normalized)
                .Take(20)
                .ToList();

            Save();
        }

        private static void Load()
        {
            try
            {
                if (!File.Exists(IdentityFilePath))
                    return;

                var json = File.ReadAllText(IdentityFilePath);
                var store = JsonSerializer.Deserialize<OperatorIdentityStore>(json);
                if (store != null)
                {
                    _store = store;
                    _store.RecentOperatorNames ??= new List<string>();
                }
            }
            catch
            {
                _store = new OperatorIdentityStore();
            }
        }

        private static void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_store, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(IdentityFilePath, json);
            }
            catch
            {
            }
        }

        private sealed class OperatorIdentityStore
        {
            public string CurrentOperatorName { get; set; } = string.Empty;
            public List<string> RecentOperatorNames { get; set; } = new();
        }
    }
}