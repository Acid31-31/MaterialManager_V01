using System;
using System.Globalization;
using System.Windows.Data;

namespace MaterialManager_V01.Converters
{
    public class StaerkeFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double staerke)
            {
                return $"{staerke:0.0} mm";
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                // Entferne " mm" und parse
                str = str.Replace(" mm", "").Trim();
                if (double.TryParse(str.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }
            return value;
        }
    }
}
