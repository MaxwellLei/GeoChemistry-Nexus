using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Interfaces
{
    /// <summary>
    /// 绘图对象自定义绘图能力接口
    /// </summary>
    public interface IPlotLayer
    {
        /// <summary>
        /// 在指定的 Plot 对象上渲染自己
        /// </summary>
        /// <param name="plot">ScottPlot 的绘图对象</param>
        void Render(Plot plot);

        /// <summary>
        /// 获取该图层对应的 ScottPlot 绘图对象 (用于命中测试、高亮等)
        /// </summary>
        IPlottable? Plottable { get; }

        /// <summary>
        /// 高亮显示（选中状态）
        /// </summary>
        void Highlight();

        /// <summary>
        /// 变暗显示（遮罩状态）
        /// </summary>
        void Dim();

        /// <summary>
        /// 恢复正常样式
        /// </summary>
        void Restore();
    }
}
