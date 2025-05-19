using EM.Maman.Models.Enums;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EM.Maman.DriverClient.Converters
{
    public class ActiveTaskStatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ActiveTaskStatus status || parameter is not string targetStatesStr)
            {
                return Visibility.Collapsed;
            }

            var targetStates = targetStatesStr.Split(',');
            bool show = false;

            foreach (var targetState in targetStates)
            {
                switch (targetState.Trim())
                {
                    case "ShowIfRetrievalPendingAtSource":
                        // Show for states indicating the task is at the source cell, awaiting authentication/pickup
                        if (status == ActiveTaskStatus.retrieval || status == ActiveTaskStatus.pending_authentication || status == ActiveTaskStatus.pending) 
                        {
                            show = true;
                        }
                        break;
                    case "ShowIfRetrievalInTransitToDest":
                        // Show for states indicating the pallet is on trolley and can be navigated to destination
                        // or is already in transit to destination.
                        if (status == ActiveTaskStatus.transit) // Assuming 'transit' is used for transit to destination
                        {
                             // And also if it's at destination awaiting unload, the "Go To Dest" might be hidden or disabled.
                             // For now, only show if explicitly in transit to destination.
                            show = true;
                        }
                        break;
                    case "ShowIfRetrievalAtDest":
                        // Show for states indicating the pallet is at destination, awaiting unload.
                        // We might need a specific ActiveTaskStatus like 'at_destination_unload_pending'
                        // For now, let's assume if it's NOT 'transit' (to dest) AND it's an InProgress retrieval task on the delivery list,
                        // it might be at destination. This is a simplification.
                        // A better check would be if current trolley position matches task destination.
                        // Let's use a placeholder status or assume 'storing' could mean at destination for unload.
                        if (status == ActiveTaskStatus.storing || status == ActiveTaskStatus.arrived_at_destination) // Example placeholder
                        {
                            show = true;
                        }
                        break;
                }
                if (show) break;
            }
            return show ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
