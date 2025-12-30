using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoChemistryNexus.Services;

namespace GeoChemistryNexus.Helpers
{
    public class LocalizedString
    {
        // 默认支持的语言，如果没有指定的语言，就使用默认支持的语言
        public string Default { get; set; } = "en-US";

        // 强制覆盖语言（用于模板预览等场景）
        public static string? OverrideLanguage { get; set; }

        // 多语言字典
        public Dictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();

        // 用于获取当前语言的文本
        public string Get()
        {
            // 优先使用覆盖语言
            string languageCode = !string.IsNullOrEmpty(OverrideLanguage) 
                ? OverrideLanguage 
                : LanguageService.CurrentLanguage;

            // 获取当前语言的翻译
            if (Translations.ContainsKey(languageCode))
            {
                return Translations[languageCode];
            }
            // 语言不存在，获取默认语言的翻译
            if (!string.IsNullOrEmpty(Default) && Translations.ContainsKey(Default))
            {
                return Translations[Default];
            }
            // 默认语言不存在，返回一个空字符串
            return string.Empty;   
        }

        // 用于设置当前语言的文本
        public void Set(string languageCode, string content)
        {
            // 优先使用覆盖语言（如果已设置），否则使用传入的 languageCode（当前语言）
            // 根据当前的显示语言来决定更新哪个 Key
            string targetLang = !string.IsNullOrEmpty(OverrideLanguage) 
                ? OverrideLanguage 
                : languageCode;

            // 确保字典已初始化
            if (Translations == null) Translations = new Dictionary<string, string>();

            Translations[targetLang] = content;
        }
    }
}
