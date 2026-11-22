using System;
using System.Text.RegularExpressions;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 内容包标识（地质温压计 PluginId / EPMA PackageId）的统一规则：
    /// 标识 = 前缀 + 随机部分。前缀表示来源（official_ 在线 / custom_ 我的），随机部分在创建时生成一次，
    /// 之后一直跟随包；在线 ⇄ 我的互转只改变前缀。
    /// </summary>
    public static class ContentPackageIdHelper
    {
        public const string OfficialPrefix = "official_";
        public const string CustomPrefix = "custom_";

        /// <summary>创建一个全新的标识（随机部分只在创建时生成）</summary>
        public static string CreateNew(bool isOfficial)
            => Compose(Guid.NewGuid().ToString("N"), isOfficial);

        /// <summary>取出标识的随机部分（去掉 official_ / custom_ 前缀）</summary>
        public static string GetCore(string? id)
        {
            string value = id?.Trim() ?? string.Empty;
            if (value.StartsWith(OfficialPrefix, StringComparison.OrdinalIgnoreCase))
                return value[OfficialPrefix.Length..];
            if (value.StartsWith(CustomPrefix, StringComparison.OrdinalIgnoreCase))
                return value[CustomPrefix.Length..];
            return value;
        }

        /// <summary>按来源给随机部分加上前缀</summary>
        public static string Compose(string core, bool isOfficial)
            => (isOfficial ? OfficialPrefix : CustomPrefix) + core;

        /// <summary>保持随机部分不变，只把前缀换成目标来源的前缀（在线 ⇄ 我的互转 / 导入个人包）</summary>
        public static string WithOrigin(string? id, bool isOfficial)
            => Compose(GetCore(id), isOfficial);

        /// <summary>标识是否已经带有目标来源的前缀</summary>
        public static bool HasOrigin(string? id, bool isOfficial)
            => id != null && id.StartsWith(isOfficial ? OfficialPrefix : CustomPrefix, StringComparison.OrdinalIgnoreCase);

        /// <summary>标识是否可安全用作包标识与文件名：字母开头，只含字母、数字、_ . -，最长 80 个字符（与 EPMA 清单规则一致）</summary>
        public static bool IsValid(string? id)
            => id != null && Regex.IsMatch(id, @"\A[A-Za-z][A-Za-z0-9_.-]{0,79}\z");
    }
}
