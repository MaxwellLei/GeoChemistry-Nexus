using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GeoChemistryNexus.Models;

namespace GeoChemistryNexus.Converter
{
    public class LineTypeToStrokeDashArrayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LineDefinition.LineType lineType)
            {
                switch (lineType)
                {
                    case LineDefinition.LineType.Solid:
                        return null; // 实线
                    case LineDefinition.LineType.Dash:
                        return new DoubleCollection { 4, 2 }; // 虚线
                    case LineDefinition.LineType.Dot:
                        return new DoubleCollection { 1, 2 }; // 点线
                    case LineDefinition.LineType.DenselyDashed:
                        return new DoubleCollection { 2, 1 }; // 密集虚线
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
