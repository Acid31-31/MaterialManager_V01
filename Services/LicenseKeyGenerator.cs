using System;
using System.Security.Cryptography;
using System.Text;

namespace MaterialManager_V01.Services
{
    /// <summary>
    /// Generiert und validiert Lizenzschlüssel für Vollversionen
    /// </summary>
    public static class LicenseKeyGenerator
    {
        // Dies sollte in Produktionsumgebung in einer Config sein
        private const string MasterSecret = "MM_V01_MASTER_SECRET_2025_PRODUCTION";

        /// <summary>
        /// Generiert einen Lizenzschlüssel für eine spezifische Hardware-ID
        /// Format: MM-XXXX-XXXX-XXXX-XXXX
        /// </summary>
        public static string GenerateLicenseKey(string hardwareId, string registeredTo, int licenseYears = 1)
        {
            try
            {
                var expiryDate = DateTime.Now.AddYears(licenseYears).ToString("yyyyMMdd");
                var data = $"{hardwareId}|{registeredTo}|{expiryDate}|{MasterSecret}";

                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(MasterSecret)))
                {
                    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                    var hashString = Convert.ToBase64String(hash).Replace("+", "").Replace("/", "").Replace("=", "").Substring(0, 16).ToUpper();
                    
                    return $"MM-{hashString.Substring(0, 4)}-{hashString.Substring(4, 4)}-{hashString.Substring(8, 4)}-{hashString.Substring(12, 4)}";
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Fehler beim Generieren des Lizenzschlüssels", ex);
            }
        }

        /// <summary>
        /// Validiert einen Lizenzschlüssel mit Fehlertoleranz für Zeitunterschiede
        /// </summary>
        public static bool ValidateLicenseKey(string licenseKey, string hardwareId, string registeredTo)
        {
            try
            {
                var normalizedInput = (licenseKey ?? string.Empty).Trim().ToUpperInvariant();

                // Unterstützung für 1-10 Jahre Laufzeit + kleiner Tagespuffer
                for (int years = 1; years <= 10; years++)
                {
                    for (int dayOffset = -2; dayOffset <= 0; dayOffset++)
                    {
                        var expiryDate = DateTime.Now.AddDays(dayOffset).AddYears(years).ToString("yyyyMMdd");
                        var data = $"{hardwareId}|{registeredTo}|{expiryDate}|{MasterSecret}";

                        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(MasterSecret));
                        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                        var hashString = Convert.ToBase64String(hash)
                            .Replace("+", "")
                            .Replace("/", "")
                            .Replace("=", "")
                            .Substring(0, 16)
                            .ToUpperInvariant();

                        var candidate = $"MM-{hashString.Substring(0, 4)}-{hashString.Substring(4, 4)}-{hashString.Substring(8, 4)}-{hashString.Substring(12, 4)}";
                        if (string.Equals(normalizedInput, candidate, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ValidateLicenseKey] Exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generiert einen Test-Lizenzschlüssel (nur für Entwicklung)
        /// </summary>
        public static string GenerateTestLicenseKey()
        {
            return "MM-TEST-1234-5678-9ABC";
        }
    }
}
