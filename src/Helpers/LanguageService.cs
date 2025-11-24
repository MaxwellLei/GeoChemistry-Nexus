using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Helpers
{
    public class LanguageService : INotifyPropertyChanged
    {
        public static string CurrentLanguage { get; set; } = "en-US";
        private readonly ResourceManager _resourceManager;

        // 使用线程安全的单例模式
        private static readonly Lazy<LanguageService> _lazy = new Lazy<LanguageService>(() => new LanguageService());
        public static LanguageService Instance => _lazy.Value;
        public event PropertyChangedEventHandler PropertyChanged;

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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("item[]"));  //字符串集合，对应资源的值
        }
    }
}
