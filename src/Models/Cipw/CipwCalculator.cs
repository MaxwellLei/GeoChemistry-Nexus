using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.Models.Cipw
{
    /// <summary>
    /// CIPW calculation message that can be localized by the presentation layer.
    /// </summary>
    public class CipwMessage
    {
        public string Key { get; set; } = string.Empty;
        public object[] Args { get; set; } = Array.Empty<object>();

        public CipwMessage(string key, params object[] args)
        {
            Key = key;
            Args = args;
        }
    }

    /// <summary>
    /// CIPW标准矿物计算结果
    /// </summary>
    public class CipwResult
    {
        /// <summary>
        /// 标准矿物摩尔数
        /// </summary>
        public Dictionary<string, double> MineralsMoles { get; set; } = new();

        /// <summary>
        /// 标准矿物质量百分比
        /// </summary>
        public Dictionary<string, double> MineralsWtPercent { get; set; } = new();

        /// <summary>
        /// 硅饱和状态 (oversaturated / saturated / undersaturated)
        /// </summary>
        public string SilicaSaturation { get; set; } = string.Empty;

        /// <summary>
        /// 铝饱和状态 (peralkaline / metaluminous / peraluminous)
        /// </summary>
        public string AluminaState { get; set; } = string.Empty;

        /// <summary>
        /// 质量平衡误差（wt%）：|100 − (标准矿物质量和 + Py/Fl/Hl 形成时释放的氧)|
        /// </summary>
        public double MassBalanceError { get; set; }

        /// <summary>
        /// 标准矿物质量百分比总和（不含 Py/Fl/Hl 形成时释放的氧）
        /// </summary>
        public double TotalMassSum { get; set; }

        /// <summary>
        /// 是否有质量平衡警告
        /// </summary>
        public bool MassBalanceWarning { get; set; }

        /// <summary>
        /// 铁的处理模式
        /// </summary>
        public string IronMode { get; set; } = string.Empty;

        /// <summary>
        /// 警告信息列表
        /// </summary>
        public List<CipwMessage> Warnings { get; set; } = new();

        /// <summary>
        /// 计算是否成功
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// 错误信息（计算失败时）
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 错误信息资源键（计算失败时）
        /// </summary>
        public string ErrorMessageKey { get; set; } = string.Empty;

        /// <summary>
        /// 错误信息格式化参数（计算失败时）
        /// </summary>
        public object[] ErrorMessageArgs { get; set; } = Array.Empty<object>();
    }

    internal class CipwCalculationException : Exception
    {
        public string ResourceKey { get; }
        public object[] Args { get; }

        public CipwCalculationException(string resourceKey, params object[] args)
            : base(resourceKey)
        {
            ResourceKey = resourceKey;
            Args = args;
        }
    }

    /// <summary>
    /// CIPW标准矿物计算内部化学状态
    /// </summary>
    internal class ChemicalState
    {
        private readonly Dictionary<string, double> _oxides;
        private readonly Dictionary<string, double> _minerals;

        public ChemicalState(Dictionary<string, double> oxides)
        {
            _oxides = new Dictionary<string, double>(oxides);
            _minerals = new Dictionary<string, double>();
        }

        public Dictionary<string, double> Minerals => _minerals;
        public Dictionary<string, double> Oxides => _oxides;

        public double Get(string key)
        {
            return _oxides.TryGetValue(key, out double value) ? value : 0.0;
        }

        public void Consume(params (string key, double value)[] items)
        {
            foreach (var (key, value) in items)
            {
                _oxides[key] = Get(key) - value;

                if (key != "SiO2" && _oxides[key] < -CipwConstants.EPS)
                    throw new CipwCalculationException("cipw_error_negative_oxide_with_value", key, _oxides[key]);

                if (Math.Abs(_oxides[key]) < CipwConstants.EPS)
                    _oxides[key] = 0.0;
            }
        }

        public void Produce(string mineral, double amount)
        {
            if (amount > CipwConstants.EPS)
            {
                if (_minerals.ContainsKey(mineral))
                    _minerals[mineral] += amount;
                else
                    _minerals[mineral] = amount;
            }
        }

        public void Clip()
        {
            var keys = _oxides.Keys.ToList();
            foreach (var key in keys)
            {
                if (Math.Abs(_oxides[key]) < CipwConstants.EPS)
                    _oxides[key] = 0.0;
                else if (key != "SiO2" && _oxides[key] < 0)
                    throw new CipwCalculationException("cipw_error_negative_oxide_with_value", key, _oxides[key]);
            }
        }

        public void CleanMinerals()
        {
            var toRemove = _minerals.Where(kv => kv.Value <= CipwConstants.EPS).Select(kv => kv.Key).ToList();
            foreach (var key in toRemove)
                _minerals.Remove(key);
        }
    }

    /// <summary>
    /// CIPW标准矿物计算器
    /// </summary>
    public static class CipwCalculator
    {
        /// <summary>
        /// 执行CIPW标准矿物计算
        /// </summary>
        /// <param name="oxides">输入氧化物含量 (wt%)</param>
        /// <param name="fe3Fraction">Fe3+/Fe总 比值</param>
        /// <param name="strict">严格模式（铁不一致时抛异常）</param>
        /// <returns>CIPW计算结果</returns>
        public static CipwResult Calculate(Dictionary<string, double> oxides, double fe3Fraction = 0.15, bool strict = false)
        {
            try
            {
                // 无水归一化
                var ox = NormalizeAnhydrous(oxides);

                // 铁的处理
                var (processedOx, ironMode, ironWarnings) = HandleIron(ox, fe3Fraction, strict);

                // 再次归一化
                ox = NormalizeAnhydrous(processedOx);

                // 转换为摩尔数
                var moles = WtPercentToMoles(ox);
                foreach (var oxide in CipwConstants.TrackedOxides)
                {
                    if (!moles.ContainsKey(oxide))
                        moles[oxide] = 0.0;
                }

                var state = new ChemicalState(moles);

                // 形成磷灰石（标准流程中最先分配 CaO）
                FormApatite(state);
                state.Clip();

                // 形成挥发分矿物
                FormVolatiles(state);
                state.Clip();

                // 形成副矿物（锆石、铬铁矿、钛铁矿）
                FormAccessories(state);
                state.Clip();

                // 判断铝饱和状态
                double alAvailable = state.Get("Al2O3");
                double naAvailable = state.Get("Na2O");
                double kAvailable = state.Get("K2O");
                double caAvailable = state.Get("CaO");

                double aCnk = alAvailable / (caAvailable + naAvailable + kAvailable + CipwConstants.EPS);
                string aluminaState;

                if ((naAvailable + kAvailable) > alAvailable)
                    aluminaState = "peralkaline";
                else if (aCnk > 1.0)
                    aluminaState = "peraluminous";
                else
                    aluminaState = "metaluminous";

                // 形成长石及残余碱
                FormFeldsparsAndResidualAlkalis(state);
                state.Clip();

                // 形成榍石与金红石（榍石仅使用钙长石分配后剩余的 CaO）
                FormTitaniteAndRutile(state);
                state.Clip();

                // 形成铁氧化物
                FormIronOxides(state);
                state.Clip();

                // 形成暗色硅酸盐矿物
                FormMaficSilicates(state);
                state.Clip();

                // 硅不饱和校正
                var (silicaSaturation, residualDeficit) = SilicaDesaturation(state);
                state.Clip();
                state.CleanMinerals();

                if (residualDeficit > CipwConstants.EPS)
                {
                    ironWarnings.Add(new CipwMessage("cipw_warning_silica_deficit_residual", $"{residualDeficit:F4}"));
                }

                // 计算质量百分比
                var wt = new Dictionary<string, double>();
                foreach (var kv in state.Minerals)
                {
                    if (CipwConstants.MineralMolarMass.ContainsKey(kv.Key) && kv.Value > 0)
                    {
                        wt[kv.Key] = kv.Value * CipwConstants.MineralMolarMass[kv.Key];
                    }
                }

                double totalInputMass = ox.Values.Sum();
                var wtPct = new Dictionary<string, double>();
                foreach (var kv in wt)
                {
                    wtPct[kv.Key] = 100.0 * kv.Value / totalInputMass;
                }

                double actualSum = wtPct.Values.Sum();

                // Py / Fl / Hl 形成时 S、F、Cl 置换了氧化物中的氧（FeO→FeS2、CaO→CaF2、Na2O→2NaCl），
                // 被释放的氧不进入任何标准矿物，质量平衡检验时需计入
                double oxygenReleasedMoles =
                    state.Minerals.GetValueOrDefault("Fl", 0.0) +
                    state.Minerals.GetValueOrDefault("Py", 0.0) +
                    state.Minerals.GetValueOrDefault("Hl", 0.0) / 2.0;
                double oxygenReleasedPct = 100.0 * oxygenReleasedMoles * CipwConstants.OxygenMolarMass / totalInputMass;

                double massErr = Math.Abs(100.0 - (actualSum + oxygenReleasedPct));

                return new CipwResult
                {
                    MineralsMoles = new Dictionary<string, double>(state.Minerals),
                    MineralsWtPercent = wtPct,
                    SilicaSaturation = silicaSaturation,
                    AluminaState = aluminaState,
                    MassBalanceError = massErr,
                    TotalMassSum = actualSum,
                    MassBalanceWarning = massErr > 1e-6,
                    IronMode = ironMode,
                    Warnings = ironWarnings,
                    Success = true
                };
            }
            catch (CipwCalculationException ex)
            {
                return new CipwResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ErrorMessageKey = ex.ResourceKey,
                    ErrorMessageArgs = ex.Args
                };
            }
            catch (Exception ex)
            {
                return new CipwResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 无水归一化到100%
        /// </summary>
        private static Dictionary<string, double> NormalizeAnhydrous(Dictionary<string, double> oxides)
        {
            var clean = oxides.Where(kv => kv.Value >= 0).ToDictionary(kv => kv.Key, kv => kv.Value);
            double total = clean.Values.Sum();

            if (total <= 0)
                throw new CipwCalculationException("cipw_error_oxide_total_positive");

            return clean.ToDictionary(kv => kv.Key, kv => 100.0 * kv.Value / total);
        }

        /// <summary>
        /// 铁的配分处理
        /// </summary>
        private static (Dictionary<string, double> oxides, string ironMode, List<CipwMessage> warnings) HandleIron(
            Dictionary<string, double> oxides, double fe3Fraction, bool strict)
        {
            if (fe3Fraction < 0.0 || fe3Fraction > 1.0)
                throw new CipwCalculationException("cipw_error_fe3_fraction_range");

            var ox = new Dictionary<string, double>(oxides);
            string ironMode;
            var warnings = new List<CipwMessage>();

            bool hasFeo = ox.ContainsKey("FeO") && ox["FeO"] > 0;
            bool hasFe2o3 = ox.ContainsKey("Fe2O3") && ox["Fe2O3"] > 0;
            bool hasFeot = ox.ContainsKey("FeOT") && ox["FeOT"] > 0;

            if (hasFeo && hasFe2o3 && !hasFeot)
            {
                ironMode = "measured";
            }
            else if ((hasFeo ^ hasFe2o3) && !hasFeot)
            {
                if (!ox.ContainsKey("FeO")) ox["FeO"] = 0.0;
                if (!ox.ContainsKey("Fe2O3")) ox["Fe2O3"] = 0.0;
                ironMode = "partial_assumed";
                warnings.Add(new CipwMessage("cipw_warning_partial_iron_missing"));
            }
            else if (hasFeot && !hasFeo && !hasFe2o3)
            {
                double feTotal = ox["FeOT"];
                ox.Remove("FeOT");
                ox["Fe2O3"] = feTotal * fe3Fraction * 1.11134;
                ox["FeO"] = feTotal * (1.0 - fe3Fraction);
                ironMode = "estimated_from_FeOT";
                warnings.Add(new CipwMessage("cipw_warning_feot_estimated", fe3Fraction));
            }
            else if (hasFeot && (hasFeo || hasFe2o3))
            {
                ironMode = "inconsistent_input";
                warnings.Add(new CipwMessage("cipw_warning_iron_inconsistent"));
                if (strict)
                    throw new CipwCalculationException("cipw_warning_iron_inconsistent");
                ox.Remove("FeOT");
            }
            else
            {
                if (!ox.ContainsKey("FeO")) ox["FeO"] = 0.0;
                if (!ox.ContainsKey("Fe2O3")) ox["Fe2O3"] = 0.0;
                ironMode = "missing";
                warnings.Add(new CipwMessage("cipw_warning_iron_missing"));
            }

            // MnO转换为FeO当量
            if (ox.ContainsKey("MnO") && ox["MnO"] > 0)
            {
                double mnWt = ox["MnO"];
                ox.Remove("MnO");
                double feoEquivalent = mnWt * (CipwConstants.MolarMass["FeO"] / CipwConstants.MolarMass["MnO"]);
                ox["FeO"] = (ox.ContainsKey("FeO") ? ox["FeO"] : 0.0) + feoEquivalent;
                warnings.Add(new CipwMessage("cipw_warning_mno_to_feo"));
            }

            return (ox, ironMode, warnings);
        }

        /// <summary>
        /// 将氧化物质量百分比转换为摩尔数
        /// </summary>
        private static Dictionary<string, double> WtPercentToMoles(Dictionary<string, double> oxides)
        {
            var moles = new Dictionary<string, double>();
            foreach (var kv in oxides)
            {
                if (CipwConstants.MolarMass.ContainsKey(kv.Key))
                {
                    if (kv.Value < 0)
                        throw new CipwCalculationException("cipw_error_negative_oxide_content", kv.Key);
                    moles[kv.Key] = kv.Value / CipwConstants.MolarMass[kv.Key];
                }
            }
            return moles;
        }

        /// <summary>
        /// 形成挥发分矿物
        /// </summary>
        private static double FormVolatiles(ChemicalState state)
        {
            double removed = 0.0;

            // 方解石 (Cc)
            double co2 = state.Get("CO2");
            if (co2 > 0)
            {
                double cc = Math.Min(co2, state.Get("CaO"));
                state.Consume(("CO2", cc), ("CaO", cc));
                state.Produce("Cc", cc);
                removed += cc;
            }

            // 萤石 (Fl)
            double fluorine = state.Get("F");
            if (fluorine > 0)
            {
                double fl = Math.Min(fluorine / 2.0, state.Get("CaO"));
                state.Consume(("F", fl * 2.0), ("CaO", fl));
                state.Produce("Fl", fl);
                removed += fl;
            }

            // 黄铁矿 (Py)
            double sulfur = state.Get("S");
            if (sulfur > 0)
            {
                double py = Math.Min(sulfur / 2.0, state.Get("FeO"));
                state.Consume(("S", py * 2.0), ("FeO", py));
                state.Produce("Py", py);
                removed += py;
            }

            // 石盐 (Hl)
            double chlorine = state.Get("Cl");
            if (chlorine > 0)
            {
                double hl = Math.Min(chlorine, state.Get("Na2O") * 2.0);
                state.Consume(("Cl", hl), ("Na2O", hl / 2.0));
                state.Produce("Hl", hl);
                removed += hl;
            }

            // 芒硝 (Th)
            double so3 = state.Get("SO3");
            if (so3 > 0)
            {
                double th = Math.Min(so3, state.Get("Na2O"));
                state.Consume(("SO3", th), ("Na2O", th));
                state.Produce("Th", th);
                removed += th;
            }

            return removed;
        }

        /// <summary>
        /// 形成磷灰石 (Ap)。1 mol P2O5 结合 10/3 mol CaO（不含 OH/F）。
        /// </summary>
        private static void FormApatite(ChemicalState state)
        {
            double ap = Math.Min(state.Get("P2O5"), state.Get("CaO") / (10.0 / 3.0));
            if (ap > 0)
            {
                state.Consume(("P2O5", ap), ("CaO", ap * (10.0 / 3.0)));
                state.Produce("Ap", ap);
            }
        }

        /// <summary>
        /// 形成副矿物（锆石、铬铁矿、钛铁矿）
        /// </summary>
        private static void FormAccessories(ChemicalState state)
        {
            // 锆石 (Z)
            double z = state.Get("ZrO2");
            if (z > 0)
            {
                state.Consume(("ZrO2", z), ("SiO2", z));
                state.Produce("Z", z);
            }

            // 铬铁矿 (Cm)
            double cm = Math.Min(state.Get("Cr2O3"), state.Get("FeO"));
            if (cm > 0)
            {
                state.Consume(("Cr2O3", cm), ("FeO", cm));
                state.Produce("Cm", cm);
            }

            // 钛铁矿 (Ilm)
            double ilm = Math.Min(state.Get("TiO2"), state.Get("FeO"));
            if (ilm > 0)
            {
                state.Consume(("TiO2", ilm), ("FeO", ilm));
                state.Produce("Ilm", ilm);
            }
        }

        /// <summary>
        /// 形成榍石与金红石。
        /// 标准 CIPW 流程中，TiO2 先与 FeO 形成钛铁矿；仍有剩余时，榍石 (Tn) 只能使用
        /// 钙长石分配之后剩余的 CaO，最后仍剩余的 TiO2 记为金红石 (Ru)。
        /// 此时 SiO2 账户可能为负（硅亏损），由后续去硅校正（Tn→Pf 等）统一处理，故不以 SiO2 余量限制 Tn。
        /// </summary>
        private static void FormTitaniteAndRutile(ChemicalState state)
        {
            // 榍石 (Tn)
            double tn = Math.Min(state.Get("TiO2"), state.Get("CaO"));
            if (tn > 0)
            {
                state.Consume(("TiO2", tn), ("CaO", tn), ("SiO2", tn));
                state.Produce("Tn", tn);
            }

            // 金红石 (Ru)
            double ru = state.Get("TiO2");
            if (ru > 0)
            {
                state.Consume(("TiO2", ru));
                state.Produce("Ru", ru);
            }
        }

        /// <summary>
        /// 形成铁氧化物
        /// </summary>
        private static void FormIronOxides(ChemicalState state)
        {
            // 磁铁矿 (Mt)
            double mt = Math.Min(state.Get("Fe2O3"), state.Get("FeO"));
            if (mt > 0)
            {
                state.Consume(("Fe2O3", mt), ("FeO", mt));
                state.Produce("Mt", mt);
            }

            // 赤铁矿 (Hm)
            double hm = state.Get("Fe2O3");
            if (hm > 0)
            {
                state.Consume(("Fe2O3", hm));
                state.Produce("Hm", hm);
            }
        }

        /// <summary>
        /// 形成长石及残余碱金属硅酸盐
        /// </summary>
        private static void FormFeldsparsAndResidualAlkalis(ChemicalState state)
        {
            // 正长石 (Or)
            double ort = Math.Min(state.Get("K2O"), state.Get("Al2O3"));
            if (ort > 0)
            {
                state.Consume(("K2O", ort), ("Al2O3", ort), ("SiO2", 6.0 * ort));
                state.Produce("Or", 2.0 * ort);
            }

            // 钠长石 (Ab)
            double ab = Math.Min(state.Get("Na2O"), state.Get("Al2O3"));
            if (ab > 0)
            {
                state.Consume(("Na2O", ab), ("Al2O3", ab), ("SiO2", 6.0 * ab));
                state.Produce("Ab", 2.0 * ab);
            }

            // 钙长石 (An)
            double an = Math.Min(state.Get("CaO"), state.Get("Al2O3"));
            if (an > 0)
            {
                state.Consume(("CaO", an), ("Al2O3", an), ("SiO2", 2.0 * an));
                state.Produce("An", an);
            }

            // 刚玉 (Cor)
            double cor = state.Get("Al2O3");
            if (cor > 0)
            {
                state.Consume(("Al2O3", cor));
                state.Produce("Cor", cor);
            }

            // 霓石 (Ac)
            double ac = Math.Min(state.Get("Na2O"), state.Get("Fe2O3"));
            if (ac > 0)
            {
                state.Consume(("Na2O", ac), ("Fe2O3", ac), ("SiO2", 4.0 * ac));
                state.Produce("Ac", 2.0 * ac);
            }

            // 残余钠 (ns)
            double naResidual = state.Get("Na2O");
            if (naResidual > 0)
            {
                state.Consume(("Na2O", naResidual), ("SiO2", naResidual));
                state.Produce("ns", naResidual);
            }

            // 残余钾 (ks)
            double kResidual = state.Get("K2O");
            if (kResidual > 0)
            {
                state.Consume(("K2O", kResidual), ("SiO2", kResidual));
                state.Produce("ks", kResidual);
            }
        }

        /// <summary>
        /// 形成暗色硅酸盐矿物
        /// </summary>
        private static void FormMaficSilicates(ChemicalState state)
        {
            double mgAvailable = state.Get("MgO");
            double feAvailable = state.Get("FeO");
            double mgfe = mgAvailable + feAvailable;

            if (mgfe > 0)
            {
                double cpxTotal = Math.Min(state.Get("CaO"), mgfe);
                double mgRatio = mgAvailable / (mgfe + CipwConstants.EPS);
                double feRatio = feAvailable / (mgfe + CipwConstants.EPS);

                double di = cpxTotal * mgRatio;
                double hd = cpxTotal * feRatio;

                if (di > 0 || hd > 0)
                {
                    state.Consume(("CaO", cpxTotal), ("MgO", di), ("FeO", hd), ("SiO2", 2.0 * cpxTotal));
                    state.Produce("Di", di);
                    state.Produce("Hd", hd);
                }
            }

            double mgLeft = state.Get("MgO");
            double feLeft = state.Get("FeO");
            double hy = mgLeft + feLeft;

            if (hy > 0)
            {
                state.Consume(("MgO", mgLeft), ("FeO", feLeft), ("SiO2", hy));
                state.Produce("En", mgLeft);
                state.Produce("Fs", feLeft);
            }

            double caLeft = state.Get("CaO");
            if (caLeft > 0 && (state.Get("MgO") + state.Get("FeO")) < CipwConstants.EPS)
            {
                state.Consume(("CaO", caLeft), ("SiO2", caLeft));
                state.Produce("Wo", caLeft);
            }
        }

        /// <summary>
        /// 硅不饱和校正
        /// </summary>
        private static (string silicaSaturation, double residualDeficit) SilicaDesaturation(ChemicalState state)
        {
            double deficit = -state.Get("SiO2");

            if (Math.Abs(deficit) < CipwConstants.EPS)
            {
                state.Oxides["SiO2"] = 0.0;
                return ("saturated", 0.0);
            }

            if (deficit < 0)
            {
                double quartz = -deficit;
                state.Produce("Q", quartz);
                state.Oxides["SiO2"] = 0.0;
                return ("oversaturated", 0.0);
            }

            // 硅不饱和处理
            // 紫苏辉石 -> 橄榄石
            double enAmount = state.Minerals.GetValueOrDefault("En", 0.0);
            double fsAmount = state.Minerals.GetValueOrDefault("Fs", 0.0);
            double hyTotal = enAmount + fsAmount;

            // 紫苏辉石 -> 橄榄石: 2(M)SiO3 -> M2SiO4 + SiO2（每 2 Hy 释放 1 SiO2）
            if (hyTotal > 0)
            {
                double reducibleHy = Math.Min(hyTotal, 2.0 * deficit);
                double enShare = enAmount / hyTotal;
                double fsShare = fsAmount / hyTotal;

                double enReduced = reducibleHy * enShare;
                double fsReduced = reducibleHy * fsShare;

                state.Minerals["En"] = Math.Max(0.0, enAmount - enReduced);
                state.Minerals["Fs"] = Math.Max(0.0, fsAmount - fsReduced);
                state.Produce("Fo", enReduced / 2.0);
                state.Produce("Fa", fsReduced / 2.0);

                deficit -= reducibleHy / 2.0;
            }

            // 标准去硅顺序（Kelsey 1965; Verma et al. 2003; Phillpots & Ague 2009）：
            // Hy→Ol → Tn→Pf → Ab→Ne → Or→Lc → Wo→Cs → Di→Cs+Ol → Lc→Kp
            // 各矿物均以化学式单位计量，每步按反应式释放 SiO2 并扣减硅亏损。

            // 榍石 -> 钙钛矿: CaTiSiO5 -> CaTiO3 + SiO2（每 1 Tn 释放 1 SiO2）
            if (deficit > CipwConstants.EPS)
            {
                double tnAmount = state.Minerals.GetValueOrDefault("Tn", 0.0);
                double reducibleTn = Math.Min(tnAmount, deficit);
                if (reducibleTn > 0)
                {
                    state.Minerals["Tn"] = Math.Max(0.0, tnAmount - reducibleTn);
                    state.Produce("Pf", reducibleTn);
                    deficit -= reducibleTn;
                }
            }

            // 钠长石 -> 霞石: NaAlSi3O8 -> NaAlSiO4 + 2SiO2（每 1 Ab 释放 2 SiO2）
            if (deficit > CipwConstants.EPS)
            {
                double abAmount = state.Minerals.GetValueOrDefault("Ab", 0.0);
                double reducibleAb = Math.Min(abAmount, deficit / 2.0);
                if (reducibleAb > 0)
                {
                    state.Minerals["Ab"] = Math.Max(0.0, abAmount - reducibleAb);
                    state.Produce("Ne", reducibleAb);
                    deficit -= 2.0 * reducibleAb;
                }
            }

            // 正长石 -> 白榴石: KAlSi3O8 -> KAlSi2O6 + SiO2（每 1 Or 释放 1 SiO2）
            if (deficit > CipwConstants.EPS)
            {
                double orAmount = state.Minerals.GetValueOrDefault("Or", 0.0);
                double reducibleOr = Math.Min(orAmount, deficit);
                if (reducibleOr > 0)
                {
                    state.Minerals["Or"] = Math.Max(0.0, orAmount - reducibleOr);
                    state.Produce("Le", reducibleOr);
                    deficit -= reducibleOr;
                }
            }

            // 硅灰石 -> 斜硅钙石: 2CaSiO3 -> Ca2SiO4 + SiO2（每 2 Wo 释放 1 SiO2）
            if (deficit > CipwConstants.EPS)
            {
                double woAmount = state.Minerals.GetValueOrDefault("Wo", 0.0);
                double reducibleWo = Math.Min(woAmount, 2.0 * deficit);
                if (reducibleWo > 0)
                {
                    state.Minerals["Wo"] = Math.Max(0.0, woAmount - reducibleWo);
                    state.Produce("Cs", reducibleWo / 2.0);
                    deficit -= reducibleWo / 2.0;
                }
            }

            // 透辉石 -> 斜硅钙石 + 橄榄石: 2CaMSi2O6 -> Ca2SiO4 + M2SiO4 + 2SiO2（每 1 Di 释放 1 SiO2）
            // Di 与 Hd 按各自比例转化，Mg/Fe 分别进入 Fo/Fa
            if (deficit > CipwConstants.EPS)
            {
                double diAmount = state.Minerals.GetValueOrDefault("Di", 0.0);
                double hdAmount = state.Minerals.GetValueOrDefault("Hd", 0.0);
                double cpxTotal = diAmount + hdAmount;
                if (cpxTotal > 0)
                {
                    double reducibleCpx = Math.Min(cpxTotal, deficit);
                    double diReduced = reducibleCpx * diAmount / cpxTotal;
                    double hdReduced = reducibleCpx * hdAmount / cpxTotal;

                    state.Minerals["Di"] = Math.Max(0.0, diAmount - diReduced);
                    state.Minerals["Hd"] = Math.Max(0.0, hdAmount - hdReduced);
                    state.Produce("Cs", reducibleCpx / 2.0);
                    state.Produce("Fo", diReduced / 2.0);
                    state.Produce("Fa", hdReduced / 2.0);
                    deficit -= reducibleCpx;
                }
            }

            // 白榴石 -> 钾霞石: KAlSi2O6 -> KAlSiO4 + SiO2（每 1 Lc 释放 1 SiO2）
            if (deficit > CipwConstants.EPS)
            {
                double leAmount = state.Minerals.GetValueOrDefault("Le", 0.0);
                double reducibleLe = Math.Min(leAmount, deficit);
                if (reducibleLe > 0)
                {
                    state.Minerals["Le"] = Math.Max(0.0, leAmount - reducibleLe);
                    state.Produce("Kp", reducibleLe);
                    deficit -= reducibleLe;
                }
            }

            state.Oxides["SiO2"] = 0.0;
            state.CleanMinerals();
            return ("undersaturated", Math.Max(0.0, deficit));
        }
    }
}
