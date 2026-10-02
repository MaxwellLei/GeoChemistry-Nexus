using System;
using System.Collections.Generic;

namespace GeoChemistryNexus.Services
{
    /// <summary>
    /// 根据 HCl 滴定终点计算水样碱度（与 alkalinity.xlsx 公式一致）。
    /// </summary>
    public static class AlkalinityCalculatorService
    {
        public const double MwHco3 = 61.02;
        public const double MwCo3 = 60.0092;
        public const double MwCaco3 = 100.0872;

        public static AlkalinityCalculationResult Calculate(
            double hclMolarity,
            double sampleVolumeMl,
            double hclVolumeMl)
        {
            if (sampleVolumeMl <= 0 || hclMolarity < 0 || hclVolumeMl < 0)
            {
                return AlkalinityCalculationResult.Invalid;
            }

            // moles HCl = V(ml) * M / 1000
            double molesHcl = (hclVolumeMl * hclMolarity) / 1000.0;
            // moles/L in sample
            double molesPerLiter = molesHcl * 1000.0 / sampleVolumeMl;
            // meq/L（对一价 HCl 等同于 mmol H+/L）
            double alkalinityMeqPerL = molesPerLiter * 1000.0;

            var rows = new List<AlkalinityResultRow>
            {
                BuildRow("HCO3-", alkalinityMeqPerL, MwHco3, equivalents: 1),
                BuildRow("CO32-", alkalinityMeqPerL, MwCo3, equivalents: 2),
                BuildRow("CaCO3", alkalinityMeqPerL, MwCaco3, equivalents: 2),
                BuildRow("0.5(CaCO3)", alkalinityMeqPerL, MwCaco3 / 2.0, equivalents: 1)
            };

            return new AlkalinityCalculationResult
            {
                IsValid = true,
                MolesHcl = molesHcl,
                MolesPerLiter = molesPerLiter,
                AlkalinityMeqPerL = alkalinityMeqPerL,
                Rows = rows
            };
        }

        private static AlkalinityResultRow BuildRow(
            string formKey,
            double alkalinityMeqPerL,
            double molecularWeight,
            int equivalents)
        {
            // g/L = meq/L * MW * (1/n) / 1000
            double gramsPerLiter = alkalinityMeqPerL * molecularWeight * (1.0 / equivalents) / 1000.0;
            double molarity = alkalinityMeqPerL / 1000.0 / equivalents;
            double ppm = gramsPerLiter * 1000.0;

            return new AlkalinityResultRow
            {
                FormKey = formKey,
                GramsPerLiter = gramsPerLiter,
                MeqPerLiter = alkalinityMeqPerL,
                Molarity = molarity,
                Ppm = ppm
            };
        }
    }

    public sealed class AlkalinityCalculationResult
    {
        public static AlkalinityCalculationResult Invalid { get; } = new()
        {
            IsValid = false,
            Rows = Array.Empty<AlkalinityResultRow>()
        };

        public bool IsValid { get; init; }
        public double MolesHcl { get; init; }
        public double MolesPerLiter { get; init; }
        public double AlkalinityMeqPerL { get; init; }
        public IReadOnlyList<AlkalinityResultRow> Rows { get; init; } = Array.Empty<AlkalinityResultRow>();
    }

    public sealed class AlkalinityResultRow
    {
        public string FormKey { get; init; } = string.Empty;
        public double GramsPerLiter { get; init; }
        public double MeqPerLiter { get; init; }
        public double Molarity { get; init; }
        public double Ppm { get; init; }
    }
}
