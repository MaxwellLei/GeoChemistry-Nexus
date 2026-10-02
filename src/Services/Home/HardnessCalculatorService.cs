using System;

namespace GeoChemistryNexus.Services
{
    /// <summary>
    /// 根据 Mg、Ca 浓度计算水硬度（与 hardness.xlsx 公式一致）。
    /// Hardness = Mg_ppm * (CaCO3_MW / Mg_MW) + Ca_ppm * (CaCO3_MW / Ca_MW)
    /// </summary>
    public static class HardnessCalculatorService
    {
        public const double MgMw = 24.305;
        public const double CaMw = 40.078;
        public const double OMw = 15.9994;
        public const double CMw = 12.011;

        public static double CaCo3Mw => CaMw + CMw + 3.0 * OMw;

        public static double FactorCa => CaCo3Mw / CaMw;

        public static double FactorMg => CaCo3Mw / MgMw;

        public static HardnessCalculationResult Calculate(double mgPpm, double caPpm)
        {
            if (mgPpm < 0 || caPpm < 0)
            {
                return HardnessCalculationResult.Invalid;
            }

            double hardness = mgPpm * FactorMg + caPpm * FactorCa;
            string classKey = Classify(hardness);

            return new HardnessCalculationResult
            {
                IsValid = true,
                HardnessAsCaCo3Ppm = hardness,
                ClassificationKey = classKey
            };
        }

        /// <summary>
        /// USGS 硬度分级（与 hardness.xlsx 一致）。
        /// Soft 0–60，Moderately hard 61–120，Hard 121–180，Very hard &gt;180。
        /// </summary>
        public static string Classify(double hardnessAsCaCo3Ppm)
        {
            if (hardnessAsCaCo3Ppm <= 60)
                return "Soft";
            if (hardnessAsCaCo3Ppm <= 120)
                return "ModeratelyHard";
            if (hardnessAsCaCo3Ppm <= 180)
                return "Hard";
            return "VeryHard";
        }
    }

    public sealed class HardnessCalculationResult
    {
        public static HardnessCalculationResult Invalid { get; } = new()
        {
            IsValid = false,
            ClassificationKey = string.Empty
        };

        public bool IsValid { get; init; }
        public double HardnessAsCaCo3Ppm { get; init; }
        public string ClassificationKey { get; init; } = string.Empty;
    }
}
