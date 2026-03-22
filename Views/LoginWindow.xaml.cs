using System.Windows;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void OnLoginClick(object sender, RoutedEventArgs e)
        {
            var username = UsernameTextBox.Text.Trim();
            var password = PasswordBox.Password;

            ErrorMessage.Text = "";

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage.Text = "⚠️ Benutzername und Passwort erforderlich!";
                return;
            }

            // Login versuchen
            if (UserService.Login(username, password))
            {
                DialogResult = true;
                Close();
            }
            else
            {
                ErrorMessage.Text = "❌ Ungültiger Benutzername oder Passwort!";
                PasswordBox.Clear();
                UsernameTextBox.Focus();
            }
        }
    }
}
