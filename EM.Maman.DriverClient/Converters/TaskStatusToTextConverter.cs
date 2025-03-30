using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace EM.Maman.DriverClient.Converters
{
    // Converts TaskStatus to text description
    public class TaskStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.Enums.TaskStatus status)
            {
                switch (status)
                {
                    case Models.Enums.TaskStatus.Created:
                        return "ממתין";
                    case Models.Enums.TaskStatus.InProgress:
                        return "בביצוע";
                    case Models.Enums.TaskStatus.Completed:
                        return "הושלם";
                    case Models.Enums.TaskStatus.Failed:
                        return "נכשל";
                    case Models.Enums.TaskStatus.Cancelled:
                        return "בוטל";
                    default:
                        return "לא ידוע";
                }
            }
            return "לא ידוע";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
