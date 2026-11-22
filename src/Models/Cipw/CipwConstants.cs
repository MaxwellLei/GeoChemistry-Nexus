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
        /// 氧原子摩尔质量 (g/mol)，与 <see cref="MolarMass"/> 中 FeO、MgO 等氧化物所用原子量一致
        /// </summary>
        public const double OxygenMolarMass = 15.999;

        /// <summary>
        /// 标准矿物摩尔质量 (g/mol)。
        /// 全部由 <see cref="MolarMass"/> 中的氧化物摩尔质量按化学式推导，
        /// 以保证标准矿物质量与输入氧化物质量严格守恒（避免手工取整造成的质量平衡偏差）。
        /// 其中 Py / Fl / Hl 含非氧阴离子，需扣除被置换出的氧。
        /// </summary>
        public static readonly Dictionary<string, double> MineralMolarMass = new()
        {
            // 硅铝矿物
            ["Q"] = MolarMass["SiO2"],
            ["Cor"] = MolarMass["Al2O3"],
            ["Or"] = (MolarMass["K2O"] + MolarMass["Al2O3"] + 6.0 * MolarMass["SiO2"]) / 2.0,
            ["Ab"] = (MolarMass["Na2O"] + MolarMass["Al2O3"] + 6.0 * MolarMass["SiO2"]) / 2.0,
            ["An"] = MolarMass["CaO"] + MolarMass["Al2O3"] + 2.0 * MolarMass["SiO2"],

            // 似长石
            ["Le"] = (MolarMass["K2O"] + MolarMass["Al2O3"] + 4.0 * MolarMass["SiO2"]) / 2.0,
            ["Ne"] = (MolarMass["Na2O"] + MolarMass["Al2O3"] + 2.0 * MolarMass["SiO2"]) / 2.0,
            ["Kp"] = (MolarMass["K2O"] + MolarMass["Al2O3"] + 2.0 * MolarMass["SiO2"]) / 2.0,

            // 铁镁硅酸盐
            ["Ac"] = (MolarMass["Na2O"] + MolarMass["Fe2O3"] + 4.0 * MolarMass["SiO2"]) / 2.0,
            ["Di"] = MolarMass["CaO"] + MolarMass["MgO"] + 2.0 * MolarMass["SiO2"],
            ["Hd"] = MolarMass["CaO"] + MolarMass["FeO"] + 2.0 * MolarMass["SiO2"],
            ["Wo"] = MolarMass["CaO"] + MolarMass["SiO2"],
            ["En"] = MolarMass["MgO"] + MolarMass["SiO2"],
            ["Fs"] = MolarMass["FeO"] + MolarMass["SiO2"],
            ["Fo"] = 2.0 * MolarMass["MgO"] + MolarMass["SiO2"],
            ["Fa"] = 2.0 * MolarMass["FeO"] + MolarMass["SiO2"],
            ["Cs"] = 2.0 * MolarMass["CaO"] + MolarMass["SiO2"],

            // 氧化物及副矿物
            ["Mt"] = MolarMass["FeO"] + MolarMass["Fe2O3"],
            ["Hm"] = MolarMass["Fe2O3"],
            ["Ilm"] = MolarMass["FeO"] + MolarMass["TiO2"],
            ["Cm"] = MolarMass["FeO"] + MolarMass["Cr2O3"],
            ["Ru"] = MolarMass["TiO2"],
            ["Tn"] = MolarMass["CaO"] + MolarMass["TiO2"] + MolarMass["SiO2"],
            ["Pf"] = MolarMass["CaO"] + MolarMass["TiO2"],
            ["Z"] = MolarMass["ZrO2"] + MolarMass["SiO2"],
            ["Ap"] = (10.0 * MolarMass["CaO"] + 3.0 * MolarMass["P2O5"]) / 3.0,

            // 挥发分、碳酸盐、盐类
            ["Cc"] = MolarMass["CaO"] + MolarMass["CO2"],
            ["Py"] = MolarMass["FeO"] + 2.0 * MolarMass["S"] - OxygenMolarMass,
            ["Fl"] = MolarMass["CaO"] + 2.0 * MolarMass["F"] - OxygenMolarMass,
            ["Hl"] = (MolarMass["Na2O"] + 2.0 * MolarMass["Cl"] - OxygenMolarMass) / 2.0,
            ["Th"] = MolarMass["Na2O"] + MolarMass["SO3"],

            // 准硅酸盐
            ["ns"] = MolarMass["Na2O"] + MolarMass["SiO2"],
            ["ks"] = MolarMass["K2O"] + MolarMass["SiO2"],
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
            ["Cs"] = "斜硅钙石",
            ["Pf"] = "钙钛矿",
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
