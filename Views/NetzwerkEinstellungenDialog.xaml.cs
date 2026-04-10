using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class NetzwerkEinstellungenDialog : Window
    {
        public NetzwerkEinstellungenDialog()
        {
            InitializeComponent();
            AktivCheck.IsChecked = NetzwerkService.IsNetzwerkModus;
            PfadBox.Text = NetzwerkService.NetzwerkPfad;
            BenutzerBox.Text = NetzwerkService.GetBenutzerName();
            ArchivPfadBox.Text = NetzwerkService.GetAuftragsArchivBasisPfad();
        }

        private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void OnCloseWindowClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnBrowsePfad(object sender, RoutedEventArgs e)
        {
            var selectedPath = SelectFolder(PfadBox.Text);
            if (!string.IsNullOrWhiteSpace(selectedPath))
                PfadBox.Text = selectedPath;
        }

        private void OnBrowseArchivPfad(object sender, RoutedEventArgs e)
        {
            var selectedPath = SelectFolder(ArchivPfadBox.Text);
            if (!string.IsNullOrWhiteSpace(selectedPath))
                ArchivPfadBox.Text = selectedPath;
        }

        private string SelectFolder(string initialPath)
        {
            var dialog = new OpenFolderDialog
            {
                Multiselect = false,
                Title = "Bitte Ordner auswählen"
            };

            if (!string.IsNullOrWhiteSpace(initialPath))
            {
                try
                {
                    var resolved = Path.GetFullPath(initialPath.Trim().Trim('"'));
                    if (Directory.Exists(resolved))
                        dialog.InitialDirectory = resolved;
                }
                catch
                {
                }
            }

            return dialog.ShowDialog(this) == true
                ? (dialog.FolderName ?? string.Empty)
                : string.Empty;
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            var aktiviert = AktivCheck.IsChecked == true;
            var pfad = PfadBox.Text?.Trim() ?? "";
            NetzwerkService.SetNetzwerkModus(aktiviert, pfad);

            var archivPfad = ArchivPfadBox.Text?.Trim() ?? "";
            NetzwerkService.SetAuftragsArchivPfad(archivPfad);

            var benutzer = BenutzerBox.Text?.Trim() ?? "";
            NetzwerkService.SetBenutzerName(benutzer);

            DialogResult = true;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
