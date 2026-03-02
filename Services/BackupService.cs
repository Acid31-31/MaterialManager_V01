using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace MaterialManager_V01.Services
{
    public static class BackupService
    {
        private static readonly string BackupDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MaterialManager_V01", "Backups");

        private static Timer? _backupTimer;
        private static Action? _backupAction;
        private static DateTime _lastBackup = DateTime.MinValue;

        /// <summary>
        /// Startet automatisches Backup alle 30 Minuten
        /// </summary>
        public static void StartAutoBackup(Action backupAction)
        {
            _backupAction = backupAction;
            _backupTimer = new Timer(_ => RunBackup(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(30));
        }

        public static void StopAutoBackup()
        {
            _backupTimer?.Dispose();
            _backupTimer = null;
        }

        private static void RunBackup()
        {
            try
            {
                _backupAction?.Invoke();
                _lastBackup = DateTime.Now;
            }
            catch { }
        }

        /// <summary>
        /// Erstellt ein Backup der Materialdaten
        /// </summary>
        public static void CreateBackup(System.Collections.Generic.IEnumerable<Models.MaterialItem> materialien)
        {
            try
            {
                if (!Directory.Exists(BackupDir))
                    Directory.CreateDirectory(BackupDir);

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
                var filename = $"Backup_{timestamp}.xlsx";
                var path = Path.Combine(BackupDir, filename);

                ExcelService.Export(path, materialien);
                _lastBackup = DateTime.Now;

                CleanOldBackups();
            }
            catch { }
        }

        /// <summary>
        /// Behält nur die letzten 20 Backups
        /// </summary>
        private static void CleanOldBackups()
        {
            try
            {
                var files = Directory.GetFiles(BackupDir, "Backup_*.xlsx")
                    .OrderByDescending(f => f)
                    .Skip(20)
                    .ToList();

                foreach (var file in files)
                {
                    try { File.Delete(file); } catch { }
                }
            }
            catch { }
        }

        /// <summary>
        /// Gibt den Zeitpunkt des letzten Backups zurück
        /// </summary>
        public static string GetLastBackupInfo()
        {
            if (_lastBackup == DateTime.MinValue)
            {
                // Prüfe ob es alte Backups gibt
                try
                {
                    if (Directory.Exists(BackupDir))
                    {
                        var latest = Directory.GetFiles(BackupDir, "Backup_*.xlsx")
                            .OrderByDescending(f => f)
                            .FirstOrDefault();

                        if (latest != null)
                            return $"Letztes Backup: {File.GetLastWriteTime(latest):dd.MM.yyyy HH:mm}";
                    }
                }
                catch { }
                return "Noch kein Backup";
            }

            return $"Letztes Backup: {_lastBackup:HH:mm}";
        }

        /// <summary>
        /// Gibt alle verfügbaren Backups zurück
        /// </summary>
        public static System.Collections.Generic.List<string> GetAvailableBackups()
        {
            var result = new System.Collections.Generic.List<string>();
            try
            {
                if (Directory.Exists(BackupDir))
                {
                    result = Directory.GetFiles(BackupDir, "Backup_*.xlsx")
                        .OrderByDescending(f => f)
                        .ToList();
                }
            }
            catch { }
            return result;
        }

        /// <summary>
        /// Stellt ein Backup wieder her
        /// </summary>
        public static System.Collections.Generic.IEnumerable<Models.MaterialItem> RestoreBackup(string backupPath)
        {
            return ExcelService.Import(backupPath);
        }

        public static string BackupFolder => BackupDir;
    }
}
