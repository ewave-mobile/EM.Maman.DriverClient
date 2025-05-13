using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace EM.Maman.DriverClient.Converters
{
    public class ConnectionStatusToBrushMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // First value is ConnectionStatus, second is IsSimulationMode
            if (values.Length < 2 || values[0] == null || values[1] == null)
                return new SolidColorBrush(Colors.Gray); // Default color

            string status = values[0].ToString();
            bool isSimulation = false;
            bool.TryParse(values[1].ToString(), out isSimulation);

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

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ConnectionStatusToTextMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // First value is ConnectionStatus, second is IsSimulationMode
            if (values.Length < 2 || values[0] == null || values[1] == null)
                return "בקר מנותק"; // Default text

            string status = values[0].ToString();
            bool isSimulation = false;
            bool.TryParse(values[1].ToString(), out isSimulation);

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

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}