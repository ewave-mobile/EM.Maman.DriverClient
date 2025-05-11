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
    public class ActiveTaskStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.Enums.ActiveTaskStatus taskType)
            {
                switch (taskType)
                {
                    case Models.Enums.ActiveTaskStatus.retrieval:
                        return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // gray
                    case Models.Enums.ActiveTaskStatus.transit:
                        return new SolidColorBrush(Color.FromRgb(33, 150, 243)); // blue
                    case Models.Enums.ActiveTaskStatus.storing:
                        return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // orange
                    case Models.Enums.ActiveTaskStatus.finished:
                        return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // green
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
