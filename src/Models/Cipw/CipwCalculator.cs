using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.Models.Cipw
{
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
        public string SilicaSaturation { get; set; }

        /// <summary>
        /// 铝饱和状态 (peralkaline / metaluminous / peraluminous)
        /// </summary>
        public string AluminaState { get; set; }

        /// <summary>
        /// 质量平衡误差
        /// </summary>
        public double MassBalanceError { get; set; }

        /// <summary>
        /// 质量总和
        /// </summary>
        public double TotalMassSum { get; set; }

        /// <summary>
        /// 是否有质量平衡警告
        /// </summary>
        public bool MassBalanceWarning { get; set; }

        /// <summary>
        /// 铁的处理模式
        /// </summary>
        public string IronMode { get; set; }

        /// <summary>
        /// 警告信息列表
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// 计算是否成功
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// 错误信息（计算失败时）
        /// </summary>
        public string ErrorMessage { get; set; }
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
                    throw new InvalidOperationException($"负值氧化物 {key}: {_oxides[key]}");

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
                    throw new InvalidOperationException($"负值氧化物 {key}: {_oxides[key]}");
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
                double volatileFractionMoles = 0.0;

                // 形成挥发分矿物
                volatileFractionMoles = FormVolatiles(state);
                state.Clip();

                // 形成副矿物
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
                double massErr = Math.Abs(100.0 - actualSum);

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
                throw new ArgumentException("氧化物总量必须大于0");

            return clean.ToDictionary(kv => kv.Key, kv => 100.0 * kv.Value / total);
        }

        /// <summary>
        /// 铁的配分处理
        /// </summary>
        private static (Dictionary<string, double> oxides, string ironMode, List<string> warnings) HandleIron(
            Dictionary<string, double> oxides, double fe3Fraction, bool strict)
        {
            if (fe3Fraction < 0.0 || fe3Fraction > 1.0)
                throw new ArgumentException("Fe3+比值必须在0到1之间");

            var ox = new Dictionary<string, double>(oxides);
            string ironMode = null;
            var warnings = new List<string>();

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
                warnings.Add("部分铁配分数据缺失（仅有FeO或Fe2O3之一）");
            }
            else if (hasFeot && !hasFeo && !hasFe2o3)
            {
                double feTotal = ox["FeOT"];
                ox.Remove("FeOT");
                ox["Fe2O3"] = feTotal * fe3Fraction * 1.11134;
                ox["FeO"] = feTotal * (1.0 - fe3Fraction);
                ironMode = "estimated_from_FeOT";
                warnings.Add($"使用固定Fe3+/Fe比值({fe3Fraction:F2})从FeOT估算铁配分");
            }
            else if (hasFeot && (hasFeo || hasFe2o3))
            {
                string msg = "铁输入不一致：同时提供了FeOT和FeO/Fe2O3";
                ironMode = "inconsistent_input";
                warnings.Add(msg);
                if (strict)
                    throw new ArgumentException(msg);
                ox.Remove("FeOT");
            }
            else
            {
                if (!ox.ContainsKey("FeO")) ox["FeO"] = 0.0;
                if (!ox.ContainsKey("Fe2O3")) ox["Fe2O3"] = 0.0;
                ironMode = "missing";
                warnings.Add("未提供铁数据");
            }

            // MnO转换为FeO当量
            if (ox.ContainsKey("MnO") && ox["MnO"] > 0)
            {
                double mnWt = ox["MnO"];
                ox.Remove("MnO");
                double feoEquivalent = mnWt * (CipwConstants.MolarMass["FeO"] / CipwConstants.MolarMass["MnO"]);
                ox["FeO"] = (ox.ContainsKey("FeO") ? ox["FeO"] : 0.0) + feoEquivalent;
                warnings.Add("MnO已按摩尔比转换为FeO当量");
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
                        throw new ArgumentException($"氧化物 {kv.Key} 的含量为负值");
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
        /// 形成副矿物
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

            // 磷灰石 (Ap)
            double ap = Math.Min(state.Get("P2O5"), state.Get("CaO") / (10.0 / 3.0));
            if (ap > 0)
            {
                state.Consume(("P2O5", ap), ("CaO", ap * (10.0 / 3.0)));
                state.Produce("Ap", ap);
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

            // 榍石 (Tn)
            double tn = Math.Min(Math.Min(state.Get("TiO2"), state.Get("CaO")), state.Get("SiO2"));
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

            // 正长石 -> 白榴石
            if (deficit > 0)
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

            // 白榴石 -> 假白榴石
            if (deficit > 0)
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

            // 钠长石 -> 霞石
            if (deficit > 0)
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

            state.Oxides["SiO2"] = 0.0;
            state.CleanMinerals();
            return ("undersaturated", Math.Max(0.0, deficit));
        }
    }
}
