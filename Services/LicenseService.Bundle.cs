using System;
using System.IO;

namespace MaterialManager_V01.Services
{
    public static partial class LicenseService
    {
        private static void EnsureLocalLicenseFromBundledSource()
        {
            try
            {
                if (File.Exists(LicenseFile))
                    return;

                var bundledLicensePath = Path.Combine(PathService.InstallDirectory, "license.dat");
                if (!File.Exists(bundledLicensePath))
                    return;

                var bundledJson = File.ReadAllText(bundledLicensePath);
                using var bundledDoc = System.Text.Json.JsonDocument.Parse(bundledJson);
                if (bundledDoc.RootElement.TryGetProperty("IsFullLicense", out var isFull) && isFull.GetBoolean())
                    return;

                var dir = Path.GetDirectoryName(LicenseFile);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.Copy(bundledLicensePath, LicenseFile, overwrite: false);
            }
            catch
            {
            }
        }
    }
}
