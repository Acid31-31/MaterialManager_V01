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
