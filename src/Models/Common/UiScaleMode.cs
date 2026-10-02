namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 界面缩放模式。Auto 按当前显示器分辨率与系统 DPI 估算；其余为固定倍率。
    /// </summary>
    public enum UiScaleMode
    {
        Auto = 0,
        Percent100 = 100,
        Percent125 = 125,
        Percent150 = 150,
        Percent200 = 200
    }
}
