using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MaterialManager_V01.Services
{
    public static class LicenseService
    {
        private static readonly string LicenseFile = PathService.LicensePath;
        private static readonly string LegacyLicenseFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MaterialManager_V01",
            ".license");

        private const int DefaultMaxDevices = 10;
        private const string CorporateKeyPrefix = "LIC-";
        private const string CorporateSecret = "MM_V01_CORPORATE_LICENSE_SECRET_2026";

        private static int _cachedRemainingTrialDays = 60;
        private static int _cachedActiveDevices;
        private static int _cachedMaxDevices = DefaultMaxDevices;
        private static string _cachedStatusMessage = "Testversion – noch 60 Tage verbleibend";

        private sealed class LicenseInfo
        {
            public string Version { get; set; } = "2";
            public bool IsFullLicense { get; set; }
            public string? LicenseKey { get; set; }
            public string? CompanyName { get; set; }
            public int MaxDevices { get; set; } = DefaultMaxDevices;
            public DateTime? ExpiryDateUtc { get; set; }
            public string HardwareId { get; set; } = string.Empty;
            public DateTime ActivatedAtUtc { get; set; } = DateTime.UtcNow;
        }

        private sealed class CorporatePayload
        {
            public string CompanyName { get; set; } = string.Empty;
            public int MaxDevices { get; set; } = DefaultMaxDevices;
            public DateTime? ExpiryDateUtc { get; set; }
        }

        public static bool IsLicenseValid()
        {
            try
            {
                var info = LoadLicenseInfo();
                if (info?.IsFullLicense == true)
                    return ValidateFullLicense(info);

                return ValidateTrialMode();
            }
            catch (Exception ex)
            {
                _cachedStatusMessage = $"Lizenzprüfung fehlgeschlagen: {ex.Message}";
                return false;
            }
        }

        public static int GetRemainingTrialDays()
        {
            _ = IsLicenseValid();
            return _cachedRemainingTrialDays;
        }

        public static DateTime? GetExpirationDate()
        {
            var info = LoadLicenseInfo();
            if (info?.IsFullLicense == true)
                return info.ExpiryDateUtc?.ToLocalTime();

            var status = TrialService.ValidateAndUpdate();
            if (status.FirstInstallUtc == default)
                return null;

            return status.FirstInstallUtc.AddDays(60).ToLocalTime();
        }

        public static string GetHardwareId()
        {
            return HardwareIdService.GetHardwareId();
        }

        public static bool ActivateFullLicense(string licenseKey, string registeredTo)
        {
            if (string.IsNullOrWhiteSpace(licenseKey) || string.IsNullOrWhiteSpace(registeredTo))
                return false;

            try
            {
                var company = registeredTo.Trim();
                var maxDevices = DefaultMaxDevices;
                DateTime? expiryUtc = null;

                if (TryParseCorporateLicenseKey(licenseKey.Trim(), out var payload))
                {
                    company = string.IsNullOrWhiteSpace(payload.CompanyName) ? company : payload.CompanyName.Trim();
                    maxDevices = payload.MaxDevices <= 0 ? DefaultMaxDevices : payload.MaxDevices;
                    expiryUtc = payload.ExpiryDateUtc;
                }
                else
                {
                    var hardwareId = GetHardwareId();
                    var validLegacy = false;
                    for (var years = 1; years <= 10; years++)
                    {
                        var generated = LicenseKeyGenerator.GenerateLicenseKey(hardwareId, company, years);
                        if (string.Equals(generated, licenseKey.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            validLegacy = true;
                            break;
                        }
                    }

                    if (!validLegacy && !LicenseKeyGenerator.ValidateLicenseKey(licenseKey.Trim(), hardwareId, company))
                        return false;
                }

                var info = new LicenseInfo
                {
                    Version = "2",
                    IsFullLicense = true,
                    LicenseKey = licenseKey.Trim(),
                    CompanyName = company,
                    MaxDevices = Math.Max(1, maxDevices),
                    ExpiryDateUtc = expiryUtc,
                    HardwareId = GetHardwareId(),
                    ActivatedAtUtc = DateTime.UtcNow
                };

                if (!SaveLicenseInfo(info))
                    return false;

                return IsLicenseValid();
            }
            catch
            {
                return false;
            }
        }

        public static string GetStatusMessage()
        {
            _ = IsLicenseValid();
            return _cachedStatusMessage;
        }

        public static void ResetTrial()
        {
            try
            {
                if (File.Exists(LicenseFile))
                    File.Delete(LicenseFile);
                if (File.Exists(LegacyLicenseFile))
                    File.Delete(LegacyLicenseFile);
            }
            catch { }

            TrialService.ResetTrial();
            _cachedRemainingTrialDays = 60;
            _cachedStatusMessage = "Testversion – noch 60 Tage verbleibend";
        }

        public static bool IsFullLicenseActive()
        {
            var info = LoadLicenseInfo();
            return info?.IsFullLicense == true;
        }

        public static string GetLicenseModeText()
        {
            var info = LoadLicenseInfo();
            if (info?.IsFullLicense == true)
                return $"Vollversion – Firma: {info.CompanyName}";

            var days = GetRemainingTrialDays();
            return $"Testversion ({days} Tage verbleibend)";
        }

        public static string GetDeviceUsageText()
        {
            var info = LoadLicenseInfo();
            if (info?.IsFullLicense != true)
                return string.Empty;

            _ = IsLicenseValid();
            return $"Aktive Geräte: {_cachedActiveDevices} / {_cachedMaxDevices}";
        }

        private static bool ValidateTrialMode()
        {
            var trial = TrialService.ValidateAndUpdate();
            _cachedRemainingTrialDays = trial.RemainingDays;

            if (trial.IsManipulated)
            {
                _cachedStatusMessage = "Testversion ungültig (Manipulation erkannt). Bitte Lizenz eingeben.";
                return false;
            }

            if (!trial.IsValid)
            {
                _cachedStatusMessage = "Testversion abgelaufen. Bitte Lizenz eingeben.";
                return false;
            }

            _cachedStatusMessage = $"Testversion – noch {trial.RemainingDays} Tage verbleibend";
            return true;
        }

        private static bool ValidateFullLicense(LicenseInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.CompanyName))
            {
                _cachedStatusMessage = "Lizenz ungültig. Firmenname fehlt.";
                return false;
            }

            if (info.ExpiryDateUtc.HasValue && DateTime.UtcNow > info.ExpiryDateUtc.Value)
            {
                _cachedStatusMessage = "Lizenz abgelaufen. Bitte neue Lizenz eingeben.";
                return false;
            }

            var device = DeviceRegistryService.RegisterOrValidateDevice(
                info.CompanyName,
                Math.Max(1, info.MaxDevices),
                GetHardwareId());

            _cachedActiveDevices = device.ActiveDevices;
            _cachedMaxDevices = device.MaxDevices <= 0 ? Math.Max(1, info.MaxDevices) : device.MaxDevices;

            if (!device.IsAllowed)
            {
                _cachedStatusMessage = $"Vollversion – Firma: {info.CompanyName}\n{device.Message}";
                return false;
            }

            _cachedRemainingTrialDays = int.MaxValue;
            _cachedStatusMessage = $"Vollversion – Firma: {info.CompanyName}\nAktive Geräte: {_cachedActiveDevices} / {_cachedMaxDevices}";
            return true;
        }

        private static LicenseInfo? LoadLicenseInfo()
        {
            try
            {
                if (File.Exists(LicenseFile))
                {
                    var json = File.ReadAllText(LicenseFile);
                    var parsed = JsonSerializer.Deserialize<LicenseInfo>(json);
                    if (parsed != null)
                        return parsed;
                }

                if (File.Exists(LegacyLicenseFile))
                {
                    var json = File.ReadAllText(LegacyLicenseFile);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    var info = new LicenseInfo
                    {
                        Version = "2",
                        IsFullLicense = root.TryGetProperty("IsFullLicense", out var full) && full.GetBoolean(),
                        LicenseKey = root.TryGetProperty("LicenseKey", out var key) ? key.GetString() : null,
                        CompanyName = root.TryGetProperty("RegisteredTo", out var reg) ? reg.GetString() : null,
                        MaxDevices = DefaultMaxDevices,
                        HardwareId = root.TryGetProperty("HardwareId", out var hw) ? (hw.GetString() ?? string.Empty) : string.Empty,
                        ActivatedAtUtc = DateTime.UtcNow
                    };

                    if (info.IsFullLicense)
                        _ = SaveLicenseInfo(info);

                    return info.IsFullLicense ? info : null;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static bool SaveLicenseInfo(LicenseInfo info)
        {
            try
            {
                var dir = Path.GetDirectoryName(LicenseFile);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(LicenseFile, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParseCorporateLicenseKey(string input, out CorporatePayload payload)
        {
            payload = new CorporatePayload();
            try
            {
                if (string.IsNullOrWhiteSpace(input) || !input.StartsWith(CorporateKeyPrefix, StringComparison.OrdinalIgnoreCase))
                    return false;

                var raw = input.Substring(CorporateKeyPrefix.Length);
                var parts = raw.Split('.');
                if (parts.Length != 2)
                    return false;

                var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
                var signature = parts[1];

                var expectedSignature = ComputeSignature(parts[0]);
                if (!string.Equals(signature, expectedSignature, StringComparison.Ordinal))
                    return false;

                var parsed = JsonSerializer.Deserialize<CorporatePayload>(payloadJson);
                if (parsed == null || string.IsNullOrWhiteSpace(parsed.CompanyName))
                    return false;

                payload = parsed;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string ComputeSignature(string payloadBase64Url)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(CorporateSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64Url));
            return Base64UrlEncode(hash);
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var padded = input.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }

            return Convert.FromBase64String(padded);
        }
    }
}
