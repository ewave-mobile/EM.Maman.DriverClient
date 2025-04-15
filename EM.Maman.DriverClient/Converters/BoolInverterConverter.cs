using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace EM.Maman.DriverClient.Converters
{
    // Converts boolean to inverted boolean or visibility
    public class BoolInverterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (parameter != null && parameter.ToString() == "Invert")
                {
                    // For explicit "Invert" parameter
                    if (targetType == typeof(Visibility))
                        return boolValue ? Visibility.Collapsed : Visibility.Visible;
                    return !boolValue;
                }
                else if (parameter != null && parameter.ToString() == "Visibility")
                {
                    // For backward compatibility
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                }

                // Default behavior is to invert the boolean
                if (targetType == typeof(Visibility))
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                return !boolValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }
    }
}
