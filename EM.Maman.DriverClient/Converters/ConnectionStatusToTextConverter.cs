using System;
using System.Globalization;
using System.Windows.Data;

namespace EM.Maman.DriverClient.Converters
{
    public class ConnectionStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value?.ToString() ?? "Disconnected";
            bool isSimulation = parameter is bool ? (bool)parameter : false;

            if (isSimulation)
            {
                return "מצב סימולציה"; // Simulation Mode
            }

            // Real PLC connection mode
            switch (status)
            {
                case "Connected":
                    return "בקר מחובר"; // Controller Connected
                case "Connecting":
                    return "מתחבר..."; // Connecting...
                case "Failed":
                    return "בקר לא נגיש"; // PLC Unreachable
                case "Disconnected":
                default:
                    return "בקר מנותק"; // Controller Disconnected
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}