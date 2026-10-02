using System;
using System.Collections.Generic;

namespace GeoChemistryNexus.Services
{
    /// <summary>
    /// 黑体辐射计算（与 black_body_radiation.xlsx 公式一致）。
    /// </summary>
    public static class BlackBodyRadiationCalculatorService
    {
        /// <summary>Stefan–Boltzmann 常量 σ，W/m²K⁴</summary>
        public const double StefanBoltzmannConstant = 5.6697e-08;

        /// <summary>Wien 位移常量，μm·K</summary>
        public const double WienConstantUmK = 2898.0;

        /// <summary>光速 c，μm/s（与工作簿 A19 一致）</summary>
        public const double SpeedOfLightUmPerS = 300000.0 * 1000.0 * 1000000.0;

        /// <summary>Planck 常量 h，J·s</summary>
        public const double PlanckConstant = 6.6256e-34;

        /// <summary>Boltzmann 常量 k，J/K</summary>
        public const double BoltzmannConstant = 1.3805e-23;

        public const double DefaultTemperatureK = 6000.0;
        public const double DefaultWavelengthStartUm = 0.01;
        public const double DefaultWavelengthStepUm = 0.01;
        public const double DefaultWavelengthEndUm = 10.01;

        public static BlackBodyRadiationResult Calculate(
            double temperatureKelvin,
            double wavelengthStartUm = DefaultWavelengthStartUm,
            double wavelengthStepUm = DefaultWavelengthStepUm,
            double wavelengthEndUm = DefaultWavelengthEndUm)
        {
            if (temperatureKelvin <= 0
                || wavelengthStartUm <= 0
                || wavelengthStepUm <= 0
                || wavelengthEndUm < wavelengthStartUm)
            {
                return BlackBodyRadiationResult.Invalid;
            }

            double totalRadiantPower = StefanBoltzmannConstant
                * Math.Pow(temperatureKelvin, 4);

            double lambdaMaxUm = WienConstantUmK / temperatureKelvin;
            double lambdaMaxNm = lambdaMaxUm * 1000.0;

            var points = BuildSpectrum(
                temperatureKelvin,
                wavelengthStartUm,
                wavelengthStepUm,
                wavelengthEndUm);

            double maxPower = 0;
            int peakIndex = 0;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].Power > maxPower)
                {
                    maxPower = points[i].Power;
                    peakIndex = i;
                }
            }

            if (maxPower > 0)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    var p = points[i];
                    points[i] = new BlackBodySpectrumPoint
                    {
                        WavelengthUm = p.WavelengthUm,
                        WavelengthNm = p.WavelengthNm,
                        Power = p.Power,
                        NormalizedPowerPercent = 100.0 * p.Power / maxPower
                    };
                }
            }

            return new BlackBodyRadiationResult
            {
                IsValid = true,
                TemperatureKelvin = temperatureKelvin,
                TotalRadiantPowerWm2 = totalRadiantPower,
                PeakWavelengthUm = lambdaMaxUm,
                PeakWavelengthNm = lambdaMaxNm,
                SpectrumPeakWavelengthUm = points.Count > 0 ? points[peakIndex].WavelengthUm : 0,
                SpectrumPeakWavelengthNm = points.Count > 0 ? points[peakIndex].WavelengthNm : 0,
                Spectrum = points
            };
        }

        /// <summary>
        /// Planck 谱功率密度，与工作簿列 C 公式一致：
        /// ((2π c² h)/λ⁵) · 1 / e^((c h)/(k T λ) − 1)
        /// </summary>
        public static double SpectralPower(double temperatureKelvin, double wavelengthUm)
        {
            if (temperatureKelvin <= 0 || wavelengthUm <= 0)
                return 0;

            double c = SpeedOfLightUmPerS;
            double h = PlanckConstant;
            double k = BoltzmannConstant;
            double lambda = wavelengthUm;
            double t = temperatureKelvin;

            double exponent = (c * h) / (k * t * lambda) - 1.0;
            if (exponent > 700)
                return 0;
            if (exponent < -700)
                return double.PositiveInfinity;

            double denom = Math.Exp(exponent);
            if (denom <= 0 || double.IsInfinity(denom))
                return 0;

            double lambda5 = Math.Pow(lambda, 5);
            if (lambda5 <= 0 || double.IsInfinity(lambda5))
                return 0;

            return (2.0 * Math.PI * c * c * h / lambda5) * (1.0 / denom);
        }

        private static List<BlackBodySpectrumPoint> BuildSpectrum(
            double temperatureKelvin,
            double startUm,
            double stepUm,
            double endUm)
        {
            var points = new List<BlackBodySpectrumPoint>();
            // 用整数步进避免浮点累积误差偏离工作簿网格
            int count = (int)Math.Floor((endUm - startUm) / stepUm + 1e-9) + 1;
            for (int i = 0; i < count; i++)
            {
                double wavelengthUm = startUm + i * stepUm;
                if (wavelengthUm > endUm + stepUm * 0.5)
                    break;

                double power = SpectralPower(temperatureKelvin, wavelengthUm);
                if (double.IsNaN(power) || double.IsInfinity(power))
                    power = 0;

                points.Add(new BlackBodySpectrumPoint
                {
                    WavelengthUm = wavelengthUm,
                    WavelengthNm = wavelengthUm * 1000.0,
                    Power = power,
                    NormalizedPowerPercent = 0
                });
            }

            return points;
        }
    }

    public sealed class BlackBodyRadiationResult
    {
        public static BlackBodyRadiationResult Invalid { get; } = new()
        {
            IsValid = false,
            Spectrum = Array.Empty<BlackBodySpectrumPoint>()
        };

        public bool IsValid { get; init; }
        public double TemperatureKelvin { get; init; }
        public double TotalRadiantPowerWm2 { get; init; }
        public double PeakWavelengthUm { get; init; }
        public double PeakWavelengthNm { get; init; }
        public double SpectrumPeakWavelengthUm { get; init; }
        public double SpectrumPeakWavelengthNm { get; init; }
        public IReadOnlyList<BlackBodySpectrumPoint> Spectrum { get; init; }
            = Array.Empty<BlackBodySpectrumPoint>();
    }

    public sealed class BlackBodySpectrumPoint
    {
        public double WavelengthUm { get; init; }
        public double WavelengthNm { get; init; }
        public double Power { get; init; }
        public double NormalizedPowerPercent { get; init; }
    }
}
