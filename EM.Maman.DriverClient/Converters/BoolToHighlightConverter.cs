using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace EM.Maman.DriverClient.Converters
{
    /// <summary>
    /// Converts a boolean to a background color for level highlights
    /// </summary>
    public class BoolToHighlightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                return new SolidColorBrush(Color.FromRgb(79, 110, 176)); // #4F6EB0 - blue highlight
            }
            return new SolidColorBrush(Color.FromRgb(229, 231, 235)); // #E5E7EB - light gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    

 


}
