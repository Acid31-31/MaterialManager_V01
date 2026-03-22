using System;
using System.IO;
using System.Threading;

namespace MaterialManager_V01.Services
{
    public static class AutoSyncManager
    {
        private static Timer _syncTimer;
        private static string _filePath;
        private static DateTime _lastLocalSave = DateTime.MinValue;
        private static DateTime _lastExternalChange = DateTime.MinValue;

        public static bool IsSyncing { get; private set; }
        public static event Action OnAutoSyncTriggered;

        public static void StartAutoSync(string filePath)
        {
            _filePath = filePath;
            _lastLocalSave = DateTime.Now;
            _lastExternalChange = DateTime.Now;
            // Prüfe alle 2 Sekunden auf Änderungen
            _syncTimer = new Timer(_ => CheckForSync(), null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
        }

        public static void RegisterLocalSave(string filePath)
        {
            _lastLocalSave = DateTime.Now;
        }

        private static void CheckForSync()
        {
            try
            {
                if (IsSyncing || string.IsNullOrEmpty(_filePath)) return;

                if (File.Exists(_filePath))
                {
                    var lastWrite = File.GetLastWriteTime(_filePath);
                    // Prüfe ob externe Datei neuer ist als letzte lokale Speicherung
                    if (lastWrite > _lastLocalSave && lastWrite > _lastExternalChange.AddSeconds(-1))
                    {
                        _lastExternalChange = lastWrite;
                        IsSyncing = true;
                        try
                        {
                            OnAutoSyncTriggered?.Invoke();
                        }
                        finally
                        {
                            IsSyncing = false;
                        }
                    }
                }
            }
            catch { IsSyncing = false; }
        }

        public static void StopAutoSync()
        {
            _syncTimer?.Dispose();
            _syncTimer = null;
        }
    }
}
