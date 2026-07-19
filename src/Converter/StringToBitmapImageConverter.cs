using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace GeoChemistryNexus.Converter
{
    public class StringToBitmapImageConverter : IValueConverter
    {
        private static readonly ConcurrentDictionary<string, BitmapImage?> Cache = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string path || string.IsNullOrWhiteSpace(path))
                return null;

            // Glyph / plain text icons are not image URIs; skip before Uri/decode work.
            if (!LooksLikeImagePath(path))
                return null;

            int decodeWidth = 0;
            if (parameter is string widthStr)
                int.TryParse(widthStr, out decodeWidth);

            string cacheKey = decodeWidth > 0 ? decodeWidth + "|" + path : path;
            return Cache.GetOrAdd(cacheKey, _ => CreateBitmap(path, decodeWidth));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static bool LooksLikeImagePath(string path)
        {
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("pack://", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Local/relative file paths usually contain a separator or drive colon.
            return path.IndexOf('/') >= 0
                   || path.IndexOf('\\') >= 0
                   || (path.Length > 2 && path[1] == ':');
        }

        private static BitmapImage? CreateBitmap(string path, int decodeWidth)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                if (decodeWidth > 0)
                    bitmap.DecodePixelWidth = decodeWidth;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}
