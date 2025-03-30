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
    // Converts TaskStatus to SolidColorBrush
    public class TaskStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.Enums.TaskStatus status)
            {
                switch (status)
                {
                    case Models.Enums.TaskStatus.Created:
                        return new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Blue - #2196F3
                    case Models.Enums.TaskStatus.InProgress:
                        return new SolidColorBrush(Color.FromRgb(255, 152, 0));  // Orange - #FF9800
                    case Models.Enums.TaskStatus.Completed:
                        return new SolidColorBrush(Color.FromRgb(76, 175, 80));  // Green - #4CAF50
                    case Models.Enums.TaskStatus.Failed:
                        return new SolidColorBrush(Color.FromRgb(244, 67, 54));  // Red - #F44336
                    case Models.Enums.TaskStatus.Cancelled:
                        return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gray - #9E9E9E
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
