using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models.Chronostrat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GeoChemistryNexus.Services
{
    /// <summary>
    /// ICS 国际年代地层表数据服务。
    /// 线程安全、懒加载单例，从 Data/Home/chronostratigraphy.json 加载数据，
    /// 并提供"按年龄反查地层"和"多语言关键词搜索"等核心 API。
    /// </summary>
    public static class ChronostratDataService
    {
        public const string DataFolderName = "Home";
        public const string DataFileName   = "chronostratigraphy.json";

        private static readonly object _syncRoot = new();
        private static ChronostratCatalog?                       _catalog;
        private static Dictionary<string, ChronostratUnitRecord>? _byId;

        // 所有叶子节点（Age 级别的期/阶），按 StartMa 降序排列，用于年龄区间查找
        private static List<ChronostratUnitRecord>? _leafsByStartMaDesc;

        // 用于年龄反查的所有概念（所有层级，包括非叶子），按 StartMa 降序
        private static List<ChronostratUnitRecord>? _allByStartMaDesc;

        // ── 公开属性 ──────────────────────────────────────────────────────────

        public static ChronostratCatalog Catalog
        {
            get { EnsureLoaded(); return _catalog!; }
        }

        public static IReadOnlyDictionary<string, ChronostratUnitRecord> ById
        {
            get { EnsureLoaded(); return _byId!; }
        }

        /// <summary>按 Order 排序的所有地质年代单元列表（显示用）。</summary>
        public static IReadOnlyList<ChronostratUnitRecord> AllUnits
        {
            get { EnsureLoaded(); return _catalog!.Units; }
        }

        // ── 核心 API ─────────────────────────────────────────────────────────

        /// <summary>
        /// 按年龄（Ma）反查其所属的最小地层单元（期/阶级别）。
        /// 同时返回从该单元到根节点的完整祖先路径（从小到大）。
        /// 如果找不到精确的期/阶，则回退到匹配的最小上级单元。
        /// </summary>
        /// <param name="ageMa">年龄值（百万年前，正值）。</param>
        /// <returns>匹配路径（从最小单元到最大单元），未找到则返回空集合。</returns>
        public static List<ChronostratUnitRecord> FindAncestorPath(double ageMa)
        {
            EnsureLoaded();
            if (ageMa < 0) return new();

            // 首先在叶节点（期/阶）中查找
            var leaf = FindBestMatch(_leafsByStartMaDesc!, ageMa);

            // 回退：若叶节点未覆盖（例如太古宙的 Eoarchean 没有子节点），在全部节点中查
            leaf ??= FindBestMatch(_allByStartMaDesc!, ageMa);

            if (leaf == null) return new();

            // 构造路径：从叶节点向上到根
            var path = new List<ChronostratUnitRecord>();
            var cur = leaf;
            while (cur != null)
            {
                path.Add(cur);
                cur = cur.Parent;
            }
            return path; // path[0] = 最小单元，path[^1] = 最大单元
        }

        /// <summary>
        /// 多语言模糊搜索（中英文、双轨制名称均可触发）。
        /// </summary>
        /// <param name="keyword">搜索关键词（不区分大小写）。</param>
        /// <returns>按层级从大到小、再按年代先后排列的匹配列表。</returns>
        public static List<ChronostratUnitRecord> Search(string keyword)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(keyword))
                return new();

            keyword = keyword.Trim();

            return _catalog!.Units
                .Where(u =>
                    u.NameEn.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    u.NameZh.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    u.NameZhChrono.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (u.Names != null && u.Names.Values.Any(val => val.Contains(keyword, StringComparison.OrdinalIgnoreCase))) ||
                    (u.StratNames != null && u.StratNames.Values.Any(val => val.Contains(keyword, StringComparison.OrdinalIgnoreCase))) ||
                    u.Id.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .OrderBy(u => u.Rank)
                .ThenBy(u => u.Order)
                .ToList();
        }

        /// <summary>
        /// 获取指定单元的完整祖先链（从父节点到根）。
        /// </summary>
        public static List<ChronostratUnitRecord> GetAncestors(ChronostratUnitRecord unit)
        {
            var ancestors = new List<ChronostratUnitRecord>();
            var cur = unit.Parent;
            while (cur != null)
            {
                ancestors.Add(cur);
                cur = cur.Parent;
            }
            return ancestors;
        }

        /// <summary>
        /// 获取顶层节点列表（无父节点的节点），按 Order 升序。
        /// </summary>
        public static IEnumerable<ChronostratUnitRecord> GetRoots()
        {
            EnsureLoaded();
            return _catalog!.Units.Where(u => u.Parent == null).OrderBy(u => u.Order);
        }

        // ── 内部实现 ─────────────────────────────────────────────────────────

        private static void EnsureLoaded()
        {
            if (_catalog != null) return;

            lock (_syncRoot)
            {
                if (_catalog != null) return;
                LoadInternal();
            }
        }

        private static void LoadInternal()
        {
            string path = AppDataPathHelper.GetBundledDataPath(DataFolderName, DataFileName);
            if (!File.Exists(path))
            {
                string curPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", DataFolderName, DataFileName);
                if (File.Exists(curPath))
                {
                    path = curPath;
                }
                else
                {
                    string devPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Data", DataFolderName, DataFileName);
                    if (File.Exists(devPath))
                        path = Path.GetFullPath(devPath);
                }
            }

            var catalog = new ChronostratCatalog();

            try
            {
                if (File.Exists(path))
                {
                    string? json = JsonHelper.ReadJsonFile(path);
                    if (!string.IsNullOrWhiteSpace(json))
                        catalog = JsonHelper.Deserialize<ChronostratCatalog>(json) ?? catalog;
                }
                else
                {
                    Debug.WriteLine($"[ChronostratDataService] Data file not found: {path}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChronostratDataService] Load failed: {ex.Message}");
            }

            _catalog = catalog;

            // 构建 id → record 字典
            _byId = new Dictionary<string, ChronostratUnitRecord>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var unit in catalog.Units)
                _byId[unit.Id] = unit;

            // 二次遍历：建立运行时树形父子引用
            foreach (var unit in catalog.Units)
            {
                if (unit.ParentId != null && _byId.TryGetValue(unit.ParentId, out var parent))
                {
                    unit.Parent = parent;
                    parent.Children.Add(unit);
                }
            }

            // 按 Order（即时代顺序）对每个节点的 Children 排序
            foreach (var unit in catalog.Units)
                unit.Children.Sort((a, b) => a.Order.CompareTo(b.Order));

            // 构建年龄区间查找索引（仅含有完整年龄数据的节点，按 StartMa 降序）
            _leafsByStartMaDesc = catalog.Units
                .Where(u => u.Rank == ChronostratRank.Age && u.StartMa.HasValue && u.EndMa.HasValue)
                .OrderByDescending(u => u.StartMa!.Value)
                .ToList();

            _allByStartMaDesc = catalog.Units
                .Where(u => u.StartMa.HasValue && u.EndMa.HasValue)
                .OrderByDescending(u => u.StartMa!.Value)
                .ToList();

            Debug.WriteLine($"[ChronostratDataService] Loaded {catalog.Units.Count} units, " +
                            $"{_leafsByStartMaDesc.Count} age-level leaves.");
        }

        /// <summary>
        /// 在已按 StartMa 降序排列的列表中查找最佳匹配：
        /// 即 StartMa >= ageMa > EndMa 的节点；若多个匹配则取 Rank 最大（最小划分）的。
        /// </summary>
        private static ChronostratUnitRecord? FindBestMatch(
            List<ChronostratUnitRecord> sortedDesc, double ageMa)
        {
            ChronostratUnitRecord? best = null;

            foreach (var unit in sortedDesc)
            {
                double start = unit.StartMa!.Value;
                double end   = unit.EndMa!.Value;

                // 区间判断：ageMa 在 (end, start] 之内
                // 底界（start）使用闭区间，顶界（end）使用开区间
                if (ageMa > start) break; // 列表已降序，后面更老，不可能再匹配

                if (ageMa <= start && ageMa > end)
                {
                    if (best == null || unit.Rank > best.Rank)
                        best = unit;
                }
            }

            return best;
        }
    }
}
