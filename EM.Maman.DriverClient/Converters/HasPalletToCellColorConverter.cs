using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace EM.Maman.DriverClient.Converters
{
    /// <summary>
    /// Converts a boolean indicating whether a cell has a pallet to a background color
    /// </summary>
    public class HasPalletToCellColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasPallet && hasPallet)
            {
                // Return a darker blue color for cells with pallets
                return new SolidColorBrush(Color.FromRgb(116, 126, 142)); // #747E8E - dark gray
            }
            // Default light gray background for empty cells
            return new SolidColorBrush(Color.FromRgb(233, 233, 233)); // #E9E9E9 - light gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
