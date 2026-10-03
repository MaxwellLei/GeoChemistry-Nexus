using System.Collections.Generic;

namespace GeoChemistryNexus.Models.Niggli
{
    /// <summary>
    /// Barth-Niggli 分子标准矿物计算所需的常量与配置
    /// Constants for Barth-Niggli Catanorm calculation
    /// </summary>
    public static class NiggliConstants
    {
        /// <summary>
        /// 数值容差（避免浮点误差引发负值判断错误）
        /// </summary>
        public const double Epsilon = 1e-12;

        /// <summary>
        /// 默认 Fe³⁺/Fe总 比值 (Le Maitre, 2002)
        /// </summary>
        public const double DefaultFe3Ratio = 0.15;

        /// <summary>
        /// 氧化物摩尔质量 (g/mol)，仅包含计算所需的氧化物
        /// Molar masses of oxides used in calculation (g/mol)
        /// </summary>
        public static readonly Dictionary<string, double> OxideMolarMass = new()
        {
            ["SiO2"]  = 60.083,
            ["TiO2"]  = 79.866,
            ["Al2O3"] = 101.961,
            ["Fe2O3"] = 159.688,
            ["FeO"]   = 71.844,
            ["MnO"]   = 70.937,
            ["MgO"]   = 40.304,
            ["CaO"]   = 56.077,
            ["Na2O"]  = 61.979,
            ["K2O"]   = 94.196,
            ["CO2"]   = 44.010,
            ["P2O5"]  = 141.944,
            ["F"]     = 19.000,
            ["S"]     = 32.060,
        };

        /// <summary>
        /// 每种氧化物化学式中的阳离子数目
        /// Number of cation atoms per formula unit for each oxide
        /// </summary>
        public static readonly Dictionary<string, int> OxideCationCount = new()
        {
            ["SiO2"]  = 1,
            ["TiO2"]  = 1,
            ["Al2O3"] = 2,
            ["Fe2O3"] = 2,
            ["FeO"]   = 1,
            ["MnO"]   = 1,
            ["MgO"]   = 1,
            ["CaO"]   = 1,
            ["Na2O"]  = 2,
            ["K2O"]   = 2,
            ["CO2"]   = 1,
            ["P2O5"]  = 2,
            ["F"]     = 1,
            ["S"]     = 1,
        };

        /// <summary>
        /// 用户输入时接受的氧化物名称列表（包含全铁输入列）
        /// Input oxide columns accepted from user (including total iron alternatives)
        /// </summary>
        public static readonly string[] InputOxides =
        {
            "SiO2", "TiO2", "Al2O3", "Fe2O3", "FeO", "FeOT",
            "MnO", "MgO", "CaO", "Na2O", "K2O", "CO2", "P2O5", "F", "S"
        };

        /// <summary>
        /// 计算时实际使用的氧化物（排除 FeOT 全铁列，铁在预处理阶段已拆分）
        /// Oxides actually consumed during Catanorm calculation after iron partitioning
        /// </summary>
        public static readonly string[] ComputedOxides =
        {
            "SiO2", "TiO2", "Al2O3", "Fe2O3", "FeO", "MnO", "MgO", "CaO", "Na2O", "K2O",
            "CO2", "P2O5", "F", "S"
        };

        /// <summary>
        /// 标准分子矿物在结果表格中的输出顺序
        /// Display order of normative minerals in result table
        /// </summary>
        public static readonly string[] MineralDisplayOrder =
        {
            "Q",   "C",   "Or",  "Plag","Ab",  "An",
            "Lc",  "Ne",  "Kp",  "Ac",  "Ns",  "Ks",
            "Di",  "Hy",  "Ol",  "Cs",  "Wo",
            "Mt",  "Hm",  "Il",  "Tn",  "Pf",  "Ru",
            "Ap",  "Fr",  "Py",  "Cc"
        };

        /// <summary>
        /// 矿物的尼格里端元组分列（后置于结果列之后）
        /// Endmember ratio columns for Niggli mineral proportions
        /// </summary>
        public static readonly string[] EndmemberRatioColumns =
        {
            "Wo%", "En%", "Fs%", "Fo%", "Fa%", "An%", "Mgr"
        };

        /// <summary>
        /// 尼格里参数（Niggli Values）显示名称列表
        /// Niggli values column names
        /// </summary>
        public static readonly string[] NiggliValueColumns =
        {
            "si", "al", "fm", "c", "alk", "k", "mg", "qz"
        };

        /// <summary>
        /// 矿物英文全称（用于帮助说明与工具提示）
        /// Full English names of normative minerals
        /// </summary>
        public static readonly Dictionary<string, string> MineralEnglishNames = new()
        {
            ["Q"]    = "Quartz",
            ["C"]    = "Corundum",
            ["Or"]   = "Orthoclase",
            ["Plag"] = "Plagioclase",
            ["Ab"]   = "Albite",
            ["An"]   = "Anorthite",
            ["Lc"]   = "Leucite",
            ["Ne"]   = "Nepheline",
            ["Kp"]   = "Kaliophilite",
            ["Ac"]   = "Acmite",
            ["Ns"]   = "Sodium Metasilicate",
            ["Ks"]   = "Potassium Metasilicate",
            ["Di"]   = "Diopside",
            ["Hy"]   = "Hypersthene",
            ["Ol"]   = "Olivine",
            ["Cs"]   = "Calcium Orthosilicate",
            ["Wo"]   = "Wollastonite",
            ["Mt"]   = "Magnetite",
            ["Hm"]   = "Hematite",
            ["Il"]   = "Ilmenite",
            ["Tn"]   = "Titanite (Sphene)",
            ["Pf"]   = "Perovskite",
            ["Ru"]   = "Rutile",
            ["Ap"]   = "Apatite",
            ["Fr"]   = "Fluorite",
            ["Py"]   = "Pyrite",
            ["Cc"]   = "Calcite",
        };
    }
}
