using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace EM.Maman.DriverClient.Converters
{
    public class BoolToBorderBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isImport)
            {
                return isImport ? new SolidColorBrush(Color.FromRgb(129, 199, 132)) : new SolidColorBrush(Color.FromRgb(255, 183, 77));
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
