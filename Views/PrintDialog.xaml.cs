using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using MaterialManager_V01.Models;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class PrintDialog : Window
    {
        private readonly IEnumerable<MaterialItem> _materialien;

        public PrintDialog(IEnumerable<MaterialItem> materialien)
        {
            InitializeComponent();
            _materialien = materialien ?? new List<MaterialItem>();
        }

        private void OnPrintBestand(object sender, RoutedEventArgs e)
        {
            try
            {
                var html = PdfExportService.GenerateBestandslisteAsPdf(_materialien);
                OpenHtmlInBrowser(html, "Bestandsliste");
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler: {ex.Message}");
            }
        }

        private void OnPrintLagerAuslastung(object sender, RoutedEventArgs e)
        {
            try
            {
                var html = PdfExportService.GenerateLagerAuslastungAsPdf(_materialien);
                OpenHtmlInBrowser(html, "LagerAuslastung");
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler: {ex.Message}");
            }
        }

        private void OnPrintAuditLog(object sender, RoutedEventArgs e)
        {
            try
            {
                var html = PdfExportService.GenerateAuditLogAsHtml();
                OpenHtmlInBrowser(html, "AuditLog");
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler: {ex.Message}");
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static void OpenHtmlInBrowser(string html, string name)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            File.WriteAllText(tempPath, html);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true
            });

            MessageBox.Show("✅ Druckvorschau wird geöffnet.\n\nBrowser: STRG+P zum Drucken", "Erfolgreich");
        }
    }
}
