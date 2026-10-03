using System;
using System.Collections.Generic;

namespace GeoChemistryNexus.Services
{
    /// <summary>
    /// 元素 ↔ 氧化物化学计量换算服务。
    /// 所有换算因子均基于标准原子量（IUPAC 2021）推导。
    /// Factor_ElemToOxide = MolarMass(Oxide) / (n * AtomicWeight(Element))
    ///   其中 n = 每个氧化物分子中该元素的原子个数。
    /// Factor_OxideToElem = 1 / Factor_ElemToOxide
    /// </summary>
    public static class OxideElementConverterService
    {
        /// <summary>
        /// 内置元素‐氧化物换算条目数据库（涵盖地球化学全岩主量与常见微量）。
        /// </summary>
        public static IReadOnlyList<OxideElementEntry> Entries { get; } = BuildEntries();

        /// <summary>
        /// 执行单向换算：
        ///   mode = ElementToOxide → input(element wt%) → output(oxide wt%)
        ///   mode = OxideToElement → input(oxide wt%)   → output(element wt%)
        /// </summary>
        public static OxideElementResult Calculate(
            OxideElementEntry entry,
            double inputValue,
            ConversionMode mode)
        {
            if (inputValue < 0)
                return OxideElementResult.Invalid;

            double factor = mode == ConversionMode.ElementToOxide
                ? entry.FactorElemToOxide
                : entry.FactorOxideToElem;

            double outputValue = inputValue * factor;

            return new OxideElementResult
            {
                IsValid      = true,
                InputValue   = inputValue,
                OutputValue  = outputValue,
                Factor       = factor,
                Mode         = mode,
                Entry        = entry
            };
        }

        // ─────────────────────────── 数据表 ───────────────────────────
        private static List<OxideElementEntry> BuildEntries()
        {
            // 氧的原子量 O = 15.9994
            const double O = 15.9994;

            // 辅助计算函数：
            //   ToOxideFactor(aw, nElem, nO) = (nElem*aw + nO*O) / (nElem*aw)
            static double F(double aw, int nElem, int nO)
            {
                double mwOxide = nElem * aw + nO * O;
                double mwElem  = nElem * aw;
                return mwOxide / mwElem;          // Element → Oxide
            }

            var list = new List<OxideElementEntry>
            {
                // ──── 地球化学主量氧化物（全岩 XRF 常规 10 项）────
                New("Si",  "SiO₂",  28.0855, F(28.0855,1,2)),
                New("Ti",  "TiO₂",  47.867,  F(47.867, 1,2)),
                New("Al",  "Al₂O₃", 26.9815, F(26.9815,2,3)),
                New("Fe",  "Fe₂O₃", 55.845,  F(55.845, 2,3)),   // 全铁以 Fe₂O₃ 计
                New("Fe",  "FeO",   55.845,  F(55.845, 1,1),  overrideOxide:"FeO"),
                New("Mn",  "MnO",   54.9380, F(54.9380,1,1)),
                New("Mg",  "MgO",   24.3050, F(24.3050,1,1)),
                New("Ca",  "CaO",   40.078,  F(40.078, 1,1)),
                New("Na",  "Na₂O",  22.9898, F(22.9898,2,1)),
                New("K",   "K₂O",   39.0983, F(39.0983,2,1)),
                New("P",   "P₂O₅",  30.9738, F(30.9738,2,5)),
                // ──── 其他常见微量/次要氧化物 ────
                New("Cr",  "Cr₂O₃", 51.9961, F(51.9961,2,3)),
                New("V",   "V₂O₅",  50.9415, F(50.9415,2,5)),
                New("Ni",  "NiO",   58.6934, F(58.6934,1,1)),
                New("Cu",  "CuO",   63.546,  F(63.546, 1,1)),
                New("Zn",  "ZnO",   65.38,   F(65.38,  1,1)),
                New("Ba",  "BaO",   137.327, F(137.327,1,1)),
                New("Sr",  "SrO",   87.62,   F(87.62,  1,1)),
                New("U",   "UO₂",   238.029, F(238.029,1,2)),
                New("Th",  "ThO₂",  232.038, F(232.038,1,2)),
                New("Ce",  "Ce₂O₃", 140.116, F(140.116,2,3)),
                New("La",  "La₂O₃", 138.905, F(138.905,2,3)),
                New("Y",   "Y₂O₃",  88.906,  F(88.906, 2,3)),
                New("Zr",  "ZrO₂",  91.224,  F(91.224, 1,2)),
                New("Nb",  "Nb₂O₅", 92.906,  F(92.906, 2,5)),
                New("Co",  "CoO",   58.933,  F(58.933, 1,1)),
                New("Sc",  "Sc₂O₃", 44.956,  F(44.956, 2,3)),
            };

            return list;
        }

        private static OxideElementEntry New(
            string elem, string oxide, double atomicWeight, double toOxideFactor,
            string? overrideOxide = null)
        {
            return new OxideElementEntry
            {
                ElementSymbol    = elem,
                OxideFormula     = overrideOxide ?? oxide,
                AtomicWeight     = atomicWeight,
                OxideMolarMass   = atomicWeight * toOxideFactor,   // 反推分子量
                FactorElemToOxide = toOxideFactor,
                FactorOxideToElem = 1.0 / toOxideFactor
            };
        }
    }

    // ─────────────────────── 数据模型 ───────────────────────
    public enum ConversionMode
    {
        ElementToOxide,
        OxideToElement
    }

    public sealed class OxideElementEntry
    {
        /// <summary>元素符号，如 "Si"、"Fe"</summary>
        public string ElementSymbol    { get; init; } = string.Empty;

        /// <summary>对应氧化物化学式，如 "SiO₂"、"Fe₂O₃"</summary>
        public string OxideFormula     { get; init; } = string.Empty;

        /// <summary>元素标准原子量 (g/mol)</summary>
        public double AtomicWeight     { get; init; }

        /// <summary>氧化物摩尔质量 (g/mol)</summary>
        public double OxideMolarMass   { get; init; }

        /// <summary>元素 → 氧化物的换算因子（乘以此数得到氧化物 wt%）</summary>
        public double FactorElemToOxide { get; init; }

        /// <summary>氧化物 → 元素的换算因子（乘以此数得到元素 wt%）</summary>
        public double FactorOxideToElem { get; init; }

        /// <summary>下拉框显示文字：区分同一元素多个氧化物的组合</summary>
        public string DisplayLabel     => $"{ElementSymbol} / {OxideFormula}";
    }

    public sealed class OxideElementResult
    {
        public static OxideElementResult Invalid { get; } = new() { IsValid = false };

        public bool              IsValid      { get; init; }
        public double            InputValue   { get; init; }
        public double            OutputValue  { get; init; }
        public double            Factor       { get; init; }
        public ConversionMode    Mode         { get; init; }
        public OxideElementEntry? Entry       { get; init; }
    }
}
