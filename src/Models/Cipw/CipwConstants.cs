using System.Collections.Generic;

namespace GeoChemistryNexus.Models.Cipw
{
    /// <summary>
    /// CIPW标准矿物计算所需的常量定义
    /// </summary>
    public static class CipwConstants
    {
        /// <summary>
        /// 数值容差
        /// </summary>
        public const double EPS = 1e-12;

        /// <summary>
        /// 默认Fe3+/Fe总比值 (Le Maitre, 2002)
        /// </summary>
        public const double FE3_FRACTION_DEFAULT = 0.15;

        /// <summary>
        /// 氧化物摩尔质量 (g/mol)
        /// </summary>
        public static readonly Dictionary<string, double> MolarMass = new()
        {
            ["SiO2"] = 60.083,
            ["Al2O3"] = 101.961,
            ["FeO"] = 71.844,
            ["Fe2O3"] = 159.688,
            ["MgO"] = 40.304,
            ["CaO"] = 56.077,
            ["Na2O"] = 61.979,
            ["K2O"] = 94.196,
            ["TiO2"] = 79.866,
            ["P2O5"] = 141.944,
            ["MnO"] = 70.937,
            ["ZrO2"] = 123.22,
            ["Cr2O3"] = 151.99,
            ["CO2"] = 44.01,
            ["S"] = 32.06,
            ["F"] = 19.00,
            ["Cl"] = 35.45,
            ["SO3"] = 80.06,
        };

        /// <summary>
        /// 标准矿物摩尔质量 (g/mol)
        /// </summary>
        public static readonly Dictionary<string, double> MineralMolarMass = new()
        {
            // 硅铝矿物
            ["Q"] = 60.083,
            ["Cor"] = 101.961,
            ["Or"] = 278.33,
            ["Ab"] = 262.22,
            ["An"] = 278.21,

            // 似长石
            ["Le"] = 218.25,
            ["Ne"] = 142.05,
            ["Kp"] = 158.16,

            // 铁镁硅酸盐
            ["Ac"] = 231.00,
            ["Di"] = 216.55,
            ["Hd"] = 248.09,
            ["Wo"] = 116.16,
            ["En"] = 100.39,
            ["Fs"] = 131.93,
            ["Fo"] = 140.69,
            ["Fa"] = 203.77,

            // 氧化物及副矿物
            ["Mt"] = 231.54,
            ["Hm"] = 159.69,
            ["Ilm"] = 151.71,
            ["Cm"] = 223.84,
            ["Ru"] = 79.87,
            ["Tn"] = 196.06,
            ["Z"] = 183.31,
            ["Ap"] = 328.86,

            // 挥发分、碳酸盐、盐类
            ["Cc"] = 100.09,
            ["Py"] = 119.98,
            ["Fl"] = 78.07,
            ["Hl"] = 58.44,
            ["Th"] = 142.04,

            // 准硅酸盐
            ["ns"] = 122.06,
            ["ks"] = 154.28,
        };

        /// <summary>
        /// 标准矿物中文名称
        /// </summary>
        public static readonly Dictionary<string, string> MineralNames = new()
        {
            ["Q"] = "石英",
            ["Cor"] = "刚玉",
            ["Or"] = "正长石",
            ["Ab"] = "钠长石",
            ["An"] = "钙长石",
            ["Le"] = "白榴石",
            ["Ne"] = "霞石",
            ["Kp"] = "假白榴石",
            ["Ac"] = "霓石",
            ["Di"] = "透辉石",
            ["Hd"] = "钙铁辉石",
            ["Wo"] = "硅灰石",
            ["En"] = "顽火辉石",
            ["Fs"] = "铁辉石",
            ["Fo"] = "镁橄榄石",
            ["Fa"] = "铁橄榄石",
            ["Mt"] = "磁铁矿",
            ["Hm"] = "赤铁矿",
            ["Ilm"] = "钛铁矿",
            ["Cm"] = "铬铁矿",
            ["Ru"] = "金红石",
            ["Tn"] = "榍石",
            ["Z"] = "锆石",
            ["Ap"] = "磷灰石",
            ["Cc"] = "方解石",
            ["Py"] = "黄铁矿",
            ["Fl"] = "萤石",
            ["Hl"] = "石盐",
            ["Th"] = "芒硝",
            ["ns"] = "偏硅酸钠",
            ["ks"] = "偏硅酸钾",
        };

        /// <summary>
        /// 跟踪计算的氧化物列表
        /// </summary>
        public static readonly string[] TrackedOxides =
        {
            "SiO2", "Al2O3", "FeO", "Fe2O3", "MgO", "CaO",
            "Na2O", "K2O", "TiO2", "P2O5", "ZrO2", "Cr2O3",
            "MnO", "CO2", "S", "F", "Cl", "SO3"
        };

        /// <summary>
        /// 用户输入的氧化物列表（含FeOT和MnO）
        /// </summary>
        public static readonly string[] InputOxides =
        {
            "SiO2", "TiO2", "Al2O3", "Fe2O3", "FeO", "FeOT",
            "MnO", "MgO", "CaO", "Na2O", "K2O", "P2O5",
            "ZrO2", "Cr2O3", "CO2", "S", "F", "Cl", "SO3"
        };
    }
}
