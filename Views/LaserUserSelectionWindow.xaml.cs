using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MaterialManager_V01.Models;
using MaterialManager_V01.Services;
using Microsoft.VisualBasic;

namespace MaterialManager_V01.Views
{
    public partial class LaserUserSelectionWindow : Window
    {
        private sealed class LaserUserItem
        {
            public required User User { get; init; }
            public string DisplayLabel => $"{User.DisplayName} ({User.Role})";
        }

        public User? SelectedUser { get; private set; }

        public LaserUserSelectionWindow()
        {
            InitializeComponent();
            LoadUsers();
        }

        private void LoadUsers()
        {
            var users = UserService.GetUsersByRoles(UserRole.LaserProgrammierer, UserRole.LaserBediener)
                .Select(u => new LaserUserItem { User = u })
                .ToList();

            UserBox.ItemsSource = users;
            UserBox.SelectedIndex = users.Count > 0 ? 0 : -1;
        }

        private void OnCreateOperatorClick(object sender, RoutedEventArgs e)
        {
            var name = Interaction.InputBox("Name für neuen Laser-Bediener eingeben:", "Bediener neu anlegen", "Laser-Bediener").Trim();
            if (string.IsNullOrWhiteSpace(name))
                return;

            var user = UserService.CreateDemoLaserUser(name, UserRole.LaserBediener);
            LoadUsers();

            var items = UserBox.ItemsSource as List<LaserUserItem>;
            var match = items?.FirstOrDefault(i => i.User.Username == user.Username);
            if (match != null)
                UserBox.SelectedItem = match;
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (UserBox.SelectedItem is not LaserUserItem item)
            {
                MessageBox.Show("Bitte einen Benutzer auswählen.", "Laser", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SelectedUser = item.User;
            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}