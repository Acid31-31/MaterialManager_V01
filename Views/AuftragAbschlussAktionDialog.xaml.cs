using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MaterialManager_V01.Views
{
    public enum AuftragAbschlussAktion
    {
        Keine,
        Bearbeiten,
        Loeschen,
        InsLagerUebernehmen
    }

    public partial class AuftragAbschlussAktionDialog : Window
    {
        public AuftragAbschlussAktion SelectedAction { get; private set; } = AuftragAbschlussAktion.Keine;

        public AuftragAbschlussAktionDialog(int materialCount)
        {
            InitializeComponent();

            if (materialCount <= 1)
            {
                InfoTextBlock.Text = "Es gibt 1 Materialposition zu diesem Auftrag.";
                HinweisTextBlock.Text = "Bitte auswählen, was mit dem Material nach dem Produktionsende passieren soll.";

                AddActionButton("Material bearbeiten", "#455A64", AuftragAbschlussAktion.Bearbeiten);
                AddActionButton("Material löschen", "#8B1E1E", AuftragAbschlussAktion.Loeschen);
                AddActionButton("Ins Lager übernehmen", "#2E7D32", AuftragAbschlussAktion.InsLagerUebernehmen);
            }
            else
            {
                InfoTextBlock.Text = $"Es gibt {materialCount} Materialpositionen zu diesem Auftrag.";
                HinweisTextBlock.Text = "Bitte auswählen, was mit den Materialien nach dem Produktionsende passieren soll.";

                AddActionButton("Ins Lager übernehmen", "#2E7D32", AuftragAbschlussAktion.InsLagerUebernehmen);
                AddActionButton("Materialien löschen", "#8B1E1E", AuftragAbschlussAktion.Loeschen);
            }

            AddCancelButton();
        }

        private void AddActionButton(string title, string colorHex, AuftragAbschlussAktion action)
        {
            var button = CreateButton(title, colorHex);
            button.Click += (_, _) =>
            {
                SelectedAction = action;
                DialogResult = true;
                Close();
            };
            ButtonPanel.Children.Add(button);
        }

        private void AddCancelButton()
        {
            var button = CreateButton("Abbrechen", "#555555");
            button.Click += (_, _) =>
            {
                SelectedAction = AuftragAbschlussAktion.Keine;
                DialogResult = false;
                Close();
            };
            ButtonPanel.Children.Add(button);
        }

        private static Button CreateButton(string title, string colorHex)
        {
            return new Button
            {
                Content = title,
                Background = (Brush)new BrushConverter().ConvertFromString(colorHex),
                Foreground = Brushes.White,
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(0, 0, 10, 10),
                MinWidth = 170,
                Cursor = System.Windows.Input.Cursors.Hand
            };
        }
    }
}