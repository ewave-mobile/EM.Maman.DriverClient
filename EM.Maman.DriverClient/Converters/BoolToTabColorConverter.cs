using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace EM.Maman.DriverClient.Converters
{
    public class BoolToTabColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected)
            {
                bool isForSelected = true;
                if (parameter is string paramStr && paramStr.ToLower() == "false")
                {
                    isForSelected = false;
                }

                // If this is for the selected tab and it is selected, or
                // if this is for the unselected tab and it is not selected
                if ((isForSelected && isSelected) || (!isForSelected && !isSelected))
                {
                    return new SolidColorBrush(Colors.White);
                }
                else
                {
                    return new SolidColorBrush(Color.FromRgb(245, 245, 245)); // #F5F5F5
                }
            }

            return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
