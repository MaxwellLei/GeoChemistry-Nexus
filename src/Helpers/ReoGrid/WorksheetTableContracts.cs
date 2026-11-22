using System;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 行头状态点的取值。颜色由控件统一决定，模块只需要说明“这一行是什么状态”。
    /// </summary>
    public enum WorksheetRowStatus
    {
        /// <summary>不显示状态点（空行或模块不关心）。</summary>
        None,
        /// <summary>已计算，没有问题（实心青绿）。</summary>
        Ok,
        /// <summary>已计算，但有警告（实心橙色）。</summary>
        Warning,
        /// <summary>计算失败或输入有误（实心红色）。</summary>
        Error,
        /// <summary>输入在计算之后被修改，结果已过期（橙色空心圈）。</summary>
        Stale,
        /// <summary>有输入数据但尚未计算（灰色空心圈）。</summary>
        NotCalculated,
    }

    /// <summary>
    /// 【可选】行头状态点。模块实现此接口并调用
    /// <see cref="ReoGridTableAppearance.SetRowStatusProvider"/> 后，行头会在行号旁绘制状态点；
    /// 不注册则行头保持 ReoGrid 原样。
    /// </summary>
    public interface IWorksheetRowStatusProvider
    {
        /// <summary>返回指定行（从 0 开始）的状态；绘制时只会请求可见行，实现必须足够快。</summary>
        WorksheetRowStatus GetRowStatus(int row);

        /// <summary>状态发生变化后触发，控件收到后会重绘行头。</summary>
        event EventHandler? RowStatusChanged;
    }

    /// <summary>
    /// 【可选】列头附加信息：表头上方的“分组行”（如 主量元素 / 标准矿物）和列头文字下方的“单位”。
    /// <list type="bullet">
    /// <item>分组行：任何列返回非空分组名时才会出现；相邻且分组名相同的列合并成一个分组带。所有列都返回 null 则不占高度。</item>
    /// <item>单位：某一列返回 null/空串则该列只显示列名（样品列、文本列直接返回 null 即可）；所有列都没有单位时列头不会加高。</item>
    /// </list>
    /// </summary>
    public interface IWorksheetColumnHeaderProvider
    {
        /// <summary>列所属分组名（已本地化）；null/空串表示该列不属于任何分组。</summary>
        string? GetGroup(int column);

        /// <summary>列单位文字；null/空串表示该列不显示单位。</summary>
        string? GetUnit(int column);

        /// <summary>分组名或单位发生变化（例如切换语言）后触发，控件会重新计算列头布局。</summary>
        event EventHandler? ColumnHeaderInfoChanged;
    }

    /// <summary>
    /// 【可选】列头显示文字：列头的 <c>Text</c> 是程序内部使用的列标识（脚本取数、导入导出、按列名查找都依赖它），
    /// 不能随语言变化；实现此接口的 <see cref="IWorksheetColumnHeaderProvider"/> 可以只改变列头“显示出来的文字”，
    /// 列头 <c>Text</c> 本身保持不变。
    /// </summary>
    public interface IWorksheetColumnHeaderLabelProvider
    {
        /// <summary>
        /// 返回该列列头要显示的文字；返回 null/空串表示直接显示列头 <c>Text</c>。
        /// 绘制时会频繁调用，实现必须足够快。
        /// </summary>
        string? GetLabel(int column, string? headerText);
    }
}
