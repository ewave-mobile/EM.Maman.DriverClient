using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace EM.Maman.DriverClient.Converters
{
    public class RowPositionToOffsetConverter : IValueConverter
    {
        // Assume each row has a fixed height, e.g., 50 pixels.
        private const double RowHeight = 50;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int row)
            {
                return row * RowHeight;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
