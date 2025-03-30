using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace EM.Maman.DriverClient.Converters
{
    // Converts TaskStatus to color
    public class TaskStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.Enums.TaskStatus status)
            {
                switch (status)
                {
                    case Models.Enums.TaskStatus.Created:
                        return "#2196F3"; // Blue
                    case Models.Enums.TaskStatus.InProgress:
                        return "#FF9800"; // Orange
                    case Models.Enums.TaskStatus.Completed:
                        return "#4CAF50"; // Green
                    case Models.Enums.TaskStatus.Failed:
                        return "#F44336"; // Red
                    case Models.Enums.TaskStatus.Cancelled:
                        return "#9E9E9E"; // Gray
                    default:
                        return "#9E9E9E"; // Gray
                }
            }
            return "#9E9E9E";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
