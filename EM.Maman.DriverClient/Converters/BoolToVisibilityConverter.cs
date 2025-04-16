using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EM.Maman.DriverClient.Converters
{
    /// <summary>
    /// Converts a boolean to a visibility for showing/hiding elements.
    /// Can optionally invert the logic based on the ConverterParameter.
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = false;
            if (value is bool b)
            {
                isVisible = b;
            }

            bool invert = false;
            if (parameter != null)
            {
                if (parameter is bool pBool)
                {
                    invert = pBool;
                }
                else if (parameter is string pString)
                {
                    // Try parsing string as bool or check for specific keywords
                    if (bool.TryParse(pString, out bool pBoolParsed))
                    {
                        invert = pBoolParsed;
                    }
                    else if (string.Equals(pString, "Inverse", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(pString, "Invert", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(pString, "True", StringComparison.OrdinalIgnoreCase)) // Treat "True" string as invert=true
                    {
                        invert = true;
                    }
                }
            }

            // Apply inversion if requested
            if (invert)
            {
                isVisible = !isVisible;
            }

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Basic ConvertBack implementation if needed, assuming parameter logic is symmetrical
            bool invert = false;
            if (parameter != null)
            {
                 if (parameter is bool pBool)
                {
                    invert = pBool;
                }
                else if (parameter is string pString)
                {
                     if (bool.TryParse(pString, out bool pBoolParsed))
                    {
                        invert = pBoolParsed;
                    }
                    else if (string.Equals(pString, "Inverse", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(pString, "Invert", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(pString, "True", StringComparison.OrdinalIgnoreCase))
                    {
                        invert = true;
                    }
                }
            }

            if (value is Visibility visibility)
            {
                bool isVisible = (visibility == Visibility.Visible);
                return invert ? !isVisible : isVisible;
            }

            return DependencyProperty.UnsetValue; // Or throw exception
        }
    }
}
