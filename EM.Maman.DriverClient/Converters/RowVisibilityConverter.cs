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
    public class RowVisibilityConverter : IMultiValueConverter
    {
        /// <summary>
        /// Expects two bound values:
        ///   values[0] = current row's Position (int)
        ///   values[1] = highest active row (int)
        /// Returns Visible if current row's Position is less than or equal to the highest active row; otherwise, returns Collapsed.
        /// </summary>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return Visibility.Collapsed;

            if (values[0] == null || values[1] == null)
                return Visibility.Collapsed;

            if (int.TryParse(values[0].ToString(), out int rowPos) &&
                int.TryParse(values[1].ToString(), out int highestRow))
            {
                return rowPos <= highestRow ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("RowVisibilityConverter does not support ConvertBack.");
        }
    }
}
