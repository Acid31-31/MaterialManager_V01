using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MaterialManager_V01.Services
{
    public static class DeviceRegistryService
    {
        public sealed class DeviceCheckResult
        {
            public bool IsAllowed { get; set; }
            public int ActiveDevices { get; set; }
            public int MaxDevices { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        private sealed class DeviceRegistryFile
        {
            public string CompanyName { get; set; } = string.Empty;
            public int MaxDevices { get; set; }
            public List<DeviceEntry> Devices { get; set; } = new();
        }

        private sealed class DeviceEntry
        {
            public string HardwareId { get; set; } = string.Empty;
            public string MachineName { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public DateTime FirstSeenUtc { get; set; }
            public DateTime LastSeenUtc { get; set; }
        }

        public static DeviceCheckResult RegisterOrValidateDevice(string companyName, int maxDevices, string hardwareId)
        {
            var result = new DeviceCheckResult { MaxDevices = Math.Max(1, maxDevices) };

            try
            {
                var filePath = GetRegistryPath();
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var registry = LoadRegistry(filePath) ?? new DeviceRegistryFile
                {
                    CompanyName = companyName,
                    MaxDevices = result.MaxDevices
                };

                if (registry.MaxDevices <= 0)
                    registry.MaxDevices = result.MaxDevices;
                if (string.IsNullOrWhiteSpace(registry.CompanyName))
                    registry.CompanyName = companyName;

                var existing = registry.Devices.FirstOrDefault(d => string.Equals(d.HardwareId, hardwareId, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.LastSeenUtc = DateTime.UtcNow;
                    SaveRegistry(filePath, registry);
                    result.IsAllowed = true;
                    result.ActiveDevices = registry.Devices.Count;
                    result.MaxDevices = registry.MaxDevices;
                    result.Message = $"Aktive Geräte: {result.ActiveDevices} / {result.MaxDevices}";
                    return result;
                }

                if (registry.Devices.Count >= registry.MaxDevices)
                {
                    result.IsAllowed = false;
                    result.ActiveDevices = registry.Devices.Count;
                    result.MaxDevices = registry.MaxDevices;
                    result.Message = $"Maximale Anzahl an Geräten ({registry.MaxDevices}) erreicht.";
                    return result;
                }

                registry.Devices.Add(new DeviceEntry
                {
                    HardwareId = hardwareId,
                    MachineName = Environment.MachineName,
                    UserName = Environment.UserName,
                    FirstSeenUtc = DateTime.UtcNow,
                    LastSeenUtc = DateTime.UtcNow
                });

                SaveRegistry(filePath, registry);

                result.IsAllowed = true;
                result.ActiveDevices = registry.Devices.Count;
                result.MaxDevices = registry.MaxDevices;
                result.Message = $"Aktive Geräte: {result.ActiveDevices} / {result.MaxDevices}";
                return result;
            }
            catch (Exception ex)
            {
                result.IsAllowed = true;
                result.ActiveDevices = 0;
                result.Message = $"Geräteprüfung übersprungen ({ex.Message}).";
                return result;
            }
        }

        private static string GetRegistryPath()
        {
            var networkPath = NetzwerkService.NetzwerkPfad;
            if (!string.IsNullOrWhiteSpace(networkPath))
            {
                try
                {
                    var rooted = Path.GetFullPath(networkPath);
                    return Path.Combine(rooted, "license_devices.json");
                }
                catch { }
            }

            var archivePath = NetzwerkService.GetAuftragsArchivBasisPfad();
            if (!string.IsNullOrWhiteSpace(archivePath))
                return Path.Combine(archivePath, "license_devices.json");

            return Path.Combine(PathService.DataDirectory, "license_devices.json");
        }

        private static DeviceRegistryFile? LoadRegistry(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<DeviceRegistryFile>(json);
            }
            catch
            {
                return null;
            }
        }

        private static void SaveRegistry(string path, DeviceRegistryFile data)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}
