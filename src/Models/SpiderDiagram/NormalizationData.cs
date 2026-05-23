using System.Collections.Generic;

namespace GeoChemistryNexus.Models.SpiderDiagram
{
    /// <summary>
    /// 蛛网图标准化参考数据集合
    /// 包含 REE 和微量元素的各种标准化方案
    /// </summary>
    public static class NormalizationData
    {
        /// <summary>
        /// REE 元素默认顺序（La 到 Lu）
        /// </summary>
        public static readonly List<string> ReeElementOrder = new List<string>
        {
            "La", "Ce", "Pr", "Nd", "Sm", "Eu", "Gd", "Tb", "Dy", "Ho", "Er", "Tm", "Yb", "Lu"
        };

        /// <summary>
        /// 微量元素默认顺序（Sun & McDonough 1989 排列）
        /// </summary>
        public static readonly List<string> TraceElementOrder = new List<string>
        {
            "Cs", "Rb", "Ba", "Th", "U", "Nb", "Ta", "K", "La", "Ce", "Pb", "Pr",
            "Sr", "Nd", "Zr", "Hf", "Sm", "Eu", "Ti", "Gd", "Tb", "Dy", "Ho",
            "Er", "Tm", "Yb", "Lu", "Y"
        };

        /// <summary>
        /// 获取所有可用的 REE 标准化方案
        /// </summary>
        public static List<NormalizationStandard> GetReeStandards()
        {
            return new List<NormalizationStandard>
            {
                ChondriteBoynton1984(),
                ChondriteSunMcDonough1995(),
                ChondriteNakamura1974(),
                ChondriteTaylorMcLennan1985()
            };
        }

        /// <summary>
        /// 获取所有可用的微量元素标准化方案
        /// </summary>
        public static List<NormalizationStandard> GetTraceElementStandards()
        {
            return new List<NormalizationStandard>
            {
                PrimitiveMantleSunMcDonough1989(),
                PrimitiveMantleMcDonoughSun1995(),
                MORBSunMcDonough1989(),
                NMORBPearce1983()
            };
        }

        #region REE Standards

        /// <summary>
        /// Boynton (1984) 球粒陨石
        /// </summary>
        public static NormalizationStandard ChondriteBoynton1984()
        {
            return new NormalizationStandard
            {
                Name = "Chondrite (Boynton, 1984)",
                ShortName = "C1 Boynton 1984",
                Reference = "Boynton W.V. (1984) Cosmochemistry of the rare earth elements: meteorite studies. In: Henderson P. (ed.) Rare Earth Element Geochemistry. Elsevier, pp. 63-114.",
                Type = "REE",
                Values = new Dictionary<string, double>
                {
                    { "La", 0.310 },
                    { "Ce", 0.808 },
                    { "Pr", 0.122 },
                    { "Nd", 0.600 },
                    { "Sm", 0.195 },
                    { "Eu", 0.0735 },
                    { "Gd", 0.259 },
                    { "Tb", 0.0474 },
                    { "Dy", 0.322 },
                    { "Ho", 0.0718 },
                    { "Er", 0.210 },
                    { "Tm", 0.0324 },
                    { "Yb", 0.209 },
                    { "Lu", 0.0322 }
                }
            };
        }

        /// <summary>
        /// Sun & McDonough (1995) 球粒陨石
        /// </summary>
        public static NormalizationStandard ChondriteSunMcDonough1995()
        {
            return new NormalizationStandard
            {
                Name = "Chondrite (Sun & McDonough, 1995)",
                ShortName = "C1 Sun & McDonough 1995",
                Reference = "Sun S.-s. and McDonough W.F. (1995) The composition of the Earth. Chemical Geology, 120, 223-253.",
                Type = "REE",
                Values = new Dictionary<string, double>
                {
                    { "La", 0.237 },
                    { "Ce", 0.613 },
                    { "Pr", 0.0928 },
                    { "Nd", 0.457 },
                    { "Sm", 0.148 },
                    { "Eu", 0.0563 },
                    { "Gd", 0.199 },
                    { "Tb", 0.0361 },
                    { "Dy", 0.246 },
                    { "Ho", 0.0546 },
                    { "Er", 0.160 },
                    { "Tm", 0.0247 },
                    { "Yb", 0.161 },
                    { "Lu", 0.0246 }
                }
            };
        }

