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
    public class TaskTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.Enums.UpdateType taskType)
            {
                switch (taskType)
                {
                    case Models.Enums.UpdateType.Import:
                        return new SolidColorBrush(Color.FromRgb(67, 160, 71)); // Green - #43A047
                    case Models.Enums.UpdateType.Export:
                        return new SolidColorBrush(Color.FromRgb(245, 127, 23)); // Orange - #F57F17
                    default:
                        return new SolidColorBrush(Colors.Gray);
                }
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
