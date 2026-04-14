using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MaterialManager_V01.Views
{
    public enum ReservierungAufhebenAktion
    {
        Keine,
        Bearbeiten,
        Loeschen,
        NurAufheben
    }

    public partial class ReservierungAufhebenAktionDialog : Window
    {
        public ReservierungAufhebenAktion SelectedAction { get; private set; } = ReservierungAufhebenAktion.Keine;

        public ReservierungAufhebenAktionDialog(string materialText)
        {
            InitializeComponent();
            InfoTextBlock.Text = string.IsNullOrWhiteSpace(materialText)
                ? "Bitte auswählen, was mit dem Material nach dem Aufheben der Reservierung passieren soll."
                : $"Material: {materialText}";
            HinweisTextBlock.Text = "Bitte auswählen, was mit dem Material nach dem Aufheben der Reservierung passieren soll.";

            AddActionButton("Material bearbeiten", "#455A64", ReservierungAufhebenAktion.Bearbeiten);
            AddActionButton("Material löschen", "#8B1E1E", ReservierungAufhebenAktion.Loeschen);
            AddActionButton("Nur Reservierung aufheben", "#2E7D32", ReservierungAufhebenAktion.NurAufheben);
            AddCancelButton();
        }

        private void AddActionButton(string title, string colorHex, ReservierungAufhebenAktion action)
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
                SelectedAction = ReservierungAufhebenAktion.Keine;
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