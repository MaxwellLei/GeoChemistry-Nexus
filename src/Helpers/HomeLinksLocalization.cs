using GeoChemistryNexus.Services;
using System.Collections.Generic;

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

        public static bool HasText(LocalizedString? value)
        {
            return !string.IsNullOrWhiteSpace(GetSortKey(value));
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
    }
}
