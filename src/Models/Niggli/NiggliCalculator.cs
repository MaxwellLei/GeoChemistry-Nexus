using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.Models.Niggli
{
    /// <summary>
    /// 经典尼格里参数 (Niggli Values / Niggli Numbers)
    /// </summary>
    public class NiggliParameters
    {
        public double Si { get; set; }
        public double Al { get; set; }
        public double Fm { get; set; }
        public double C { get; set; }
        public double Alk { get; set; }
        public double Ti { get; set; }
        public double P { get; set; }
        public double K { get; set; }
        public double Mg { get; set; }
        public double Qz { get; set; }

        public Dictionary<string, double> ToDictionary()
        {
            return new Dictionary<string, double>
            {
                ["si"] = Math.Round(Si, 2),
                ["al"] = Math.Round(Al, 2),
                ["fm"] = Math.Round(Fm, 2),
                ["c"] = Math.Round(C, 2),
                ["alk"] = Math.Round(Alk, 2),
                ["ti"] = Math.Round(Ti, 2),
                ["p"] = Math.Round(P, 2),
                ["k"] = Math.Round(K, 3),
                ["mg"] = Math.Round(Mg, 3),
                ["qz"] = Math.Round(Qz, 2)
            };
        }
    }

    /// <summary>
    /// 矿物端元分子比例与固溶体成分 (Mineral Endmember Ratios)
    /// </summary>
    public class NiggliEndmemberCompositions
    {
        /// <summary>
        /// 透辉石中的硅灰石端元百分比 Wo%
        /// </summary>
        public double WollastonitePercent { get; set; }

        /// <summary>
        /// 透辉石中的顽火辉石端元百分比 En%
        /// </summary>
        public double EnstatitePercent { get; set; }

        /// <summary>
        /// 透辉石中的铁辉石端元百分比 Fs%
        /// </summary>
        public double FerrosilitePercent { get; set; }

        /// <summary>
        /// 橄榄石中的镁橄榄石端元百分比 Fo%
        /// </summary>
        public double ForsteritePercent { get; set; }

        /// <summary>
        /// 橄榄石中的铁橄榄石端元百分比 Fa%
        /// </summary>
        public double FayalitePercent { get; set; }

        /// <summary>
        /// 斜长石号 An% (An / (Ab + An) * 100)
        /// </summary>
        public double AnorthitePercent { get; set; }

        /// <summary>
        /// 镁指数 (Mg / (Mg + Fe2+)) * 100
        /// </summary>
        public double MagnesiumRatio { get; set; }
    }

    /// <summary>
    /// 支持多语言参数化格式化的 Niggli 消息/步骤对象
    /// </summary>
    public class NiggliMessage
    {
        public string Key { get; set; } = string.Empty;
        public object[] Args { get; set; } = Array.Empty<object>();
        public string Fallback { get; set; } = string.Empty;

        public NiggliMessage() { }

        public NiggliMessage(string key, string fallback, params object[] args)
        {
            Key = key;
            Fallback = fallback;
            Args = args ?? Array.Empty<object>();
        }

        public string Format(Func<string, string?>? translator = null)
        {
            string? template = translator?.Invoke(Key);
            if (string.IsNullOrEmpty(template))
            {
                template = Fallback;
            }
            if (string.IsNullOrEmpty(template))
            {
                template = Key;
            }

            return Args?.Length > 0 ? string.Format(template, Args) : template;
        }

        public override string ToString() => Format();
    }

    /// <summary>
    /// Niggli 计算结果
    /// </summary>
    public class NiggliResult
    {
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;
        public string ErrorKey { get; set; } = string.Empty;

        /// <summary>
        /// 标准矿物阳离子百分比 (Catanorm Mineral Cation %)
        /// </summary>
        public Dictionary<string, double> Minerals { get; set; } = new();

        /// <summary>
        /// 经典尼格里参数 (Niggli Numbers: si, al, fm, c, alk, k, mg, qz)
        /// </summary>
        public NiggliParameters Parameters { get; set; } = new();

        /// <summary>
        /// 端元组分比例
        /// </summary>
        public NiggliEndmemberCompositions Endmembers { get; set; } = new();

        /// <summary>
        /// 硅饱和度状态 ("oversaturated", "saturated", "undersaturated")
        /// </summary>
        public string SilicaSaturation { get; set; } = string.Empty;

        /// <summary>
        /// 铝饱和度状态 ("peraluminous", "metaluminous", "peralkaline")
        /// </summary>
        public string AluminaState { get; set; } = string.Empty;

        /// <summary>
        /// 初始阳离子总和 (归一化前)
        /// </summary>
        public double RawCationSum { get; set; }

        /// <summary>
        /// 标准矿物百分比总和 (Sum)
        /// </summary>
        public double MineralSum { get; set; }

        /// <summary>
        /// 执行的去硅反应记录
        /// </summary>
        public List<NiggliMessage> DesilicationSteps { get; set; } = new();

        /// <summary>
        /// 计算过程产生的警告信息
        /// </summary>
        public List<NiggliMessage> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Barth-Niggli Catanorm 计算引擎
    /// 严格遵循 Niggli (1948) 与 Barth 的阳离子分子标准矿物分配体系，
    /// 修复了历史 R 脚本中的白榴石去硅赋键笔误，并增加经典尼格里参数双轨计算。
    /// </summary>
    public static class NiggliCalculator
    {
        /// <summary>
        /// 执行 Niggli 标准分子矿物及尼格里参数计算
        /// </summary>
        /// <param name="rawOxides">输入的全岩主量氧化物重量百分比 (wt%)</param>
        /// <param name="fe3Ratio">当缺少 Fe2O3/FeO 分价时的 Fe3+/Fe总 比值 (默认 0.15)</param>
        /// <returns>计算结果结构体</returns>
        public static NiggliResult Calculate(Dictionary<string, double> rawOxides, double fe3Ratio = NiggliConstants.DefaultFe3Ratio)
        {
            var result = new NiggliResult();

            try
            {
                if (rawOxides == null || rawOxides.Count == 0)
                {
                    result.Success = false;
                    result.ErrorKey = "niggli_msg_no_data";
                    result.ErrorMessage = "输入数据为空";
                    return result;
                }

                // 1. 铁的氧化状态拆分与预处理
                var oxides = PartitionIron(rawOxides, fe3Ratio, result.Warnings);

                // 2. 计算经典尼格里参数 (Niggli Numbers: si, al, fm, c, alk, k, mg, qz)
                result.Parameters = CalculateNiggliNumbers(oxides);

                // 3. 计算阳离子毫克分子比例 (Millications) 并归一化为 100% 阳离子基数
                var (cations, rawSum) = ConvertToNormalizedCations(oxides);
                result.RawCationSum = rawSum;

                if (rawSum <= NiggliConstants.Epsilon)
                {
                    result.Success = false;
                    result.ErrorKey = "niggli_error_cation_sum_zero";
                    result.ErrorMessage = "全岩阳离子总和不足或数据无效";
                    return result;
                }

                // 4. 判断铝饱和状态 (Alumina Saturation)
                double alVal = cations.GetValueOrDefault("Al");
                double naVal = cations.GetValueOrDefault("Na");
                double kVal = cations.GetValueOrDefault("K");
                double caVal = cations.GetValueOrDefault("Ca");
                double alkTotal = naVal + kVal;

                if (alVal > alkTotal + caVal)
                {
                    result.AluminaState = "peraluminous"; // 过铝质
                }
                else if (alVal > alkTotal)
                {
                    result.AluminaState = "metaluminous"; // 准铝质
                }
                else
                {
                    result.AluminaState = "peralkaline"; // 过碱质
                }

                // 5. 执行 Barth-Niggli Catanorm 标准矿物分配与去硅反应链
                var state = new CatanormExecutionState(cations);
                state.ExecuteAllocation(result.DesilicationSteps);

                // 6. 提取矿物分配结果
                result.Minerals = state.ExtractMinerals();
                result.Endmembers = state.BuildEndmembers();
                result.MineralSum = state.CalculateSum();

                // 7. 判断硅饱和度 (Silica Saturation)
                if (result.Minerals.TryGetValue("Q", out double q) && q > 0.001)
                {
                    result.SilicaSaturation = "oversaturated"; // 硅过饱和 (含石英)
                }
                else if (result.Minerals.TryGetValue("Ne", out double ne) && ne > 0.001 ||
                         result.Minerals.TryGetValue("Lc", out double lc) && lc > 0.001 ||
                         result.Minerals.TryGetValue("Kp", out double kp) && kp > 0.001 ||
                         result.Minerals.TryGetValue("Ol", out double ol) && ol > 0.001)
                {
                    result.SilicaSaturation = "undersaturated"; // 硅不饱和 (含似长石/橄榄石)
                }
                else
                {
                    result.SilicaSaturation = "saturated"; // 硅饱和
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 处理铁的价态分配
        /// </summary>
        private static Dictionary<string, double> PartitionIron(
            Dictionary<string, double> input, double fe3Ratio, List<NiggliMessage> warnings)
        {
            var output = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in input)
            {
                if (kv.Value > 0)
                {
                    output[kv.Key] = kv.Value;
                }
            }

            bool hasFe2O3 = output.TryGetValue("Fe2O3", out double fe2o3) && fe2o3 > 0;
            bool hasFeO = output.TryGetValue("FeO", out double feo) && feo > 0;
            bool hasFeOT = output.TryGetValue("FeOT", out double feot) && feot > 0;

            if (hasFe2O3 && hasFeO)
            {
                // 已实测 Fe2O3 和 FeO，直接使用
            }
            else if (hasFeOT)
            {
                // 由全铁 FeOT 拆分
                double feTotalMoles = feot / NiggliConstants.OxideMolarMass["FeO"];
                double fe3Moles = feTotalMoles * fe3Ratio;
                double fe2Moles = feTotalMoles * (1.0 - fe3Ratio);

                output["Fe2O3"] = (fe3Moles / 2.0) * NiggliConstants.OxideMolarMass["Fe2O3"];
                output["FeO"] = fe2Moles * NiggliConstants.OxideMolarMass["FeO"];
                output.Remove("FeOT");
                warnings.Add(new NiggliMessage("niggli_warning_feot_estimated", $"使用全铁 FeOT 估算价态 (Fe³⁺/Fe = {fe3Ratio:F2})", $"{fe3Ratio:F2}"));
            }
            else if (hasFe2O3 && !hasFeO)
            {
                // 仅提供了 Fe2O3，按全铁拆分
                double feTotalMoles = (fe2o3 * 2.0) / NiggliConstants.OxideMolarMass["Fe2O3"];
                double fe3Moles = feTotalMoles * fe3Ratio;
                double fe2Moles = feTotalMoles * (1.0 - fe3Ratio);

                output["Fe2O3"] = (fe3Moles / 2.0) * NiggliConstants.OxideMolarMass["Fe2O3"];
                output["FeO"] = fe2Moles * NiggliConstants.OxideMolarMass["FeO"];
            }
            else if (hasFeO && !hasFe2O3)
            {
                // 仅提供了 FeO，按全铁拆分
                double feTotalMoles = feo / NiggliConstants.OxideMolarMass["FeO"];
                double fe3Moles = feTotalMoles * fe3Ratio;
                double fe2Moles = feTotalMoles * (1.0 - fe3Ratio);

                output["Fe2O3"] = (fe3Moles / 2.0) * NiggliConstants.OxideMolarMass["Fe2O3"];
                output["FeO"] = fe2Moles * NiggliConstants.OxideMolarMass["FeO"];
            }

            return output;
        }

        /// <summary>
        /// 计算经典尼格里参数 (Niggli Values: si, al, fm, c, alk, k, mg, qz)
        /// </summary>
        private static NiggliParameters CalculateNiggliNumbers(Dictionary<string, double> oxides)
        {
            var p = new NiggliParameters();

            double GetMol(string ox)
            {
                if (oxides.TryGetValue(ox, out double wt) && wt > 0 &&
                    NiggliConstants.OxideMolarMass.TryGetValue(ox, out double mw))
                {
                    return wt / mw;
                }
                return 0.0;
            }

            double molSi  = GetMol("SiO2");
            double molAl  = GetMol("Al2O3");
            double molFe3 = GetMol("Fe2O3");
            double molFe2 = GetMol("FeO");
            double molMn  = GetMol("MnO");
            double molMg  = GetMol("MgO");
            double molCa  = GetMol("CaO");
            double molNa  = GetMol("Na2O");
            double molK   = GetMol("K2O");
            double molTi  = GetMol("TiO2");
            double molP   = GetMol("P2O5");

            double al = molAl;
            double fm = molFe2 + 2.0 * molFe3 + molMn + molMg;
            double c  = molCa;
            double alk = molNa + molK;

            double baseSum = al + fm + c + alk;
            if (baseSum > NiggliConstants.Epsilon)
            {
                p.Si  = (molSi / baseSum) * 100.0;
                p.Al  = (al / baseSum) * 100.0;
                p.Fm  = (fm / baseSum) * 100.0;
                p.C   = (c / baseSum) * 100.0;
                p.Alk = (alk / baseSum) * 100.0;
                p.Ti  = (molTi / baseSum) * 100.0;
                p.P   = (molP / baseSum) * 100.0;

                double alkTotal = molNa + molK;
                p.K = alkTotal > NiggliConstants.Epsilon ? (molK / alkTotal) : 0.0;

                double fmTotal = molMg + molFe2 + 2.0 * molFe3 + molMn;
                p.Mg = fmTotal > NiggliConstants.Epsilon ? (molMg / fmTotal) : 0.0;

                // 经典尼格里石英指数 qz
                if (p.Al > p.Alk)
                {
                    p.Qz = p.Si - (100.0 + 4.0 * p.Al);
                }
                else
                {
                    p.Qz = p.Si - (100.0 + 3.0 * p.Al + p.Alk);
                }
            }

            return p;
        }

        /// <summary>
        /// 将氧化物重量百分比换算为阳离子毫克分子，并归一化为 100% 阳离子基数 (Catanorm Basis)
        /// </summary>
        private static (Dictionary<string, double> Cations, double RawSum) ConvertToNormalizedCations(
            Dictionary<string, double> oxides)
        {
            var rawCations = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            double totalCationMoles = 0.0;

            var oxideToElement = new Dictionary<string, string>
            {
                ["SiO2"]  = "Si",
                ["TiO2"]  = "Ti",
                ["Al2O3"] = "Al",
                ["Fe2O3"] = "Fe3",
                ["FeO"]   = "Fe2",
                ["MnO"]   = "Mn",
                ["MgO"]   = "Mg",
                ["CaO"]   = "Ca",
                ["Na2O"]  = "Na",
                ["K2O"]   = "K",
                ["CO2"]   = "CO2",
                ["P2O5"]  = "P",
                ["F"]     = "F",
                ["S"]     = "S"
            };

            foreach (var ox in NiggliConstants.ComputedOxides)
            {
                if (oxides.TryGetValue(ox, out double wt) && wt > 0 &&
                    NiggliConstants.OxideMolarMass.TryGetValue(ox, out double mw) &&
                    NiggliConstants.OxideCationCount.TryGetValue(ox, out int count))
                {
                    double catMoles = (wt / mw) * count;
                    string elem = oxideToElement[ox];
                    rawCations[elem] = catMoles;
                    totalCationMoles += catMoles;
                }
            }

            var normalized = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            if (totalCationMoles > NiggliConstants.Epsilon)
            {
                double factor = 100.0 / totalCationMoles;
                foreach (var kv in rawCations)
                {
                    normalized[kv.Key] = kv.Value * factor;
                }
            }

            return (normalized, totalCationMoles);
        }

        /// <summary>
        /// 内部状态管理类：用于执行 Catanorm 阶段分配与连续去硅反应
        /// </summary>
        private class CatanormExecutionState
        {
            // 剩余阳离子百分比池
            public double Si, Ti, Al, Fe3, Fe2, Mn, Mg, Ca, Na, K, CO2, P, F, S;

            // 矿物生成量 (阳离子百分比)
            public double Qtz, Cor, Ort, Plag, Ab, An, Lc, Ne, Kp, Ac, Ns, Ks,
                          Hy, Di, Wo, En, Fs, Ol, Cs, Mt, Hm, Il, Tn, Pf, Ru,
                          Ap, Fr, Py, Cc;

            public double MagnesiumRatio;

            public CatanormExecutionState(Dictionary<string, double> cations)
            {
                Si  = cations.GetValueOrDefault("Si");
                Ti  = cations.GetValueOrDefault("Ti");
                Al  = cations.GetValueOrDefault("Al");
                Fe3 = cations.GetValueOrDefault("Fe3");
                Fe2 = cations.GetValueOrDefault("Fe2");
                Mn  = cations.GetValueOrDefault("Mn");
                Mg  = cations.GetValueOrDefault("Mg");
                Ca  = cations.GetValueOrDefault("Ca");
                Na  = cations.GetValueOrDefault("Na");
                K   = cations.GetValueOrDefault("K");
                CO2 = cations.GetValueOrDefault("CO2");
                P   = cations.GetValueOrDefault("P");
                F   = cations.GetValueOrDefault("F");
                S   = cations.GetValueOrDefault("S");
            }

            public void ExecuteAllocation(List<NiggliMessage> stepLog)
            {
                // 1. Mn 与 Fe2+ 合并
                Fe2 += Mn;
                Mn = 0;

                // 2. 方解石 (Calcite): Cc = 2 * CO2
                Cc = 2.0 * CO2;
                Ca -= CO2;
                CO2 = 0;

                // 3. 磷灰石 (Apatite):
                if (P < 3.0 * F)
                {
                    Ap = 3.0 * P;
                    Ca -= 1.667 * P;
                    F -= 0.3333333333333333 * P;
                }
                else
                {
                    Ap = 2.666666666666667 * P + F;
                    Ca -= 1.666666666666667 * P;
                    F = 0;
                }
                P = 0;

                // 4. 萤石 (Fluorite): Fr = 1.5 * F
                Fr = 1.5 * F;
                Ca -= 0.5 * F;
                F = 0;

                // 5. 黄铁矿 (Pyrite): Py = 1.5 * S
                Py = 1.5 * S;
                Fe2 -= 0.5 * S;
                S = 0;

                // 6. 钛铁矿 (Ilmenite):
                if (Ti <= Fe2)
                {
                    Il = 2.0 * Ti;
                    Fe2 -= Ti;
                    Ti = 0;
                }
                else
                {
                    Il = 2.0 * Fe2;
                    Ti -= Fe2;
                    Fe2 = 0;
                }

                // 7. 正长石 (Orthoclase):
                if (K <= Al)
                {
                    Ort = 5.0 * K;
                    Al -= K;
                    Si -= 3.0 * K;
                    K = 0;
                }
                else
                {
                    Ort = 5.0 * Al;
                    K -= Al;
                    Si -= 3.0 * Al;
                    Al = 0;
                }

                // 偏硅酸钾 (Ks):
                Ks = 1.5 * K;
                Si -= 0.5 * K;
                K = 0;

                // 8. 钠长石 (Albite):
                if (Na <= Al)
                {
                    Ab = 5.0 * Na;
                    Al -= Na;
                    Si -= 3.0 * Na;
                    Na = 0;
                }
                else
                {
                    Ab = 5.0 * Al;
                    Na -= Al;
                    Si -= 3.0 * Al;
                    Al = 0;
                }

                // 9. 霓石 (Acmite):
                if (Na <= Fe3)
                {
                    Ac = 4.0 * Na;
                    Fe3 -= Na;
                    Si -= 2.0 * Na;
                    Na = 0;
                }
                else
                {
                    Ac = 4.0 * Fe3;
                    Na -= Fe3;
                    Si -= 2.0 * Fe3;
                    Fe3 = 0;
                }

                // 偏硅酸钠 (Ns):
                Ns = 1.5 * Na;
                Si -= 0.5 * Na;
                Na = 0;

                // 10. 钙长石 (Anorthite):
                if (Ca <= 0.5 * Al)
                {
                    An = 5.0 * Ca;
                    Al -= 2.0 * Ca;
                    Si -= 2.0 * Ca;
                    Ca = 0;
                }
                else
                {
                    An = 2.5 * Al;
                    Ca -= 0.5 * Al;
                    Si -= Al;
                    Al = 0;
                }

                // 11. 榍石 (Titanite):
                if (Ti <= Ca)
                {
                    Tn = 3.0 * Ti;
                    Ca -= Ti;
                    Si -= Ti;
                    Ti = 0;
                }
                else
                {
                    Tn = 3.0 * Ca;
                    Ti -= Ca;
                    Si -= Ca;
                    Ca = 0;
                }

                // 12. 金红石 (Rutile):
                Ru = Ti;
                Ti = 0;

                // 13. 刚玉 (Corundum):
                Cor = Al;
                Al = 0;

                // 14. 磁铁矿 (Magnetite):
                if (Fe3 <= 2.0 * Fe2)
                {
                    Mt = 1.5 * Fe3;
                    Fe2 -= 0.5 * Fe3;
                    Fe3 = 0;
                }
                else
                {
                    Mt = 3.0 * Fe2;
                    Fe3 -= 2.0 * Fe2;
                    Fe2 = 0;
                }

                // 15. 赤铁矿 (Hematite):
                Hm = Fe3;
                Fe3 = 0;

                // 16. 辉石组分 (Wollastonite, Enstatite, Ferrosilite):
                Wo = 2.0 * Ca;
                Si -= Ca;
                Ca = 0;

                En = 2.0 * Mg;
                Si -= Mg;
                Mg = 0;

                Fs = 2.0 * Fe2;
                Si -= Fe2;
                Fe2 = 0;

                Hy = En + Fs;
                MagnesiumRatio = (Hy > NiggliConstants.Epsilon) ? (100.0 * En / Hy) : 0.0;

                // 17. 透辉石 (Diopside): Wo 与 Hy 结合
                if (Hy < Wo)
                {
                    Di = 2.0 * Hy;
                    Wo -= Hy;
                    Hy = 0;
                }
                else
                {
                    Di = 2.0 * Wo;
                    Hy -= Wo;
                    Wo = 0;
                }

                // 18. 硅饱和平衡与 7 步连续去硅链 (Desilication Sequence)
                if (Si >= 0)
                {
                    Qtz = Si;
                    stepLog.Add(new NiggliMessage("niggli_step_oversaturated", $"硅过饱和：形成石英 Q = {Qtz:F2}%", $"{Qtz:F2}"));
                    return;
                }

                double deficit = -Si;
                stepLog.Add(new NiggliMessage("niggli_step_deficit_init", $"检测到硅亏损：初始亏损量 D = {deficit:F2}%", $"{deficit:F2}"));

                // Step 1: Hy -> Ol (正辉石去硅为橄榄石，释放 Si)
                if (Hy >= 4.0 * deficit)
                {
                    Ol = 3.0 * deficit;
                    Hy -= 4.0 * deficit;
                    stepLog.Add(new NiggliMessage("niggli_step1_hy_ol_done", $"去硅反应 Step 1: 正辉石 -> 橄榄石 (Hy → Ol)，消耗 Hy {4.0 * deficit:F2}%，完全消除硅亏损", $"{4.0 * deficit:F2}"));
                    deficit = 0;
                    return;
                }
                else if (Hy > NiggliConstants.Epsilon)
                {
                    Ol = 0.75 * Hy;
                    deficit -= 0.25 * Hy;
                    stepLog.Add(new NiggliMessage("niggli_step1_hy_ol_part", $"去硅反应 Step 1: 正辉石 -> 橄榄石 (Hy → Ol)，全部 Hy ({Hy:F2}%) 转化为 Ol，剩余亏损 D = {deficit:F2}%", $"{Hy:F2}", $"{deficit:F2}"));
                    Hy = 0;
                }

                // Step 2: Tn -> Pf (榍石去硅为钙钛矿)
                if (Tn >= 3.0 * deficit)
                {
                    Pf = 2.0 * deficit;
                    Tn -= 3.0 * deficit;
                    stepLog.Add(new NiggliMessage("niggli_step2_tn_pf_done", $"去硅反应 Step 2: 榍石 -> 钙钛矿 (Tn → Pf)，消耗 Tn {3.0 * deficit:F2}%，完全消除硅亏损", $"{3.0 * deficit:F2}"));
                    deficit = 0;
                    return;
                }
                else if (Tn > NiggliConstants.Epsilon)
                {
                    Pf = 0.6666666666666666 * Tn;
                    deficit -= 0.3333333333333333 * Tn;
                    stepLog.Add(new NiggliMessage("niggli_step2_tn_pf_part", $"去硅反应 Step 2: 榍石 -> 钙钛矿 (Tn → Pf)，全部 Tn 转化，剩余亏损 D = {deficit:F2}%", $"{deficit:F2}"));
                    Tn = 0;
                }

                // Step 3: Ab -> Ne (钠长石去硅为霞石)
                if (Ab >= 2.5 * deficit)
                {
                    Ne = 1.5 * deficit;
                    Ab -= 2.5 * deficit;
                    stepLog.Add(new NiggliMessage("niggli_step3_ab_ne_done", $"去硅反应 Step 3: 钠长石 -> 霞石 (Ab → Ne)，消耗 Ab {2.5 * deficit:F2}%，完全消除硅亏损", $"{2.5 * deficit:F2}"));
                    deficit = 0;
                    return;
                }
                else if (Ab > NiggliConstants.Epsilon)
                {
                    Ne = 0.6 * Ab;
                    deficit -= 0.4 * Ab;
                    stepLog.Add(new NiggliMessage("niggli_step3_ab_ne_part", $"去硅反应 Step 3: 钠长石 -> 霞石 (Ab → Ne)，全部 Ab 转化，剩余亏损 D = {deficit:F2}%", $"{deficit:F2}"));
                    Ab = 0;
                }

                // Step 4: Ort -> Lc (正长石去硅为白榴石)
                if (Ort >= 5.0 * deficit)
                {
                    Lc = 4.0 * deficit;
                    Ort -= 5.0 * deficit;
                    stepLog.Add(new NiggliMessage("niggli_step4_or_lc_done", $"去硅反应 Step 4: 正长石 -> 白榴石 (Or → Lc)，消耗 Or {5.0 * deficit:F2}%，完全消除硅亏损", $"{5.0 * deficit:F2}"));
                    deficit = 0;
                    return;
                }
                else if (Ort > NiggliConstants.Epsilon)
                {
                    Lc = 0.8 * Ort;
                    deficit -= 0.2 * Ort;
                    stepLog.Add(new NiggliMessage("niggli_step4_or_lc_part", $"去硅反应 Step 4: 正长石 -> 白榴石 (Or → Lc)，全部 Or 转化，剩余亏损 D = {deficit:F2}%", $"{deficit:F2}"));
                    Ort = 0;
                }

                // Step 5: Lc -> Kp (白榴石进一步去硅为钾霞石)
                // 注意：原 norm.R 脚本在 line 663 将产物赋给了未定义的 y$p，此处已精准修复赋给 Kp！
                if (Lc >= 4.0 * deficit)
                {
                    Kp += 3.0 * deficit;
                    Lc -= 4.0 * deficit;
                    stepLog.Add(new NiggliMessage("niggli_step5_lc_kp_done", $"去硅反应 Step 5: 白榴石 -> 钾霞石 (Lc → Kp)，消耗 Lc {4.0 * deficit:F2}%，完全消除硅亏损", $"{4.0 * deficit:F2}"));
                    deficit = 0;
                    return;
                }
                else if (Lc > NiggliConstants.Epsilon)
                {
                    Kp += 0.75 * Lc;
                    deficit -= 0.25 * Lc;
                    stepLog.Add(new NiggliMessage("niggli_step5_lc_kp_part", $"去硅反应 Step 5: 白榴石 -> 钾霞石 (Lc → Kp)，全部 Lc 转化，剩余亏损 D = {deficit:F2}%", $"{deficit:F2}"));
                    Lc = 0;
                }

                // Step 6: Wo -> Cs (硅灰石去硅为硅钙石 / 假硅灰石)
                if (Wo >= 4.0 * deficit)
                {
                    Cs = 3.0 * deficit;
                    Wo -= 4.0 * deficit;
                    stepLog.Add(new NiggliMessage("niggli_step6_wo_cs_done", $"去硅反应 Step 6: 硅灰石 -> 硅钙石 (Wo → Cs)，消耗 Wo {4.0 * deficit:F2}%，完全消除硅亏损", $"{4.0 * deficit:F2}"));
                    deficit = 0;
                    return;
                }
                else if (Wo > NiggliConstants.Epsilon)
                {
                    Cs = 0.75 * Wo;
                    deficit -= 0.25 * Wo;
                    stepLog.Add(new NiggliMessage("niggli_step6_wo_cs_part", $"去硅反应 Step 6: 硅灰石 -> 硅钙石 (Wo → Cs)，全部 Wo 转化，剩余亏损 D = {deficit:F2}%", $"{deficit:F2}"));
                    Wo = 0;
                }

                // Step 7: Di -> Cs + Ol (透辉石去硅为硅钙石与橄榄石)
                if (Di >= 4.0 * deficit)
                {
                    Cs += 1.5 * deficit;
                    Ol += 1.5 * deficit;
                    Di -= 4.0 * deficit;
                    stepLog.Add(new NiggliMessage("niggli_step7_di_cs_ol_done", $"去硅反应 Step 7: 透辉石 -> 硅钙石 + 橄榄石 (Di → Cs + Ol)，消耗 Di {4.0 * deficit:F2}%，完全消除硅亏损", $"{4.0 * deficit:F2}"));
                    deficit = 0;
                }
                else if (Di > NiggliConstants.Epsilon)
                {
                    Cs += 0.375 * Di;
                    Ol += 0.375 * Di;
                    deficit -= 0.25 * Di;
                    stepLog.Add(new NiggliMessage("niggli_step7_di_cs_ol_part", $"去硅反应 Step 7: 透辉石 -> 硅钙石 + 橄榄石 (Di → Cs + Ol)，全部 Di 转化完毕"));
                    Di = 0;
                }
            }

            public Dictionary<string, double> ExtractMinerals()
            {
                Plag = Ab + An;

                var mins = new Dictionary<string, double>
                {
                    ["Q"]    = Qtz,
                    ["C"]    = Cor,
                    ["Or"]   = Ort,
                    ["Plag"] = Plag,
                    ["Ab"]   = Ab,
                    ["An"]   = An,
                    ["Lc"]   = Lc,
                    ["Ne"]   = Ne,
                    ["Kp"]   = Kp,
                    ["Ac"]   = Ac,
                    ["Ns"]   = Ns,
                    ["Ks"]   = Ks,
                    ["Di"]   = Di,
                    ["Hy"]   = Hy,
                    ["Ol"]   = Ol,
                    ["Cs"]   = Cs,
                    ["Wo"]   = Wo,
                    ["Mt"]   = Mt,
                    ["Hm"]   = Hm,
                    ["Il"]   = Il,
                    ["Tn"]   = Tn,
                    ["Pf"]   = Pf,
                    ["Ru"]   = Ru,
                    ["Ap"]   = Ap,
                    ["Fr"]   = Fr,
                    ["Py"]   = Py,
                    ["Cc"]   = Cc
                };

                // 清理极微量浮点误差
                foreach (var k in mins.Keys.ToList())
                {
                    if (mins[k] < NiggliConstants.Epsilon)
                    {
                        mins[k] = 0.0;
                    }
                    else
                    {
                        mins[k] = Math.Round(mins[k], 4);
                    }
                }

                return mins;
            }

            public NiggliEndmemberCompositions BuildEndmembers()
            {
                var end = new NiggliEndmemberCompositions
                {
                    MagnesiumRatio = Math.Round(MagnesiumRatio, 2)
                };

                if (Di > NiggliConstants.Epsilon)
                {
                    end.WollastonitePercent = 50.00;
                    end.EnstatitePercent = Math.Round(MagnesiumRatio / 2.0, 2);
                    end.FerrosilitePercent = Math.Round(50.00 - end.EnstatitePercent, 2);
                }

                if (Ol > NiggliConstants.Epsilon)
                {
                    end.ForsteritePercent = Math.Round(MagnesiumRatio, 2);
                    end.FayalitePercent = Math.Round(100.00 - end.ForsteritePercent, 2);
                }

                double totalPlag = Ab + An;
                if (totalPlag > NiggliConstants.Epsilon)
                {
                    end.AnorthitePercent = Math.Round(100.0 * An / totalPlag, 2);
                }

                return end;
            }

            public double CalculateSum()
            {
                // 注意：在 Niggli 体系中，Plag = Ab + An，因此求和时用 Plag（排除 Ab, An），
                // 且不重复相加 Wo%、En%、Fs% 等比值，确保总和反映实际标准矿物阳离子百分比。
                double sum = Qtz + Cor + Ort + Plag + Lc + Ne + Kp + Ac + Ns + Ks +
                             Hy + Di + Ol + Cs + Wo + Mt + Hm + Il + Tn + Pf + Ru +
                             Ap + Fr + Py + Cc;
                return Math.Round(sum, 2);
            }
        }
    }
}
