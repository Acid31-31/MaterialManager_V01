using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

internal static class Program
{
    private static bool _rebootRequired;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool MoveFileEx(string? lpExistingFileName, string? lpNewFileName, int dwFlags);

    private const int MoveFileReplaceExisting = 0x1;
    private const int MoveFileDelayUntilReboot = 0x4;

    [STAThread]
    private static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            var options = UpdateInstallerOptions.Parse(args);
            if (string.IsNullOrWhiteSpace(options.TargetDirectory))
                options.TargetDirectory = ResolveTargetDirectory();

            if (string.IsNullOrWhiteSpace(options.TargetDirectory))
                throw new InvalidOperationException("Kein Zielpfad übergeben oder gefunden.");

            if (RequiresElevation(options.TargetDirectory) && !IsRunningAsAdministrator())
            {
                RelaunchElevated(args, options.TargetDirectory, options.WaitProcessId);
                return;
            }

            if (!Directory.Exists(options.TargetDirectory))
                throw new DirectoryNotFoundException($"Zielpfad nicht gefunden: {options.TargetDirectory}");

            if (options.WaitProcessId > 0)
                WaitForProcessExit(options.WaitProcessId, TimeSpan.FromMinutes(2));
            else
                WaitForKnownMaterialManagerProcesses(TimeSpan.FromMinutes(2));

            var tempRoot = Path.Combine(Path.GetTempPath(), "MaterialManager_UpdateInstaller", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(tempRoot);

            var zipPath = Path.Combine(tempRoot, "payload.zip");
            using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
            {
                if (resource == null)
                    throw new InvalidOperationException("Update-Payload nicht gefunden.");

                using var file = File.Create(zipPath);
                resource.CopyTo(file);
            }

            var extractDir = Path.Combine(tempRoot, "payload");
            ZipFile.ExtractToDirectory(zipPath, extractDir, true);
            CleanTargetDirectory(options.TargetDirectory);
            CopyDirectory(extractDir, options.TargetDirectory);

            if (_rebootRequired)
            {
                MessageBox.Show(
                    "Einige Dateien waren gesperrt und werden beim nächsten Neustart automatisch ersetzt.\n\nBitte den PC neu starten.",
                    "MaterialManager Update",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (!TryLaunchUpdatedApplication(options.TargetDirectory, out var launchError))
            {
                MessageBox.Show(
                    "Update wurde installiert, aber die App konnte nicht automatisch gestartet werden.\n\n" + launchError,
                    "MaterialManager Update",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            Thread.Sleep(2000);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Update konnte nicht installiert werden:\n\n" + ex.Message,
                "MaterialManager Update",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Environment.Exit(1);
        }
    }

    private static bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static bool RequiresElevation(string targetDirectory)
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        return targetDirectory.StartsWith(programFiles, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(programFilesX86) && targetDirectory.StartsWith(programFilesX86, StringComparison.OrdinalIgnoreCase));
    }

    private static void RelaunchElevated(string[] originalArgs, string targetDirectory, int waitProcessId)
    {
        var argumentParts = new List<string>
        {
            "--target",
            QuoteArgument(targetDirectory)
        };

        if (waitProcessId > 0)
        {
            argumentParts.Add("--waitpid");
            argumentParts.Add(waitProcessId.ToString());
        }

        var argumentString = string.Join(" ", argumentParts);
        var currentExe = Environment.ProcessPath ?? throw new InvalidOperationException("Installer-Pfad konnte nicht ermittelt werden.");

        Process.Start(new ProcessStartInfo
        {
            FileName = currentExe,
            Arguments = argumentString,
            UseShellExecute = true,
            Verb = "runas"
        });
    }

    private static string QuoteArgument(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        return value.Contains(' ') || value.Contains('"')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }

    private static string SanitizeTargetDirectory(string value, UpdateInstallerOptions options)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var sanitized = value.Trim().Trim('"');
        var waitPidMarker = sanitized.IndexOf(" --waitpid ", StringComparison.OrdinalIgnoreCase);
        if (waitPidMarker >= 0)
        {
            var waitPidValue = sanitized[(waitPidMarker + " --waitpid ".Length)..].Trim().Trim('"');
            if (options.WaitProcessId <= 0 && int.TryParse(waitPidValue, out var parsedPid))
                options.WaitProcessId = parsedPid;

            sanitized = sanitized[..waitPidMarker].TrimEnd();
        }

        return sanitized.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static void WaitForProcessExit(int processId, TimeSpan timeout)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            if (!process.WaitForExit((int)timeout.TotalMilliseconds))
                TryTerminateProcess(process);
        }
        catch
        {
        }
    }

