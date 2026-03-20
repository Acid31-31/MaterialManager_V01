using System.Windows;

namespace MaterialManager_V01.Views
{
    public partial class StartModeWindow : Window
    {
        public StartModeWindow()
        {
            InitializeComponent();
        }

        private void OnStandardClick(object sender, RoutedEventArgs e)
        {
            var window = new MainWindow();
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