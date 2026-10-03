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
        public static string CurrentLanguage { get; set; } = AppCultureRegistry.DefaultAppLanguage;
        private readonly ResourceManager _resourceManager;
        private CultureInfo _cachedCulture;

        // 使用线程安全的单例模式
        private static readonly Lazy<LanguageService> _lazy = new Lazy<LanguageService>(() => new LanguageService());
        public static LanguageService Instance => _lazy.Value;
        public event PropertyChangedEventHandler? PropertyChanged;

        public LanguageService()
        {
            //获取此命名空间下Resources的Lang的资源
            _resourceManager = new ResourceManager("GeoChemistryNexus.Data.Language.Language", typeof(LanguageService).Assembly);
            _cachedCulture = new CultureInfo(AppCultureRegistry.ResolveAppLanguage(CurrentLanguage));
        }

        public static void InitializeLanguage()
        {
            string? language = ConfigHelper.GetConfig("language");
            if (!string.IsNullOrEmpty(language))
            {
                CurrentLanguage = AppCultureRegistry.ResolveAppLanguage(language);
                Instance.ChangeLanguage(new CultureInfo(CurrentLanguage));
                return;
            }

            // 首次启动：按系统 UI 语言选择默认语言并写入配置
            string detected = AppCultureRegistry.ResolveFromSystemCulture();
            ConfigHelper.SetConfig("language", detected);
            CurrentLanguage = detected;
            Instance.ChangeLanguage(new CultureInfo(detected));
        }

        // 主动获取语言设置
        public static string GetLanguage()
        {
            string? language = ConfigHelper.GetConfig("language");
            if (!string.IsNullOrEmpty(language))
            {
                return language;
            }
            return string.Empty;
        }

        // 获取语言的友好显示名称
        public static string GetLanguageDisplayName(string code)
        {
            return AppCultureRegistry.GetDisplayName(code);
        }

        public string this[string name]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                return _resourceManager.GetString(name, _cachedCulture) ?? string.Empty;
            }
        }

        public static string GetString(string key, string fallback)
        {
            return string.IsNullOrEmpty(Instance[key]) ? fallback : Instance[key]!;
        }

        public void ChangeLanguage(CultureInfo cultureInfo)
        {
            CurrentLanguage = AppCultureRegistry.ResolveAppLanguage(cultureInfo.Name);
            _cachedCulture = new CultureInfo(CurrentLanguage);
            CultureInfo.CurrentCulture = _cachedCulture;
            CultureInfo.CurrentUICulture = _cachedCulture;
            CultureInfo.DefaultThreadCurrentCulture = _cachedCulture;
            CultureInfo.DefaultThreadCurrentUICulture = _cachedCulture;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));  //字符串集合，对应资源的值
        }

        public static void RefreshCurrentCulture()
        {
            if (!string.IsNullOrEmpty(CurrentLanguage))
            {
                var resolved = AppCultureRegistry.ResolveAppLanguage(CurrentLanguage);
                Instance._cachedCulture = new CultureInfo(resolved);
                CultureInfo.CurrentCulture = Instance._cachedCulture;
                CultureInfo.CurrentUICulture = Instance._cachedCulture;
                // DefaultThreadCurrentCulture 只能影响新线程，对当前线程无效
                CultureInfo.DefaultThreadCurrentCulture = Instance._cachedCulture;
                CultureInfo.DefaultThreadCurrentUICulture = Instance._cachedCulture;
                
                // 通知UI更新绑定
                Instance.PropertyChanged?.Invoke(Instance, new PropertyChangedEventArgs("Item[]"));
            }
        }
    }
}
