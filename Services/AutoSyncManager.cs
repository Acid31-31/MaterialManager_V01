using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace MaterialManager_V01.Services
{
    public static class AutoSyncManager
    {
        private static Timer? _syncTimer;
        private static readonly object _syncLock = new();
        private static readonly Dictionary<string, DateTime> _lastKnownWriteTimes = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, DateTime> _ignoreUntilByPath = new(StringComparer.OrdinalIgnoreCase);
        private static List<string> _filePaths = new();

        public static bool IsSyncing { get; private set; }
        public static event Action? OnAutoSyncTriggered;

        public static void StartAutoSync(string filePath)
        {
            var paths = BuildWatchedPaths(filePath);
            if (paths.Count == 0)
                return;

            lock (_syncLock)
            {
                _filePaths = paths;
                foreach (var path in _filePaths)
                    _lastKnownWriteTimes[path] = GetWriteTimeUtc(path);

                _syncTimer?.Dispose();
                _syncTimer = new Timer(_ => CheckForSync(), null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
            }
        }

        public static void RegisterLocalSave(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            lock (_syncLock)
            {
                _ignoreUntilByPath[filePath] = DateTime.UtcNow.AddSeconds(4);
                _lastKnownWriteTimes[filePath] = GetWriteTimeUtc(filePath);
            }
        }

        private static void CheckForSync()
        {
            try
            {
                lock (_syncLock)
                {
                    if (IsSyncing || _filePaths.Count == 0)
                        return;

                    CleanupExpiredIgnores();

                    var changed = false;
                    foreach (var path in _filePaths)
                    {
                        var currentWrite = GetWriteTimeUtc(path);
                        _lastKnownWriteTimes.TryGetValue(path, out var previousWrite);

                        if (currentWrite <= previousWrite)
                            continue;

                        _lastKnownWriteTimes[path] = currentWrite;

                        if (_ignoreUntilByPath.TryGetValue(path, out var ignoreUntil) && ignoreUntil > DateTime.UtcNow)
                            continue;

                        changed = true;
                    }

                    if (!changed)
                        return;

                    IsSyncing = true;
                }

                RaiseTriggeredSafely();
            }
            catch (Exception ex)
            {
                LogSyncError("AutoSync-Prüfung fehlgeschlagen", ex);
            }
            finally
            {
                IsSyncing = false;
            }
        }

        private static void RaiseTriggeredSafely()
        {
            var handlers = OnAutoSyncTriggered?.GetInvocationList();
            if (handlers == null)
                return;

            foreach (var handler in handlers)
            {
                try
                {
                    ((Action)handler)();
                }
                catch (Exception ex)
                {
                    LogSyncError("AutoSync-Handler fehlgeschlagen", ex);
                }
            }
        }

        private static List<string> BuildWatchedPaths(string filePath)
        {
            var result = new List<string>();
            if (!string.IsNullOrWhiteSpace(filePath))
                result.Add(filePath);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                result.Add(Path.Combine(directory, "auftraege.json"));

            return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static DateTime GetWriteTimeUtc(string path)
        {
            try
            {
                return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private static void CleanupExpiredIgnores()
        {
            var now = DateTime.UtcNow;
            var expired = _ignoreUntilByPath.Where(kvp => kvp.Value <= now).Select(kvp => kvp.Key).ToList();
            foreach (var key in expired)
                _ignoreUntilByPath.Remove(key);
        }

        public static void StopAutoSync()
        {
            lock (_syncLock)
            {
                _syncTimer?.Dispose();
                _syncTimer = null;
                _filePaths.Clear();
                _lastKnownWriteTimes.Clear();
                _ignoreUntilByPath.Clear();
            }
        }

        private static void LogSyncError(string title, Exception ex)
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
