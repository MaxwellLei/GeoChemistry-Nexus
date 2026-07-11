using System;
using System.Collections.Generic;

namespace GeoChemistryNexus.Helpers
{
    public class IconItem
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public static class HomeIconHelper
    {
        public const string DefaultIcon = "\uE774";

        public static bool IsUrlIcon(string? icon)
        {
            if (string.IsNullOrWhiteSpace(icon))
                return false;

            return icon.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || icon.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        public static string ResolveIcon(string icon)
        {
            if (string.IsNullOrWhiteSpace(icon))
                return DefaultIcon;

            return icon.Trim();
        }

        /// <summary>
        /// 首页链接编辑用的预设 Segoe MDL2 图标列表。
        /// </summary>
        public static List<IconItem> CreatePresetIcons()
        {
            return new List<IconItem>
            {
                new IconItem { Name = "网页 (Globe)", Code = "\uE774" },
                new IconItem { Name = "链接 (Link)", Code = "\uE71B" },
                new IconItem { Name = "收藏 (Star)", Code = "\uE734" },
                new IconItem { Name = "主页 (Home)", Code = "\uE80F" },
                new IconItem { Name = "文档 (Document)", Code = "\uE8A5" },
                new IconItem { Name = "邮件 (Mail)", Code = "\uE715" },
                new IconItem { Name = "云端 (Cloud)", Code = "\uE753" },
                new IconItem { Name = "设置 (Settings)", Code = "\uE713" },
                new IconItem { Name = "搜索 (Search)", Code = "\uE721" }
            };
        }
    }
}
