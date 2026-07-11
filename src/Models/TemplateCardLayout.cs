namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 自适应模式下的卡片大小档位（偏好边长，含外边距）。
    /// </summary>
    public enum TemplateCardSizePreset
    {
        Compact = 0,
        Standard = 1
    }

    /// <summary>
    /// 图解模板卡片布局设置快照。
    /// </summary>
    public sealed class TemplateCardLayoutSettings
    {
        public TemplateCardSizePreset SizePreset { get; init; }
    }
}
