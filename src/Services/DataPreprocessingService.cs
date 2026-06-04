using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using unvell.ReoGrid;

namespace GeoChemistryNexus.Services
{
    public static class DataPreprocessingOptionCodes
    {
        public const string IronValenceAutoEstimate = "iron_valence_auto_estimate";
        public const string IronValenceBackCalculate = "iron_valence_back_calculate";
        public const string IronValenceEmpiricalRatio = "iron_valence_empirical_ratio";

        public const string OutlierMarkOnly = "outlier_mark_only";
        public const string OutlierIqr = "outlier_iqr";
        public const string OutlierThreeSigma = "outlier_three_sigma";

        public const string MissingKeep = "missing_keep";
        public const string MissingMean = "missing_mean";
        public const string MissingMedian = "missing_median";

        public const string DetectionReplaceZero = "detection_replace_zero";
        public const string DetectionReplaceHalf = "detection_replace_half";
        public const string DetectionReplaceNull = "detection_replace_null";
    }

    public sealed class DataPreprocessingOptions
    {
        public bool IncludeAnhydrousNormalization { get; set; }
        public bool IncludeIronValenceEstimation { get; set; }
        public bool IncludeGeochemicalIndexCalculation { get; set; }
        public bool IncludeDataCleaning { get; set; }
        public bool ExcludeVolatiles { get; set; }
        public bool NormalizeToHundred { get; set; }
        public bool AutoBackfillIronOxides { get; set; }
        public bool CalculateMgNumber { get; set; }
        public bool CalculateACNK { get; set; }
        public bool CalculateANK { get; set; }
        public bool StandardizeBelowDetectionLimitText { get; set; }
        public bool CreateAuditColumns { get; set; }
        public bool KeepOriginalColumns { get; set; }
        public string IronValenceMethod { get; set; } = DataPreprocessingOptionCodes.IronValenceAutoEstimate;
        public string OutlierStrategy { get; set; } = DataPreprocessingOptionCodes.OutlierMarkOnly;
        public string MissingValueStrategy { get; set; } = DataPreprocessingOptionCodes.MissingKeep;
        public string DetectionLimitStrategy { get; set; } = DataPreprocessingOptionCodes.DetectionReplaceHalf;
        public double Fe3Fraction { get; set; } = 0.15;
    }

    public sealed class DataPreprocessingRunResult
    {
        public string WorksheetName { get; set; } = string.Empty;
        public int DataRowCount { get; set; }
        public int NumericColumnCount { get; set; }
        public int DetectionLimitReplacementCount { get; set; }
        public int MissingValueFillCount { get; set; }
        public int OutlierMarkedCount { get; set; }
        public int OutlierReplacedCount { get; set; }
        public int CalculatedRowCount { get; set; }
        public string Summary { get; set; } = string.Empty;
    }

    public sealed class DataPreprocessingWorksheetData
    {
        public string WorksheetName { get; set; } = string.Empty;
        public List<string> Headers { get; set; } = new();
        public List<List<string>> Rows { get; set; } = new();
        public DataPreprocessingRunResult RunResult { get; set; } = new();
    }

    internal sealed class ParsedCellValue
    {
        public string OriginalText { get; set; } = string.Empty;
        public double? Value { get; set; }
        public bool IsMissing { get; set; }
        public bool IsDetectionLimit { get; set; }
        public double? DetectionLimit { get; set; }
        public List<string> Notes { get; } = new();
    }

    internal sealed class NumericColumnProfile
    {
        public int Index { get; set; }
        public string Header { get; set; } = string.Empty;
        public List<ParsedCellValue> Cells { get; } = new();
    }

    internal sealed class IronEstimationResult
    {
        public double? FeO { get; set; }
        public double? Fe2O3 { get; set; }
        public string Mode { get; set; } = "未处理";
    }

    public static class DataPreprocessingService
    {
        private static readonly string[] MajorOxideOrder =
        {
            "SiO2", "TiO2", "Al2O3", "FeOT", "FeO", "Fe2O3", "MnO",
            "MgO", "CaO", "Na2O", "K2O", "P2O5", "Cr2O3", "NiO"
        };

        private static readonly HashSet<string> VolatileOxides = new(StringComparer.OrdinalIgnoreCase)
        {
            "LOI", "H2O", "H2O+", "H2O-", "CO2", "SO3", "S", "Cl", "F"
        };

