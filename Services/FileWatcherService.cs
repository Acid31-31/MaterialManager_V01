using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace MaterialManager_V01.Services
{
    public static class FileWatcherService
    {
        private static FileSystemWatcher? _watcher;
        private static Timer? _debounceTimer;
        private static string _lastChangedPath = string.Empty;
        private static string? _watchedDirectory;
        private static HashSet<string> _watchedFiles = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, DateTime> _ignoreUntilByPath = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _syncLock = new();

        public static event Action<string>? OnFileChanged;

        public static void StartWatching(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return;

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
                    try
                    {
                        if (e.GetException() is InternalBufferOverflowException)
                        {
                            var restartPath = filePath;
                            StopWatching();
                            StartWatching(restartPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWatcherError("Watcher-Fehler beim Neustart", ex);
                    }
                };

                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                LogWatcherError("Watcher konnte nicht gestartet werden", ex);
            }
        }

        public static void RegisterLocalWrite(string? fullPath, int ignoreMilliseconds = 3000)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fullPath))
                    return;

                lock (_syncLock)
                {
                    _ignoreUntilByPath[fullPath] = DateTime.UtcNow.AddMilliseconds(ignoreMilliseconds);
                }
            }
            catch
            {
            }
        }

        private static void HandleWatcherEvent(string fullPath)
        {
            try
            {
                var fileName = Path.GetFileName(fullPath);
                if (!_watchedFiles.Contains(fileName))
                    return;

                if (ShouldIgnorePath(fullPath))
                    return;

                _lastChangedPath = fullPath;
                _debounceTimer?.Dispose();
                _debounceTimer = new Timer(_ => RaiseFileChangedSafely(), null, 1500, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                LogWatcherError("Watcher-Ereignis konnte nicht verarbeitet werden", ex);
            }
        }

        private static bool ShouldIgnorePath(string fullPath)
        {
            lock (_syncLock)
            {
                CleanupExpiredIgnores();
                return _ignoreUntilByPath.TryGetValue(fullPath, out var untilUtc) && untilUtc > DateTime.UtcNow;
            }
        }

        private static void CleanupExpiredIgnores()
        {
            var now = DateTime.UtcNow;
            var expired = _ignoreUntilByPath.Where(kvp => kvp.Value <= now).Select(kvp => kvp.Key).ToList();
            foreach (var key in expired)
                _ignoreUntilByPath.Remove(key);
        }

        private static void RaiseFileChangedSafely()
        {
            try
            {
                var path = _lastChangedPath;
                if (string.IsNullOrWhiteSpace(path) || ShouldIgnorePath(path))
                    return;

                var handlers = OnFileChanged?.GetInvocationList();
                if (handlers == null || handlers.Length == 0)
                    return;

                foreach (var handler in handlers)
                {
                    try
                    {
                        ((Action<string>)handler)(path);
                    }
                    catch (Exception ex)
                    {
                        LogWatcherError($"Watcher-Handler fehlgeschlagen für '{path}'", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                LogWatcherError("Watcher-Auslösung fehlgeschlagen", ex);
            }
        }

        public static void StopWatching()
        {
            try
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
            catch (Exception ex)
            {
                LogWatcherError("Watcher konnte nicht gestoppt werden", ex);
            }
        }

        private static void LogWatcherError(string title, Exception ex)
        {
            try
            {
                File.AppendAllText(PathService.LogPath,
                    $"\n[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] {title}\n{ex.Message}\n{ex.StackTrace}\n");
            }
            catch
            {
            }
        }
    }
}
