using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 图解/模板内容语言的实例级上下文。null 表示使用 App 界面语言。
    /// </summary>
    public class ContentLanguageContext : INotifyPropertyChanged
    {
        private string? _contentLanguage;

        public string? ContentLanguage
        {
            get => _contentLanguage;
            set
            {
                if (string.Equals(_contentLanguage, value, System.StringComparison.OrdinalIgnoreCase))
                    return;

                _contentLanguage = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
