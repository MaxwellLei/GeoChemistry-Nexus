using System;
using System.Collections.Generic;
using System.Linq;

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
        /// 获取所有可用的 REE 标准化方案（按分组顺序，组内按年份升序）
        /// </summary>
        public static List<NormalizationStandard> GetReeStandards()
        {
            return SortStandards(new List<NormalizationStandard>
            {
                Configure(ChondriteNakamura1977(), NormalizationCategories.Chondrite, 1977),
                Configure(ChondriteTaylorMcLennan1985(), NormalizationCategories.Chondrite, 1985),
                Configure(ChondriteSunMcDonough1989(), NormalizationCategories.Chondrite, 1989),
                Configure(ChondriteSunMcDonough1995(), NormalizationCategories.Chondrite, 1995, isRecommended: true),
                Configure(ChondritePalmeONeill2014(), NormalizationCategories.Chondrite, 2014),
                Configure(ChondriteONeill2016(), NormalizationCategories.Chondrite, 2016),
                Configure(NascGromet1984(), NormalizationCategories.Shale, 1984),
                Configure(PaasTaylorMcLennan1985(), NormalizationCategories.Shale, 1985),
                Configure(PaasPourmand2012(), NormalizationCategories.Shale, 2012),
                Configure(EuropeanShaleBau2018(), NormalizationCategories.Shale, 2018),
                Configure(UpperContinentalCrustMcLennan2001(), NormalizationCategories.Crust, 2001),
                Configure(UpperContinentalCrustRudnickGao2003(), NormalizationCategories.Crust, 2003),
                Configure(BulkContinentalCrustRudnickGao2003(), NormalizationCategories.Crust, 2003),
                Configure(LowerContinentalCrustRudnickGao2003(), NormalizationCategories.Crust, 2003),
                Configure(PrimitiveMantleReeSunMcDonough1989(), NormalizationCategories.Mantle, 1989),
                Configure(PrimitiveMantleReeMcDonoughSun1995(), NormalizationCategories.Mantle, 1995),
                Configure(NMorbReeSunMcDonough1989(), NormalizationCategories.Basalt, 1989),
                Configure(EMorbReeSunMcDonough1989(), NormalizationCategories.Basalt, 1989),
                Configure(OibReeSunMcDonough1989(), NormalizationCategories.Basalt, 1989)
            });
        }

        /// <summary>
        /// 获取所有可用的微量元素标准化方案（按分组顺序，组内按年份升序）
        /// </summary>
        public static List<NormalizationStandard> GetTraceElementStandards()
        {
            return SortStandards(new List<NormalizationStandard>
            {
                Configure(ChondriteTraceSunMcDonough1989(), NormalizationCategories.Chondrite, 1989),
                Configure(UpperContinentalCrustTraceRudnickGao2003(), NormalizationCategories.Crust, 2003),
                Configure(PrimitiveMantleSunMcDonough1989(), NormalizationCategories.Mantle, 1989, isRecommended: true),
                Configure(PrimitiveMantleMcDonoughSun1995(), NormalizationCategories.Mantle, 1995),
                Configure(NMORBPearce1983(), NormalizationCategories.Basalt, 1983),
                Configure(MORBSunMcDonough1989(), NormalizationCategories.Basalt, 1989),
                Configure(EMorbSunMcDonough1989(), NormalizationCategories.Basalt, 1989),
                Configure(OibSunMcDonough1989(), NormalizationCategories.Basalt, 1989)
            });
        }

        /// <summary>
        /// 获取指定图类型的推荐默认标准化方案
        /// </summary>
        public static NormalizationStandard GetRecommendedStandard(string diagramType)
        {
            var standards = diagramType == "REE"
                ? GetReeStandards()
                : GetTraceElementStandards();

            return standards.FirstOrDefault(s => s.IsRecommended)
                   ?? standards.FirstOrDefault()
                   ?? throw new InvalidOperationException($"No normalization standards available for {diagramType}.");
        }

        private static NormalizationStandard Configure(
            NormalizationStandard standard,
            string categoryKey,
            int year,
            bool isRecommended = false)
        {
            standard.CategoryKey = categoryKey;
            standard.Year = year;
            standard.IsRecommended = isRecommended;
            // 默认英文回退；UI 层会再写入本地化 Category
            standard.Category = GetCategoryFallback(categoryKey);
            return standard;
        }

        private static List<NormalizationStandard> SortStandards(List<NormalizationStandard> standards)
        {
            return standards
                .OrderBy(s =>
                {
                    int index = Array.IndexOf(NormalizationCategories.DisplayOrder, s.CategoryKey);
                    return index < 0 ? int.MaxValue : index;
                })
                .ThenBy(s => s.Year)
                .ThenBy(s => s.Name, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// 分组标题英文回退（本地化失败时使用）
        /// </summary>
        public static string GetCategoryFallback(string categoryKey)
        {
            return categoryKey switch
            {
                NormalizationCategories.Chondrite => "Chondrite",
                NormalizationCategories.Shale => "Shale / Sedimentary",
                NormalizationCategories.Crust => "Continental Crust",
                NormalizationCategories.Mantle => "Primitive Mantle",
                NormalizationCategories.Basalt => "Basalt End-members",
                _ => categoryKey
            };
        }

        #region REE Standards

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
        /// Nakamura (1977) 球粒陨石
        /// </summary>
        public static NormalizationStandard ChondriteNakamura1977()
        {
            return new NormalizationStandard
            {
                Name = "Chondrite (Nakamura, 1977)",
                ShortName = "C1 Nakamura 1977",
                Reference = "Nakamura N. (1974) Determination of REE, Ba, Fe, Mg, Na and K in carbonaceous and ordinary chondrites. Geochimica et Cosmochimica Acta, 38, 757-775.",
                Type = "REE",
                Values = new Dictionary<string, double>
                {
                    { "La", 0.33 },
                    { "Ce", 0.865 },
                    { "Pr", 0.112 },
                    { "Nd", 0.63 },
                    { "Sm", 0.203 },
                    { "Eu", 0.077 },
                    { "Gd", 0.276 },
                    { "Tb", 0.047 },
                    { "Dy", 0.343 },
                    { "Ho", 0.07 },
                    { "Er", 0.225 },
                    { "Tm", 0.03 },
                    { "Yb", 0.22 },
                    { "Lu", 0.034 }
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

        /// <summary>
        /// Sun & McDonough (1989) 球粒陨石
        /// </summary>
        public static NormalizationStandard ChondriteSunMcDonough1989()
        {
            return new NormalizationStandard
            {
                Name = "Chondrite (Sun & McDonough, 1989)",
                ShortName = "C1 Sun & McDonough 1989",
                Reference = "Sun S.-s. and McDonough W.F. (1989) Chemical and isotopic systematics of oceanic basalts: implications for mantle composition and processes. In: Saunders A.D. and Norry M.J. (eds.) Magmatism in the Ocean Basins. Geological Society, London, Special Publications, 42, 313-345.",
                Type = "REE",
                Values = new Dictionary<string, double>
                {
                    { "La", 0.237 },
                    { "Ce", 0.612 },
                    { "Pr", 0.095 },
                    { "Nd", 0.467 },
                    { "Sm", 0.153 },
                    { "Eu", 0.058 },
                    { "Gd", 0.2055 },
                    { "Tb", 0.0374 },
                    { "Dy", 0.254 },
                    { "Ho", 0.0566 },
                    { "Er", 0.1655 },
                    { "Tm", 0.0255 },
                    { "Yb", 0.17 },
                    { "Lu", 0.0254 }
                }
            };
        }

        /// <summary>
        /// Palme & O'Neill (2014) 球粒陨石
        /// </summary>
        public static NormalizationStandard ChondritePalmeONeill2014()
        {
            return new NormalizationStandard
            {
                Name = "Chondrite (Palme & O'Neill, 2014)",
                ShortName = "C1 Palme & O'Neill 2014",
                Reference = "Palme H. and O'Neill H.St.C. (2014) Cosmochemical estimates of mantle composition. In: Holland H.D. and Turekian K.K. (eds.) Treatise on Geochemistry (Second Edition), 3, 1-39.",
                Type = "REE",
                Values = new Dictionary<string, double>
                {
                    { "La", 0.2414 },
                    { "Ce", 0.6194 },
                    { "Pr", 0.0939 },
                    { "Nd", 0.4737 },
                    { "Sm", 0.1536 },
                    { "Eu", 0.05883 },
                    { "Gd", 0.2069 },
                    { "Tb", 0.03797 },
                    { "Dy", 0.2558 },
                    { "Ho", 0.05644 },
                    { "Er", 0.1655 },
                    { "Tm", 0.02609 },
                    { "Yb", 0.1687 },
                    { "Lu", 0.02503 }
                }
            };
        }

        /// <summary>
        /// O'Neill (2016) 球粒陨石
        /// </summary>
        public static NormalizationStandard ChondriteONeill2016()
        {
            return new NormalizationStandard
            {
                Name = "Chondrite (O'Neill, 2016)",
                ShortName = "C1 O'Neill 2016",
                Reference = "O'Neill H.S.C. (2016) The smoothness and shapes of chondrite-normalized rare earth element patterns in basalts. Journal of Petrology, 57, 1463-1508.",
                Type = "REE",
                Values = new Dictionary<string, double>
                {
                    { "La", 0.2472 },
                    { "Ce", 0.6308 },
                    { "Pr", 0.095 },
                    { "Nd", 0.4793 },
                    { "Sm", 0.15419 },
                    { "Eu", 0.0592 },
                    { "Gd", 0.2059 },
                    { "Tb", 0.0375 },
                    { "Dy", 0.254 },
                    { "Ho", 0.0554 },
                    { "Er", 0.1645 },
                    { "Tm", 0.0258 },
                    { "Yb", 0.1684 },
                    { "Lu", 0.0251 }
                }
            };
        }

        /// <summary>
        /// Taylor & McLennan (1985) PAAS
        /// </summary>
        public static NormalizationStandard PaasTaylorMcLennan1985()
        {
            return new NormalizationStandard
            {
                Name = "PAAS (Taylor & McLennan, 1985)",
                ShortName = "PAAS Taylor & McLennan 1985",
                Reference = "Taylor S.R. and McLennan S.M. (1985) The Continental Crust: Its Composition and Evolution. Blackwell Scientific Publications, Oxford.",
                Type = "REE",
                Values = ReeValues(
                    38.2, 80, 8.9, 32, 5.6, 1.1, 4.7, 0.77, 4.4, 1.0, 2.9, 0.4, 2.8, 0.43)
            };
        }

        /// <summary>
        /// Pourmand et al. (2012) PAAS
        /// </summary>
        public static NormalizationStandard PaasPourmand2012()
        {
            return new NormalizationStandard
            {
                Name = "PAAS (Pourmand et al., 2012)",
                ShortName = "PAAS Pourmand 2012",
                Reference = "Pourmand A., Dauphas N. and Ireland T.J. (2012) A novel extraction chromatography and MC-ICP-MS technique for rapid analysis of REE, Sc and Y: revising CI-chondrite and Post-Archean Australian Shale (PAAS) abundances. Chemical Geology, 291, 38-54.",
                Type = "REE",
                Values = ReeValues(
                    44.56, 88.25, 10.15, 37.32, 6.884, 1.215, 6.043, 0.8914, 5.325, 1.053, 3.075, 0.451, 3.012, 0.4386)
            };
        }

        /// <summary>
        /// Gromet et al. (1984) NASC
        /// </summary>
        public static NormalizationStandard NascGromet1984()
        {
            return new NormalizationStandard
            {
                Name = "NASC (Gromet et al., 1984)",
                ShortName = "NASC Gromet 1984",
                Reference = "Gromet L.P., Haskin L.A., Korotev R.L. and Dymek R.F. (1984) The \"North American shale composite\": its compilation, major and trace element characteristics. Geochimica et Cosmochimica Acta, 48, 2469-2482.",
                Type = "REE",
                Values = ReeValues(
                    32, 67, 7.9, 27.4, 5.6, 1.2, 5.2, 0.85, 5.8, 1.04, 3.4, 0.5, 3.1, 0.46)
            };
        }

        /// <summary>
        /// Bau et al. (2018) 欧洲页岩
        /// </summary>
        public static NormalizationStandard EuropeanShaleBau2018()
        {
            return new NormalizationStandard
            {
                Name = "European Shale (Bau et al., 2018)",
                ShortName = "EUS Bau 2018",
                Reference = "Bau M., Schmidt K., Pack A., Bendel V. and Kraemer D. (2018) The European Shale: an improved data set for normalisation of rare earth element and yttrium concentrations in environmental and biological samples from Europe. Applied Geochemistry, 90, 142-149.",
                Type = "REE",
                Values = ReeValues(
                    44.3, 88.5, 10.6, 39.5, 7.3, 1.48, 6.34, 0.944, 5.86, 1.17, 3.43, 0.492, 3.26, 0.485)
            };
        }

        /// <summary>
        /// McLennan (2001) 上地壳
        /// </summary>
        public static NormalizationStandard UpperContinentalCrustMcLennan2001()
        {
            return new NormalizationStandard
            {
                Name = "UCC (McLennan, 2001)",
                ShortName = "UCC McLennan 2001",
                Reference = "McLennan S.M. (2001) Relationships between the trace element composition of sedimentary rocks and upper continental crust. Geochemistry, Geophysics, Geosystems, 2.",
                Type = "REE",
                Values = ReeValues(
                    30, 64, 7.1, 26, 4.5, 0.88, 3.8, 0.64, 3.5, 0.8, 2.3, 0.33, 2.2, 0.32)
            };
        }

        /// <summary>
        /// Rudnick & Gao (2003) 上地壳
        /// </summary>
        public static NormalizationStandard UpperContinentalCrustRudnickGao2003()
        {
            return new NormalizationStandard
            {
                Name = "UCC (Rudnick & Gao, 2003)",
                ShortName = "UCC Rudnick & Gao 2003",
                Reference = "Rudnick R.L. and Gao S. (2003) Composition of the continental crust. In: Holland H.D. and Turekian K.K. (eds.) Treatise on Geochemistry, 3, 1-64.",
                Type = "REE",
                Values = ReeValues(
                    31, 63, 7.1, 27, 4.7, 1.0, 4.0, 0.7, 3.9, 0.83, 2.3, 0.3, 1.96, 0.31)
            };
        }

        /// <summary>
        /// Rudnick & Gao (2003) 全地壳
        /// </summary>
        public static NormalizationStandard BulkContinentalCrustRudnickGao2003()
        {
            return new NormalizationStandard
            {
                Name = "BCC (Rudnick & Gao, 2003)",
                ShortName = "BCC Rudnick & Gao 2003",
                Reference = "Rudnick R.L. and Gao S. (2003) Composition of the continental crust. In: Holland H.D. and Turekian K.K. (eds.) Treatise on Geochemistry, 3, 1-64.",
                Type = "REE",
                Values = ReeValues(
                    20, 43, 4.9, 20, 3.9, 1.1, 3.7, 0.6, 3.6, 0.77, 2.1, 0.28, 1.9, 0.3)
            };
        }

        /// <summary>
        /// Rudnick & Gao (2003) 下地壳
        /// </summary>
        public static NormalizationStandard LowerContinentalCrustRudnickGao2003()
        {
            return new NormalizationStandard
            {
                Name = "LCC (Rudnick & Gao, 2003)",
                ShortName = "LCC Rudnick & Gao 2003",
                Reference = "Rudnick R.L. and Gao S. (2003) Composition of the continental crust. In: Holland H.D. and Turekian K.K. (eds.) Treatise on Geochemistry, 3, 1-64.",
                Type = "REE",
                Values = ReeValues(
                    8, 20, 2.4, 11, 2.8, 1.1, 3.1, 0.48, 3.1, 0.68, 1.9, 0.24, 1.5, 0.25)
            };
        }

        /// <summary>
        /// Sun & McDonough (1989) 原始地幔 REE
        /// </summary>
        public static NormalizationStandard PrimitiveMantleReeSunMcDonough1989()
        {
            return new NormalizationStandard
            {
                Name = "Primitive Mantle (Sun & McDonough, 1989)",
                ShortName = "PM Sun & McDonough 1989",
                Reference = "Sun S.-s. and McDonough W.F. (1989) Chemical and isotopic systematics of oceanic basalts: implications for mantle composition and processes. In: Saunders A.D. and Norry M.J. (eds.) Magmatism in the Ocean Basins. Geological Society, London, Special Publications, 42, 313-345.",
                Type = "REE",
                Values = ReeValues(
                    0.648, 1.675, 0.254, 1.25, 0.406, 0.154, 0.544, 0.099, 0.674, 0.149, 0.438, 0.068, 0.441, 0.0675)
            };
        }

        /// <summary>
        /// McDonough & Sun (1995) 原始地幔 REE
        /// </summary>
        public static NormalizationStandard PrimitiveMantleReeMcDonoughSun1995()
        {
            return new NormalizationStandard
            {
                Name = "Primitive Mantle (McDonough & Sun, 1995)",
                ShortName = "PM McDonough & Sun 1995",
                Reference = "McDonough W.F. and Sun S.-s. (1995) The composition of the Earth. Chemical Geology, 120, 223-253.",
                Type = "REE",
                Values = ReeValues(
                    0.687, 1.775, 0.276, 1.354, 0.444, 0.168, 0.596, 0.108, 0.737, 0.164, 0.48, 0.074, 0.493, 0.074)
            };
        }

        /// <summary>
        /// Sun & McDonough (1989) N-MORB REE
        /// </summary>
        public static NormalizationStandard NMorbReeSunMcDonough1989()
        {
            return new NormalizationStandard
            {
                Name = "N-MORB (Sun & McDonough, 1989)",
                ShortName = "N-MORB Sun & McDonough 1989",
                Reference = "Sun S.-s. and McDonough W.F. (1989) Chemical and isotopic systematics of oceanic basalts: implications for mantle composition and processes. In: Saunders A.D. and Norry M.J. (eds.) Magmatism in the Ocean Basins. Geological Society, London, Special Publications, 42, 313-345.",
                Type = "REE",
                Values = ReeValues(
                    2.5, 7.5, 1.32, 7.3, 2.63, 1.02, 3.68, 0.67, 4.55, 1.01, 2.97, 0.456, 3.05, 0.455)
            };
        }

        /// <summary>
        /// Sun & McDonough (1989) E-MORB REE
        /// </summary>
        public static NormalizationStandard EMorbReeSunMcDonough1989()
        {
            return new NormalizationStandard
            {
                Name = "E-MORB (Sun & McDonough, 1989)",
                ShortName = "E-MORB Sun & McDonough 1989",
                Reference = "Sun S.-s. and McDonough W.F. (1989) Chemical and isotopic systematics of oceanic basalts: implications for mantle composition and processes. In: Saunders A.D. and Norry M.J. (eds.) Magmatism in the Ocean Basins. Geological Society, London, Special Publications, 42, 313-345.",
                Type = "REE",
                Values = ReeValues(
                    6.3, 15, 2.05, 9, 2.6, 0.91, 2.97, 0.53, 3.55, 0.79, 2.31, 0.356, 2.37, 0.354)
            };
        }

        /// <summary>
        /// Sun & McDonough (1989) OIB REE
        /// </summary>
        public static NormalizationStandard OibReeSunMcDonough1989()
        {
            return new NormalizationStandard
            {
                Name = "OIB (Sun & McDonough, 1989)",
                ShortName = "OIB Sun & McDonough 1989",
                Reference = "Sun S.-s. and McDonough W.F. (1989) Chemical and isotopic systematics of oceanic basalts: implications for mantle composition and processes. In: Saunders A.D. and Norry M.J. (eds.) Magmatism in the Ocean Basins. Geological Society, London, Special Publications, 42, 313-345.",
                Type = "REE",
                Values = ReeValues(
                    37, 80, 9.7, 38.5, 10, 3, 7.62, 1.05, 5.6, 1.06, 2.62, 0.35, 2.16, 0.3)
            };
        }

        /// <summary>
        /// 按 La-Lu 顺序构建 REE 标准化值字典
        /// </summary>
        private static Dictionary<string, double> ReeValues(
            double la, double ce, double pr, double nd, double sm, double eu, double gd,
            double tb, double dy, double ho, double er, double tm, double yb, double lu)
        {
            return new Dictionary<string, double>
            {
                { "La", la },
                { "Ce", ce },
                { "Pr", pr },
                { "Nd", nd },
                { "Sm", sm },
                { "Eu", eu },
                { "Gd", gd },
                { "Tb", tb },
                { "Dy", dy },
                { "Ho", ho },
                { "Er", er },
                { "Tm", tm },
                { "Yb", yb },
                { "Lu", lu }
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
                    { "Cs", 0.032 },
                    { "Rb", 0.635 },
                    { "Ba", 6.989 },
                    { "Th", 0.085 },
                    { "U", 0.021 },
                    { "Nb", 0.713 },
                    { "Ta", 0.041 },
                    { "K", 250.0 },
                    { "La", 0.687 },
                    { "Ce", 1.775 },
                    { "Pb", 0.185 },
                    { "Pr", 0.276 },
                    { "Sr", 21.1 },
                    { "Nd", 1.354 },
                    { "Zr", 11.2 },
                    { "Hf", 0.309 },
                    { "Sm", 0.444 },
                    { "Eu", 0.168 },
                    { "Ti", 1300.0 },
                    { "Gd", 0.596 },
                    { "Tb", 0.108 },
                    { "Dy", 0.737 },
                    { "Ho", 0.164 },
                    { "Er", 0.480 },
                    { "Tm", 0.074 },
                    { "Yb", 0.493 },
                    { "Lu", 0.074 },
                    { "Y", 4.55 }
                }
            };
        }

        /// <summary>
        /// Sun & McDonough (1989) 球粒陨石（微量元素）
        /// </summary>
        public static NormalizationStandard ChondriteTraceSunMcDonough1989()
        {
            return new NormalizationStandard
            {
                Name = "Chondrite (Sun & McDonough, 1989)",
                ShortName = "C1 Sun & McDonough 1989",
                Reference = "Sun S.-s. and McDonough W.F. (1989) Chemical and isotopic systematics of oceanic basalts: implications for mantle composition and processes. In: Saunders A.D. and Norry M.J. (eds.) Magmatism in the Ocean Basins. Geological Society, London, Special Publications, 42, 313-345.",
                Type = "TraceElement",
                Values = new Dictionary<string, double>
                {
                    { "Cs", 0.188 },
                    { "Rb", 2.32 },
                    { "Ba", 2.41 },
                    { "Th", 0.029 },
                    { "U", 0.008 },
                    { "Nb", 0.246 },
                    { "Ta", 0.014 },
                    { "K", 545.0 },
                    { "La", 0.237 },
                    { "Ce", 0.612 },
                    { "Pb", 2.47 },
                    { "Pr", 0.095 },
                    { "Sr", 7.26 },
                    { "Nd", 0.467 },
                    { "Zr", 3.87 },
                    { "Hf", 0.1066 },
                    { "Sm", 0.153 },
                    { "Eu", 0.058 },
                    { "Ti", 445.0 },
                    { "Gd", 0.2055 },
                    { "Tb", 0.0374 },
                    { "Dy", 0.254 },
                    { "Ho", 0.0566 },
                    { "Er", 0.1655 },
                    { "Tm", 0.0255 },
                    { "Yb", 0.17 },
                    { "Lu", 0.0254 },
                    { "Y", 1.57 }
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
        /// Sun & McDonough (1989) E-MORB
        /// </summary>
        public static NormalizationStandard EMorbSunMcDonough1989()
        {
            return new NormalizationStandard
            {
                Name = "E-MORB (Sun & McDonough, 1989)",
                ShortName = "E-MORB Sun & McDonough 1989",
                Reference = "Sun S.-s. and McDonough W.F. (1989) Chemical and isotopic systematics of oceanic basalts: implications for mantle composition and processes. In: Saunders A.D. and Norry M.J. (eds.) Magmatism in the Ocean Basins. Geological Society, London, Special Publications, 42, 313-345.",
                Type = "TraceElement",
                Values = new Dictionary<string, double>
                {
                    { "Cs", 0.063 },
                    { "Rb", 5.04 },
                    { "Ba", 57.0 },
                    { "Th", 0.6 },
                    { "U", 0.18 },
                    { "Nb", 8.3 },
                    { "Ta", 0.47 },
                    { "K", 2100.0 },
                    { "La", 6.3 },
                    { "Ce", 15.0 },
                    { "Pb", 0.6 },
                    { "Pr", 2.05 },
                    { "Sr", 155.0 },
                    { "Nd", 9.0 },
                    { "Zr", 73.0 },
                    { "Hf", 2.03 },
                    { "Sm", 2.6 },
                    { "Eu", 0.91 },
                    { "Ti", 6000.0 },
                    { "Gd", 2.97 },
                    { "Tb", 0.53 },
                    { "Dy", 3.55 },
                    { "Ho", 0.79 },
                    { "Er", 2.31 },
                    { "Tm", 0.356 },
                    { "Yb", 2.37 },
                    { "Lu", 0.354 },
                    { "Y", 22.0 }
                }
            };
        }

        /// <summary>
        /// Sun & McDonough (1989) OIB
        /// </summary>
        public static NormalizationStandard OibSunMcDonough1989()
        {
            return new NormalizationStandard
            {
                Name = "OIB (Sun & McDonough, 1989)",
                ShortName = "OIB Sun & McDonough 1989",
                Reference = "Sun S.-s. and McDonough W.F. (1989) Chemical and isotopic systematics of oceanic basalts: implications for mantle composition and processes. In: Saunders A.D. and Norry M.J. (eds.) Magmatism in the Ocean Basins. Geological Society, London, Special Publications, 42, 313-345.",
                Type = "TraceElement",
                Values = new Dictionary<string, double>
                {
                    { "Cs", 0.387 },
                    { "Rb", 31.0 },
                    { "Ba", 350.0 },
                    { "Th", 4.0 },
                    { "U", 1.02 },
                    { "Nb", 48.0 },
                    { "Ta", 2.7 },
                    { "K", 12000.0 },
                    { "La", 37.0 },
                    { "Ce", 80.0 },
                    { "Pb", 3.2 },
                    { "Pr", 9.7 },
                    { "Sr", 660.0 },
                    { "Nd", 38.5 },
                    { "Zr", 280.0 },
                    { "Hf", 7.8 },
                    { "Sm", 10.0 },
                    { "Eu", 3.0 },
                    { "Ti", 17200.0 },
                    { "Gd", 7.62 },
                    { "Tb", 1.05 },
                    { "Dy", 5.6 },
                    { "Ho", 1.06 },
                    { "Er", 2.62 },
                    { "Tm", 0.35 },
                    { "Yb", 2.16 },
                    { "Lu", 0.3 },
                    { "Y", 29.0 }
                }
            };
        }

        /// <summary>
        /// Rudnick & Gao (2003) 上地壳（微量元素）
        /// </summary>
        public static NormalizationStandard UpperContinentalCrustTraceRudnickGao2003()
        {
            return new NormalizationStandard
            {
                Name = "UCC (Rudnick & Gao, 2003)",
                ShortName = "UCC Rudnick & Gao 2003",
                Reference = "Rudnick R.L. and Gao S. (2003) Composition of the continental crust. In: Holland H.D. and Turekian K.K. (eds.) Treatise on Geochemistry, 3, 1-64.",
                Type = "TraceElement",
                Values = new Dictionary<string, double>
                {
                    { "Cs", 4.9 },
                    { "Rb", 84.0 },
                    { "Ba", 628.0 },
                    { "Th", 10.5 },
                    { "U", 2.7 },
                    { "Nb", 12.0 },
                    { "Ta", 0.9 },
                    { "La", 31.0 },
                    { "Ce", 63.0 },
                    { "Pb", 17.0 },
                    { "Pr", 7.1 },
                    { "Sr", 320.0 },
                    { "Nd", 27.0 },
                    { "Zr", 193.0 },
                    { "Hf", 5.3 },
                    { "Sm", 4.7 },
                    { "Eu", 1.0 },
                    { "Gd", 4.0 },
                    { "Tb", 0.7 },
                    { "Dy", 3.9 },
                    { "Ho", 0.83 },
                    { "Er", 2.3 },
                    { "Tm", 0.3 },
                    { "Yb", 1.96 },
                    { "Lu", 0.31 },
                    { "Y", 21.0 }
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
