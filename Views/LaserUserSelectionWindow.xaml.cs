using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class LaserUserSelectionWindow : Window
    {
        public string? SelectedName { get; private set; }

        public LaserUserSelectionWindow()
        {
            InitializeComponent();
            LoadNames();
        }

        private void LoadNames()
        {
            NameBox.ItemsSource = OperatorIdentityService.RecentOperatorNames.ToList();
            NameBox.Text = OperatorIdentityService.CurrentOperatorName;
            Loaded += (_, _) =>
            {
                if (NameBox.Template.FindName("PART_EditableTextBox", NameBox) is TextBox textBox)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }
                else
                {
                    NameBox.Focus();
                }
            };
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            var name = NameBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Bitte einen Namen eingeben.", "Anmeldung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            OperatorIdentityService.SetCurrentOperatorName(name);
            SelectedName = name;
            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void OnWindowCloseClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}