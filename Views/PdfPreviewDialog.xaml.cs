using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class PdfPreviewDialog : Window
    {
        private string _pdfPath;

        public PdfPreviewDialog(string pdfPath)
        {
            InitializeComponent();
            _pdfPath = pdfPath;
            LoadPdfInfo();
        }

        private void LoadPdfInfo()
        {
            try
            {
                if (!File.Exists(_pdfPath))
                {
                    ShowError($"PDF-Datei nicht gefunden:\n{_pdfPath}");
                    return;
                }

                var fileInfo = new FileInfo(_pdfPath);
                FileNameTextBlock.Text = fileInfo.Name;
                FilePathTextBlock.Text = _pdfPath;
                FileSizeTextBlock.Text = FormatFileSize(fileInfo.Length);
                ModifiedDateTextBlock.Text = fileInfo.LastWriteTime.ToString("dd.MM.yyyy HH:mm");
                
                InfoTextBlock.Text = $"Dateiname: {fileInfo.Name}\n" +
                                   $"Größe: {FormatFileSize(fileInfo.Length)}\n" +
                                   $"Geändert: {fileInfo.LastWriteTime:dd.MM.yyyy HH:mm}\n\n" +
                                   $"Klicken Sie auf 'Extern öffnen' um die PDF in der Standard-Anwendung anzuzeigen.";
            }
            catch (Exception ex)
            {
                ShowError($"Fehler beim Laden der PDF-Informationen:\n{ex.Message}");
            }
        }

        private void OnOpenExternalClick(object sender, RoutedEventArgs e)
        {
            if (PdfOpenService.TryOpenPdf(_pdfPath, this, "PDF-Vorschau"))
                StatusTextBlock.Text = "PDF wird extern geöffnet...";
        }

        private void OnCopyPathClick(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Clipboard.SetText(_pdfPath);
                StatusTextBlock.Text = "Dateipfad wurde in die Zwischenablage kopiert";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Kopieren:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShowError(string message)
        {
            FileNameTextBlock.Text = "Fehler";
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
