using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace EM.Maman.DriverClient.Converters
{
    /// <summary>
    /// Converts a finger position to its level number (position/100)
    /// </summary>
    public class FingerPositionToLevelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int position)
            {
                return position / 100;
            }
            return 0; // Default to level 0 if conversion fails
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
