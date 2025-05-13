using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace EM.Maman.DriverClient.Converters
{
    public class EnumToItemsSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(value is Type enumType) || !enumType.IsEnum)
                return null;

            return Enum.GetValues(enumType)
                .Cast<object>()
                .Select(e => new EnumValueItem { Value = e, DisplayName = e.ToString() })
                .ToList();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class EnumValueItem
    {
        public object Value { get; set; }
        public string DisplayName { get; set; }
    }
}
