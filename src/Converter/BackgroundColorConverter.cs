using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GeoChemistryNexus.Converter
{
    public class BackgroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ScottPlot.Color scottPlotColor)
            {
                // Convert ScottPlot.Color to SolidColorBrush for WPF Background
                return new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                    scottPlotColor.A,
                    scottPlotColor.R,
                    scottPlotColor.G,
                    scottPlotColor.B));
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush solidColorBrush)
            {
                // Convert SolidColorBrush back to ScottPlot.Color
                var color = solidColorBrush.Color;
                return new ScottPlot.Color(
                    color.R,
                    color.G,
                    color.B,
                    color.A);
            }
            return null;
        }
    }
}