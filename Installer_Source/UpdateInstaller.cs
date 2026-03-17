using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

internal static class Program
{
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
            CopyDirectory(extractDir, options.TargetDirectory);

            var targetExe = Path.Combine(options.TargetDirectory, "MaterialManager_V01.exe");
            if (File.Exists(targetExe))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = targetExe,
                    WorkingDirectory = options.TargetDirectory,
                    UseShellExecute = true
                });
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
        var finalArgs = originalArgs.ToList();
        if (!finalArgs.Any(a => string.Equals(a, "--target", StringComparison.OrdinalIgnoreCase)))
        {
            finalArgs.Add("--target");
            finalArgs.Add(targetDirectory);
        }

        if (waitProcessId > 0 && !finalArgs.Any(a => string.Equals(a, "--waitpid", StringComparison.OrdinalIgnoreCase)))
        {
            finalArgs.Add("--waitpid");
            finalArgs.Add(waitProcessId.ToString());
        }

        var argumentString = string.Join(" ", finalArgs.Select(QuoteArgument));
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
            ? "\"" + value.Replace("\"", "\\\"") + "\""
            : value;
    }

    private static void WaitForProcessExit(int processId, TimeSpan timeout)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            process.WaitForExit((int)timeout.TotalMilliseconds);
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
    }

    private static string ResolveTargetDirectory()
    {
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

            File.Copy(file, destinationFile, true);
        }
    }

    private sealed class UpdateInstallerOptions
    {
        public string TargetDirectory { get; set; } = string.Empty;
        public int WaitProcessId { get; private set; }

        public static UpdateInstallerOptions Parse(string[] args)
        {
            var options = new UpdateInstallerOptions();

            for (var i = 0; i < args.Length; i++)
            {
                var current = args[i];
                if (string.Equals(current, "--target", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    options.TargetDirectory = args[++i];
                }
                else if (string.Equals(current, "--waitpid", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length && int.TryParse(args[++i], out var pid))
                {
                    options.WaitProcessId = pid;
                }
            }

            return options;
        }
    }
}