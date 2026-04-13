using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace MaterialManager_V01.Services
{
    public static class FileWatcherService
    {
        private static FileSystemWatcher _watcher;
        private static Timer _debounceTimer;
        private static string _lastChangedPath;
        private static string _watchedDirectory;
        private static HashSet<string> _watchedFiles = new(StringComparer.OrdinalIgnoreCase);

        public static event Action<string> OnFileChanged;

        public static void StartWatching(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            var directory = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName) || !Directory.Exists(directory))
                return;

            var watchedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                fileName,
                "auftraege.json"
            };

            if (_watcher != null
                && string.Equals(_watchedDirectory, directory, StringComparison.OrdinalIgnoreCase)
                && _watchedFiles.SetEquals(watchedFiles))
                return;

            StopWatching();

            _watchedDirectory = directory;
            _watchedFiles = watchedFiles;

            _watcher = new FileSystemWatcher(directory)
            {
                Filter = "*.*",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
            };

            _watcher.Changed += (s, e) => HandleWatcherEvent(e.FullPath);
            _watcher.Renamed += (s, e) => HandleWatcherEvent(e.FullPath);
            _watcher.Created += (s, e) => HandleWatcherEvent(e.FullPath);

            _watcher.Error += (s, e) =>
            {
                if (e.GetException() is InternalBufferOverflowException)
                {
                    var restartPath = filePath;
                    StopWatching();
                    StartWatching(restartPath);
                }
            };

            _watcher.EnableRaisingEvents = true;
            System.Diagnostics.Debug.WriteLine($"[FileWatcher] Überwache jetzt: {directory} ({string.Join(", ", _watchedFiles.OrderBy(x => x))})");
        }

        private static void HandleWatcherEvent(string fullPath)
        {
            try
            {
                var fileName = Path.GetFileName(fullPath);
                if (!_watchedFiles.Contains(fileName))
                    return;

                _lastChangedPath = fullPath;
                _debounceTimer?.Dispose();
                _debounceTimer = new Timer(_ =>
                {
                    System.Diagnostics.Debug.WriteLine($"[FileWatcher] Änderung erkannt (debounced): {_lastChangedPath}");
                    OnFileChanged?.Invoke(_lastChangedPath);
                }, null, 500, Timeout.Infinite);
            }
            catch
            {
            }
        }

        public static void StopWatching()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }

            _watchedDirectory = null;
            _watchedFiles.Clear();
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }
    }
}
