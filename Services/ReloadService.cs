using System;
using System.IO;

namespace MaterialManager_V01.Services
{
    public static class ReloadService
    {
        private static DateTime _lastLoadTime = DateTime.MinValue;
        private static DateTime _lastCheckTime = DateTime.MinValue;

        public static void RegisterLoad(string filePath)
        {
            _lastLoadTime = DateTime.Now;
            _lastCheckTime = DateTime.Now;
        }

        public static bool ShouldReload(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return false;

                var lastWrite = File.GetLastWriteTime(filePath);
                
                // Prüfe ob Datei neuer ist als last load
                // Tolerance: 100ms um Timing-Probleme zu vermeiden
                bool needsReload = lastWrite > _lastLoadTime.AddMilliseconds(-100);
                
                return needsReload;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Prüft ob die Datei sich häufig ändert (externe Änderungen)
        /// </summary>
        public static bool HasRecentChanges(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return false;

                var lastWrite = File.GetLastWriteTime(filePath);
                var now = DateTime.Now;
                
                // Wenn letzte Änderung weniger als 5 Sekunden alt ist
                return (now - lastWrite).TotalSeconds < 5;
            }
            catch
            {
                return false;
            }
        }
    }
}
