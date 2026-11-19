using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.Models.HarkerDiagram
{
    /// <summary>变量分组：决定设置页里 chip 的分组标题。</summary>
    public enum HarkerVariableGroup
    {
        /// <summary>主量元素氧化物（wt%）</summary>
        Major,

        /// <summary>派生变量（由其他列计算得到）</summary>
        Derived,

        /// <summary>微量元素（ppm）</summary>
        Trace
    }

    /// <summary>
    /// Harker 多联图可选变量。
    /// Key 用作数据表列头与图层标识；Label 仅用于界面显示（带下标）；
    /// 绘图坐标轴标签使用 <see cref="AxisText"/>（纯 ASCII，避免字体缺字）。
    /// </summary>
    public sealed record HarkerVariable(
        string Key,
        string Label,
        string Unit,
        HarkerVariableGroup Group,
        double DefaultMin,
        double DefaultMax)
    {
        /// <summary>设置页下拉框 / 图层树中显示的名称，如 “SiO₂ (wt%)”。</summary>
        public string DisplayName => string.IsNullOrEmpty(Unit) ? Label : $"{Label} ({Unit})";

        /// <summary>坐标轴标签，如 “SiO2 (wt%)”。</summary>
        public string AxisText => string.IsNullOrEmpty(Unit) ? Key : $"{Key} ({Unit})";
    }

    /// <summary>Harker 多联图的预设变量清单。</summary>
    public static class HarkerVariableCatalog
    {
        public static IReadOnlyList<HarkerVariable> All { get; } =
        [
            new("SiO2", "SiO₂", "wt%", HarkerVariableGroup.Major, 40, 80),
            new("TiO2", "TiO₂", "wt%", HarkerVariableGroup.Major, 0, 3),
            new("Al2O3", "Al₂O₃", "wt%", HarkerVariableGroup.Major, 10, 22),
            new("Fe2O3T", "Fe₂O₃ᵀ", "wt%", HarkerVariableGroup.Major, 0, 16),
            new("MgO", "MgO", "wt%", HarkerVariableGroup.Major, 0, 10),
            new("CaO", "CaO", "wt%", HarkerVariableGroup.Major, 0, 14),
            new("Na2O", "Na₂O", "wt%", HarkerVariableGroup.Major, 0, 7),
            new("K2O", "K₂O", "wt%", HarkerVariableGroup.Major, 0, 6),
            new("P2O5", "P₂O₅", "wt%", HarkerVariableGroup.Major, 0, 1),
            new("MnO", "MnO", "wt%", HarkerVariableGroup.Major, 0, 0.4),
            new("Na2O+K2O", "Na₂O+K₂O", "wt%", HarkerVariableGroup.Derived, 0, 12),
            new("Mg#", "Mg#", string.Empty, HarkerVariableGroup.Derived, 0, 80),
            new("Zr", "Zr", "ppm", HarkerVariableGroup.Trace, 0, 400),
            new("Rb", "Rb", "ppm", HarkerVariableGroup.Trace, 0, 200),
            new("Sr", "Sr", "ppm", HarkerVariableGroup.Trace, 0, 600),
            new("Ba", "Ba", "ppm", HarkerVariableGroup.Trace, 0, 1500),
            new("Cr", "Cr", "ppm", HarkerVariableGroup.Trace, 0, 400),
            new("Ni", "Ni", "ppm", HarkerVariableGroup.Trace, 0, 200),
        ];

        /// <summary>可作为 X 轴（所有面板共用横轴）的变量，默认 SiO2。</summary>
        public static IReadOnlyList<HarkerVariable> XOptions { get; } =
            new[] { "SiO2", "MgO", "TiO2", "K2O" }.Select(Get).ToList();

        /// <summary>默认勾选的 Y 变量（顺序即默认面板顺序）。</summary>
        public static IReadOnlyList<string> DefaultYKeys { get; } =
            ["TiO2", "Al2O3", "Fe2O3T", "MgO", "CaO", "Na2O", "K2O", "P2O5"];

        public static HarkerVariable Get(string key)
        {
            return All.FirstOrDefault(v => string.Equals(v.Key, key, StringComparison.OrdinalIgnoreCase))
                   ?? throw new ArgumentException($"未知的 Harker 变量：{key}", nameof(key));
        }
    }
}
