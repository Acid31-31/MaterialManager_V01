using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MaterialManager_V01.Views
{
    public partial class BestaetigungsDialog : Window
    {
        public bool Confirmed { get; private set; }

        public BestaetigungsDialog(string title, string message, string confirmText = "Bestätigen", string cancelText = "Abbrechen", string confirmColorHex = "#1976D2")
        {
            InitializeComponent();
            Title = title;
            TitleTextBlock.Text = title;
            MessageTextBlock.Text = message;

            AddButton(confirmText, confirmColorHex, true);
            AddButton(cancelText, "#555555", false);
        }

        private void AddButton(string title, string colorHex, bool confirmed)
        {
            var button = new Button
            {
                Content = title,
                Background = (Brush)new BrushConverter().ConvertFromString(colorHex),
                Foreground = Brushes.White,
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(0, 0, 10, 10),
                MinWidth = 170,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            button.Click += (_, _) =>
            {
                Confirmed = confirmed;
                DialogResult = confirmed;
                Close();
            };

            ButtonPanel.Children.Add(button);
        }
    }
}