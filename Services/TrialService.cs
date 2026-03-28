using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Win32;

namespace MaterialManager_V01.Services
{
    public static class TrialService
    {
        private const int TrialDays = 60;
        private const string RegistryKeyPath = "Software\\MaterialManager_V01\\Trial";
        private const string StartKey = "FirstInstallUtc";
        private const string LastRunKey = "LastRunUtc";

        private static string LocalTrialFile => Path.Combine(PathService.DataDirectory, "trial_local.dat");
        private static string HiddenTrialFile => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "MaterialManager_V01",
            ".trial_hidden.dat");

        public sealed class TrialStatus
        {
            public bool IsValid { get; set; }
            public bool IsManipulated { get; set; }
            public int RemainingDays { get; set; }
            public DateTime FirstInstallUtc { get; set; }
            public DateTime LastRunUtc { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        public static TrialStatus ValidateAndUpdate()
        {
            try
            {
                var nowUtc = DateTime.UtcNow;
                var snapshots = ReadAllSnapshots();

                if (snapshots.Count == 0)
                {
                    var created = new TrialSnapshot { FirstInstallUtc = nowUtc, LastRunUtc = nowUtc };
                    WriteAllSnapshots(created);
                    return BuildStatus(created, nowUtc, false, "Testversion gestartet.");
                }

                var firstInstall = snapshots[0].FirstInstallUtc;
                var lastRun = snapshots[0].LastRunUtc;
                var manipulated = false;

                foreach (var snapshot in snapshots)
                {
                    if (snapshot.FirstInstallUtc != firstInstall)
                        manipulated = true;

                    if (snapshot.LastRunUtc > lastRun)
                        lastRun = snapshot.LastRunUtc;
                }

                if (nowUtc < lastRun.AddMinutes(-5))
                    manipulated = true;

                if (manipulated)
                {
                    return new TrialStatus
                    {
                        IsValid = false,
                        IsManipulated = true,
                        RemainingDays = 0,
                        FirstInstallUtc = firstInstall,
                        LastRunUtc = lastRun,
                        Message = "Manipulation der Testversion erkannt."
                    };
                }

                var normalized = new TrialSnapshot
                {
                    FirstInstallUtc = firstInstall,
                    LastRunUtc = nowUtc
                };
                WriteAllSnapshots(normalized);

                return BuildStatus(normalized, nowUtc, false, "Testversion gültig.");
            }
            catch (Exception ex)
            {
                return new TrialStatus
                {
                    IsValid = false,
                    IsManipulated = true,
                    RemainingDays = 0,
                    Message = $"Fehler bei der Testversionsprüfung: {ex.Message}"
                };
            }
        }

        public static void ResetTrial()
        {
            TryDelete(LocalTrialFile);
            TryDelete(HiddenTrialFile);
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(RegistryKeyPath, throwOnMissingSubKey: false);
            }
            catch { }
        }

        private static TrialStatus BuildStatus(TrialSnapshot snapshot, DateTime nowUtc, bool manipulated, string message)
        {
            var elapsedDays = (int)Math.Floor((nowUtc - snapshot.FirstInstallUtc).TotalDays);
            var remaining = Math.Max(0, TrialDays - elapsedDays);

            return new TrialStatus
            {
                IsValid = !manipulated && remaining > 0,
                IsManipulated = manipulated,
                RemainingDays = remaining,
                FirstInstallUtc = snapshot.FirstInstallUtc,
                LastRunUtc = snapshot.LastRunUtc,
                Message = message
            };
        }

        private static List<TrialSnapshot> ReadAllSnapshots()
        {
            var result = new List<TrialSnapshot>();

            var local = ReadSnapshotFromFile(LocalTrialFile);
            if (local != null) result.Add(local);

            var hidden = ReadSnapshotFromFile(HiddenTrialFile);
            if (hidden != null) result.Add(hidden);

            var reg = ReadSnapshotFromRegistry();
            if (reg != null) result.Add(reg);

            return result;
        }

        private static void WriteAllSnapshots(TrialSnapshot snapshot)
        {
            WriteSnapshotToFile(LocalTrialFile, snapshot, hidden: false);
            WriteSnapshotToFile(HiddenTrialFile, snapshot, hidden: true);
            WriteSnapshotToRegistry(snapshot);
        }

        private static TrialSnapshot? ReadSnapshotFromFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                var lines = File.ReadAllLines(path);
                if (lines.Length < 2)
                    return null;

                if (!DateTime.TryParse(lines[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var first))
                    return null;
                if (!DateTime.TryParse(lines[1], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var last))
                    return null;

                return new TrialSnapshot { FirstInstallUtc = first, LastRunUtc = last };
            }
            catch
            {
                return null;
            }
        }

        private static void WriteSnapshotToFile(string path, TrialSnapshot snapshot, bool hidden)
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllLines(path, new[]
                {
                    snapshot.FirstInstallUtc.ToString("O", CultureInfo.InvariantCulture),
                    snapshot.LastRunUtc.ToString("O", CultureInfo.InvariantCulture)
                });

                if (hidden)
                {
                    try
                    {
                        var fi = new FileInfo(path);
                        fi.Attributes = FileAttributes.Hidden | FileAttributes.System;
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static TrialSnapshot? ReadSnapshotFromRegistry()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false);
                if (key == null) return null;

                var firstRaw = key.GetValue(StartKey) as string;
                var lastRaw = key.GetValue(LastRunKey) as string;
                if (string.IsNullOrWhiteSpace(firstRaw) || string.IsNullOrWhiteSpace(lastRaw))
                    return null;

                if (!DateTime.TryParse(firstRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var first))
                    return null;
                if (!DateTime.TryParse(lastRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var last))
                    return null;

                return new TrialSnapshot { FirstInstallUtc = first, LastRunUtc = last };
            }
            catch
            {
                return null;
            }
        }

        private static void WriteSnapshotToRegistry(TrialSnapshot snapshot)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath, writable: true);
                if (key == null) return;

                key.SetValue(StartKey, snapshot.FirstInstallUtc.ToString("O", CultureInfo.InvariantCulture));
                key.SetValue(LastRunKey, snapshot.LastRunUtc.ToString("O", CultureInfo.InvariantCulture));
            }
            catch { }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch { }
        }

        private sealed class TrialSnapshot
        {
            public DateTime FirstInstallUtc { get; set; }
            public DateTime LastRunUtc { get; set; }
        }
    }
}
