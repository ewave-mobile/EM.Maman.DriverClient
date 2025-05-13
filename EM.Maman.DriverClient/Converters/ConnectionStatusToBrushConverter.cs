using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace EM.Maman.DriverClient.Converters
{
    public class ConnectionStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value?.ToString() ?? "Disconnected";
            bool isSimulation = parameter is bool ? (bool)parameter : false;

            if (isSimulation)
            {
                // Blue for simulation mode
                return new SolidColorBrush(Colors.DodgerBlue);
            }

            // Real PLC connection mode
            switch (status)
            {
                case "Connected":
                    return new SolidColorBrush(Colors.Green);
                case "Connecting":
                    return new SolidColorBrush(Colors.Orange);
                case "Failed":
                    return new SolidColorBrush(Colors.Red);
                case "Disconnected":
                default:
                    return new SolidColorBrush(Colors.Gray);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}