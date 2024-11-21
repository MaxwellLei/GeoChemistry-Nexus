using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GeoChemistryNexus.Converter
{
    public class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ScottPlot.Color scottPlotColor)
            {
                return System.Windows.Media.Color.FromArgb(
                    scottPlotColor.A,
                    scottPlotColor.R,
                    scottPlotColor.G,
                    scottPlotColor.B);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Media.Color wpfColor)
            {
                return new ScottPlot.Color(
                    wpfColor.R,
                    wpfColor.G,
                    wpfColor.B,
                    wpfColor.A);
            }
            return null;
        }
    }
}
