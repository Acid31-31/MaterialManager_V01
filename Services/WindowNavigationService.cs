using System.Windows;
using MaterialManager_V01.Views;

namespace MaterialManager_V01.Services
{
    public static class WindowNavigationService
    {
        public static void NavigateToStart(Window currentWindow)
        {
            var startWindow = new StartModeWindow();
            Application.Current.MainWindow = startWindow;
            startWindow.Show();
            currentWindow.Close();
        }

        public static void NavigateToMain(Window currentWindow)
        {
            var mainWindow = new MainWindow();
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
            currentWindow.Close();
        }
    }
}
