using System;
using System.Security.Cryptography;
using System.Text;

namespace MaterialManager_V01.Services
{
    /// <summary>
    /// Generiert eindeutige Hardware-IDs für Lizenzierung
    /// </summary>
    public static class HardwareIdService
    {
        /// <summary>
        /// Generiert eine eindeutige Hardware-ID basierend auf Systeminformationen
        /// </summary>
        public static string GetHardwareId()
        {
            try
            {
                // Kombiniere Systeminformationen zu eindeutiger ID
                var machineId = Environment.MachineName;
                var processorCount = Environment.ProcessorCount;
                var osVersion = Environment.OSVersion.ToString();
                var userName = Environment.UserName;

                var combined = $"{machineId}_{processorCount}_{osVersion}_{userName}";
                using (var sha256 = SHA256.Create())
                {
                    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                    return Convert.ToBase64String(hash).Substring(0, 24).Replace("+", "").Replace("/", "");
                }
            }
            catch
            {
                return "UNKNOWN_HARDWARE_ID";
            }
        }
    }
}
