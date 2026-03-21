using System.Linq;
using System.Windows;
using System.Windows.Input;
using MaterialManager_V01.Models;
using MaterialManager_V01.Services;
using Microsoft.VisualBasic;

namespace MaterialManager_V01.Views
{
    public partial class LaserUserSelectionWindow : Window
    {
        public User? SelectedUser { get; private set; }

        public LaserUserSelectionWindow()
        {
            InitializeComponent();
            LoadUsers();
        }

        private void LoadUsers()
        {
            var currentSelection = (UserBox.SelectedItem as User)?.Username;

            var users = UserService.GetUsersByRoles(UserRole.LaserProgrammierer, UserRole.LaserBediener)
                .OrderBy(u => u.Role == UserRole.LaserProgrammierer ? 0 : 1)
                .ThenBy(u => u.DisplayName)
                .ToList();

            UserBox.ItemsSource = users;

            var selected = users.FirstOrDefault(u => u.Username == currentSelection)
                ?? users.FirstOrDefault(u => u.Role == UserRole.LaserProgrammierer)
                ?? users.FirstOrDefault();

            UserBox.SelectedItem = selected;
        }

        private void OnCreateOperatorClick(object sender, RoutedEventArgs e)
        {
            var name = Interaction.InputBox("Name für neuen Laser-Bediener eingeben:", "Bediener neu anlegen", "Laser-Bediener").Trim();
            if (string.IsNullOrWhiteSpace(name))
                return;

            var user = UserService.CreateDemoLaserUser(name, UserRole.LaserBediener);
            LoadUsers();

            var items = UserBox.ItemsSource as System.Collections.Generic.List<User>;
            var match = items?.FirstOrDefault(i => i.Username == user.Username);
            if (match != null)
                UserBox.SelectedItem = match;
        }

        private void OnDeleteOperatorClick(object sender, RoutedEventArgs e)
        {
            if (UserBox.SelectedItem is not User item)
            {
                MessageBox.Show("Bitte zuerst einen Bediener auswählen.", "Laser", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (item.Role != UserRole.LaserBediener)
            {
                MessageBox.Show("Nur Laser-Bediener können hier gelöscht werden.", "Laser", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bediener '{item.DisplayName}' wirklich löschen?",
                "Bediener löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            if (!UserService.DeleteDemoUser(item.Username, UserRole.LaserBediener))
            {
                MessageBox.Show("Bediener konnte nicht gelöscht werden.", "Laser", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadUsers();
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (UserBox.SelectedItem is not User item)
            {
                MessageBox.Show("Bitte einen Benutzer auswählen.", "Laser", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SelectedUser = item;
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