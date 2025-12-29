using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Services
{
    public class LanguageService : INotifyPropertyChanged
    {
        public static string CurrentLanguage { get; set; } = "en-US";
        private readonly ResourceManager _resourceManager;

        // 使用线程安全的单例模式
        private static readonly Lazy<LanguageService> _lazy = new Lazy<LanguageService>(() => new LanguageService());
        public static LanguageService Instance => _lazy.Value;
        public event PropertyChangedEventHandler? PropertyChanged;

        public LanguageService()
        {
            //获取此命名空间下Resources的Lang的资源
            _resourceManager = new ResourceManager("GeoChemistryNexus.Data.Language.Language", typeof(LanguageService).Assembly);
        }

        public static void InitializeLanguage()
        {
            string language = ConfigHelper.GetConfig("language");
            if (language != "")
            {
                CurrentLanguage = language;
                LanguageService.Instance.ChangeLanguage(new System.Globalization.CultureInfo(CurrentLanguage));
            }
            else
            {
                LanguageService.Instance.ChangeLanguage(new System.Globalization.CultureInfo("en-US"));
            }
        }

        // 主动获取语言设置
        public static string GetLanguage()
        {
            string language = ConfigHelper.GetConfig("language");
            if (language != "")
            {
                return language;
            }
            return string.Empty;
        }

        // 获取语言的友好显示名称
        public static string GetLanguageDisplayName(string code)
        {
            if (string.IsNullOrEmpty(code)) return string.Empty;

            return code switch
            {
                "zh-CN" => "简体中文 (zh-CN)",
                "zh-TW" => "繁体中文 (zh-TW)",
                "en-US" => "English (en-US)",
                "ja-JP" => "日本語 (ja-JP)",
                "ru-RU" => "Русский (ru-RU)",
                "ko-KR" => "한국어 (ko-KR)",
                "de-DE" => "Deutsch (de-DE)",
                "es-ES" => "Español (es-ES)",
                _ => code
            };
        }

        public string this[string name]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }
                return _resourceManager.GetString(name);
            }
        }

        public void ChangeLanguage(CultureInfo cultureInfo)
        {
            CurrentLanguage = cultureInfo.Name;
            CultureInfo.CurrentCulture = cultureInfo;
            CultureInfo.CurrentUICulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));  //字符串集合，对应资源的值
        }

        public static void RefreshCurrentCulture()
        {
            if (!string.IsNullOrEmpty(CurrentLanguage))
            {
                var culture = new CultureInfo(CurrentLanguage);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                // DefaultThreadCurrentCulture 只能影响新线程，对当前线程无效
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
                
                // 通知UI更新绑定
                Instance.PropertyChanged?.Invoke(Instance, new PropertyChangedEventArgs("Item[]"));
            }
        }
    }
}
