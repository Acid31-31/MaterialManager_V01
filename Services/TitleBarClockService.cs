using System.Windows;
using System.Windows.Controls;
using MaterialManager_V01.Views;

namespace MaterialManager_V01.Services
{
    public static class TitleBarClockService
    {
        public static void AttachToWindow(Window window)
        {
            if (window.GetType().FullName?.Contains("PopupRoot") == true)
                return;

            if (window.IsLoaded)
                TryWrapContent(window);
            else
            {
                window.Loaded -= OnWindowLoaded;
                window.Loaded += OnWindowLoaded;
            }
        }

        private static void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Window window)
                TryWrapContent(window);
        }

        private static void TryWrapContent(Window window)
        {
            if (window.Content is not FrameworkElement content)
                return;

            if (content.Tag as string == "TitleBarClockHost")
                return;

            var host = new Grid { Tag = "TitleBarClockHost" };
            window.Content = null;
            host.Children.Add(content);

            var clock = new DigitalClockControl
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 4, 0, 0)
            };
            Panel.SetZIndex(clock, 1000);
            host.Children.Add(clock);

            window.Content = host;
        }
    }
}
