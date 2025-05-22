using System;
using System.Globalization;
using System.Windows.Data;

namespace EM.Maman.DriverClient.Converters
{
    public class UldTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string uldType && !string.IsNullOrEmpty(uldType))
            {
                // Potentially map to more descriptive names or localize
                // For now, just return the ULD type code if it's not empty
                // Example: "AKE" -> "AKE Container" or "קיצור (AKE)"
                // If UldType is already user-friendly, this direct return is fine.
                return uldType;
            }
            return "---"; // Default or placeholder if no ULD type
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
