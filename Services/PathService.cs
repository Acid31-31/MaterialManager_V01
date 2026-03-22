using System;
using System.IO;

namespace MaterialManager_V01.Services
{
    /// <summary>
    /// Zentrale Verwaltung aller Dateipfade
    /// WICHTIG: Verwendet %APPDATA% statt C:\Program Files (kein Admin nötig!)
    /// </summary>
    public static class PathService
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MaterialManager_V01"
        );

        /// <summary>
        /// Hauptverzeichnis für alle Anwendungsdaten in APPDATA
        /// Z.B.: C:\Users\[USER]\AppData\Local\MaterialManager_V01\
        /// </summary>
        public static string DataDirectory
        {
            get
            {
                if (!Directory.Exists(AppDataFolder))
                    Directory.CreateDirectory(AppDataFolder);
                return AppDataFolder;
            }
        }

        /// <summary>
        /// Datenbank-Datei
        /// Z.B.: C:\Users\[USER]\AppData\Local\MaterialManager_V01\materialmanager.db
        /// </summary>
        public static string DatabasePath => Path.Combine(DataDirectory, "materialmanager.db");

        /// <summary>
        /// Log-Datei für Fehler und Debug-Ausgaben
        /// </summary>
        public static string LogPath => Path.Combine(DataDirectory, "startup_log.txt");

        /// <summary>
        /// Backup-Verzeichnis
        /// </summary>
        public static string BackupDirectory
        {
            get
            {
                var path = Path.Combine(DataDirectory, "Backups");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        }

        /// <summary>
        /// Lizenz-Datei
        /// </summary>
        public static string LicensePath => Path.Combine(DataDirectory, "license.dat");

        /// <summary>
        /// Hardware-ID-Datei
        /// </summary>
        public static string HardwareIdPath => Path.Combine(DataDirectory, "hwid.dat");

        /// <summary>
        /// Einstellungen/Config-Datei
        /// </summary>
        public static string ConfigPath => Path.Combine(DataDirectory, "config.json");

        /// <summary>
        /// Installationsverzeichnis (nur für LESEN von Dateien!)
        /// Z.B.: C:\Program Files\MaterialManager_V01\
        /// WICHTIG: NICHT in diesen Ordner schreiben (Admin-Rechte nötig!)
        /// </summary>
        public static string InstallDirectory => AppDomain.CurrentDomain.BaseDirectory;
    }
}
