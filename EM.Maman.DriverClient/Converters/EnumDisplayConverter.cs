using System;
using System.Globalization;
using System.Windows.Data;

namespace EM.Maman.DriverClient.Converters
{
    public class EnumDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            // Return a friendly name based on the enum value
            switch (value.ToString())
            {
                // StorageTypeEnum
                case "REG":
                    return "רגיל";
                case "FRZ":
                    return "קירור";
                case "PHARMA":
                    return "פארמה";
                
                // CargoType
                case "ULD":
                    return "ULD";
                case "AWB":
                    return "AWB";
                
                // HeightType
                case "LOW":
                    return "נמוך";
                case "MED":
                    return "בינוני";
                case "HIGH":
                    return "גבוה";
                
                default:
                    return value.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This method is not needed for this implementation
            return Binding.DoNothing;
        }
    }
}