    private static void WaitForKnownMaterialManagerProcesses(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            var running = Process.GetProcessesByName("MaterialManager_V01");
            if (running.Length == 0)
                return;

            Thread.Sleep(500);
        }

        foreach (var process in Process.GetProcessesByName("MaterialManager_V01"))
            TryTerminateProcess(process);
    }

    private static void TryTerminateProcess(Process process)
    {
        try
        {
            if (process.HasExited)
                return;

            process.Kill(true);
            process.WaitForExit(10000);
        }
        catch
        {
        }
    }

    private static string ResolveTargetDirectory()
    {
        var fromRunningProcess = ReadInstallLocationFromRunningProcess();
        if (!string.IsNullOrWhiteSpace(fromRunningProcess))
            return fromRunningProcess;

        var fromRegistry = ReadInstallLocationFromRegistry();
        if (!string.IsNullOrWhiteSpace(fromRegistry))
            return fromRegistry;

        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "MaterialManager_V01"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "MaterialManager"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MaterialManager_V01"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MaterialManager")
        };

        return candidates.FirstOrDefault(IsValidInstallDirectory) ?? string.Empty;
    }

    private static string ReadInstallLocationFromRunningProcess()
    {
        try
        {
            foreach (var process in Process.GetProcessesByName("MaterialManager_V01"))
            {
                try
                {
                    var exePath = process.MainModule?.FileName;
                    var directory = string.IsNullOrWhiteSpace(exePath) ? string.Empty : Path.GetDirectoryName(exePath) ?? string.Empty;
                    if (IsValidInstallDirectory(directory))
                        return directory;
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

        return string.Empty;
    }

    private static string ReadInstallLocationFromRegistry()
    {
        var keys = new[]
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\MaterialManager_V01",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\MaterialManager_V01"
        };

        foreach (var key in keys)
        {
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
                {
                    try
                    {
                        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                        using var subKey = baseKey.OpenSubKey(key);
                        var installLocation = subKey?.GetValue("InstallLocation") as string;
                        if (!string.IsNullOrWhiteSpace(installLocation) && IsValidInstallDirectory(installLocation))
                            return installLocation;
                    }
                    catch
                    {
                    }
                }
            }
        }

        return string.Empty;
    }

    private static bool IsValidInstallDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return false;

        return File.Exists(Path.Combine(path, "MaterialManager_V01.exe"));
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        foreach (var directory in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, directory);
            Directory.CreateDirectory(Path.Combine(destinationDir, relative));
        }

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, file);
            var destinationFile = Path.Combine(destinationDir, relative);
            var destinationParent = Path.GetDirectoryName(destinationFile);
            if (!string.IsNullOrWhiteSpace(destinationParent))
                Directory.CreateDirectory(destinationParent);

            CopyFileWithRetry(file, destinationFile, retryCount: 40, retryDelayMs: 500);
        }
    }

    private static void CopyFileWithRetry(string sourceFile, string destinationFile, int retryCount, int retryDelayMs)
    {
        Exception? lastError = null;

        for (var attempt = 1; attempt <= retryCount; attempt++)
        {
            try
            {
                if (File.Exists(destinationFile))
                    File.SetAttributes(destinationFile, FileAttributes.Normal);

                File.Copy(sourceFile, destinationFile, true);
                return;
            }
            catch (IOException ex)
            {
                lastError = ex;
            }
            catch (UnauthorizedAccessException ex)
            {
                lastError = ex;
            }

            Thread.Sleep(retryDelayMs);
        }

        if (TryScheduleReplaceOnReboot(sourceFile, destinationFile))
        {
            _rebootRequired = true;
            return;
        }

        throw new IOException($"Datei konnte nicht ersetzt werden: {destinationFile}\n{lastError?.Message}", lastError);
    }

    private static bool TryScheduleReplaceOnReboot(string sourceFile, string destinationFile)
    {
        try
        {
            var tempReplacement = destinationFile + ".mmupd";
            var tempParent = Path.GetDirectoryName(tempReplacement);
            if (!string.IsNullOrWhiteSpace(tempParent) && !Directory.Exists(tempParent))
                Directory.CreateDirectory(tempParent);

            File.Copy(sourceFile, tempReplacement, true);
            return MoveFileEx(tempReplacement, destinationFile, MoveFileReplaceExisting | MoveFileDelayUntilReboot);
        }
        catch
        {
            return false;
        }
    }

    private static void CleanTargetDirectory(string targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(targetDirectory) || !Directory.Exists(targetDirectory))
            return;

        foreach (var file in Directory.GetFiles(targetDirectory, "*", SearchOption.AllDirectories))
        {
            try
            {
                DeleteFileWithRetry(file, retryCount: 20, retryDelayMs: 250);
            }
            catch
            {
            }
        }

        var directories = Directory
            .GetDirectories(targetDirectory, "*", SearchOption.AllDirectories)
            .OrderByDescending(d => d.Length)
            .ToList();

        foreach (var directory in directories)
        {
            try
            {
                if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                    Directory.Delete(directory, false);
            }
            catch
            {
            }
        }
    }

    private static void DeleteFileWithRetry(string path, int retryCount, int retryDelayMs)
    {
        Exception? lastError = null;

        for (var attempt = 1; attempt <= retryCount; attempt++)
        {
            try
            {
                if (!File.Exists(path))
                    return;

                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
                return;
            }
            catch (IOException ex)
            {
                lastError = ex;
            }
            catch (UnauthorizedAccessException ex)
            {
                lastError = ex;
            }

            Thread.Sleep(retryDelayMs);
        }

        throw new IOException($"Datei konnte nicht gelöscht werden: {path}\n{lastError?.Message}", lastError);
    }

    private static bool TryLaunchUpdatedApplication(string targetDirectory, out string error)
    {
        error = string.Empty;

        try
        {
            var preferredExe = Path.Combine(targetDirectory, "MaterialManager_V01.exe");
            string? launchExe = null;

            for (var attempt = 0; attempt < 8; attempt++)
            {
                if (File.Exists(preferredExe))
                {
                    launchExe = preferredExe;
                    break;
                }

                var candidates = Directory.Exists(targetDirectory)
                    ? Directory.GetFiles(targetDirectory, "*.exe", SearchOption.TopDirectoryOnly)
                        .Where(p =>
                            !string.Equals(Path.GetFileName(p), "UpdateInstaller.exe", StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(Path.GetFileName(p), "MaterialManager.LicenseGenerator.exe", StringComparison.OrdinalIgnoreCase))
                        .ToList()
                    : new List<string>();

                if (candidates.Count > 0)
                {
                    launchExe = candidates[0];
                    break;
                }

                Thread.Sleep(500);
            }

            if (string.IsNullOrWhiteSpace(launchExe) || !File.Exists(launchExe))
            {
                error = $"Keine startbare EXE im Zielordner gefunden: {targetDirectory}";
                return false;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = launchExe,
                Arguments = "--updated",
                WorkingDirectory = targetDirectory,
                UseShellExecute = true
            });

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private sealed class UpdateInstallerOptions
    {
        public string TargetDirectory { get; set; } = string.Empty;
        public int WaitProcessId { get; set; }

        public static UpdateInstallerOptions Parse(string[] args)
        {
            var options = new UpdateInstallerOptions();

            for (var i = 0; i < args.Length; i++)
            {
                var current = args[i];
                if (string.Equals(current, "--target", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    options.TargetDirectory = SanitizeTargetDirectory(args[++i], options);
                }
                else if (string.Equals(current, "--waitpid", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length && int.TryParse(args[++i], out var pid))
                {
                    options.WaitProcessId = pid;
                }
            }

            options.TargetDirectory = SanitizeTargetDirectory(options.TargetDirectory, options);
            return options;
        }
    }
}