using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace GeoChemistryNexus.Converter
{
    /// <summary>
    /// 把字符串拆成单个字符，供窄轨里逐字纵向排列。仅用于单向绑定。
    /// </summary>
    public class StringToCharListConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string ?? string.Empty;
            var chars = new List<string>(text.Length);
            foreach (var ch in text)
            {
                if (ch is '\r' or '\n' or '\t')
                    continue;
                chars.Add(ch.ToString());
            }
            return chars;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
