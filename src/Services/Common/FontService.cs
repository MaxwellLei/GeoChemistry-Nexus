using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.Windows.Markup;
using System.Windows.Media;

namespace GeoChemistryNexus.Services
{
    public static class FontService
    {
        private static List<string>? _cachedFontNames;
        private static readonly object _lock = new object();
        private static bool _isInitialized = false;
        private static Task? _initializationTask;

        /// <summary>
        /// 获取缓存的字体列表
        /// </summary>
        public static List<string> GetFontNames()
        {
            if (_isInitialized && _cachedFontNames != null)
            {
                return _cachedFontNames;
            }

            lock (_lock)
            {
                if (!_isInitialized)
                {
                    InitializeFonts();
                }
            }

            return _cachedFontNames ?? new List<string> { "Arial" };
        }

        /// <summary>
        /// 异步获取字体列表
        /// </summary>
        public static async Task<List<string>> GetFontNamesAsync()
        {
            if (_isInitialized && _cachedFontNames != null)
            {
                return _cachedFontNames;
            }

            if (_initializationTask == null)
            {
                lock (_lock)
                {
                    if (_initializationTask == null)
                    {
                        _initializationTask = Task.Run(() => InitializeFonts());
                    }
                }
            }

            await _initializationTask;
            return _cachedFontNames ?? new List<string> { "Arial" };
        }

        private static void InitializeFonts()
        {
            try
            {
                var currentLang = XmlLanguage.GetLanguage(System.Globalization.CultureInfo.CurrentUICulture.IetfLanguageTag);
                _cachedFontNames = Fonts.SystemFontFamilies
                    .Select(f => GetFontDisplayName(f, currentLang))
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.CurrentCultureIgnoreCase)
                    .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }
            catch (Exception)
            {
                // 如果获取字体失败，提供一些默认字体
                _cachedFontNames = new List<string>
                {
                    "Arial", "Times New Roman", "Calibri", "Verdana", "Tahoma"
                };
            }
            finally
            {
                _isInitialized = true;
            }
        }

        private static string GetFontDisplayName(FontFamily fontFamily, XmlLanguage language)
        {
            if (fontFamily.FamilyNames != null
                && fontFamily.FamilyNames.TryGetValue(language, out var localizedName)
                && !string.IsNullOrWhiteSpace(localizedName))
            {
                return localizedName;
            }

            return fontFamily.Source ?? string.Empty;
        }

        /// <summary>
        /// 强制刷新字体缓存
        /// </summary>
        public static void RefreshFonts()
        {
            lock (_lock)
            {
                _isInitialized = false;
                _cachedFontNames = null;
                _initializationTask = null;
            }
        }
    }
}
