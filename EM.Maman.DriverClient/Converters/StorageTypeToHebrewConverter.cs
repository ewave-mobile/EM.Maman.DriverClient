using EM.Maman.Models.Enums;
using System;
using System.Globalization;
using System.Windows.Data;

namespace EM.Maman.DriverClient.Converters
{
    public class StorageTypeToHebrewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is StorageTypeEnum storageType)
            {
                switch (storageType)
                {
                    case StorageTypeEnum.REG:
                        return "רגיל";
                    case StorageTypeEnum.FRZ:
                        return "קירור";
                    case StorageTypeEnum.PHARMA:
                        return "פארמה";
                    default:
                        return string.Empty;
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
