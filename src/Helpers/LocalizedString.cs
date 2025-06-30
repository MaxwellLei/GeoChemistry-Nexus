using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Helpers
{
    public class LocalizedString
    {
        // 默认支持的语言，如果没有指定的语言，就使用默认支持的语言
        public string Default { get; set; } = "en-US";

        // 多语言字典
        public Dictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();

        // 用于获取当前语言的文本
        public string Get()
        {
            string languageCode = LanguageService.CurrentLanguage;
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
            Translations[Default] = content;
        }
    }
}
