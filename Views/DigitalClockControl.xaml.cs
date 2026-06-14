using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MaterialManager_V01.Views
{
    public partial class DigitalClockControl : UserControl
    {
        private readonly DispatcherTimer _timer = new();

        public DigitalClockControl()
        {
            InitializeComponent();
            UpdateDisplay();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (_, _) => UpdateDisplay();
            Loaded += (_, _) => _timer.Start();
            Unloaded += (_, _) => _timer.Stop();
        }

        private void UpdateDisplay()
        {
            var now = DateTime.Now;
            var culture = CultureInfo.GetCultureInfo("de-DE");
            DateTextBlock.Text = now.ToString("ddd, dd.MM.yyyy", culture);
            TimeTextBlock.Text = now.ToString("HH:mm:ss", culture);
        }
    }
}