        private static readonly HashSet<string> NonNumericHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Sample", "SampleID", "ID", "No", "Name", "Label", "Category", "Group", "RockType", "Comment", "Comments", "Note", "Notes"
        };

        private static readonly Dictionary<string, string[]> OxideAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["SiO2"] = new[] { "SiO2", "SiO_2" },
            ["TiO2"] = new[] { "TiO2", "TiO_2" },
            ["Al2O3"] = new[] { "Al2O3", "Al2O_3" },
            ["FeOT"] = new[] { "FeOT", "TFeO", "FeO*", "FeOt", "FeO_total", "FeOTotal", "TotalFeAsFeO" },
            ["FeO"] = new[] { "FeO", "FeOt", "Fe2+" },
            ["Fe2O3"] = new[] { "Fe2O3", "Fe2O_3", "Fe203", "Fe3+" },
            ["MnO"] = new[] { "MnO" },
            ["MgO"] = new[] { "MgO" },
            ["CaO"] = new[] { "CaO" },
            ["Na2O"] = new[] { "Na2O", "Na2O " },
            ["K2O"] = new[] { "K2O", "K20" },
            ["P2O5"] = new[] { "P2O5", "P2O_5" },
            ["Cr2O3"] = new[] { "Cr2O3", "Cr2O_3" },
            ["NiO"] = new[] { "NiO" },
            ["LOI"] = new[] { "LOI", "L.O.I", "Ig.loss", "LossOnIgnition" },
            ["H2O"] = new[] { "H2O", "H2O+", "H2O-" },
            ["CO2"] = new[] { "CO2", "CO_2" }
        };

        private static readonly Dictionary<string, double> MolecularWeights = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Al2O3"] = 101.961,
            ["CaO"] = 56.077,
            ["Na2O"] = 61.979,
            ["K2O"] = 94.196,
            ["MgO"] = 40.3044,
            ["FeO"] = 71.844,
            ["Fe2O3"] = 159.687
        };

        public static DataPreprocessingWorksheetData ProcessWorksheet(Worksheet sourceSheet, DataPreprocessingOptions options)
        {
            if (sourceSheet == null)
            {
                throw new ArgumentNullException(nameof(sourceSheet));
            }

            var range = sourceSheet.UsedRange;
            int effectiveColumnCount = GetEffectiveColumnCount(sourceSheet, range);
            if (effectiveColumnCount <= 0 || range.Rows <= 0)
            {
                throw new InvalidOperationException(L("dataPrep_noProcessableData", "The current worksheet has no processable data. Please import a table that contains field headers first."));
            }

            var originalHeaders = new List<string>();
            for (int col = 0; col < effectiveColumnCount; col++)
            {
                string header = sourceSheet.ColumnHeaders[col]?.Text?.Trim();
                originalHeaders.Add(string.IsNullOrWhiteSpace(header) ? $"Column_{col + 1}" : header);
            }

            int startRow = Math.Max(0, range.Row);
            int dataRowCount = range.Rows > 0 ? range.EndRow - startRow + 1 : 0;
            var originalRows = new List<List<string>>();
            for (int row = startRow; row <= range.EndRow; row++)
            {
                var rowValues = new List<string>();
                for (int col = 0; col < effectiveColumnCount; col++)
                {
                    rowValues.Add(sourceSheet.GetCellText(row, col) ?? sourceSheet.GetCellData(row, col)?.ToString() ?? string.Empty);
                }
                originalRows.Add(rowValues);
            }

            var numericProfiles = BuildNumericProfiles(originalHeaders, originalRows, options);
            var rowNotes = Enumerable.Range(0, dataRowCount).Select(_ => new List<string>()).ToList();
            var canonicalMap = BuildCanonicalColumnMap(originalHeaders);

            var result = new DataPreprocessingRunResult
            {
                WorksheetName = CreateProcessedWorksheetName(sourceSheet.Name),
                DataRowCount = dataRowCount,
                NumericColumnCount = numericProfiles.Count
            };

            foreach (var profile in numericProfiles)
            {
                for (int rowIndex = 0; rowIndex < profile.Cells.Count; rowIndex++)
                {
                    if (profile.Cells[rowIndex].IsDetectionLimit)
                    {
                        result.DetectionLimitReplacementCount++;
                    }

                    foreach (string note in profile.Cells[rowIndex].Notes)
                    {
                        rowNotes[rowIndex].Add($"{profile.Header}: {note}");
                    }
                }
            }

            if (options.IncludeDataCleaning)
            {
                ApplyOutlierProcessing(numericProfiles, rowNotes, options, result);
                ApplyMissingValueFill(numericProfiles, rowNotes, options, result);
            }

            var cleanedNumericLookup = BuildCleanedLookup(numericProfiles);
            var outputHeaders = new List<string>();
            var outputRows = new List<List<string>>();

            if (options.KeepOriginalColumns || !options.IncludeDataCleaning)
            {
                outputHeaders.AddRange(originalHeaders);
            }
            else
            {
                outputHeaders.AddRange(originalHeaders);
            }

            if (options.IncludeDataCleaning && options.KeepOriginalColumns)
            {
                foreach (var profile in numericProfiles)
                {
                    outputHeaders.Add($"Clean_{profile.Header}");
                }
            }

            var normalizedOxides = canonicalMap
                .Where(kv => MajorOxideOrder.Contains(kv.Key, StringComparer.OrdinalIgnoreCase) || VolatileOxides.Contains(kv.Key))
                .Select(kv => kv.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(k => Array.IndexOf(MajorOxideOrder, k) >= 0 ? Array.IndexOf(MajorOxideOrder, k) : 100)
                .ToList();

            if (options.IncludeAnhydrousNormalization)
            {
                foreach (var oxide in normalizedOxides.Where(o => !VolatileOxides.Contains(o) || !options.ExcludeVolatiles))
                {
                    outputHeaders.Add($"Norm_{oxide}");
                }
                outputHeaders.Add("Anhydrous_Total");
            }

            if (options.IncludeIronValenceEstimation)
            {
                outputHeaders.Add("FeO_Est");
                outputHeaders.Add("Fe2O3_Est");
                outputHeaders.Add("Iron_Mode");
            }

            if (options.IncludeGeochemicalIndexCalculation)
            {
                if (options.CalculateMgNumber) outputHeaders.Add("Mg_Number");
                if (options.CalculateACNK) outputHeaders.Add("A_CNK");
                if (options.CalculateANK) outputHeaders.Add("A_NK");
            }

            if (options.CreateAuditColumns)
            {
                outputHeaders.Add("Cleaning_Flag");
                outputHeaders.Add("Cleaning_Notes");
            }

            for (int rowIndex = 0; rowIndex < dataRowCount; rowIndex++)
            {
                var outputRow = new List<string>();
                var originalRow = originalRows[rowIndex];

                if (options.IncludeDataCleaning && !options.KeepOriginalColumns)
                {
                    for (int colIndex = 0; colIndex < originalHeaders.Count; colIndex++)
                    {
                        if (cleanedNumericLookup.TryGetValue(colIndex, out var values))
                        {
                            outputRow.Add(FormatNullable(values[rowIndex]));
                        }
                        else
                        {
                            outputRow.Add(originalRow[colIndex]);
                        }
                    }
                }
                else
                {
                    outputRow.AddRange(originalRow);
                }

                if (options.IncludeDataCleaning && options.KeepOriginalColumns)
                {
                    foreach (var profile in numericProfiles)
                    {
                        outputRow.Add(FormatNullable(profile.Cells[rowIndex].Value));
                    }
                }

                var rowOxides = BuildRowOxides(canonicalMap, rowIndex, cleanedNumericLookup);
                var normalized = options.IncludeAnhydrousNormalization
                    ? NormalizeAnhydrous(rowOxides, options.ExcludeVolatiles, options.NormalizeToHundred)
                    : new Dictionary<string, double>(rowOxides, StringComparer.OrdinalIgnoreCase);

                if (options.IncludeAnhydrousNormalization)
                {
                    foreach (var oxide in normalizedOxides.Where(o => !VolatileOxides.Contains(o) || !options.ExcludeVolatiles))
                    {
                        outputRow.Add(FormatNullable(normalized.TryGetValue(oxide, out double value) ? value : null));
                    }

                    double total = normalized.Values.Sum();
                    outputRow.Add(FormatNullable(total));
                }

                var iron = EstimateIron(rowOxides, options);
                if (options.IncludeIronValenceEstimation)
                {
                    outputRow.Add(FormatNullable(iron.FeO));
                    outputRow.Add(FormatNullable(iron.Fe2O3));
                    outputRow.Add(iron.Mode);
                }

                if (options.IncludeGeochemicalIndexCalculation)
                {
                    var calculationSource = normalized.Count > 0 ? normalized : rowOxides;
                    if (options.CalculateMgNumber)
                    {
                        outputRow.Add(FormatNullable(CalculateMgNumber(calculationSource, iron)));
                    }
                    if (options.CalculateACNK)
                    {
                        outputRow.Add(FormatNullable(CalculateACNK(calculationSource)));
                    }
                    if (options.CalculateANK)
                    {
                        outputRow.Add(FormatNullable(CalculateANK(calculationSource)));
                    }

                    result.CalculatedRowCount++;
                }

                if (options.CreateAuditColumns)
                {
                    string noteText = string.Join(" | ", rowNotes[rowIndex].Distinct());
                    outputRow.Add(string.IsNullOrWhiteSpace(noteText) ? "OK" : "CHECK");
                    outputRow.Add(noteText);
                }

                outputRows.Add(outputRow);
            }

            result.Summary =
                LF(
                    "dataPrep_processingSummary",
                    "Processed {0} rows and identified {1} numeric columns; detection-limit replacements {2}, outliers marked {3}, outliers cleared {4}, missing values filled {5}.",
                    result.DataRowCount,
                    result.NumericColumnCount,
                    result.DetectionLimitReplacementCount,
                    result.OutlierMarkedCount,
                    result.OutlierReplacedCount,
                    result.MissingValueFillCount);

            return new DataPreprocessingWorksheetData
            {
                WorksheetName = result.WorksheetName,
                Headers = outputHeaders,
                Rows = outputRows,
                RunResult = result
            };
        }

        public static IReadOnlyList<string> DetectOxideColumns(IReadOnlyList<string> headers)
        {
            return headers
                .Select(GetCanonicalHeader)
                .Where(h => !string.IsNullOrWhiteSpace(h))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<NumericColumnProfile> BuildNumericProfiles(IReadOnlyList<string> headers, IReadOnlyList<List<string>> rows, DataPreprocessingOptions options)
        {
            var profiles = new List<NumericColumnProfile>();

            for (int colIndex = 0; colIndex < headers.Count; colIndex++)
            {
                string header = headers[colIndex];
                bool headerSuggestsNumeric = GetCanonicalHeader(header) != null || !NonNumericHeaders.Contains(header.Trim());
                int nonEmptyCount = 0;
                int parseableCount = 0;
                var parsedCells = new List<ParsedCellValue>();

                for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    string text = rows[rowIndex][colIndex];
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        nonEmptyCount++;
                    }

                    var parsed = ParseCell(text, options);
                    if (parsed.Value.HasValue || parsed.IsMissing || parsed.IsDetectionLimit)
                    {
                        parseableCount++;
                    }

                    parsedCells.Add(parsed);
                }

                bool isNumeric = nonEmptyCount > 0
                    && headerSuggestsNumeric
                    && parseableCount >= Math.Max(2, (int)Math.Ceiling(nonEmptyCount * 0.6));

                if (!isNumeric)
                {
                    continue;
                }

                var profile = new NumericColumnProfile
                {
                    Index = colIndex,
                    Header = header
                };
                profile.Cells.AddRange(parsedCells);
                profiles.Add(profile);
            }

            return profiles;
        }

        private static ParsedCellValue ParseCell(string text, DataPreprocessingOptions options)
        {
            var parsed = new ParsedCellValue
            {
                OriginalText = text ?? string.Empty
            };

            string normalized = (text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                parsed.IsMissing = true;
                return parsed;
            }

            string lower = normalized.ToLowerInvariant();
            bool isBelowDetection = lower.StartsWith("<")
                || lower.StartsWith("＜")
                || lower.StartsWith("<=")
                || lower.Contains("b.d.l")
                || lower == "bdl"
                || lower == "b.d.l."
                || lower == "below detection limit"
                || lower == "n.d."
                || lower == "nd";

            if (isBelowDetection && options.StandardizeBelowDetectionLimitText)
            {
                parsed.IsDetectionLimit = true;
                parsed.DetectionLimit = TryExtractDetectionLimit(normalized);
                parsed.Value = options.DetectionLimitStrategy switch
                {
                    DataPreprocessingOptionCodes.DetectionReplaceZero => 0d,
                    DataPreprocessingOptionCodes.DetectionReplaceHalf => parsed.DetectionLimit.HasValue ? parsed.DetectionLimit.Value / 2d : 0d,
                    DataPreprocessingOptionCodes.DetectionReplaceNull => null,
                    _ => parsed.DetectionLimit.HasValue ? parsed.DetectionLimit.Value / 2d : 0d
                };

                parsed.Notes.Add(options.DetectionLimitStrategy switch
                {
                    DataPreprocessingOptionCodes.DetectionReplaceZero => L("dataPrep_noteDetectionReplacedWithZero", "Detection-limit value replaced with 0"),
                    DataPreprocessingOptionCodes.DetectionReplaceHalf => parsed.DetectionLimit.HasValue
                        ? L("dataPrep_noteDetectionReplacedWithHalf", "Detection-limit value replaced with half of the limit")
                        : L("dataPrep_noteDetectionNoLimitFallbackZero", "b.d.l. has no numeric limit; treated as 0"),
                    DataPreprocessingOptionCodes.DetectionReplaceNull => L("dataPrep_noteDetectionReplacedWithNull", "Detection-limit value replaced with null"),
                    _ => L("dataPrep_noteDetectionStandardized", "Detection-limit value standardized")
                });

                return parsed;
            }

            if (TryParseDouble(normalized, out double value))
            {
                parsed.Value = value;
                return parsed;
            }

            parsed.IsMissing = true;
            parsed.Notes.Add(L("dataPrep_noteNonNumericAsMissing", "Unable to parse as numeric; treated as missing"));
            return parsed;
        }

        private static void ApplyOutlierProcessing(List<NumericColumnProfile> profiles, IList<List<string>> rowNotes, DataPreprocessingOptions options, DataPreprocessingRunResult result)
        {
            if (options.OutlierStrategy == DataPreprocessingOptionCodes.OutlierMarkOnly)
            {
                foreach (var profile in profiles)
                {
                    MarkOutliers(profile, rowNotes, options.OutlierStrategy, result, replaceWithNull: false);
                }
                return;
            }

            bool replaceWithNull = options.OutlierStrategy == DataPreprocessingOptionCodes.OutlierIqr
                || options.OutlierStrategy == DataPreprocessingOptionCodes.OutlierThreeSigma;
            foreach (var profile in profiles)
            {
                MarkOutliers(profile, rowNotes, options.OutlierStrategy, result, replaceWithNull);
            }
        }

        private static void MarkOutliers(NumericColumnProfile profile, IList<List<string>> rowNotes, string strategy, DataPreprocessingRunResult result, bool replaceWithNull)
        {
            var validPairs = profile.Cells
                .Select((cell, rowIndex) => new { cell.Value, RowIndex = rowIndex })
                .Where(x => x.Value.HasValue)
                .Select(x => (Value: x.Value!.Value, x.RowIndex))
                .ToList();

            if (validPairs.Count < 4)
            {
                return;
            }

            double minThreshold;
            double maxThreshold;

            if (strategy == DataPreprocessingOptionCodes.OutlierIqr)
            {
                var ordered = validPairs.Select(x => x.Value).OrderBy(x => x).ToList();
                double q1 = Percentile(ordered, 0.25);
                double q3 = Percentile(ordered, 0.75);
                double iqr = q3 - q1;
                minThreshold = q1 - 1.5 * iqr;
                maxThreshold = q3 + 1.5 * iqr;
            }
            else
            {
                double mean = validPairs.Average(x => x.Value);
                double variance = validPairs.Average(x => Math.Pow(x.Value - mean, 2));
                double std = Math.Sqrt(variance);
                minThreshold = mean - 3 * std;
                maxThreshold = mean + 3 * std;
            }

            foreach (var pair in validPairs)
            {
                if (pair.Value >= minThreshold && pair.Value <= maxThreshold)
                {
                    continue;
                }

                rowNotes[pair.RowIndex].Add(LF("dataPrep_noteOutlierDetected", "{0} detected as outlier", profile.Header));
                result.OutlierMarkedCount++;

                if (replaceWithNull)
                {
                    profile.Cells[pair.RowIndex].Value = null;
                    result.OutlierReplacedCount++;
                }
            }
        }

        private static void ApplyMissingValueFill(List<NumericColumnProfile> profiles, IList<List<string>> rowNotes, DataPreprocessingOptions options, DataPreprocessingRunResult result)
        {
            if (options.MissingValueStrategy == DataPreprocessingOptionCodes.MissingKeep)
            {
                return;
            }

            foreach (var profile in profiles)
            {
                var validValues = profile.Cells
                    .Where(cell => cell.Value.HasValue)
                    .Select(cell => cell.Value!.Value)
                    .OrderBy(x => x)
                    .ToList();

                if (validValues.Count == 0)
                {
                    continue;
                }

                double fillValue = options.MissingValueStrategy == DataPreprocessingOptionCodes.MissingMedian
                    ? Percentile(validValues, 0.5)
                    : validValues.Average();

                for (int rowIndex = 0; rowIndex < profile.Cells.Count; rowIndex++)
                {
                    if (profile.Cells[rowIndex].Value.HasValue)
                    {
                        continue;
                    }

                    profile.Cells[rowIndex].Value = fillValue;
                    rowNotes[rowIndex].Add(LF("dataPrep_noteMissingFilled", "{0} filled using {1}", profile.Header, GetMissingValueStrategyDisplayName(options.MissingValueStrategy)));
                    result.MissingValueFillCount++;
                }
            }
        }

        private static Dictionary<int, List<double?>> BuildCleanedLookup(IEnumerable<NumericColumnProfile> profiles)
        {
            return profiles.ToDictionary(
                profile => profile.Index,
                profile => profile.Cells.Select(cell => cell.Value).ToList());
        }

        private static Dictionary<string, int> BuildCanonicalColumnMap(IReadOnlyList<string> headers)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headers.Count; i++)
            {
                string canonical = GetCanonicalHeader(headers[i]);
                if (string.IsNullOrWhiteSpace(canonical) || map.ContainsKey(canonical))
                {
                    continue;
                }

                map[canonical] = i;
            }

            return map;
        }

        private static Dictionary<string, double> BuildRowOxides(Dictionary<string, int> canonicalMap, int rowIndex, IReadOnlyDictionary<int, List<double?>> cleanedNumericLookup)
        {
            var rowOxides = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in canonicalMap)
            {
                if (!cleanedNumericLookup.TryGetValue(item.Value, out var values))
                {
                    continue;
                }

                double? value = rowIndex >= 0 && rowIndex < values.Count ? values[rowIndex] : null;
                if (value.HasValue)
                {
                    rowOxides[item.Key] = Math.Max(0, value.Value);
                }
            }

            return rowOxides;
        }

        private static Dictionary<string, double> NormalizeAnhydrous(Dictionary<string, double> rowOxides, bool excludeVolatiles, bool normalizeToHundred)
        {
            var normalized = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var source = rowOxides
                .Where(kv => !excludeVolatiles || !VolatileOxides.Contains(kv.Key))
                .Where(kv => kv.Value >= 0)
                .ToList();

            double total = source.Sum(kv => kv.Value);
            if (total <= 0)
            {
                return normalized;
            }

            double factor = normalizeToHundred ? 100d / total : 1d;
            foreach (var item in source)
            {
                normalized[item.Key] = item.Value * factor;
            }

            return normalized;
        }

        private static IronEstimationResult EstimateIron(Dictionary<string, double> rowOxides, DataPreprocessingOptions options)
        {
            double? feot = rowOxides.TryGetValue("FeOT", out double feotValue) ? feotValue : null;
            double? feo = rowOxides.TryGetValue("FeO", out double feoValue) ? feoValue : null;
            double? fe2o3 = rowOxides.TryGetValue("Fe2O3", out double fe2o3Value) ? fe2o3Value : null;

            if (feo.HasValue && fe2o3.HasValue)
            {
                return new IronEstimationResult
                {
                    FeO = feo,
                    Fe2O3 = fe2o3,
                    Mode = L("dataPrep_ironModeMeasured", "Measured FeO + Fe2O3")
                };
            }

            double fraction = Math.Min(0.95, Math.Max(0.01, options.Fe3Fraction));

            if (feot.HasValue)
            {
                if (options.IronValenceMethod == DataPreprocessingOptionCodes.IronValenceBackCalculate)
                {
                    if (feo.HasValue && !fe2o3.HasValue)
                    {
                        return new IronEstimationResult
                        {
                            FeO = feo,
                            Fe2O3 = Math.Max(0, (feot.Value - feo.Value) * 1.11134),
                            Mode = L("dataPrep_ironModeFromFeotAndFeo", "Derived Fe2O3 from FeOT + FeO")
                        };
                    }

                    if (!feo.HasValue && fe2o3.HasValue)
                    {
                        return new IronEstimationResult
                        {
                            FeO = Math.Max(0, feot.Value - fe2o3.Value * 0.8998),
                            Fe2O3 = fe2o3,
                            Mode = L("dataPrep_ironModeFromFeotAndFe2o3", "Derived FeO from FeOT + Fe2O3")
                        };
                    }
                }

                return new IronEstimationResult
                {
                    FeO = feot.Value * (1d - fraction),
                    Fe2O3 = feot.Value * fraction * 1.11134,
                    Mode = options.IronValenceMethod == DataPreprocessingOptionCodes.IronValenceEmpiricalRatio
                        ? L("dataPrep_ironModeFeotEmpirical", "FeOT empirical ratio correction")
                        : L("dataPrep_ironModeFeotAuto", "FeOT automatic estimation")
                };
            }

            if (options.AutoBackfillIronOxides)
            {
                if (feo.HasValue && !fe2o3.HasValue && options.IronValenceMethod == DataPreprocessingOptionCodes.IronValenceEmpiricalRatio)
                {
                    double feTotal = feo.Value / Math.Max(0.0001, 1d - fraction);
                    return new IronEstimationResult
                    {
                        FeO = feo.Value,
                        Fe2O3 = Math.Max(0, feTotal * fraction * 1.11134),
                        Mode = L("dataPrep_ironModeBackfillFe2o3", "Empirically backfilled Fe2O3 from FeO")
                    };
                }

                if (!feo.HasValue && fe2o3.HasValue && options.IronValenceMethod == DataPreprocessingOptionCodes.IronValenceEmpiricalRatio)
                {
                    double feTotal = (fe2o3.Value / 1.11134) / Math.Max(0.0001, fraction);
                    return new IronEstimationResult
                    {
                        FeO = Math.Max(0, feTotal * (1d - fraction)),
                        Fe2O3 = fe2o3.Value,
                        Mode = L("dataPrep_ironModeBackfillFeo", "Empirically backfilled FeO from Fe2O3")
                    };
                }
            }

            return new IronEstimationResult
            {
                FeO = feo,
                Fe2O3 = fe2o3,
                Mode = L("dataPrep_ironModeInsufficient", "Insufficient iron data")
            };
        }

        private static double? CalculateMgNumber(Dictionary<string, double> oxides, IronEstimationResult iron)
        {
            if (!oxides.TryGetValue("MgO", out double mgo))
            {
                return null;
            }

            double? feo = iron.FeO;
            if (!feo.HasValue && oxides.TryGetValue("FeOT", out double feot))
            {
                feo = feot;
            }

            if (!feo.HasValue)
            {
                return null;
            }

            double mgMoles = mgo / MolecularWeights["MgO"];
            double feMoles = feo.Value / MolecularWeights["FeO"];
            if (mgMoles + feMoles <= 0)
            {
                return null;
            }

            return 100d * mgMoles / (mgMoles + feMoles);
        }

        private static double? CalculateACNK(Dictionary<string, double> oxides)
        {
            if (!oxides.TryGetValue("Al2O3", out double al2o3)
                || !oxides.TryGetValue("CaO", out double cao)
                || !oxides.TryGetValue("Na2O", out double na2o)
                || !oxides.TryGetValue("K2O", out double k2o))
            {
                return null;
            }

            double denominator = cao / MolecularWeights["CaO"] + na2o / MolecularWeights["Na2O"] + k2o / MolecularWeights["K2O"];
            if (denominator <= 0)
            {
                return null;
            }

            return (al2o3 / MolecularWeights["Al2O3"]) / denominator;
        }

        private static double? CalculateANK(Dictionary<string, double> oxides)
        {
            if (!oxides.TryGetValue("Al2O3", out double al2o3)
                || !oxides.TryGetValue("Na2O", out double na2o)
                || !oxides.TryGetValue("K2O", out double k2o))
            {
                return null;
            }

            double denominator = na2o / MolecularWeights["Na2O"] + k2o / MolecularWeights["K2O"];
            if (denominator <= 0)
            {
                return null;
            }

            return (al2o3 / MolecularWeights["Al2O3"]) / denominator;
        }

        private static string GetCanonicalHeader(string header)
        {
            string normalized = (header ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            foreach (var item in OxideAliases)
            {
                if (item.Value.Any(alias => string.Equals(alias, normalized, StringComparison.OrdinalIgnoreCase)))
                {
                    return item.Key;
                }
            }

            return null;
        }

        private static bool TryParseDouble(string text, out double value)
        {
            string normalized = (text ?? string.Empty).Trim();
            normalized = normalized.Replace("，", ",").Replace("％", "").Replace("%", "");

            if (double.TryParse(normalized, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            if (double.TryParse(normalized, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value))
            {
                return true;
            }

            normalized = normalized.Replace(",", string.Empty);
            return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static double? TryExtractDetectionLimit(string text)
        {
            string normalized = (text ?? string.Empty)
                .Replace("＜", "<")
                .Replace("<=", "<")
                .Replace(" ", string.Empty)
                .Trim();

            int markerIndex = normalized.IndexOf('<');
            string numericPart = markerIndex >= 0 ? normalized[(markerIndex + 1)..] : normalized;

            return TryParseDouble(numericPart, out double limit) ? limit : null;
        }

        private static double Percentile(IReadOnlyList<double> orderedValues, double percentile)
        {
            if (orderedValues == null || orderedValues.Count == 0)
            {
                return double.NaN;
            }

            if (orderedValues.Count == 1)
            {
                return orderedValues[0];
            }

            double position = (orderedValues.Count - 1) * percentile;
            int lowerIndex = (int)Math.Floor(position);
            int upperIndex = (int)Math.Ceiling(position);

            if (lowerIndex == upperIndex)
            {
                return orderedValues[lowerIndex];
            }

            double fraction = position - lowerIndex;
            return orderedValues[lowerIndex] + (orderedValues[upperIndex] - orderedValues[lowerIndex]) * fraction;
        }

        private static string FormatNullable(double? value)
        {
            return value.HasValue ? value.Value.ToString("0.#######", CultureInfo.InvariantCulture) : string.Empty;
        }

        private static string CreateProcessedWorksheetName(string sourceName)
        {
            string prefix = string.IsNullOrWhiteSpace(sourceName) ? "Processed" : $"Processed_{sourceName}";
            return $"{prefix}_{DateTime.Now:HHmmss}";
        }

        private static string GetMissingValueStrategyDisplayName(string strategy)
        {
            return strategy switch
            {
                DataPreprocessingOptionCodes.MissingMean => L("dataPrep_strategyMissingMean", "Mean Imputation"),
                DataPreprocessingOptionCodes.MissingMedian => L("dataPrep_strategyMissingMedian", "Median Imputation"),
                _ => L("dataPrep_strategyMissingKeep", "Keep Missing")
            };
        }

        private static string L(string key, string fallback)
        {
            return LanguageService.Instance[key] ?? fallback;
        }

        private static string LF(string key, string fallback, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, L(key, fallback), args);
        }

        private static int GetEffectiveColumnCount(Worksheet sourceSheet, RangePosition range)
        {
            int lastHeaderIndex = -1;
            for (int col = 0; col < sourceSheet.ColumnCount; col++)
            {
                string header = sourceSheet.ColumnHeaders[col]?.Text;
                if (!string.IsNullOrWhiteSpace(header))
                {
                    lastHeaderIndex = col;
                }
            }

            int usedRangeCols = range.Cols > 0 ? range.EndCol + 1 : 0;
            return Math.Max(lastHeaderIndex + 1, usedRangeCols);
        }
    }
}
