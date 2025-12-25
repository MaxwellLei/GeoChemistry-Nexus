using System;
using System.Globalization;
using System.Windows.Data;

namespace GeoChemistryNexus.Converter
{
    public class NumericTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // double
            if (value == null) return 0.0;
            
            double result = 0.0;
            if (value is int i) result = (double)i;
            else if (value is float f) result = (double)f;
            else if (value is double d) result = d;
            else if (value is long l) result = (double)l;
            else if (value is short s) result = (double)s;
            else if (value is byte b) result = (double)b;
            else if (double.TryParse(value.ToString(), out double parsed)) result = parsed;

            // 显示时保留四位小数 
            return Math.Round(result, 4);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (targetType == typeof(int)) return System.Convert.ToInt32(d);
                if (targetType == typeof(float)) return System.Convert.ToSingle(d);
                if (targetType == typeof(long)) return System.Convert.ToInt64(d);
                if (targetType == typeof(short)) return System.Convert.ToInt16(d);
                if (targetType == typeof(byte)) return System.Convert.ToByte(d);
                if (targetType == typeof(double)) return d;
            }
            return value;
        }
    }
}
