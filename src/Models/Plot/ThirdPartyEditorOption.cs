using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 设置页“默认第三方编辑程序”下拉中的一项。
    /// Id 与配置项 default_third_party_app 的取值一致（CorelDRAW / Inkscape / Adobe Illustrator / Custom）。
    /// </summary>
    public partial class ThirdPartyEditorOption : ObservableObject
    {
        public const string CustomId = "Custom";

        public ThirdPartyEditorOption(string id, string name, string badgeText, string badgeBackground, string badgeForeground)
        {
            Id = id;
            displayName = name;
            triggerText = name;
            BadgeText = badgeText;
            BadgeBackground = CreateBrush(badgeBackground);
            BadgeForeground = CreateBrush(badgeForeground);
        }

        /// <summary>配置值 / SelectedValue。</summary>
        public string Id { get; }

        /// <summary>是否为“自定义”项（徽标使用文件夹图形，颜色跟随主题色）。</summary>
        public bool IsCustom => Id == CustomId;

        /// <summary>下拉列表中显示的名称。</summary>
        [ObservableProperty]
        private string displayName;

        /// <summary>收起状态（触发器）上显示的名称；自定义程序已指定时显示文件名。</summary>
        [ObservableProperty]
        private string triggerText;

        /// <summary>字母徽标文字（非官方 Logo）。</summary>
        public string BadgeText { get; }

        public Brush BadgeBackground { get; }

        public Brush BadgeForeground { get; }

        /// <summary>右侧状态文字：已检测到 / 未检测到 / 已指定 / 选择 .exe。</summary>
        [ObservableProperty]
        private string statusText = string.Empty;

        /// <summary>状态为“正常”（绿色）还是“未就绪”（灰色）。</summary>
        [ObservableProperty]
        private bool isStatusOk;

        private static Brush CreateBrush(string hex)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
            brush.Freeze();
            return brush;
        }
    }
}
