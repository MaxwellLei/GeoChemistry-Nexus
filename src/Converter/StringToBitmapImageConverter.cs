using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace GeoChemistryNexus.Converter
{
    public class StringToBitmapImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrEmpty(path))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;

                    // 允许通过参数覆盖 DecodePixelWidth
                    if (parameter is string widthStr && int.TryParse(widthStr, out int width))
                    {
                        bitmap.DecodePixelWidth = width;
                    }
                    else
                    {
                        bitmap.DecodePixelWidth = 640; // Default width
                    }
                    
                    bitmap.EndInit();
                    bitmap.Freeze(); // 冻结对象，确保跨线程访问安全并提升性能
                    return bitmap;
                }
                catch
                {
                    return null;
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
