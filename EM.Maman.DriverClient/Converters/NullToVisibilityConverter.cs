using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EM.Maman.DriverClient.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool notNull = value != null;
            string paramString = parameter as string;

            if (paramString == "VisibleIfNull" || paramString == "CollapsedIfNotNull")
            {
                return notNull ? Visibility.Collapsed : Visibility.Visible;
            }
            // Default: VisibleIfNotNull or CollapsedIfNull
            return notNull ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
