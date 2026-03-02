using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MaterialManager_V01.Converters
{
    public class PercentToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Brushes.Green;
            if (!double.TryParse(value.ToString(), out var pct)) return Brushes.Green;

            if (pct >= 90) return Brushes.Red;
            if (pct >= 70) return Brushes.Orange;
            return Brushes.Green;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
