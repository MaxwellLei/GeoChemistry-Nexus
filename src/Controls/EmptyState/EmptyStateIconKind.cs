namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// EmptyStateView 可用的动态插画类型。
    /// 新增插画步骤：
    ///   1. 在 Controls/EmptyState/Illustrations 下新增一个 UserControl（Viewbox + Canvas，动画写在 UserControl.Triggers）；
    ///   2. 在此枚举中添加一项；
    ///   3. 在 EmptyStateView.xaml 的 IllustrationStyle 中添加对应的 DataTrigger。
    /// </summary>
    public enum EmptyStateIconKind
    {
        /// <summary>不显示插画</summary>
        None = 0,

        /// <summary>地质温压计（未选择模板）</summary>
        Geothermometer,

        /// <summary>图解模板库（搜索无结果）</summary>
        DiagramSearch,

        /// <summary>个人图解模板为空</summary>
        PersonalDiagram,

        /// <summary>个人（自定义）温压计为空</summary>
        PersonalGeothermometer,

        /// <summary>主页推荐内容（搜索无结果）</summary>
        HomeSearch
    }
}
