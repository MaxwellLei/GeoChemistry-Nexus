using CommunityToolkit.Mvvm.ComponentModel;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 图解语言切换下拉列表中的单个语言选项
    /// </summary>
    public partial class DiagramLanguageOption : ObservableObject
    {
        public DiagramLanguageOption(string code, string displayName, bool isDefault, bool isCurrent)
        {
            Code = code;
            DisplayName = displayName;
            IsDefault = isDefault;
            _isCurrent = isCurrent;
        }

        /// <summary>语言代码，例如 zh-CN</summary>
        public string Code { get; }

        /// <summary>语言显示名称</summary>
        public string DisplayName { get; }

        /// <summary>是否为模板默认语言</summary>
        public bool IsDefault { get; }

        /// <summary>是否为当前正在使用的语言</summary>
        [ObservableProperty]
        private bool _isCurrent;
    }
}
