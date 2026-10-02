using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Services
{
    /// <summary>
    /// 指向当前活跃图解的 ContentLanguageContext，供属性面板等 UI 子树使用。
    /// </summary>
    public sealed class ContentLanguageScope
    {
        public static ContentLanguageScope Instance { get; } = new();

        public ContentLanguageContext? Active { get; set; }
    }
}
