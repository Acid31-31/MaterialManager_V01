using System.Windows;
using System.Windows.Input;
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

        private bool PromptForOperatorName()
        {
            var dlg = new LaserUserSelectionWindow { Owner = this };
            return dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.SelectedName);
        }

        private void OnStandardClick(object sender, RoutedEventArgs e)
        {
            if (!PromptForOperatorName())
                return;

            var window = new MainWindow();
            Application.Current.MainWindow = window;
            window.Show();
            Close();
        }

        private void OnLagerClick(object sender, RoutedEventArgs e)
        {
            if (!PromptForOperatorName())
                return;

            var window = new LagerDemoWindow();
            Application.Current.MainWindow = window;
            window.Show();
            Close();
        }

        private void OnLaserClick(object sender, RoutedEventArgs e)
        {
            if (!PromptForOperatorName())
                return;

            var window = new LaserDemoWindow();
            Application.Current.MainWindow = window;
            window.Show();
            Close();
        }
    }
}