        /// <summary>
        /// Nakamura (1974) 球粒陨石
        /// </summary>
        public static NormalizationStandard ChondriteNakamura1974()
        {
            return new NormalizationStandard
            {
                Name = "Chondrite (Nakamura, 1974)",
                ShortName = "C1 Nakamura 1974",
                Reference = "Nakamura N. (1974) Determination of REE, Ba, Fe, Mg, Na and K in carbonaceous and ordinary chondrites. Geochimica et Cosmochimica Acta, 38, 757-775.",
                Type = "REE",
                Values = new Dictionary<string, double>
                {
                    { "La", 0.329 },
                    { "Ce", 0.865 },
                    { "Pr", 0.112 },
                    { "Nd", 0.630 },
                    { "Sm", 0.203 },
                    { "Eu", 0.0770 },
                    { "Gd", 0.276 },
                    { "Tb", 0.0470 },
                    { "Dy", 0.343 },
                    { "Ho", 0.0757 },
                    { "Er", 0.225 },
                    { "Tm", 0.0326 },
                    { "Yb", 0.220 },
                    { "Lu", 0.0339 }
                }
            };
        }

        /// <summary>
        /// Taylor & McLennan (1985) 球粒陨石
        /// </summary>
        public static NormalizationStandard ChondriteTaylorMcLennan1985()
        {
            return new NormalizationStandard
            {
                Name = "Chondrite (Taylor & McLennan, 1985)",
                ShortName = "C1 Taylor & McLennan 1985",
                Reference = "Taylor S.R. and McLennan S.M. (1985) The Continental Crust: Its Composition and Evolution. Blackwell, Oxford.",
                Type = "REE",
                Values = new Dictionary<string, double>
                {
                    { "La", 0.367 },
                    { "Ce", 0.957 },
                    { "Pr", 0.137 },
                    { "Nd", 0.711 },
                    { "Sm", 0.231 },
                    { "Eu", 0.087 },
                    { "Gd", 0.306 },
                    { "Tb", 0.058 },
                    { "Dy", 0.381 },
                    { "Ho", 0.0851 },
                    { "Er", 0.249 },
                    { "Tm", 0.0356 },
                    { "Yb", 0.248 },
                    { "Lu", 0.0381 }
                }
            };
        }

        #endregion

        #region Trace Element Standards

        /// <summary>
        /// Sun & McDonough (1989) 原始地幔
        /// </summary>
        public static NormalizationStandard PrimitiveMantleSunMcDonough1989()
        {
            return new NormalizationStandard
            {
                Name = "Primitive Mantle (Sun & McDonough, 1989)",
                ShortName = "PM Sun & McDonough 1989",
                Reference = "Sun S.-s. and McDonough W.F. (1989) Chemical and isotopic systematics of oceanic basalts: implications for mantle composition and processes. In: Saunders A.D. and Norry M.J. (eds.) Magmatism in the Ocean Basins. Geological Society, London, Special Publications, 42, 313-345.",
                Type = "TraceElement",
                Values = new Dictionary<string, double>
                {
                    { "Cs", 0.0210 },
                    { "Rb", 0.600 },
                    { "Ba", 6.600 },
                    { "Th", 0.0795 },
                    { "U", 0.0203 },
                    { "Nb", 0.658 },
                    { "Ta", 0.037 },
                    { "K", 240.0 },
                    { "La", 0.648 },
                    { "Ce", 1.675 },
                    { "Pb", 0.150 },
                    { "Pr", 0.254 },
                    { "Sr", 19.9 },
                    { "Nd", 1.250 },
                    { "Zr", 10.5 },
                    { "Hf", 0.283 },
                    { "Sm", 0.406 },
                    { "Eu", 0.154 },
                    { "Ti", 1205.0 },
                    { "Gd", 0.544 },
                    { "Tb", 0.099 },
                    { "Dy", 0.674 },
                    { "Ho", 0.149 },
                    { "Er", 0.438 },
                    { "Tm", 0.068 },
                    { "Yb", 0.441 },
                    { "Lu", 0.0675 },
                    { "Y", 4.30 }
                }
            };
        }

