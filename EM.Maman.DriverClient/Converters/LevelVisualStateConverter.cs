using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace EM.Maman.DriverClient.Converters
{

    /// <summary>
    /// Converter to apply slashed pattern when viewing content from a different level
    /// </summary>
    public class LevelVisualStateConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == null || values[1] == null)
                return null;

            if (int.TryParse(values[0].ToString(), out int itemLevel) &&
                int.TryParse(values[1].ToString(), out int currentViewLevel))
            {
                // If the item is on a different level than currently viewed
                if (itemLevel != currentViewLevel && currentViewLevel != 0)
                {
                    // Return a diagonal hatched brush
                    var brush = new DrawingBrush
                    {
                        TileMode = TileMode.Tile,
                        Viewport = new Rect(0, 0, 10, 10),
                        ViewportUnits = BrushMappingMode.Absolute
                    };

                    var drawingGroup = new DrawingGroup();
                    using (DrawingContext dc = drawingGroup.Open())
                    {
                        // Background
                        dc.DrawRectangle(
                            parameter?.ToString() == "finger" ? Brushes.LightGray : Brushes.Silver,
                            null,
                            new Rect(0, 0, 10, 10));

                        // Diagonal line
                        dc.DrawLine(
                            new Pen(Brushes.DarkGray, 1),
                            new Point(0, 0),
                            new Point(10, 10));
                    }

                    brush.Drawing = drawingGroup;
                    return brush;
                }
            }

            // Return null to use the default brush
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
