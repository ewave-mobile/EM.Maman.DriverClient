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
    /// <summary>
    /// Converts a boolean to its inverse value and then to a visibility
    /// </summary>
    public class BoolInverterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Invert the boolean value
                bool invertedValue = !boolValue;

                // Convert to visibility
                if (targetType == typeof(Visibility))
                {
                    return invertedValue ? Visibility.Visible : Visibility.Collapsed;
                }
                
                // Return the inverted boolean if the target type is boolean
                return invertedValue;
            }
            
            // Default to collapsed if not a boolean
            if (targetType == typeof(Visibility))
            {
                return Visibility.Collapsed;
            }
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                // Convert back from visibility to inverted boolean
                bool visibleValue = visibility == Visibility.Visible;
                
                // Return the inverted value
                return !visibleValue;
            }
            
            if (value is bool boolValue)
            {
                // Return the inverted boolean
                return !boolValue;
            }
            
            return false;
        }
    }
}
