using System;
using System.Globalization;
using System.Windows.Data;

namespace EM.Maman.DriverClient.Converters
{
    public class BoolToPreviewTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isImport)
            {
                return isImport ? "תצוגה מקדימה - מדבקת יבוא" : "תצוגה מקדימה - מדבקת יצוא";
            }
            return "תצוגה מקדימה";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
