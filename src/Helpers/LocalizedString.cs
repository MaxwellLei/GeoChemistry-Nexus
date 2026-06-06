using System.Collections.Generic;
using GeoChemistryNexus.Services;

namespace GeoChemistryNexus.Helpers
{
    public class LocalizedString
    {
        public string Default { get; set; } = AppCultureRegistry.DefaultContentLanguage;

        public Dictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();

        public string Get(ContentLanguageContext? ctx = null)
        {
            string requested = ResolveRequestedLanguage(ctx);
            return AppCultureRegistry.GetLocalizedValue(Translations, requested, Default);
        }

        public void Set(string content, ContentLanguageContext? ctx = null, string? languageCode = null)
        {
            string targetLang = !string.IsNullOrEmpty(languageCode)
                ? languageCode
                : ResolveRequestedLanguage(ctx);

            if (Translations == null)
                Translations = new Dictionary<string, string>();

            Translations[targetLang] = content;
        }

        private static string ResolveRequestedLanguage(ContentLanguageContext? ctx)
        {
            if (!string.IsNullOrEmpty(ctx?.ContentLanguage))
                return ctx.ContentLanguage;

            if (!string.IsNullOrEmpty(ContentLanguageScope.Instance.Active?.ContentLanguage))
                return ContentLanguageScope.Instance.Active.ContentLanguage;

            return LanguageService.CurrentLanguage;
        }
    }
}
