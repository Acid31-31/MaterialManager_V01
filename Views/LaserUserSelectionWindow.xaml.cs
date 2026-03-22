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
            RecentNamesList.ItemsSource = OperatorIdentityService.RecentOperatorNames.ToList();
            NameTextBox.Text = OperatorIdentityService.CurrentOperatorName;
            Loaded += (_, _) =>
            {
                NameTextBox.Focus();
                NameTextBox.SelectAll();
            };
        }

        private void OnRecentNameDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RecentNamesList.SelectedItem is string name)
            {
                NameTextBox.Text = name;
                NameTextBox.Focus();
                NameTextBox.SelectAll();
            }
        }

        private void OnDeleteNameClick(object sender, RoutedEventArgs e)
        {
            var name = (RecentNamesList.SelectedItem as string ?? NameTextBox.Text)?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Bitte zuerst einen Namen auswählen.", "Anmeldung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show($"Name '{name}' aus der Liste löschen?", "Anmeldung", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
                return;

            if (!OperatorIdentityService.RemoveOperatorName(name))
            {
                MessageBox.Show("Name konnte nicht gelöscht werden.", "Anmeldung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            LoadNames();
            NameTextBox.Focus();
            NameTextBox.SelectAll();
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            var name = NameTextBox.Text?.Trim();
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