        /// <summary>
        /// McDonough & Sun (1995) 原始地幔
        /// </summary>
        public static NormalizationStandard PrimitiveMantleMcDonoughSun1995()
        {
            return new NormalizationStandard
            {
                Name = "Primitive Mantle (McDonough & Sun, 1995)",
                ShortName = "PM McDonough & Sun 1995",
                Reference = "McDonough W.F. and Sun S.-s. (1995) The composition of the Earth. Chemical Geology, 120, 223-253.",
                Type = "TraceElement",
                Values = new Dictionary<string, double>
                {
                    { "Cs", 0.0210 },
                    { "Rb", 0.600 },
                    { "Ba", 6.600 },
                    { "Th", 0.0795 },
                    { "U", 0.0203 },
                    { "Nb", 0.658 },
                    { "Ta", 0.037 },
                    { "K", 240.0 },
                    { "La", 0.648 },
                    { "Ce", 1.675 },
                    { "Pb", 0.150 },
                    { "Pr", 0.254 },
                    { "Sr", 19.9 },
                    { "Nd", 1.250 },
                    { "Zr", 10.5 },
                    { "Hf", 0.283 },
                    { "Sm", 0.406 },
                    { "Eu", 0.154 },
                    { "Ti", 1205.0 },
                    { "Gd", 0.544 },
                    { "Tb", 0.099 },
                    { "Dy", 0.674 },
                    { "Ho", 0.149 },
                    { "Er", 0.438 },
                    { "Tm", 0.068 },
                    { "Yb", 0.441 },
                    { "Lu", 0.0675 },
                    { "Y", 4.30 }
                }
            };
        }

        /// <summary>
        /// Sun & McDonough (1989) N-MORB
        /// </summary>
        public static NormalizationStandard MORBSunMcDonough1989()
        {
            return new NormalizationStandard
            {
                Name = "N-MORB (Sun & McDonough, 1989)",
                ShortName = "N-MORB Sun & McDonough 1989",
                Reference = "Sun S.-s. and McDonough W.F. (1989) Chemical and isotopic systematics of oceanic basalts.",
                Type = "TraceElement",
                Values = new Dictionary<string, double>
                {
                    { "Cs", 0.007 },
                    { "Rb", 0.56 },
                    { "Ba", 6.30 },
                    { "Th", 0.12 },
                    { "U", 0.047 },
                    { "Nb", 2.33 },
                    { "Ta", 0.132 },
                    { "K", 600.0 },
                    { "La", 2.50 },
                    { "Ce", 7.50 },
                    { "Pb", 0.30 },
                    { "Pr", 1.32 },
                    { "Sr", 90.0 },
                    { "Nd", 7.30 },
                    { "Zr", 74.0 },
                    { "Hf", 2.05 },
                    { "Sm", 2.63 },
                    { "Eu", 1.02 },
                    { "Ti", 7600.0 },
                    { "Gd", 3.68 },
                    { "Tb", 0.67 },
                    { "Dy", 4.55 },
                    { "Ho", 1.01 },
                    { "Er", 2.97 },
                    { "Tm", 0.456 },
                    { "Yb", 3.05 },
                    { "Lu", 0.455 },
                    { "Y", 28.0 }
                }
            };
        }

        /// <summary>
        /// Pearce (1983) N-MORB
        /// </summary>
        public static NormalizationStandard NMORBPearce1983()
        {
            return new NormalizationStandard
            {
                Name = "N-MORB (Pearce, 1983)",
                ShortName = "N-MORB Pearce 1983",
                Reference = "Pearce J.A. (1983) Role of the sub-continental lithosphere in magma genesis at active continental margins.",
                Type = "TraceElement",
                Values = new Dictionary<string, double>
                {
                    { "Cs", 0.018 },
                    { "Rb", 2.0 },
                    { "Ba", 20.0 },
                    { "Th", 0.20 },
                    { "U", 0.10 },
                    { "Nb", 3.5 },
                    { "Ta", 0.18 },
                    { "K", 1500.0 },
                    { "La", 3.40 },
                    { "Ce", 10.0 },
                    { "Pb", 0.60 },
                    { "Pr", 1.70 },
                    { "Sr", 120.0 },
                    { "Nd", 8.0 },
                    { "Zr", 90.0 },
                    { "Hf", 2.40 },
                    { "Sm", 3.30 },
                    { "Eu", 1.20 },
                    { "Ti", 9000.0 },
                    { "Gd", 4.50 },
                    { "Tb", 0.79 },
                    { "Dy", 5.20 },
                    { "Ho", 1.10 },
                    { "Er", 3.40 },
                    { "Tm", 0.50 },
                    { "Yb", 3.40 },
                    { "Lu", 0.50 },
                    { "Y", 30.0 }
                }
            };
        }

        #endregion
    }
}
