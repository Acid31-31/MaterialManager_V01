using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MaterialManager_V01.Models;

namespace MaterialManager_V01.Views
{
    public partial class ResteSucheErgebnisDialog : Window
    {
        public MaterialItem? AusgewaehltesMaterial { get; private set; }
        public string Auftragsnummer { get; private set; } = "";

        public ResteSucheErgebnisDialog(List<MaterialItem> gefundeneMaterialien)
        {
            InitializeComponent();
            ErgebnisGrid.ItemsSource = gefundeneMaterialien;

            if (gefundeneMaterialien.Any())
            {
                ErgebnisGrid.SelectedIndex = 0;
            }
        }

        private void OnMaterialDoppelklick(object sender, MouseButtonEventArgs e)
        {
            // Bei Doppelklick direkt reservieren (wenn Auftragsnummer eingegeben)
            if (ErgebnisGrid.SelectedItem is MaterialItem && !string.IsNullOrWhiteSpace(AuftragsnummerBox.Text))
            {
                OnReservieren(sender, e);
            }
        }

        private void OnReservieren(object sender, RoutedEventArgs e)
        {
            if (ErgebnisGrid.SelectedItem is MaterialItem material)
            {
                if (string.IsNullOrWhiteSpace(AuftragsnummerBox.Text))
                {
                    MessageBox.Show("Bitte Auftragsnummer eingeben!", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    AuftragsnummerBox.Focus();
                    return;
                }

                AusgewaehltesMaterial = material;
                Auftragsnummer = AuftragsnummerBox.Text.Trim();
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Bitte ein Material auswählen!", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnAbbrechen(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
