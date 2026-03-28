using System.Windows;
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
