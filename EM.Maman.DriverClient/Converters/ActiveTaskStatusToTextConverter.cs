using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace EM.Maman.DriverClient.Converters
{
    public class ActiveTaskStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.Enums.ActiveTaskStatus status)
            {
                switch (status)
                {
                    case Models.Enums.ActiveTaskStatus.retrieval:
                        return "בשליפה";
                    case Models.Enums.ActiveTaskStatus.transit:
                        return "בנסיעה";
                    case Models.Enums.ActiveTaskStatus.storing:
                        return "באחסון";
                    case Models.Enums.ActiveTaskStatus.finished:
                        return "הושלם";
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
