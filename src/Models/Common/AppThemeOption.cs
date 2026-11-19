using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Services;
using System.Windows.Media;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 设置页“主题颜色”下拉/色块列表中的一项。
    /// </summary>
    public partial class AppThemeOption : ObservableObject
    {
        private readonly string _languageKey;
        private readonly string _fallbackName;

        public AppThemeOption(string id, string languageKey, string fallbackName, Brush swatchBrush)
        {
            Id = id;
            _languageKey = languageKey;
            _fallbackName = fallbackName;
            SwatchBrush = swatchBrush;
            _displayName = ResolveDisplayName();
        }

        public string Id { get; }

        public Brush SwatchBrush { get; }

        [ObservableProperty]
        private string _displayName;

        /// <summary>
        /// 界面语言切换后刷新显示名称。
        /// </summary>
        public void RefreshDisplayName() => DisplayName = ResolveDisplayName();

        private string ResolveDisplayName() => LanguageService.GetString(_languageKey, _fallbackName);
    }
}
