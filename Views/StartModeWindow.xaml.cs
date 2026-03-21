using System.Linq;
using System.Windows;
using System.Windows.Input;
using MaterialManager_V01.Models;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class StartModeWindow : Window
    {
        public StartModeWindow()
        {
            InitializeComponent();
        }

        private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnStandardClick(object sender, RoutedEventArgs e)
        {
            var window = new MainWindow();
            Application.Current.MainWindow = window;
            window.Show();
            Close();
        }

        private void OnLagerClick(object sender, RoutedEventArgs e)
        {
            var user = UserService.GetUsersByRoles(UserRole.Lagerarbeiter, UserRole.Manager, UserRole.Admin)
                .FirstOrDefault(u => u.Role == UserRole.Lagerarbeiter)
                ?? UserService.CreateDemoUser("Lager Demo", UserRole.Lagerarbeiter);

            var window = new LagerDemoWindow(user);
            Application.Current.MainWindow = window;
            window.Show();
            Close();
        }

        private void OnLaserClick(object sender, RoutedEventArgs e)
        {
            var dlg = new LaserUserSelectionWindow { Owner = this };
            if (dlg.ShowDialog() != true || dlg.SelectedUser == null)
                return;

            var window = new LaserDemoWindow(dlg.SelectedUser);
            Application.Current.MainWindow = window;
            window.Show();
            Close();
        }
    }
}