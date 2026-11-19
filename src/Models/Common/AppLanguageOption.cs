using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 设置页“界面语言”选择器（Controls/Common/LanguageSelector）中的一项：国旗（或角标）+ 母语名 + 地区。
    /// </summary>
    public partial class AppLanguageOption : ObservableObject
    {
        public AppLanguageOption(
            string code,
            string nativeName,
            string regionName,
            string? flagKey,
            string? badgeText,
            string localizedName,
            string? extraInfo = null)
        {
            Code = code;
            NativeName = nativeName;
            RegionName = regionName;
            FlagKey = flagKey;
            BadgeText = badgeText ?? string.Empty;
            LocalizedName = localizedName;

            TriggerText = $"{nativeName}（{regionName}）";

            // 副标题：当前界面语言下的语言名 · 地区（缺少翻译时自动省略该段）[· 文化代码]
            var parts = new System.Collections.Generic.List<string>(3);
            if (!string.IsNullOrWhiteSpace(localizedName)) parts.Add(localizedName);
            if (!string.IsNullOrWhiteSpace(regionName)) parts.Add(regionName);
            if (!string.IsNullOrWhiteSpace(extraInfo)) parts.Add(extraInfo);
            SubText = string.Join(" · ", parts);

            SearchText = string.Concat(nativeName, regionName, localizedName, code).ToLowerInvariant();
        }

        /// <summary>文化代码（BCP 47），如 zh-CN。</summary>
        public string Code { get; }

        /// <summary>母语名称，如 简体中文、English。</summary>
        public string NativeName { get; }

        /// <summary>地区名称（母语），如 中国、United States。</summary>
        public string RegionName { get; }

        /// <summary>国旗资源键（对应 Styles.xaml 中的 Flag.XX）；为空时使用 <see cref="BadgeText"/> 角标。</summary>
        public string? FlagKey { get; }

        /// <summary>没有国旗时显示的角标文字。</summary>
        public string BadgeText { get; }

        /// <summary>当前界面语言下的语言名称（来自多语言资源，可能为空）。</summary>
        public string LocalizedName { get; }

        public bool HasFlag => !string.IsNullOrEmpty(FlagKey);

        /// <summary>触发按钮上显示的文字：母语名（地区）。</summary>
        public string TriggerText { get; }

        /// <summary>下拉列表项的副标题。</summary>
        public string SubText { get; }

        /// <summary>用于搜索匹配的小写文本。</summary>
        public string SearchText { get; }

        /// <summary>是否为当前选中的语言（列表中显示对勾与浅色底）。</summary>
        [ObservableProperty]
        private bool _isCurrent;

        public bool Matches(string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return true;

            return SearchText.Contains(keyword.Trim().ToLowerInvariant(), StringComparison.Ordinal);
        }
    }
}
