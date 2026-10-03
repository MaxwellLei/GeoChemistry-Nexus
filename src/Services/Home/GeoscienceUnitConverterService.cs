using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GeoChemistryNexus.Services
{
    public enum GeoUnitCategory
    {
        Grade,          // 品位 / 浓度
        Pressure,       // 压力 / 应力
        Scale,          // 微区尺度
        Temperature,    // 温度 / 地温
        Time            // 同位素年代
    }

    public sealed class GeoUnitDefinition
    {
        public string Id { get; init; } = string.Empty;
        public GeoUnitCategory Category { get; init; }
        public string Symbol { get; init; } = string.Empty;
        public string NameKey { get; init; } = string.Empty;
        public string DefaultName { get; init; } = string.Empty;
        public string DescKey { get; init; } = string.Empty;
        public string DefaultDesc { get; init; } = string.Empty;

        public string Name => LanguageService.Instance[NameKey] is { Length: > 0 } n ? n : DefaultName;
        public string Description => LanguageService.Instance[DescKey] is { Length: > 0 } d ? d : DefaultDesc;

        // 换算至基准标准单位
        public Func<double, double> ToStandard { get; init; } = v => v;
        // 从基准标准单位换算至当前单位
        public Func<double, double> FromStandard { get; init; } = v => v;

        public string DisplayLabel => $"{Symbol}  ({Name})";
    }

    public sealed class GeoPresetItem
    {
        public GeoUnitCategory Category { get; init; }
        public string TitleKey { get; init; } = string.Empty;
        public string DefaultTitle { get; init; } = string.Empty;
        public double Value { get; init; }
        public string UnitSymbol { get; init; } = string.Empty;
        public string NoteKey { get; init; } = string.Empty;
        public string DefaultNote { get; init; } = string.Empty;

        public string Title => LanguageService.Instance[TitleKey] is { Length: > 0 } t ? t : DefaultTitle;
        public string ContextNote => LanguageService.Instance[NoteKey] is { Length: > 0 } n ? n : DefaultNote;

        public string DisplayBadge => $"{Title}: {Value} {UnitSymbol}";
    }

    public static class GeoscienceUnitConverterService
    {
        // ─────────────────────────── 单位定义库 ───────────────────────────
        public static IReadOnlyList<GeoUnitDefinition> AllUnits { get; } = BuildUnits();

        public static IReadOnlyList<GeoPresetItem> AllPresets { get; } = BuildPresets();

        public static IReadOnlyList<GeoUnitDefinition> GetUnits(GeoUnitCategory category) =>
            AllUnits.Where(u => u.Category == category).ToList();

        public static IReadOnlyList<GeoPresetItem> GetPresets(GeoUnitCategory category) =>
            AllPresets.Where(p => p.Category == category).ToList();

        public static double Convert(double value, GeoUnitDefinition fromUnit, GeoUnitDefinition toUnit)
        {
            if (fromUnit == null || toUnit == null) return value;
            if (fromUnit.Category != toUnit.Category) return double.NaN;
            if (fromUnit.Id == toUnit.Id) return value;

            double standardValue = fromUnit.ToStandard(value);
            return toUnit.FromStandard(standardValue);
        }

        /// <summary>
        /// 自适应智能格式化：
        /// - 常规数值：去除无效尾零，输出简洁浮点；
        /// - 极小值（|x| &lt; 1e-4 且非 0）或超大值（|x| &gt;= 1e7）：自适应科学计数法。
        /// </summary>
        public static string FormatSmart(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "-";

            if (value == 0.0)
                return "0";

            double abs = Math.Abs(value);

            // 科学计数法阈值
            if (abs < 1e-4 || abs >= 1e7)
            {
                string sci = value.ToString("0.######e+0", CultureInfo.InvariantCulture);
                return sci.Replace("e+00", "e0")
                          .Replace("e-00", "e-0")
                          .Replace("e+0", "e+")
                          .Replace("e-0", "e-");
            }

            // 常规浮点，保留最多 8 位有效小数，自动抹除末尾冗余 0
            string result = value.ToString("0.########", CultureInfo.InvariantCulture);
            if (result == "0" && value != 0)
            {
                return value.ToString("G6", CultureInfo.InvariantCulture);
            }

            return result;
        }

        private static List<GeoUnitDefinition> BuildUnits()
        {
            return new List<GeoUnitDefinition>
            {
                // ════════════ 1. 品位 / 浓度 (Base: g/t = ppm) ════════════
                new()
                {
                    Id = "g_t",
                    Category = GeoUnitCategory.Grade,
                    Symbol = "g/t",
                    NameKey = "geo_u_g_t_name",
                    DefaultName = "克/吨",
                    DescKey = "geo_u_g_t_desc",
                    DefaultDesc = "固体矿产与选冶最常用品位单位 (1 g/t = 1 ppm = 1 mg/kg)",
                    ToStandard = v => v,
                    FromStandard = v => v
                },
                new()
                {
                    Id = "ppm",
                    Category = GeoUnitCategory.Grade,
                    Symbol = "ppm",
                    NameKey = "geo_u_ppm_name",
                    DefaultName = "百万分比 (mg/kg)",
                    DescKey = "geo_u_ppm_desc",
                    DefaultDesc = "微量元素地球化学质量浓度 (1 ppm = 10⁻⁶ = 1 g/t)",
                    ToStandard = v => v,
                    FromStandard = v => v
                },
                new()
                {
                    Id = "wt_pct",
                    Category = GeoUnitCategory.Grade,
                    Symbol = "wt%",
                    NameKey = "geo_u_wt_pct_name",
                    DefaultName = "质量百分比",
                    DescKey = "geo_u_wt_pct_desc",
                    DefaultDesc = "主量元素与高品位矿石常用 (1 wt% = 10,000 g/t)",
                    ToStandard = v => v * 10000.0,
                    FromStandard = v => v / 10000.0
                },
                new()
                {
                    Id = "ppb",
                    Category = GeoUnitCategory.Grade,
                    Symbol = "ppb",
                    NameKey = "geo_u_ppb_name",
                    DefaultName = "十亿分比 (μg/kg)",
                    DescKey = "geo_u_ppb_desc",
                    DefaultDesc = "超微量元素与稀贵金属铂族元素 (1 ppb = 10⁻⁹ = 0.001 g/t)",
                    ToStandard = v => v * 0.001,
                    FromStandard = v => v * 1000.0
                },
                new()
                {
                    Id = "oz_t",
                    Category = GeoUnitCategory.Grade,
                    Symbol = "oz/t",
                    NameKey = "geo_u_oz_t_name",
                    DefaultName = "金衡盎司/公吨",
                    DescKey = "geo_u_oz_t_desc",
                    DefaultDesc = "公制吨金衡盎司 (1 troy oz = 31.1034768 g, 1 oz/t ≈ 31.1035 g/t)",
                    ToStandard = v => v * 31.1034768,
                    FromStandard = v => v / 31.1034768
                },
                new()
                {
                    Id = "oz_st",
                    Category = GeoUnitCategory.Grade,
                    Symbol = "oz/st",
                    NameKey = "geo_u_oz_st_name",
                    DefaultName = "金衡盎司/短吨 (opt)",
                    DescKey = "geo_u_oz_st_desc",
                    DefaultDesc = "北美矿业常用短吨 (1 short ton ≈ 0.907185 t, 1 opt ≈ 34.2857 g/t)",
                    ToStandard = v => v * (31.1034768 / 0.90718474),
                    FromStandard = v => v / (31.1034768 / 0.90718474)
                },

                // ════════════ 2. 压力 / 应力 (Base: MPa) ════════════
                new()
                {
                    Id = "GPa",
                    Category = GeoUnitCategory.Pressure,
                    Symbol = "GPa",
                    NameKey = "geo_u_gpa_name",
                    DefaultName = "吉帕",
                    DescKey = "geo_u_gpa_desc",
                    DefaultDesc = "深部岩石圈与地幔高压 (1 GPa = 10 kbar = 1,000 MPa)",
                    ToStandard = v => v * 1000.0,
                    FromStandard = v => v / 1000.0
                },
                new()
                {
                    Id = "kbar",
                    Category = GeoUnitCategory.Pressure,
                    Symbol = "kbar",
                    NameKey = "geo_u_kbar_name",
                    DefaultName = "千巴",
                    DescKey = "geo_u_kbar_desc",
                    DefaultDesc = "变质相与实验岩石学经典单位 (1 kbar = 100 MPa = 0.1 GPa)",
                    ToStandard = v => v * 100.0,
                    FromStandard = v => v / 100.0
                },
                new()
                {
                    Id = "MPa",
                    Category = GeoUnitCategory.Pressure,
                    Symbol = "MPa",
                    NameKey = "geo_u_mpa_name",
                    DefaultName = "兆帕",
                    DescKey = "geo_u_mpa_desc",
                    DefaultDesc = "地应力与地壳浅层流体静水压力 (1 MPa = 10 bar = 10⁶ Pa)",
                    ToStandard = v => v,
                    FromStandard = v => v
                },
                new()
                {
                    Id = "bar",
                    Category = GeoUnitCategory.Pressure,
                    Symbol = "bar",
                    NameKey = "geo_u_bar_name",
                    DefaultName = "巴",
                    DescKey = "geo_u_bar_desc",
                    DefaultDesc = "实验热力学常用气压单位 (1 bar = 0.1 MPa = 100 kPa)",
                    ToStandard = v => v * 0.1,
                    FromStandard = v => v * 10.0
                },
                new()
                {
                    Id = "atm",
                    Category = GeoUnitCategory.Pressure,
                    Symbol = "atm",
                    NameKey = "geo_u_atm_name",
                    DefaultName = "标准大气压",
                    DescKey = "geo_u_atm_desc",
                    DefaultDesc = "物理标准大气压 (1 atm = 101.325 kPa = 0.101325 MPa)",
                    ToStandard = v => v * 0.101325,
                    FromStandard = v => v / 0.101325
                },
                new()
                {
                    Id = "psi",
                    Category = GeoUnitCategory.Pressure,
                    Symbol = "psi",
                    NameKey = "geo_u_psi_name",
                    DefaultName = "磅力/平方英寸",
                    DescKey = "geo_u_psi_desc",
                    DefaultDesc = "石油天然气与钻井工程地层压力 (1 MPa ≈ 145.038 psi)",
                    ToStandard = v => v * 0.006894757293168,
                    FromStandard = v => v / 0.006894757293168
                },

                // ════════════ 3. 微区尺度 (Base: μm) ════════════
                new()
                {
                    Id = "mm",
                    Category = GeoUnitCategory.Scale,
                    Symbol = "mm",
                    NameKey = "geo_u_mm_name",
                    DefaultName = "毫米",
                    DescKey = "geo_u_mm_desc",
                    DefaultDesc = "矿物斑晶与手标本薄片肉眼尺度 (1 mm = 1,000 μm)",
                    ToStandard = v => v * 1000.0,
                    FromStandard = v => v / 1000.0
                },
                new()
                {
                    Id = "um",
                    Category = GeoUnitCategory.Scale,
                    Symbol = "μm",
                    NameKey = "geo_u_um_name",
                    DefaultName = "微米",
                    DescKey = "geo_u_um_desc",
                    DefaultDesc = "电子探针 (EPMA) 束斑与 SEM 观察尺度 (1 μm = 1,000 nm)",
                    ToStandard = v => v,
                    FromStandard = v => v
                },
                new()
                {
                    Id = "nm",
                    Category = GeoUnitCategory.Scale,
                    Symbol = "nm",
                    NameKey = "geo_u_nm_name",
                    DefaultName = "纳米",
                    DescKey = "geo_u_nm_desc",
                    DefaultDesc = "粘土矿物层间间隙与纳米矿物颗粒 (1 nm = 10 Å = 0.001 μm)",
                    ToStandard = v => v * 0.001,
                    FromStandard = v => v * 1000.0
                },
                new()
                {
                    Id = "angstrom",
                    Category = GeoUnitCategory.Scale,
                    Symbol = "Å",
                    NameKey = "geo_u_angstrom_name",
                    DefaultName = "埃",
                    DescKey = "geo_u_angstrom_desc",
                    DefaultDesc = "晶格常数与 X 射线衍射特征波长 (1 Å = 0.1 nm = 10⁻⁴ μm)",
                    ToStandard = v => v * 0.0001,
                    FromStandard = v => v * 10000.0
                },
                new()
                {
                    Id = "pm",
                    Category = GeoUnitCategory.Scale,
                    Symbol = "pm",
                    NameKey = "geo_u_pm_name",
                    DefaultName = "皮米",
                    DescKey = "geo_u_pm_desc",
                    DefaultDesc = "离子有效半径与原子轨道尺度 (1 Å = 100 pm, 1 pm = 10⁻⁶ μm)",
                    ToStandard = v => v * 0.000001,
                    FromStandard = v => v * 1000000.0
                },

                // ════════════ 4. 温度 / 地温 (Base: ℃) ════════════
                new()
                {
                    Id = "celsius",
                    Category = GeoUnitCategory.Temperature,
                    Symbol = "℃",
                    NameKey = "geo_u_celsius_name",
                    DefaultName = "摄氏度",
                    DescKey = "geo_u_celsius_desc",
                    DefaultDesc = "地学常规温度与地温梯度主流单位",
                    ToStandard = v => v,
                    FromStandard = v => v
                },
                new()
                {
                    Id = "kelvin",
                    Category = GeoUnitCategory.Temperature,
                    Symbol = "K",
                    NameKey = "geo_u_kelvin_name",
                    DefaultName = "开尔文",
                    DescKey = "geo_u_kelvin_desc",
                    DefaultDesc = "热力学状态方程与绝对温标 (0 ℃ = 273.15 K)",
                    ToStandard = v => v - 273.15,
                    FromStandard = v => v + 273.15
                },
                new()
                {
                    Id = "fahrenheit",
                    Category = GeoUnitCategory.Temperature,
                    Symbol = "℉",
                    NameKey = "geo_u_fahrenheit_name",
                    DefaultName = "华氏度",
                    DescKey = "geo_u_fahrenheit_desc",
                    DefaultDesc = "北美油气田与钻井工程地层温标 (℉ = ℃ × 1.8 + 32)",
                    ToStandard = v => (v - 32.0) / 1.8,
                    FromStandard = v => v * 1.8 + 32.0
                },

                // ════════════ 5. 同位素年代 (Base: Ma) ════════════
                new()
                {
                    Id = "Ga",
                    Category = GeoUnitCategory.Time,
                    Symbol = "Ga",
                    NameKey = "geo_u_ga_name",
                    DefaultName = "十亿年 (Gyr)",
                    DescKey = "geo_u_ga_desc",
                    DefaultDesc = "前寒武纪深时与地球早期演化 (1 Ga = 1,000 Ma = 10⁹ 年)",
                    ToStandard = v => v * 1000.0,
                    FromStandard = v => v / 1000.0
                },
                new()
                {
                    Id = "Ma",
                    Category = GeoUnitCategory.Time,
                    Symbol = "Ma",
                    NameKey = "geo_u_ma_name",
                    DefaultName = "百万年 (Myr)",
                    DescKey = "geo_u_ma_desc",
                    DefaultDesc = "显生宙与同位素定年主流单位 (1 Ma = 10⁶ 年)",
                    ToStandard = v => v,
                    FromStandard = v => v
                },
                new()
                {
                    Id = "ka",
                    Category = GeoUnitCategory.Time,
                    Symbol = "ka",
                    NameKey = "geo_u_ka_name",
                    DefaultName = "千年 (kyr)",
                    DescKey = "geo_u_ka_desc",
                    DefaultDesc = "第四纪冰期与 ¹⁴C / OSL 定年 (1 ka = 1,000 年 = 0.001 Ma)",
                    ToStandard = v => v * 0.001,
                    FromStandard = v => v * 1000.0
                },
                new()
                {
                    Id = "yr",
                    Category = GeoUnitCategory.Time,
                    Symbol = "yr",
                    NameKey = "geo_u_yr_name",
                    DefaultName = "年 (a)",
                    DescKey = "geo_u_yr_desc",
                    DefaultDesc = "绝对日历年 (1 Ma = 1,000,000 年)",
                    ToStandard = v => v * 0.000001,
                    FromStandard = v => v * 1000000.0
                }
            };
        }

        private static List<GeoPresetItem> BuildPresets()
        {
            return new List<GeoPresetItem>
            {
                // 品位 / 浓度
                new()
                {
                    Category = GeoUnitCategory.Grade,
                    TitleKey = "geo_p_gold_cutoff_title",
                    DefaultTitle = "金矿边界品位",
                    Value = 1.0,
                    UnitSymbol = "g/t",
                    NoteKey = "geo_p_gold_cutoff_note",
                    DefaultNote = "常规岩金工业开采参考边界品位"
                },
                new()
                {
                    Category = GeoUnitCategory.Grade,
                    TitleKey = "geo_p_porphyry_cu_title",
                    DefaultTitle = "斑岩铜矿典型",
                    Value = 0.5,
                    UnitSymbol = "wt%",
                    NoteKey = "geo_p_porphyry_cu_note",
                    DefaultNote = "超大型斑岩铜矿平均工业品位 (~5000 ppm)"
                },
                new()
                {
                    Category = GeoUnitCategory.Grade,
                    TitleKey = "geo_p_pge_au_title",
                    DefaultTitle = "PGE/Au 超微量",
                    Value = 10.0,
                    UnitSymbol = "ppb",
                    NoteKey = "geo_p_pge_au_note",
                    DefaultNote = "地幔橄榄岩与火成岩铂族元素常见丰度"
                },
                new()
                {
                    Category = GeoUnitCategory.Grade,
                    TitleKey = "geo_p_high_grade_ore_title",
                    DefaultTitle = "富矿石 (1 opt)",
                    Value = 1.0,
                    UnitSymbol = "oz/st",
                    NoteKey = "geo_p_high_grade_ore_note",
                    DefaultNote = "北美高品位金矿常用盎司短吨指标 (≈34.29 g/t)"
                },

                // 压力 / 应力
                new()
                {
                    Category = GeoUnitCategory.Pressure,
                    TitleKey = "geo_p_fluid_press_title",
                    DefaultTitle = "地下 1 km 流体压",
                    Value = 27.0,
                    UnitSymbol = "MPa",
                    NoteKey = "geo_p_fluid_press_note",
                    DefaultNote = "地壳浅部静岩与孔隙流体压力"
                },
                new()
                {
                    Category = GeoUnitCategory.Pressure,
                    TitleKey = "geo_p_moho_press_title",
                    DefaultTitle = "地壳下部/莫霍面",
                    Value = 1.0,
                    UnitSymbol = "GPa",
                    NoteKey = "geo_p_moho_press_note",
                    DefaultNote = "大陆地壳底部典型压力 (~10 kbar)"
                },
                new()
                {
                    Category = GeoUnitCategory.Pressure,
                    TitleKey = "geo_p_trans_zone_title",
                    DefaultTitle = "地幔过渡带顶界",
                    Value = 14.0,
                    UnitSymbol = "GPa",
                    NoteKey = "geo_p_trans_zone_note",
                    DefaultNote = "410 km 橄榄石相变高压边界"
                },
                new()
                {
                    Category = GeoUnitCategory.Pressure,
                    TitleKey = "geo_p_std_atm_title",
                    DefaultTitle = "地表常压 (标况)",
                    Value = 1.0,
                    UnitSymbol = "atm",
                    NoteKey = "geo_p_std_atm_note",
                    DefaultNote = "101.325 kPa，实验室参考态"
                },

                // 微区尺度
                new()
                {
                    Category = GeoUnitCategory.Scale,
                    TitleKey = "geo_p_phenocryst_title",
                    DefaultTitle = "显微镜斑晶",
                    Value = 1.0,
                    UnitSymbol = "mm",
                    NoteKey = "geo_p_phenocryst_note",
                    DefaultNote = "常规偏光显微镜宏观视域 (1000 μm)"
                },
                new()
                {
                    Category = GeoUnitCategory.Scale,
                    TitleKey = "geo_p_epma_beam_title",
                    DefaultTitle = "EPMA 电子探针束斑",
                    Value = 1.0,
                    UnitSymbol = "μm",
                    NoteKey = "geo_p_epma_beam_note",
                    DefaultNote = "定量微区电子探针聚焦束斑典型直径"
                },
                new()
                {
                    Category = GeoUnitCategory.Scale,
                    TitleKey = "geo_p_sem_nano_title",
                    DefaultTitle = "SEM 纳晶颗粒",
                    Value = 100.0,
                    UnitSymbol = "nm",
                    NoteKey = "geo_p_sem_nano_note",
                    DefaultNote = "扫描电镜下黏土矿物与纳米矿物"
                },
                new()
                {
                    Category = GeoUnitCategory.Scale,
                    TitleKey = "geo_p_cu_ka_title",
                    DefaultTitle = "Cu Kα 射线波长",
                    Value = 1.5418,
                    UnitSymbol = "Å",
                    NoteKey = "geo_p_cu_ka_note",
                    DefaultNote = "X射线单晶/粉末衍射 Cu-Kα 特征波长"
                },

                // 温度 / 地温
                new()
                {
                    Category = GeoUnitCategory.Temperature,
                    TitleKey = "geo_p_surface_temp_title",
                    DefaultTitle = "地表常温",
                    Value = 25.0,
                    UnitSymbol = "℃",
                    NoteKey = "geo_p_surface_temp_note",
                    DefaultNote = "标准实验室环境参考态 (298.15 K)"
                },
                new()
                {
                    Category = GeoUnitCategory.Temperature,
                    TitleKey = "geo_p_hydrothermal_title",
                    DefaultTitle = "中低温热液",
                    Value = 300.0,
                    UnitSymbol = "℃",
                    NoteKey = "geo_p_hydrothermal_note",
                    DefaultNote = "石英脉型金矿流体包裹体典型均一温度"
                },
                new()
                {
                    Category = GeoUnitCategory.Temperature,
                    TitleKey = "geo_p_solidus_title",
                    DefaultTitle = "花岗岩固相线",
                    Value = 650.0,
                    UnitSymbol = "℃",
                    NoteKey = "geo_p_solidus_note",
                    DefaultNote = "含水长英质岩浆结晶成岩固相线"
                },

                // 同位素年代
                new()
                {
                    Category = GeoUnitCategory.Time,
                    TitleKey = "geo_p_earth_age_title",
                    DefaultTitle = "地球形成年龄",
                    Value = 4.54,
                    UnitSymbol = "Ga",
                    NoteKey = "geo_p_earth_age_note",
                    DefaultNote = "陨石铅同位素确定的地球诞生年龄"
                },
                new()
                {
                    Category = GeoUnitCategory.Time,
                    TitleKey = "geo_p_cambrian_base_title",
                    DefaultTitle = "寒武纪底界",
                    Value = 538.8,
                    UnitSymbol = "Ma",
                    NoteKey = "geo_p_cambrian_base_note",
                    DefaultNote = "显生宙与寒武纪大爆发金钉子底界"
                },
                new()
                {
                    Category = GeoUnitCategory.Time,
                    TitleKey = "geo_p_k_pg_boundary_title",
                    DefaultTitle = "K-Pg 白垩纪灭绝",
                    Value = 66.0,
                    UnitSymbol = "Ma",
                    NoteKey = "geo_p_k_pg_boundary_note",
                    DefaultNote = "恐龙灭绝与希克苏鲁伯撞击事件界线"
                },
                new()
                {
                    Category = GeoUnitCategory.Time,
                    TitleKey = "geo_p_quaternary_base_title",
                    DefaultTitle = "第四纪底界",
                    Value = 2.58,
                    UnitSymbol = "Ma",
                    NoteKey = "geo_p_quaternary_base_note",
                    DefaultNote = "更新世大冰期与人类演化纪元开端"
                }
            };
        }
    }
}
