using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MaterialManager_V01.Services
{
    /// <summary>
    /// Verwaltet die 30-Tage Demo-Lizenz
    /// </summary>
    public static class LicenseService
    {
        private static readonly string LicenseFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MaterialManager_V01",
            ".license"
        );

        private const int TrialDays = 30;
        private const string LicenseSecret = "MM_V01_SECRET_2025";

        private class LicenseInfo
        {
            public string FirstStartDate { get; set; } = "";
            public string Hash { get; set; } = "";
            public bool IsFullLicense { get; set; }
            public string? LicenseKey { get; set; }
            public string? RegisteredTo { get; set; }
            public string? HardwareId { get; set; }
        }

        /// <summary>
        /// Prüft, ob die Demo- oder Vollversion-Lizenz gültig ist
        /// </summary>
        /// <returns>True wenn gültig, False wenn abgelaufen</returns>
        public static bool IsLicenseValid()
        {
            try
            {
                var licenseInfo = LoadLicenseInfo();

                if (licenseInfo == null || string.IsNullOrWhiteSpace(licenseInfo.FirstStartDate))
                {
                    // Erste Nutzung - Lizenz erstellen
                    CreateTrialLicense();
                    return true;
                }

                if (licenseInfo.IsFullLicense && !string.IsNullOrWhiteSpace(licenseInfo.LicenseKey))
                    return true;

                // Verifiziere Hash
                if (!VerifyHash(licenseInfo.FirstStartDate, licenseInfo.Hash))
                {
                    // Manipulation erkannt - Lizenz zurücksetzen
                    CreateTrialLicense();
                    return true;
                }

                // Parse Startdatum
                if (DateTime.TryParse(licenseInfo.FirstStartDate, out var firstStart))
                {
                    var daysUsed = (DateTime.Now - firstStart).TotalDays;
                    return daysUsed <= TrialDays;
                }

                return false;
            }
            catch
            {
                // Bei Fehler: Lizenz erstellen
                CreateTrialLicense();
                return true;
            }
        }

        /// <summary>
        /// Gibt die Anzahl verbleibender Testtage zurück
        /// </summary>
        public static int GetRemainingTrialDays()
        {
            try
            {
                var licenseInfo = LoadLicenseInfo();
                if (licenseInfo == null || string.IsNullOrWhiteSpace(licenseInfo.FirstStartDate))
                    return TrialDays;

                if (licenseInfo.IsFullLicense)
                    return int.MaxValue;

                if (DateTime.TryParse(licenseInfo.FirstStartDate, out var firstStart))
                {
                    var daysUsed = (DateTime.Now - firstStart).TotalDays;
                    var remaining = TrialDays - (int)Math.Ceiling(daysUsed);
                    return Math.Max(0, remaining);
                }

                return 0;
            }
            catch
            {
                return TrialDays;
            }
        }

        /// <summary>
        /// Gibt das Ablaufdatum zurück
        /// </summary>
        public static DateTime? GetExpirationDate()
        {
            try
            {
                var licenseInfo = LoadLicenseInfo();
                if (licenseInfo == null || string.IsNullOrWhiteSpace(licenseInfo.FirstStartDate))
                    return DateTime.Now.AddDays(TrialDays);

                if (licenseInfo.IsFullLicense)
                    return null;

                if (DateTime.TryParse(licenseInfo.FirstStartDate, out var firstStart))
                {
                    return firstStart.AddDays(TrialDays);
                }

                return null;
            }
            catch
            {
                return null;
            }
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
                var hardwareId = GetHardwareId();
                
                if (!LicenseKeyGenerator.ValidateLicenseKey(licenseKey, hardwareId, registeredTo.Trim()))
                {
                    System.Diagnostics.Debug.WriteLine("[License] FEHLER: Lizenzschlüssel ungültig!");
                    System.Diagnostics.Debug.WriteLine($"[License] Hardware-ID: {hardwareId}");
                    System.Diagnostics.Debug.WriteLine($"[License] Registered To: {registeredTo}");
                    System.Diagnostics.Debug.WriteLine($"[License] Input Key: {licenseKey}");
                    return false;
                }

                var info = LoadLicenseInfo() ?? new LicenseInfo();
                info.IsFullLicense = true;
                info.LicenseKey = licenseKey.Trim();
                info.RegisteredTo = registeredTo.Trim();
                info.HardwareId = hardwareId;
                if (string.IsNullOrWhiteSpace(info.FirstStartDate))
                    info.FirstStartDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                info.Hash = ComputeHash(info.FirstStartDate);

                if (!SaveLicenseInfo(info))
                {
                    System.Diagnostics.Debug.WriteLine("[License] FEHLER: Lizenzdatei konnte nicht gespeichert werden.");
                    return false;
                }

                // Zusätzliche Verifikation
                var verify = LoadLicenseInfo();
                if (verify == null || !verify.IsFullLicense || string.IsNullOrWhiteSpace(verify.RegisteredTo))
                {
                    System.Diagnostics.Debug.WriteLine("[License] FEHLER: Lizenzdatei-Verifikation fehlgeschlagen.");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine("[License] ✓ Lizenz erfolgreich aktiviert!");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[License] Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[License] StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        private static void CreateTrialLicense()
        {
            try
            {
                var firstStart = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var hash = ComputeHash(firstStart);

                var licenseInfo = new LicenseInfo
                {
                    FirstStartDate = firstStart,
                    Hash = hash
                };

                _ = SaveLicenseInfo(licenseInfo);
            }
            catch { }
        }

        private static LicenseInfo? LoadLicenseInfo()
        {
            try
            {
                if (!File.Exists(LicenseFile))
                    return null;

                var json = File.ReadAllText(LicenseFile);
                return JsonSerializer.Deserialize<LicenseInfo>(json);
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
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(info);
                File.WriteAllText(LicenseFile, json);

                // Datei verstecken (falls erlaubt)
                try
                {
                    var fileInfo = new FileInfo(LicenseFile);
                    fileInfo.Attributes = FileAttributes.Hidden | FileAttributes.System;
                }
                catch
                {
                    // Nicht kritisch für Funktion
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string ComputeHash(string input)
        {
            // Einfacher Hash mit Machine-spezifischen Daten
            var machineId = Environment.MachineName + Environment.UserName;
            var combined = input + machineId + "MaterialManager_Secret_2024";
            
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(combined);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        private static bool VerifyHash(string input, string expectedHash)
        {
            try
            {
                var computedHash = ComputeHash(input);
                return computedHash == expectedHash;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gibt eine formatierte Status-Nachricht zurück
        /// </summary>
        public static string GetStatusMessage()
        {
            var licenseInfo = LoadLicenseInfo();
            if (licenseInfo != null && licenseInfo.IsFullLicense && !string.IsNullOrWhiteSpace(licenseInfo.RegisteredTo))
                return $"Vollversion - Registriert auf: {licenseInfo.RegisteredTo}";

            var remaining = GetRemainingTrialDays();
            var expiration = GetExpirationDate();

            if (remaining > 0 && remaining != int.MaxValue)
            {
                return $"Demo-Version - Noch {remaining} Tage verbleibend\n" +
                       $"Ablaufdatum: {expiration:dd.MM.yyyy}";
            }

            return "Demo-Version abgelaufen!\n" +
                   "Bitte kontaktieren Sie uns für eine Vollversion.";
        }

        /// <summary>
        /// Setzt die Demo-Lizenz auf 30 Tage zurück (für Neuvergabe)
        /// </summary>
        public static void ResetTrial()
        {
            try
            {
                if (File.Exists(LicenseFile))
                    File.Delete(LicenseFile);
                CreateTrialLicense();
            }
            catch { }
        }

        public static bool IsFullLicenseActive()
        {
            try
            {
                var licenseInfo = LoadLicenseInfo();
                return licenseInfo != null && licenseInfo.IsFullLicense && !string.IsNullOrWhiteSpace(licenseInfo.LicenseKey);
            }
            catch
            {
                return false;
            }
        }
    }
}
