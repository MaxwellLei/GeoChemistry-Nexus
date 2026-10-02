using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace GeoChemistryNexus.Converter
{
    public class StringToBitmapImageConverter : IValueConverter
    {
        private const int MaxCacheCapacity = 64;
        private static readonly object CacheLock = new();
        private static readonly Dictionary<string, LinkedListNode<(string Key, BitmapImage? Image)>> CacheMap = new();
        private static readonly LinkedList<(string Key, BitmapImage? Image)> CacheOrder = new();

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

            lock (CacheLock)
            {
                if (CacheMap.TryGetValue(cacheKey, out var existingNode))
                {
                    CacheOrder.Remove(existingNode);
                    CacheOrder.AddFirst(existingNode);
                    return existingNode.Value.Image;
                }

                var bitmap = CreateBitmap(path, decodeWidth);
                var newNode = CacheOrder.AddFirst((cacheKey, bitmap));
                CacheMap[cacheKey] = newNode;

                while (CacheMap.Count > MaxCacheCapacity)
                {
                    var oldest = CacheOrder.Last;
                    if (oldest == null) break;
                    CacheOrder.RemoveLast();
                    CacheMap.Remove(oldest.Value.Key);
                }

                return bitmap;
            }
        }

        public static void ClearCache()
        {
            lock (CacheLock)
            {
                CacheMap.Clear();
                CacheOrder.Clear();
            }
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
