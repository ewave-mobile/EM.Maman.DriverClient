using System;
using System.Globalization;
using System.Windows.Data;
using EM.Maman.Models.Enums; // Assuming HeightType enum is here

namespace EM.Maman.DriverClient.Converters
{
    public class HeightLevelToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is HeightType heightType)
            {
                switch (heightType)
                {
                    case HeightType.LOW:
                        return "נמוך";
                    case HeightType.MED:
                        return "בינוני";
                    case HeightType.HIGH:
                        return "גבוה";
                    default:
                        return "לא ידוע";
                }
            }
            // Fallback for int? HeightLevel if that's used directly
            else if (value is int intHeight)
            {
                // You might have a mapping for integer levels to strings too
                // For example, if 1 is Low, 2 is Medium, etc.
                // This is a placeholder, adjust as per your actual integer mapping
                if (intHeight == 1) return "נמוך (1)";
                if (intHeight == 2) return "בינוני (2)";
                if (intHeight == 3) return "גבוה (3)";
                return $"רמה {intHeight}";
            }
            return "-"; // Default placeholder
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
