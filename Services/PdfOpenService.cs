using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace MaterialManager_V01.Services
{
    public static class PdfOpenService
    {
        private static readonly object Sync = new();
        private static DateTime _lastOpenUtc = DateTime.MinValue;
        private static readonly TimeSpan Cooldown = TimeSpan.FromMilliseconds(1200);

        public static bool TryOpenPdf(string pdfPath, Window? owner, string title = "PDF")
        {
            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
            {
                MessageBox.Show(owner,
                    "PDF-Datei nicht gefunden.",
                    title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            lock (Sync)
            {
                var now = DateTime.UtcNow;
                if (now - _lastOpenUtc < Cooldown)
                    return true;

                _lastOpenUtc = now;
            }

            try
            {
                if (TryOpenWithEdge(pdfPath))
                    return true;

                Process.Start(new ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                });

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner,
                    $"PDF konnte nicht geöffnet werden:\n{ex.Message}",
                    title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        private static bool TryOpenWithEdge(string pdfPath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "msedge.exe",
                    Arguments = QuoteArgument(pdfPath),
                    UseShellExecute = true
                };

                var process = Process.Start(psi);
                return process != null;
            }
            catch
            {
                return false;
            }
        }

        private static string QuoteArgument(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "\"\"";

            return value.Contains(' ') || value.Contains('"')
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }
    }
}
