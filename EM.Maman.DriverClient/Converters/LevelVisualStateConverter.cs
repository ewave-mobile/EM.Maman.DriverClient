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
            // Make sure we have both the item level and current view level
            if (values.Length < 2 || values[0] == null || values[1] == null)
                return Brushes.LightGray; // Default background if we can't determine state

            int itemLevel = 0;
            int currentViewLevel = 0;

            // Parse the item level from the first binding
            if (values[0] is int)
                itemLevel = (int)values[0];
            else if (int.TryParse(values[0].ToString(), out int parsedItemLevel))
                itemLevel = parsedItemLevel;

            // Parse the current view level from the second binding
            if (values[1] is int)
                currentViewLevel = (int)values[1];
            else if (int.TryParse(values[1].ToString(), out int parsedViewLevel))
                currentViewLevel = parsedViewLevel;

            // Check if this is a finger control (passed as a parameter)
            bool isFingerControl = parameter?.ToString() == "finger";

            // Fingers are always shown on all levels, but with different visual styles
            if (isFingerControl)
            {
                // Determine if the finger is on its actual level or we're viewing from another level
                bool showSlashedPattern = (itemLevel != currentViewLevel);

                if (showSlashedPattern)
                {
                    // Create a brush with diagonal slashed pattern
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
                            Brushes.LightGray,  // Light gray background
                            null,
                            new Rect(0, 0, 10, 10));

                        // Diagonal line
                        dc.DrawLine(
                            new Pen(Brushes.DarkGray, 1), // Dark gray slash
                            new Point(0, 0),
                            new Point(10, 10));
                    }

                    brush.Drawing = drawingGroup;
                    return brush;
                }
                else
                {
                    // Regular background for fingers on their own level
                    return Brushes.LightGray;
                }
            }
            else
            {
                // For other elements (not fingers)
                bool isDifferentLevel = (itemLevel != currentViewLevel && currentViewLevel != 0);

                if (isDifferentLevel)
                {
                    // Apply slashed pattern
                    var brush = new DrawingBrush
                    {
                        TileMode = TileMode.Tile,
                        Viewport = new Rect(0, 0, 10, 10),
                        ViewportUnits = BrushMappingMode.Absolute
                    };

                    var drawingGroup = new DrawingGroup();
                    using (DrawingContext dc = drawingGroup.Open())
                    {
                        dc.DrawRectangle(
                            Brushes.Silver,
                            null,
                            new Rect(0, 0, 10, 10));

                        dc.DrawLine(
                            new Pen(Brushes.DarkGray, 1),
                            new Point(0, 0),
                            new Point(10, 10));
                    }

                    brush.Drawing = drawingGroup;
                    return brush;
                }
            }

            // Return null to use the default brush for the element
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
