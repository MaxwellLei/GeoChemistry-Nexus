using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models.PeriodicTable;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GeoChemistryNexus.Services
{
    /// <summary>
    /// 地球化学增强周期表数据与分组定义。
    /// </summary>
    public static class PeriodicTableDataService
    {
        public const string DataFolderName = "Home";
        public const string DataFileName = "elements_geochem.json";

        private static readonly object SyncRoot = new();
        private static ElementGeochemCatalog? _catalog;
        private static IReadOnlyList<ElementGeochemRecord>? _elements;
        private static IReadOnlyDictionary<string, ElementGeochemRecord>? _bySymbol;

        /// <summary>
        /// 地球化学常用分组（REE / HFSE / LILE / PGE / Goldschmidt 等）。
        /// </summary>
        public static IReadOnlyList<ElementGroupDefinition> Groups { get; } = BuildGroups();

        public static ElementGeochemCatalog Catalog
        {
            get
            {
                EnsureLoaded();
                return _catalog!;
            }
        }

        public static IReadOnlyList<ElementGeochemRecord> Elements
        {
            get
            {
                EnsureLoaded();
                return _elements!;
            }
        }

        public static bool TryGetBySymbol(string symbol, out ElementGeochemRecord? record)
        {
            EnsureLoaded();
            if (_bySymbol!.TryGetValue(symbol, out var found))
            {
                record = found;
                return true;
            }

            record = null;
            return false;
        }

        public static ElementGroupDefinition? FindGroup(string groupKey)
        {
            return Groups.FirstOrDefault(g =>
                string.Equals(g.Key, groupKey, StringComparison.OrdinalIgnoreCase));
        }

        public static HashSet<string> GetGroupSymbols(string groupKey)
        {
            var group = FindGroup(groupKey);
            return group == null
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(group.Symbols, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 标准展示坐标：主表 7×18 + 镧系/锕系两行（自第 3 列起）。
        /// </summary>
        public static (int Row, int Column) GetDisplayPosition(ElementGeochemRecord element)
        {
            int z = element.AtomicNumber;

            // La–Lu → 展示第 8 行（索引 8），从第 3 列起
            if (z >= 57 && z <= 71)
                return (8, z - 57 + 3);

            // Ac–Lr → 展示第 9 行
            if (z >= 89 && z <= 103)
                return (9, z - 89 + 3);

            int row = element.Period;
            int col = element.GroupId ?? 0;
            if (row <= 0 || col <= 0)
                return (0, 0);

            return (row, col);
        }

        public static string NormalizeGoldschmidtClass(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            // 兼容 litophile / lithophile 两种拼写
            if (string.Equals(raw, "litophile", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(raw, "lithophile", StringComparison.OrdinalIgnoreCase))
                return "Lithophile";

            if (string.Equals(raw, "siderophile", StringComparison.OrdinalIgnoreCase))
                return "Siderophile";

            if (string.Equals(raw, "chalcophile", StringComparison.OrdinalIgnoreCase))
                return "Chalcophile";

            if (string.Equals(raw, "atmophile", StringComparison.OrdinalIgnoreCase))
                return "Atmophile";

            if (string.Equals(raw, "synthetic", StringComparison.OrdinalIgnoreCase))
                return "Synthetic";

            return char.ToUpperInvariant(raw[0]) + raw[1..].ToLowerInvariant();
        }

        public static IReadOnlyList<OxideFormulaInfo> BuildCommonOxides(ElementGeochemRecord element)
        {
            if (element.AtomicWeight is not > 0)
                return Array.Empty<OxideFormulaInfo>();

            // 常见地质氧化物：优先主价态中的正价
            var positiveMain = element.OxidationStates
                .Where(o => o.State > 0 && string.Equals(o.Category, "main", StringComparison.OrdinalIgnoreCase))
                .Select(o => o.State)
                .Distinct()
                .OrderBy(v => v)
                .ToList();

            if (positiveMain.Count == 0)
            {
                positiveMain = element.OxidationStates
                    .Where(o => o.State > 0)
                    .Select(o => o.State)
                    .Distinct()
                    .OrderBy(v => v)
                    .Take(3)
                    .ToList();
            }

            // Fe 同时给出 FeO / Fe2O3（地质常用）
            if (string.Equals(element.Symbol, "Fe", StringComparison.OrdinalIgnoreCase))
            {
                if (!positiveMain.Contains(2)) positiveMain.Insert(0, 2);
                if (!positiveMain.Contains(3)) positiveMain.Add(3);
            }

            var results = new List<OxideFormulaInfo>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (int ox in positiveMain)
            {
                var info = OxideMolecularWeightHelper.TryCreateOxide(element.Symbol, element.AtomicWeight.Value, ox);
                if (info == null || !seen.Add(info.Formula))
                    continue;
                results.Add(info);
            }

            return results;
        }

        private static void EnsureLoaded()
        {
            if (_catalog != null)
                return;

            lock (SyncRoot)
            {
                if (_catalog != null)
                    return;

                string path = AppDataPathHelper.GetBundledDataPath(DataFolderName, DataFileName);
                var catalog = new ElementGeochemCatalog();

                try
                {
                    if (File.Exists(path))
                    {
                        string? json = JsonHelper.ReadJsonFile(path);
                        if (!string.IsNullOrWhiteSpace(json))
                            catalog = JsonHelper.Deserialize<ElementGeochemCatalog>(json) ?? catalog;
                    }
                    else
                    {
                        Debug.WriteLine($"[PeriodicTableDataService] Data file not found: {path}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PeriodicTableDataService] Load failed: {ex.Message}");
                }

                _catalog = catalog;
                _elements = new ReadOnlyCollection<ElementGeochemRecord>(
                    (catalog.Elements ?? new List<ElementGeochemRecord>())
                        .OrderBy(e => e.AtomicNumber)
                        .ToList());
                _bySymbol = _elements.ToDictionary(
                    e => e.Symbol,
                    e => e,
                    StringComparer.OrdinalIgnoreCase);
            }
        }

        private static IReadOnlyList<ElementGroupDefinition> BuildGroups()
        {
            // REE: La–Lu + Y；LREE/HREE 按火成岩常用划分；
            // HFSE / LILE / PGE 为核心元素集合；过渡金属与 Goldschmidt 由元素字段推导。
            return new List<ElementGroupDefinition>
            {
                new("REE", "periodic_table_group_ree",
                    "Y", "La", "Ce", "Pr", "Nd", "Pm", "Sm", "Eu", "Gd", "Tb", "Dy", "Ho", "Er", "Tm", "Yb", "Lu"),
                new("LREE", "periodic_table_group_lree",
                    "La", "Ce", "Pr", "Nd", "Pm", "Sm", "Eu"),
                new("HREE", "periodic_table_group_hree",
                    "Y", "Gd", "Tb", "Dy", "Ho", "Er", "Tm", "Yb", "Lu"),
                new("HFSE", "periodic_table_group_hfse",
                    "Ti", "Zr", "Hf", "Nb", "Ta", "Th", "U"),
                new("LILE", "periodic_table_group_lile",
                    "K", "Rb", "Cs", "Ba", "Sr"),
                new("PGE", "periodic_table_group_pge",
                    "Ru", "Rh", "Pd", "Os", "Ir", "Pt"),
                new("TransitionMetals", "periodic_table_group_transition", Array.Empty<string>())
                {
                    IsDerived = true,
                    DerivedKind = ElementGroupDerivedKind.TransitionMetals
                },
                new("Lithophile", "periodic_table_group_lithophile", Array.Empty<string>())
                {
                    IsDerived = true,
                    DerivedKind = ElementGroupDerivedKind.Goldschmidt,
                    GoldschmidtKey = "lithophile"
                },
                new("Siderophile", "periodic_table_group_siderophile", Array.Empty<string>())
                {
                    IsDerived = true,
                    DerivedKind = ElementGroupDerivedKind.Goldschmidt,
                    GoldschmidtKey = "siderophile"
                },
                new("Chalcophile", "periodic_table_group_chalcophile", Array.Empty<string>())
                {
                    IsDerived = true,
                    DerivedKind = ElementGroupDerivedKind.Goldschmidt,
                    GoldschmidtKey = "chalcophile"
                },
                new("Atmophile", "periodic_table_group_atmophile", Array.Empty<string>())
                {
                    IsDerived = true,
                    DerivedKind = ElementGroupDerivedKind.Goldschmidt,
                    GoldschmidtKey = "atmophile"
                }
            };
        }
    }

    public enum ElementGroupDerivedKind
    {
        None = 0,
        TransitionMetals = 1,
        Goldschmidt = 2
    }

    public sealed class ElementGroupDefinition
    {
        public ElementGroupDefinition(string key, string titleKey, params string[] symbols)
        {
            Key = key;
            TitleKey = titleKey;
            Symbols = symbols ?? Array.Empty<string>();
        }

        public string Key { get; }
        public string TitleKey { get; }
        public IReadOnlyList<string> Symbols { get; }
        public bool IsDerived { get; init; }
        public ElementGroupDerivedKind DerivedKind { get; init; }
        public string? GoldschmidtKey { get; init; }
    }

    public static class OxideMolecularWeightHelper
    {
        private const double OxygenAtomicWeight = 15.999;

        public static OxideFormulaInfo? TryCreateOxide(string symbol, double atomicWeight, int oxidationState)
        {
            if (string.IsNullOrWhiteSpace(symbol) || atomicWeight <= 0 || oxidationState <= 0)
                return null;

            // 电荷平衡：x*ox + y*(-2) = 0 → 取最小整数比
            int elementCount = 2;
            int oxygenCount = oxidationState;
            int g = Gcd(elementCount, oxygenCount);
            elementCount /= g;
            oxygenCount /= g;

            // 若氧为偶数且元素为 2，可约简为常见写法（如 TiO2 而非 Ti2O4）
            // 上面已用 gcd 处理。

            string formula = FormatOxideFormula(symbol, elementCount, oxygenCount);
            double mw = elementCount * atomicWeight + oxygenCount * OxygenAtomicWeight;
            return new OxideFormulaInfo(formula, mw);
        }

        private static string FormatOxideFormula(string symbol, int elementCount, int oxygenCount)
        {
            string left = elementCount == 1 ? symbol : $"{symbol}{ToSubscript(elementCount)}";
            string right = oxygenCount == 1 ? "O" : $"O{ToSubscript(oxygenCount)}";
            return left + right;
        }

        private static string ToSubscript(int value)
        {
            return value.ToString();
        }

        private static int Gcd(int a, int b)
        {
            while (b != 0)
            {
                int t = a % b;
                a = b;
                b = t;
            }

            return Math.Abs(a);
        }
    }
}
