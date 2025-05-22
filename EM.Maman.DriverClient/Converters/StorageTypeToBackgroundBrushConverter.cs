using EM.Maman.Models.Enums;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace EM.Maman.DriverClient.Converters
{
    public class StorageTypeToBackgroundBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is StorageTypeEnum storageType)
            {
                switch (storageType)
                {
                    case StorageTypeEnum.FRZ:
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCE8FF")); // Light Blue
                    case StorageTypeEnum.REG:
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEFDB")); // Light Orange
                    case StorageTypeEnum.PHARMA:
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f2e6ff")); // Light Purple
                    default:
                        return new SolidColorBrush(Colors.Transparent); // Default or fallback color
                }
            }
            return new SolidColorBrush(Colors.Transparent); // Default or fallback color
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
