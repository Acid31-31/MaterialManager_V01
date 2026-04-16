using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class ArchivAuftraegeDialog : Window
    {
        private readonly int _jahr;
        public ObservableCollection<ArchivAuftragEintrag> ArchivAuftraege { get; } = new();

        public ArchivAuftraegeDialog(int startKw, int jahr)
        {
            InitializeComponent();
            DataContext = this;
            _jahr = jahr;

            for (var kw = 1; kw <= 53; kw++)
                KwComboBox.Items.Add($"KW {kw:D2} ({_jahr})");

            KwComboBox.SelectedIndex = Math.Clamp(startKw - 1, 0, 52);
            LoadArchiv();
        }

        private int GetSelectedKw()
        {
            return KwComboBox.SelectedIndex + 1;
        }

        private void LoadArchiv()
        {
            ArchivAuftraege.Clear();
            foreach (var item in AuftragArchivService.GetArchivedOrdersForWeek(_jahr, GetSelectedKw()))
                ArchivAuftraege.Add(item);

            UpdateEmptyState();
        }

        private void UpdateEmptyState()
        {
            var hasItems = ArchivAuftraege.Count > 0;
            EmptyStatePanel.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;
            EmptyStateText.Text = hasItems
                ? string.Empty
                : $"Für KW {GetSelectedKw():D2} ({_jahr}) wurden keine Aufträge archiviert.\n\nDas kann z. B. bei Feiertagen oder Stillstand normal sein.";
        }

        private ArchivAuftragEintrag? GetSelectedEntry()
        {
            return ArchivGrid.SelectedItem as ArchivAuftragEintrag;
        }

        private void OnKwChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            LoadArchiv();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            LoadArchiv();
        }

        private void OnOpenFolderClick(object sender, RoutedEventArgs e)
        {
            var entry = GetSelectedEntry();
            if (entry == null)
            {
                MessageBox.Show("Bitte zuerst einen Archivauftrag auswählen.", "Archiv", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.IsNullOrWhiteSpace(entry.ErstePdfPfad) && File.Exists(entry.ErstePdfPfad))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{entry.ErstePdfPfad}\"",
                    UseShellExecute = true
                });
                return;
            }

            if (!Directory.Exists(entry.OrdnerPfad))
            {
                MessageBox.Show("Archivordner wurde nicht gefunden.", "Archiv", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{entry.OrdnerPfad}\"",
                UseShellExecute = true
            });
        }

        private void OnOpenPdfClick(object sender, RoutedEventArgs e)
        {
            var entry = GetSelectedEntry();
            if (entry == null)
            {
                MessageBox.Show("Bitte zuerst einen Archivauftrag auswählen.", "Archiv", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(entry.ErstePdfPfad) || !File.Exists(entry.ErstePdfPfad))
            {
                MessageBox.Show("Für diesen Archivauftrag wurde keine PDF gefunden.", "Archiv", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PdfOpenService.TryOpenPdf(entry.ErstePdfPfad, this, "Archiv");
        }

        private void OnOpenBegruendungClick(object sender, RoutedEventArgs e)
        {
            var entry = (sender as FrameworkElement)?.DataContext as ArchivAuftragEintrag
                        ?? GetSelectedEntry();
            if (entry == null)
            {
                MessageBox.Show("Bitte zuerst einen Archivauftrag auswählen.", "Archiv", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(entry.ProduktionsBegruendung))
            {
                MessageBox.Show("Für diesen Auftrag wurde keine Begründung hinterlegt.", "Begründung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show(entry.ProduktionsBegruendung, $"Begründung – Auftrag {entry.Auftragsnummer}", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
