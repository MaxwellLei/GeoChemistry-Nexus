using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Services
{
    public interface IContentLanguageScope
    {
        ContentLanguageContext? Active { get; set; }
    }

    /// <summary>
    /// 指向当前活跃图解的 ContentLanguageContext，供属性面板等 UI 子树使用。
    /// </summary>
    public sealed class ContentLanguageScope : IContentLanguageScope
    {
        public static ContentLanguageScope Instance { get; } = new();

        public ContentLanguageContext? Active { get; set; }
    }
}
