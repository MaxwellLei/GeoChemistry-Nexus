using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.Helpers
{
    public static class HomeLinksLocalization
    {
        public static string ResolveForApp(LocalizedString? value)
        {
            if (value == null)
                return string.Empty;

            string appLang = AppCultureRegistry.ResolveAppLanguage(LanguageService.CurrentLanguage);
            return AppCultureRegistry.GetLocalizedValue(value.Translations, appLang, value.Default);
        }

        public static string ResolveForContext(LocalizedString? value, ContentLanguageContext? context)
        {
            if (value == null)
                return string.Empty;

            string requested = !string.IsNullOrEmpty(context?.ContentLanguage)
                ? context.ContentLanguage
                : LanguageService.CurrentLanguage;

            return AppCultureRegistry.GetLocalizedValue(value.Translations, requested, value.Default);
        }

        public static string GetSortKey(LocalizedString? value)
        {
            return AppCultureRegistry.GetLocalizedValue(
                value?.Translations,
                AppCultureRegistry.DefaultContentLanguage,
                value?.Default ?? AppCultureRegistry.DefaultContentLanguage);
        }

        public static bool HasText(LocalizedString? value, ContentLanguageContext? context = null)
        {
            if (value == null)
                return false;

            if (!string.IsNullOrWhiteSpace(ResolveForContext(value, context)))
                return true;

            if (!string.IsNullOrWhiteSpace(GetSortKey(value)))
                return true;

            return value.Translations?.Values.Any(v => !string.IsNullOrWhiteSpace(v)) ?? false;
        }

        public static LocalizedString FromPlain(string text, string? languageCode = null)
        {
            languageCode ??= AppCultureRegistry.DefaultContentLanguage;
            var localized = new LocalizedString
            {
                Default = AppCultureRegistry.DefaultContentLanguage
            };

            if (!string.IsNullOrWhiteSpace(text))
                localized.Set(text.Trim(), languageCode: languageCode);

            return localized;
        }

        public static LocalizedString Clone(LocalizedString? value)
        {
            if (value == null)
                return new LocalizedString();

            return new LocalizedString
            {
                Default = value.Default,
                Translations = value.Translations == null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string>(value.Translations)
            };
        }

        public static LocalizedString FromDictionary(Dictionary<string, string>? dict)
        {
            if (dict == null || dict.Count == 0)
                return new LocalizedString();

            return new LocalizedString
            {
                Default = AppCultureRegistry.DefaultContentLanguage,
                Translations = new Dictionary<string, string>(dict)
            };
        }

        public static Dictionary<string, string> ToDictionary(LocalizedString? value)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (value?.Translations == null)
                return result;

            foreach (var pair in value.Translations)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
                    continue;

                result[pair.Key] = pair.Value.Trim();
            }

            return result;
        }
    }
}
