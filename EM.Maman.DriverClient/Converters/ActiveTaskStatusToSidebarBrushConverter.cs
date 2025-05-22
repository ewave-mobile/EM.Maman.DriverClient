using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using EM.Maman.Models.Enums; // Assuming ActiveTaskStatus is here

namespace EM.Maman.DriverClient.Converters
{
    public class ActiveTaskStatusToSidebarBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ActiveTaskStatus status)
            {
                switch (status)
                {
                    // Working statuses (Red)
                    case ActiveTaskStatus.retrieval:
                    case ActiveTaskStatus.transit:
                    case ActiveTaskStatus.storing:
                    case ActiveTaskStatus.authentication:
                    case ActiveTaskStatus.arrived_at_destination: // Considered active as it might require immediate action or is a key step in progress
                        return Brushes.Red;

                    // Completed status (Green)
                    case ActiveTaskStatus.finished:
                        return Brushes.Green;
                    
                    // Pending or default statuses (Gray)
                    case ActiveTaskStatus.New:
                    case ActiveTaskStatus.pending:
                    case ActiveTaskStatus.pending_authentication:
                    default:
                        return Brushes.Gray;
                }
            }
            return Brushes.Gray; // Default if conversion fails or value is not the expected ActiveTaskStatus
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
