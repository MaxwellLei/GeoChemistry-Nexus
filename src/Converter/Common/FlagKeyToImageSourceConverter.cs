using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GeoChemistryNexus.Converter
{
    /// <summary>
    /// 国旗键（如 "CN"）→ Styles.xaml 中定义的 Flag.CN 矢量图（DrawingImage）。
    /// </summary>
    public class FlagKeyToImageSourceConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string key || string.IsNullOrWhiteSpace(key))
                return null;

            return Application.Current?.TryFindResource($"Flag.{key}") as ImageSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
