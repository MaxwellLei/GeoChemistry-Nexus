using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SkiaSharp.HarfBuzz.SKShaper;
using unvell.ReoGrid;
using unvell.ReoGrid.Formula;
using HandyControl.Tools.Extension;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 自定义函数
    /// </summary>
    public static class CustomizeFuncHelper
    {
        public static void RegisterAllFunctions()
        {
            // 锆石 Ti 温度计算，主量
            // Loucks et al. (2020)
            FormulaExtension.CustomFunctions["Zircon_Ti_Loucks_2020"] = (cell, args) =>
            {
                // 检查参数数量
                if (args.Length < 4)
                {
                    return LanguageService.Instance["missing_parameters"];
                }

                try
                {
                    // 计算各种化合物的原子质量
                    double ti = Convert.ToDouble(args[0]);
                    double p = Convert.ToDouble(args[1]);
                    double aTiO2 = Convert.ToDouble(args[2]);
                    double aSiO2 = Convert.ToDouble(args[3]);

                    // 计算温度，单位 K
                    double temperature = ((-4800 + (0.4748 * (p - 1000))) / (Math.Log10(ti) - 5.711 - Math.Log10(aTiO2) + Math.Log10(aSiO2)));

                    return temperature;
                }
                catch
                {
                    return null;
                }
            };

            // 锆石 Zr 温度计算，主量
            // Watson and Harrison (1983)
            FormulaExtension.CustomFunctions["Zircon_Zr_Principal_Watson_and_Harrison_1983"] = (cell, args) =>
            {
                // 检查参数数量
                if (args.Length < 10)
                {
                    return LanguageService.Instance["missing_parameters"];
                }

                try
                {
                    // 转换参数为float
                    float[] values = new float[10];
                    for (int i = 0; i < 10; i++)
                    {
                        values[i] = Convert.ToSingle(args[i]);
                    }

                    // 计算各种化合物的原子质量
                    float tsiO2 = ChemicalHelper.CalAtomicMass(values[1], 60.083f, 10000);
                    float tal203 = ChemicalHelper.CalAtomicMass(values[2], 101.961f, 2 * 10000);
                    float tfe2O3 = ChemicalHelper.CalAtomicMass(values[3], 159.687f, 10000);
                    float tfeO = ChemicalHelper.CalAtomicMass(values[4], 71.844f, 10000);
                    float tmgO = ChemicalHelper.CalAtomicMass(values[5], 40.304f, 10000);
                    float tp2O5 = ChemicalHelper.CalAtomicMass(values[6], 141.943f, 2 * 10000);
                    float tcaO = ChemicalHelper.CalAtomicMass(values[7], 56.077f, 10000);
                    float tk2O = ChemicalHelper.CalAtomicMass(values[8], 94.195f, 2 * 10000);
                    float tna2O = ChemicalHelper.CalAtomicMass(values[9], 61.979f, 2 * 10000);

                    // 求和
                    float tempTotal = tsiO2 + tal203 + tfe2O3 + tfeO + tmgO + tp2O5 + tcaO + tk2O + tna2O;

                    // 归一化
                    float normalizedTsiO2 = tsiO2 / tempTotal;
                    float normalizedTal203 = tal203 / tempTotal;
                    float normalizedTcaO = tcaO / tempTotal;
                    float normalizedTk2O = tk2O / tempTotal;
                    float normalizedTna2O = tna2O / tempTotal;

                    // 计算m值
                    float m = (2 * normalizedTcaO + normalizedTk2O + normalizedTna2O) / (normalizedTsiO2 * normalizedTal203);

                    // 计算温度
                    double temperature = 12900 / (Math.Log(496000 / values[0]) + 0.85 * m + 2.95);

                    return temperature;
                }
                catch
                {
                    return null;
                }
            };

            // 锆石 Zr 温度计算，饱和温度
            // Watson and Harrison (1983)
            FormulaExtension.CustomFunctions["Zircon_Zr_Saturation_Watson_and_Harrison_1983"] = (cell, args) =>
            {
                if (args.Length < 6)
                {
                    return LanguageService.Instance["missing_parameters"];
                }

                try
                {
                    // 获取参数
                    float row0 = Convert.ToSingle(args[0]);
                    float row1 = Convert.ToSingle(args[1]);
                    float row2 = Convert.ToSingle(args[2]);
                    float row3 = Convert.ToSingle(args[3]);
                    float row4 = Convert.ToSingle(args[4]);
                    float row5 = Convert.ToSingle(args[5]);

                    // 计算原子质量
                    float tsiO2 = ChemicalHelper.CalAtomicMass(row1, 60.083f);
                    float tal203 = ChemicalHelper.CalAtomicMass(row2, 101.961f, 2);
                    float tcaO = ChemicalHelper.CalAtomicMass(row3, 56.077f);
                    float tk20 = ChemicalHelper.CalAtomicMass(row4, 94.195f, 2);
                    float tna2O = ChemicalHelper.CalAtomicMass(row5, 61.979f, 2);

                    // 计算临时值
                    float tempRsM = (2 * tcaO + tk20 + tna2O) / (tsiO2 * tal203);

                    // 计算开尔文温度
                    float tK = (float)(12900 / (Math.Log(496000 / row0) + 0.85 * tempRsM + 2.95));

                    // 返回温度 K
                    return tK;
                }
                catch
                {
                    return null;
                }
            };

            // 闪锌矿 GGIMFis 温度计算
            // Frenzel et al. (2016)
            FormulaExtension.CustomFunctions["Sphalerite_GGIMFis_Frenzel_2016"] = (cell, args) =>
            {
                if (args.Length < 5)
                {
                    return LanguageService.Instance["missing_parameters"];
                }

                try
                {
                    // 获取参数
                    float ga = Convert.ToSingle(args[0]);
                    float ge = Convert.ToSingle(args[1]);
                    float fe = Convert.ToSingle(args[2]);
                    float mn = Convert.ToSingle(args[3]);
                    float inConcentration = Convert.ToSingle(args[4]);
                    // 计算 PC1*
                    float pc1Star = (float)((Math.Log(ga) * 0.22 + Math.Log(ge) * 0.22) -
                                           Math.Log(fe) * 0.37 - Math.Log(mn) * 0.20 -
                                           Math.Log(inConcentration) * 0.11);
                    // 计算温度 T-K
                    float tK = (float)((-54.4 * pc1Star + 208) + 273.15);
                    return tK;
                }
                catch
                {
                    // HACK: 需要优化提示
                    return null;
                }
            };

            // 闪锌矿 ΔFeS 温度计算
            FormulaExtension.CustomFunctions["Sphalerite_FeS_Scott_and_Barne_1971"] = (cell, args) =>
            {
                if (args.Length < 1)
                {
                    return LanguageService.Instance["missing_parameters"];
                }

                try
                {
                    double deltaMolePercentFeS = (double)args[0];

                    // 检查输入值范围
                    if (deltaMolePercentFeS < 0.0 || deltaMolePercentFeS > 4.4)
                    {
                        // 输入值超出范围
                        return LanguageService.Instance["input_out_of_range"] + "(0.0-4.4)";
                    }

                    // 计算温度
                    const double slope = -39.7727;
                    const double intercept = 525.0;
                    double tK = (slope * deltaMolePercentFeS) + intercept + 273.15;
                    return tK;
                }
                catch
                {
                    return null;
                }
            };

            // 石英 TitaniQ 温度计算
            FormulaExtension.CustomFunctions["Quatz_Ti_Wark_and_Watson_2006"] = (cell, args) =>
            {
                if (args.Length < 2)
                {
                    return LanguageService.Instance["missing_parameters"];
                }

                try
                {
                    // 获取Ti
                    double titaniumConcentration = Convert.ToDouble(args[0]);
                    // 获取TiO2活度参数
                    double titaniumDioxideActivity = Convert.ToDouble(args[1]);
                    // 参数验证
                    if (titaniumConcentration <= 0) 
                        { return "Ti" + LanguageService.Instance["must_be_positive_number"]; }  // Ti 必须是正数
                    if (titaniumDioxideActivity <= 0 || titaniumDioxideActivity > 1) 
                        { return ""; }     // TiO2活度必须在(0,1]范围内

                    // 计算温度 - 基于Wark和Watson (2006)公式
                    double logValue = Math.Log10(titaniumConcentration / titaniumDioxideActivity);
                    double tK = (-3765 / (logValue - 5.69));
                    return tK;
                }
                catch
                {
                    return null;
                }
            };

            // 黑云母 Ti 温度计算
            FormulaExtension.CustomFunctions["Biotite_Ti_Henry_2005"] = (cell, args) =>
            {
                if (args.Length < 2)
                {
                    return LanguageService.Instance["missing_parameters"];
                }

                try
                {

                    // 获取输入参数
                    double ti = Convert.ToDouble(args[0]);
                    double mg = Convert.ToDouble(args[1]);
                    double fe = Convert.ToDouble(args[2]);

                    // 常数定义
                    const double a = -2.3594;
                    const double b = 4.6482e-9;
                    const double c = -1.7283;

                    // 计算温度
                    double xMg = mg / (mg + fe);
                    double numerator = Math.Log(ti) - a - c * Math.Pow(xMg, 3);
                    double tK = Math.Pow(numerator / b, 1.0 / 3.0) + 273.15;

                    return tK;
                }
                catch
                {
                    return null;
                }
            };

            // 角闪石 Si* 温度计算
            FormulaExtension.CustomFunctions["Amphibole_Si_Ridolfi_2010"] = (cell, args) =>
            {
                if (args.Length < 12)
                {
                    return LanguageService.Instance["missing_parameters"];
                }

                try
                {
                    double sio2 = Convert.ToDouble(args[0]);  // SiO2
                    double tio2 = Convert.ToDouble(args[1]);  // TiO2
                    double al2o3 = Convert.ToDouble(args[2]); // Al2O3
                    double cr2o3 = Convert.ToDouble(args[3]); // Cr2O3
                    double feo = Convert.ToDouble(args[4]);   // FeO
                    double mno = Convert.ToDouble(args[5]);   // MnO
                    double mgo = Convert.ToDouble(args[6]);   // MgO
                    double cao = Convert.ToDouble(args[7]);   // CaO
                    double na2o = Convert.ToDouble(args[8]);  // Na2O
                    double k2o = Convert.ToDouble(args[9]);   // K2O
                    double f = Convert.ToDouble(args[10]);    // F
                    double cl = Convert.ToDouble(args[11]);   // Cl
                    if (sio2 == 0) return LanguageService.Instance["error"];  // 如果主要成分为0则返回空

                    // 氧化物摩尔质量常量
                    const double MOL_SIO2 = 60.084;
                    const double MOL_TIO2 = 79.898;
                    const double MOL_AL2O3 = 101.961;
                    const double MOL_CR2O3 = 151.9902;
                    const double MOL_FEO = 71.846;
                    const double MOL_MNO = 70.937;
                    const double MOL_MGO = 40.304;
                    const double MOL_CAO = 56.079;
                    const double MOL_NA2O = 61.979;
                    const double MOL_K2O = 94.195;
                    const double MOL_F = 18.998;
                    const double MOL_CL = 35.453;
                    const double MOL_FE2O3 = 159.691;
                    const double OXYGEN_ATOMS = 23 * 15.999;

                    // 氧原子数归一化因子
                    var temp_sum_for_b44 = (sio2 * 0.532554423806671) + (tio2 * 0.400485619164435) +
                                           (al2o3 * 0.470738811898667) + (cr2o3 * 0.315790096993096) +
                                           (feo * 0.222684631016341) + (mno * 0.225538153572889) +
                                           (mgo * 0.396958118300913) + (cao * 0.285293960305997) +
                                           (na2o * 0.258135820197164) + (k2o * 0.169849779712299);
                    double b44 = OXYGEN_ATOMS / temp_sum_for_b44;

                    // 各氧化物的阳离子数
                    double b45 = (sio2 * b44) / MOL_SIO2;
                    double b46 = (tio2 * b44) / MOL_TIO2;
                    double b47 = (al2o3 * b44) / MOL_AL2O3 * 2;
                    double b48 = (cr2o3 * b44) / MOL_CR2O3 * 2;
                    double b49 = (feo * b44) / MOL_FEO;
                    double b50 = (mno * b44) / MOL_MNO;
                    double b51 = (mgo * b44) / MOL_MGO;
                    double b52 = (cao * b44) / MOL_CAO;
                    double b53 = (na2o * b44) / MOL_NA2O * 2;
                    double b54 = (k2o * b44) / MOL_K2O * 2;
                    double b57 = (f * b44) / MOL_F;
                    double b58 = (cl * b44) / MOL_CL;

                    // 阳离子数求和
                    double b62 = b45 + b46 + b47 + b48 + b49 + b50 + b51;
                    if (b62 == 0) return LanguageService.Instance["error"];
                    double b78 = b62 + b52;
                    if (b78 == 0) return LanguageService.Instance["error"];

                    // 以13或15个阳离子为基础进行归一化计算
                    double b63 = (b45 * 13) / b62; double b79 = (b45 * 15) / b78;
                    double b64 = (b46 * 13) / b62; double b80 = (b46 * 15) / b78;
                    double b65 = (b47 * 13) / b62; double b81 = (b47 * 15) / b78;
                    double b66 = (b48 * 13) / b62; double b82 = (b48 * 15) / b78;
                    double b67 = (b49 * 13) / b62; double b83 = (b49 * 15) / b78;
                    double b68 = (b50 * 13) / b62; double b84 = (b50 * 15) / b78;
                    double b69 = (b51 * 13) / b62; double b85 = (b51 * 15) / b78;
                    double b70 = (b52 * 13) / b62; double b86 = (b52 * 15) / b78;
                    double b71 = (b53 * 13) / b62; double b87 = (b53 * 15) / b78;
                    double b72 = (b54 * 13) / b62; double b88 = (b54 * 15) / b78;

                    bool is_sum_lt_8 = (b63 + b64 + b65) < 8;

                    // 根据条件选择归一化结果
                    double b96 = is_sum_lt_8 ? b79 : b63;
                    double b97 = is_sum_lt_8 ? b80 : b64;
                    double b98 = is_sum_lt_8 ? b81 : b65;
                    double b99 = is_sum_lt_8 ? b82 : b66;

                    double b102 = is_sum_lt_8 ? b84 : b68;
                    double b103 = is_sum_lt_8 ? b85 : b69;
                    double b104 = (b63 + b64 + b65) < 8 ? b86 : b70;
                    double b105 = is_sum_lt_8 ? b87 : b71;
                    double b106 = is_sum_lt_8 ? b88 : b72;

                    // Fe3+ 和 Fe2+ 的分配计算
                    double b94 = (b70 < 1.5)
                        ? (b79 * 4 + b80 * 4 + b81 * 3 + b82 * 3 + b83 * 2 + b84 * 2 + b85 * 2 + b86 * 2 + b87 + b88)
                        : (b63 * 4 + b64 * 4 + b65 * 3 + b66 * 3 + b67 * 2 + b68 * 2 + b69 * 2 + b70 * 2 + b71 + b72);

                    double b100 = b94 > 46 ? 0 : 46 - b94; // Fe3+
                    double b101 = (is_sum_lt_8 ? b83 : b67) - b100; // Fe2+

                    // T, M, A位阳离子分配计算
                    double b131 = b96; // Si(T1)
                    double b132 = Math.Min(8 - b131, b98); // Al(T1)
                    double b133 = Math.Min(8 - b131 - b132, b97); // Ti(T1)

                    double b136 = b98 - b132; // Al(M2)
                    double b137 = b97 - b133; // Ti(M2)
                    double b138 = b99; // Cr(M2)
                    double b139 = b100; // Fe3+(M2)
                    double b140 = b103; // Mg(M2)
                    double b142 = b102; // Mn(M2)
                    double b141 = Math.Min(5 - b136 - b137 - b138 - b139 - b140 - b142, b101); // Fe2+(M2)

                    double b145 = b100 + b101 - b139 - b141; // Mg(M1, M3)
                    double b146 = b104; // Ca(M4)
                    double b147 = Math.Min(2 - b145 - b146, b105); // Na(M4)

                    double b150 = b105 - b147; // Na(A)
                    double b151 = b106; // K(A)
                    double b152 = b150 + b151; // A-site vacancy

                    // 温度计算关键参数
                    double b164;
                    if (b104 < 1.5)
                    {
                        b164 = 0;
                    }
                    else
                    {
                        b164 = b131 + b132 / 15 - b133 * 2 - b136 / 2 - b137 / 1.8 + b139 / 9 + b141 / 3.3 +
                               b140 / 26 + b146 / 5 + b147 / 1.3 - b150 / 15 + (1 - b152) / 2.3;
                    }

                    // 分类/有效性检查
                    string b112;

                    double b59 = 2 - b57 - b58;
                    double b21 = (b59 * 17) / b44 / 2;
                    double b27 = (b100 * b62) / 13 / b44 * MOL_FE2O3 / 2;
                    double b28 = (b101 * b62) / 13 / b44 * MOL_FEO;
                    double[] b37_values = { sio2, tio2, al2o3, cr2o3, b27, b28, mno, mgo, cao, na2o, k2o, f, cl, b21 };
                    double b37 = 0;
                    foreach (var val in b37_values) b37 += val;

                    double b41 = -(f * 0.421070639014633 + cl * 0.225636758525372);
                    double? b39 = b37 + b41;

                    double b155 = b103 / (b103 + b141 + b145);

                    string b110 = "ok";
                    if (b39 < 98 || b139 < 0 || b141 < 0 || b150 < 0 || b152 > 1 || b155 < 0.5)
                    {
                        b110 = "wrong";
                    }

                    if (b164 == 0) b112 = "low-Ca";
                    else if (b110 == "wrong") b112 = "invalid";
                    else
                    {
                        double denominator = b136 + b132;
                        if (denominator == 0) return "Error";
                        double b111 = b136 / denominator;

                        if (b111 > 0.21) b112 = "Xenocryst";
                        else if (b131 >= 6.5) b112 = "Mg-Hbl";
                        else if (b152 > 0.5) b112 = "Mg-Hst";
                        else b112 = "Tsch-Prg";
                    }

                    // 最终温度计算
                    if (b164 == 0 || b112 == "invalid")
                    {
                        return "invalid";
                    }

                    double tK = -151.487 * b164 + 2041 + 273.15;
                    return tK;
                }
                catch
                {
                    // HACK: 需要优化提示
                    return null;
                }
            };

            // 绿泥石 Al4 温度计算
            FormulaExtension.CustomFunctions["Chlorite_Al4_Jowett_1991"] = (cell, args) =>
            {
                if (args.Length < 17)
                {
                    return LanguageService.Instance["missing_parameters"];
                }

                try
                {
                    // 获取输入参数
                    double sio2 = Convert.ToDouble(args[0]);
                    double tio2 = Convert.ToDouble(args[1]);
                    double al2o3 = Convert.ToDouble(args[2]);
                    double feo = Convert.ToDouble(args[3]);
                    double mno = Convert.ToDouble(args[4]);
                    double mgo = Convert.ToDouble(args[5]);
                    double cao = Convert.ToDouble(args[6]);
                    double na2o = Convert.ToDouble(args[7]);
                    double k2o = Convert.ToDouble(args[8]);
                    double bao = Convert.ToDouble(args[9]);
                    double rb2o = Convert.ToDouble(args[10]);
                    double cso = Convert.ToDouble(args[11]);
                    double zno = Convert.ToDouble(args[12]);
                    double f = Convert.ToDouble(args[13]);
                    double cl = Convert.ToDouble(args[14]);
                    double cr2o3 = Convert.ToDouble(args[15]);
                    double nio = Convert.ToDouble(args[16]);

                    // 计算氧原子数基准值
                    double oxygenBasisSum = (sio2 * 2 / 60.09) + (tio2 * 2 / 79.9) + (al2o3 * 3 / 101.96) +
                                           (cr2o3 * 3 / 152) + (feo / 71.85) + (mno / 70.94) + (mgo / 40.31) +
                                           (nio / 74.708) + (zno / 81.38) + (cao / 56.08) + (na2o / 61.98) +
                                           (k2o / 94.22) + (bao / 153.36) + (rb2o / 186.936) + (f * 0.5 / 19) +
                                           (cl * 0.5 / 35.45);

                    // 氧原子数基准值为零
                    if (oxygenBasisSum == 0) return LanguageService.Instance["oxygen_atom_benchmark_zero"];

                    // 计算Fe阳离子数
                    double feCation = 28 * (feo / 71.85) / oxygenBasisSum;

                    // 计算阳离子总数
                    double totalCations = ((28 * (sio2 / 60.09) / oxygenBasisSum)) + ((28 * (tio2 / 79.9) / oxygenBasisSum)) +
                                         ((28 * (al2o3 * 2 / 101.96) / oxygenBasisSum)) + ((28 * (cr2o3 * 2 / 152) / oxygenBasisSum)) +
                                         feCation + (28 * (mno / 70.94) / oxygenBasisSum) + (28 * (mgo / 40.31) / oxygenBasisSum) +
                                         (28 * (nio / 74.708) / oxygenBasisSum) + (28 * (zno / 81.38) / oxygenBasisSum) +
                                         (28 * (cao / 56.08) / oxygenBasisSum) + (28 * (na2o * 2 / 61.98) / oxygenBasisSum) +
                                         (28 * (k2o * 2 / 94.22) / oxygenBasisSum) + (28 * (bao / 153.36) / oxygenBasisSum) +
                                         (28 * (rb2o * 2 / 186.936) / oxygenBasisSum);

                    // 计算阳离子空位数
                    double cationVacancy = 20 - totalCations;
                    double nonNegativeVacancy = Math.Max(0, cationVacancy);
                    double feForChargeBalance = feCation - cationVacancy;
                    double feInOctahedral = (nonNegativeVacancy > feCation) ? 0 : feForChargeBalance;
                    double feInTetrahedral = Math.Min(feCation, nonNegativeVacancy);

                    // 计算归一化因子
                    double oxygenNormFactor = ((28 * (sio2 / 60.09) / oxygenBasisSum) * 2) + ((28 * (tio2 / 79.9) / oxygenBasisSum) * 2) +
                                             ((28 * (al2o3 * 2 / 101.96) / oxygenBasisSum) * 1.5) + ((28 * (cr2o3 * 2 / 152) / oxygenBasisSum) * 1.5) +
                                             (feInTetrahedral * 1.5) + feInOctahedral + (28 * (mno / 70.94) / oxygenBasisSum) +
                                             (28 * (mgo / 40.31) / oxygenBasisSum) + (28 * (nio / 74.708) / oxygenBasisSum) +
                                             (28 * (zno / 81.38) / oxygenBasisSum) + (28 * (cao / 56.08) / oxygenBasisSum) +
                                             (28 * (na2o * 2 / 61.98) / oxygenBasisSum) + (28 * (k2o * 2 / 94.22) / oxygenBasisSum) +
                                             (28 * (bao / 153.36) / oxygenBasisSum) + (28 * (rb2o * 2 / 186.936) / oxygenBasisSum) +
                                             (28 * (f / 19) / oxygenBasisSum) + (28 * (cl / 35.45) / oxygenBasisSum);

                    // 归一化因子为零
                    if (oxygenNormFactor == 0) return LanguageService.Instance["normalization_factor_is_zero"];

                    // 计算四面体中的Si和Al
                    double siInTetrahedral = (((28 * (sio2 / 60.09) / oxygenBasisSum) * 2) * (28 / oxygenNormFactor)) / 2;
                    double alForTetrahedral_calc = (28 * (al2o3 * 2 / 101.96) / oxygenBasisSum);
                    double alInTetrahedral_intermediate = (siInTetrahedral + alForTetrahedral_calc > 8) ? (8 - siInTetrahedral) : alForTetrahedral_calc;
                    double alInTetrahedral = Math.Max(0, alInTetrahedral_intermediate);

                    // 计算最终温度参数
                    double feOctahedral_normalized = (feInOctahedral * (28 / oxygenNormFactor)) / 2;
                    double mgCation_normalized = (28 * (mgo / 40.31) / oxygenBasisSum) * (28 / oxygenNormFactor);
                    double denominator = feOctahedral_normalized + mgCation_normalized / 2;

                    // 温度计算分母为零
                    if (denominator == 0) return LanguageService.Instance["temperature_calculation_denominator_zero"];

                    double tValueInput = (alInTetrahedral / 2) + 0.1 * (feOctahedral_normalized / denominator);

                    // 计算最终温度
                    return 319 * tValueInput - 68.7 + 273.15;
                }
                catch
                {
                    // HACK: 需要优化提示
                    return null;
                }
            };

            // 毒砂矿物组合
            FormulaExtension.CustomFunctions["DefineArsenopyriteAssemblage"] = (cell, args) => {
                try
                {
                    if (args.Length < 1) return null;
                    string assemblageName = Convert.ToString(args[0]).ToUpper();
                    switch (assemblageName)
                    {
                        case "ASP_PO_LO": return 0;
                        case "ASP_PY_PO": return 1;
                        case "ASP_PY_AS": return 2;
                        case "ASP_PO_L": return 3;
                        case "ASP_AS_Lo": return 4;
                        case "ASP_AY_L": return 5;
                        default: return null;
                    }
                }catch { return LanguageService.Instance["error"]; }

            };

            // 毒砂温度计计算
            FormulaExtension.CustomFunctions["Arsenopyrite_Assemblage_Kretschmar_and_Scott_1976"] = (cell, args) =>
            {
                if (args.Length < 2)
                {
                    return LanguageService.Instance["missing_parameters"];
                }

                try
                {
                    double atomicPercentAs;
                    int assemblage;

                    if (!double.TryParse(Convert.ToString(args[0]), out atomicPercentAs)) return null;
                    if (!int.TryParse(Convert.ToString(args[1]), out assemblage)) return null;

                    double temperature = double.NaN;

                    switch (assemblage)
                    {
                        case 0: // Asp_Po_Lo
                            if (atomicPercentAs >= 33.61 && atomicPercentAs <= 38.68)
                            {
                                temperature = 79.29 * atomicPercentAs - 2364.93;
                            }
                            break;

                        case 1: // Asp_Py_Po
                            if (atomicPercentAs >= 29.98 && atomicPercentAs <= 33.1)
                            {
                                temperature = 61.22 * atomicPercentAs - 1535.31;
                            }
                            break;

                        case 2: // Asp_Py_As
                            if (atomicPercentAs <= 30.22 && atomicPercentAs >= 29.12)
                            {
                                temperature = 57.27 * atomicPercentAs - 1367.78;
                            }
                            break;

                        case 3: // Asp_Po_L
                            if (atomicPercentAs >= 33.1 && atomicPercentAs <= 38.68)
                            {
                                temperature = 37.81 * atomicPercentAs - 760.63;
                            }
                            break;
                        case 4: // Asp_As_Lo
                            if (atomicPercentAs >= 33.61 && atomicPercentAs <= 38.68)
                            {
                                temperature = 74.72 * atomicPercentAs - 2188.22;
                            }
                            break;
                        case 5: // Asp_Py_L
                            if (atomicPercentAs >= 33.22 && atomicPercentAs <= 33.1)
                            {
                                temperature = 44.44 * atomicPercentAs - 980.11;
                            }
                            break;
                    }

                    return double.IsNaN(temperature) ? null : temperature + 273.15;
                }
                catch
                {
                    // HACK: 需要优化提示
                    return null;
                }
            };
        }

    }
}
