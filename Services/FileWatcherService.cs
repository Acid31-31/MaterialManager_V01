using System;
using System.IO;
using System.Threading;

namespace MaterialManager_V01.Services
{
    public static class FileWatcherService
    {
        private static FileSystemWatcher _watcher;
        private static Timer _debounceTimer;
        private static string _lastChangedPath;
        
        public static event Action<string> OnFileChanged;

        public static void StartWatching(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            var directory = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName) || !Directory.Exists(directory))
                return;

            if (_watcher != null)
            {
                if (string.Equals(_watcher.Path, directory, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(_watcher.Filter, fileName, StringComparison.OrdinalIgnoreCase))
                    return;

                StopWatching();
            }

            _watcher = new FileSystemWatcher(directory)
            {
                Filter = fileName,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
            };

            // ✅ DEBOUNCE: Mehrere Events zusammenfassen (Excel triggert oft mehrfach)
            _watcher.Changed += (s, e) => 
            {
                if (e.ChangeType == WatcherChangeTypes.Changed)
                {
                    _lastChangedPath = e.FullPath;
                    _debounceTimer?.Dispose();
                    _debounceTimer = new Timer(_ => 
                    {
                        System.Diagnostics.Debug.WriteLine($"[FileWatcher] Änderung erkannt (debounced): {_lastChangedPath}");
                        OnFileChanged?.Invoke(_lastChangedPath);
                    }, null, 500, Timeout.Infinite);
                }
            };
            
            _watcher.Renamed += (s, e) => OnFileChanged?.Invoke(e.FullPath);
            _watcher.Created += (s, e) => OnFileChanged?.Invoke(e.FullPath);
            
            _watcher.Error += (s, e) => 
            {
                if (e.GetException() is InternalBufferOverflowException)
                {
                    StopWatching();
                    StartWatching(filePath);
                }
            };
            
            _watcher.EnableRaisingEvents = true;
            System.Diagnostics.Debug.WriteLine($"[FileWatcher] Überwache jetzt: {filePath}");
        }

        public static void StopWatching()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }
            
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }
    }
}
