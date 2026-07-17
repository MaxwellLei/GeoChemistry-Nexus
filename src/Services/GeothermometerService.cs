using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using unvell.ReoGrid;
using unvell.ReoGrid.Formula;

namespace GeoChemistryNexus.Services
{
    /// <summary>
    /// 地质温压计（GTM）管理服务
    /// 从 LiteDB 加载温压计，通过 Jint 注册 JS 脚本为 ReoGrid 自定义函数
    /// 支持 ZIP 导入/导出，从服务器下载更新
    /// </summary>
    public static class GeothermometerService
    {
        // 服务器地址
        private const string DefaultServerBaseUrl = "https://geochemistrynexus-1303234197.cos.ap-hongkong.myqcloud.com/Geothermometer";
        private const string GeoTIndexFileName = "GeoT-index.json";
        private const string GeoTListFileName = "GeoT-List.json";

        /// <summary>
        /// JS 脚本中注入的数学辅助函数
        /// </summary>
        private const string MathHelpers = @"
            var log = Math.log;
            var log10 = function(x) { return Math.log(x) / Math.LN10; };
            var abs = Math.abs;
            var sqrt = Math.sqrt;
            var pow = Math.pow;
            var exp = Math.exp;
            var min = Math.min;
            var max = Math.max;
            var round = Math.round;
            var floor = Math.floor;
            var ceil = Math.ceil;
            var PI = Math.PI;
            var E = Math.E;
        ";

        private static readonly JsonSerializerOptions JsonOptions = JsonHelper.DefaultOptions;

        private static readonly List<GeothermometerEntity> _loadedEntities = new();
        private static readonly object _registryLock = new();
        private static readonly ConcurrentDictionary<string, Lazy<CachedScriptEngine>> _scriptEngines =
            new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<Guid, HashSet<string>> _entityFormulaNames = new();
        private static string _serverBaseUrl = DefaultServerBaseUrl;

        /// <summary>
        /// 本地 GeoT-List.json 存储路径
        /// </summary>
        private static string LocalListFilePath =>
            AppDataPathHelper.GetDataPath("Plugins", "GeoT-List.json");

        private static string LocalMineralCategoriesFilePath =>
            GeoTMineralCategoryHelper.LocalConfigPath;

        /// <summary>
        /// 获取已加载的全部温压计实体（摘要）
        /// </summary>
        public static IReadOnlyList<GeothermometerEntity> LoadedEntities => _loadedEntities.AsReadOnly();

        public static void SetServerBaseUrl(string url)
        {
            if (!string.IsNullOrWhiteSpace(url))
                _serverBaseUrl = url.TrimEnd('/');
        }

        /// <summary>
        /// 初始化：从 LiteDB 加载所有温压计并注册公式
        /// </summary>
        public static void Initialize()
        {
            ReloadPlugins();
        }

        /// <summary>
        /// 从数据库全量加载温压计并注册 JS 公式（启动或批量同步后使用）
        /// </summary>
        public static void ReloadPlugins()
        {
            lock (_registryLock)
            {
                ClearRegisteredFormulas();
                _loadedEntities.Clear();
                _entityFormulaNames.Clear();

                var dbService = GeothermometerDatabaseService.Instance;
                var summaries = dbService.GetSummaries()
                    .OrderBy(e => e.PluginId, StringComparer.Ordinal)
                    .ToList();

                var formulaNameConflicts = FindAllFormulaNameConflicts(summaries);
                foreach (var conflict in formulaNameConflicts)
                {
                    Debug.WriteLine(
                        $"[GeothermometerService] 公式名冲突: '{conflict.FormulaName}' " +
                        $"已被 '{conflict.ExistingName}' ({conflict.ExistingPluginId}) 使用，" +
                        $"'{conflict.CandidateName}' ({conflict.CandidatePluginId}) 的注册已跳过");
                }

                var registeredFormulaNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var summary in summaries)
                {
                    _loadedEntities.Add(CloneSummary(summary));

                    var registrationEntity = dbService.GetEntityForRegistration(summary.Id);
                    if (registrationEntity == null || string.IsNullOrEmpty(registrationEntity.ScriptContent))
                        continue;

                    RegisterEntityFormulas(registrationEntity, registeredFormulaNames);
                }
            }
        }

        /// <summary>
        /// 增量更新单个温压计的内存摘要与公式注册（保存/导入后使用）
        /// </summary>
        public static void UpsertLoadedPlugin(GeothermometerEntity entity)
        {
            if (entity == null)
                return;

            lock (_registryLock)
            {
                UnregisterEntityFormulas(entity.Id);

                var summary = ToSummary(entity);
                int index = _loadedEntities.FindIndex(e => e.Id == entity.Id);
                if (index >= 0)
                    _loadedEntities[index] = summary;
                else
                    _loadedEntities.Add(summary);

                if (!string.IsNullOrEmpty(entity.ScriptContent))
                {
                    var occupied = GetOccupiedFormulaNames(excludeEntityId: entity.Id);
                    RegisterEntityFormulas(entity, occupied);
                }
            }
        }

        /// <summary>
        /// 从内存摘要与公式注册中移除指定温压计
        /// </summary>
        public static void UnloadPlugin(Guid entityId)
        {
            lock (_registryLock)
            {
                UnregisterEntityFormulas(entityId);
                _loadedEntities.RemoveAll(e => e.Id == entityId);
            }
        }

        private static void ClearRegisteredFormulas()
        {
            var customFunctions = FormulaExtension.CustomFunctions;
            if (customFunctions != null)
            {
                foreach (var formulaNames in _entityFormulaNames.Values)
                {
                    foreach (var formulaName in formulaNames)
                        customFunctions.Remove(formulaName);
                }
            }

            _entityFormulaNames.Clear();
            _scriptEngines.Clear();
        }

        private static void UnregisterEntityFormulas(Guid entityId)
        {
            if (_entityFormulaNames.TryGetValue(entityId, out var formulaNames))
            {
                var customFunctions = FormulaExtension.CustomFunctions;
                if (customFunctions != null)
                {
                    foreach (var formulaName in formulaNames)
                        customFunctions.Remove(formulaName);
                }

                _entityFormulaNames.Remove(entityId);
            }

            _scriptEngines.TryRemove(entityId.ToString("N"), out _);
        }

        private static HashSet<string> GetOccupiedFormulaNames(Guid? excludeEntityId)
        {
            var occupied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in _entityFormulaNames)
            {
                if (excludeEntityId.HasValue && pair.Key == excludeEntityId.Value)
                    continue;

                occupied.UnionWith(pair.Value);
            }

            return occupied;
        }

        private static void RegisterEntityFormulas(
            GeothermometerEntity entity,
            HashSet<string> registeredFormulaNames)
        {
            if (entity == null || string.IsNullOrEmpty(entity.ScriptContent))
                return;

            var registeredForEntity = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(entity.FormulaName)
                && registeredFormulaNames.Add(entity.FormulaName))
            {
                RegisterScriptFormula(entity);
                registeredForEntity.Add(entity.FormulaName);
            }

            if (entity.AdditionalFormulas != null)
            {
                foreach (var af in entity.AdditionalFormulas)
                {
                    if (af == null
                        || string.IsNullOrEmpty(af.FormulaName)
                        || string.IsNullOrEmpty(af.FunctionName))
                        continue;

                    if (!registeredFormulaNames.Add(af.FormulaName))
                        continue;

                    RegisterAdditionalFormula(entity, af);
                    registeredForEntity.Add(af.FormulaName);
                }
            }

            if (registeredForEntity.Count > 0)
                _entityFormulaNames[entity.Id] = registeredForEntity;
        }

        private static CachedScriptEngine GetOrCreateScriptEngine(GeothermometerEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrEmpty(entity.ScriptContent))
                throw new InvalidOperationException("ScriptContent is empty.");

            string cacheKey = entity.Id.ToString("N");
            string script = entity.ScriptContent;
            // 统一使用较长超时：表格单元格与详细计算共用同一缓存引擎
            var lazy = _scriptEngines.GetOrAdd(cacheKey, _ => new Lazy<CachedScriptEngine>(() =>
            {
                var engine = new Engine(cfg => cfg.TimeoutInterval(TimeSpan.FromSeconds(10)));
                engine.Execute(MathHelpers);
                engine.Execute(script);
                return new CachedScriptEngine(engine);
            }, LazyThreadSafetyMode.ExecutionAndPublication));

            return lazy.Value;
        }

        /// <summary>
        /// 注册主公式
        /// </summary>
        private static void RegisterScriptFormula(GeothermometerEntity entity)
        {
            try
            {
                if (FormulaExtension.CustomFunctions == null || string.IsNullOrEmpty(entity.FormulaName))
                    return;

                var registrationEntity = CloneForRegistration(entity);

                FormulaExtension.CustomFunctions[entity.FormulaName] = (cell, args) =>
                {
                    try
                    {
                        var cached = GetOrCreateScriptEngine(registrationEntity);
                        lock (cached.SyncRoot)
                        {
                            var doubleArgs = new double[args.Length];
                            for (int i = 0; i < args.Length; i++)
                                doubleArgs[i] = Convert.ToDouble(args[i]);

                            var result = cached.Engine.Invoke("calculate", new object[] { doubleArgs });
                            if (result == null || result.IsNull() || result.IsUndefined())
                                return null;

                            return Convert.ToDouble(result.AsNumber());
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[GeothermometerService] 脚本执行失败 [{entity.FormulaName}]: {ex.Message}");
                        return null;
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GeothermometerService] 注册脚本公式失败 [{entity.FormulaName}]: {ex.Message}");
            }
        }

        /// <summary>
        /// 注册附加公式
        /// </summary>
        private static void RegisterAdditionalFormula(GeothermometerEntity entity, AdditionalFormula af)
        {
            if (string.IsNullOrEmpty(af.FormulaName) || string.IsNullOrEmpty(af.FunctionName))
                return;

            try
            {
                if (FormulaExtension.CustomFunctions == null)
                    return;

                var registrationEntity = CloneForRegistration(entity);
                string jsFuncName = af.FunctionName;

                FormulaExtension.CustomFunctions[af.FormulaName] = (cell, args) =>
                {
                    try
                    {
                        var cached = GetOrCreateScriptEngine(registrationEntity);
                        lock (cached.SyncRoot)
                        {
                            var jsArgs = new object[args.Length];
                            for (int i = 0; i < args.Length; i++)
                                jsArgs[i] = args[i];

                            var result = cached.Engine.Invoke(jsFuncName, jsArgs);
                            if (result == null || result.IsNull() || result.IsUndefined())
                                return null;
                            if (result.IsNumber())
                                return Convert.ToDouble(result.AsNumber());
                            if (result.IsString())
                                return result.AsString();
                            return result.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[GeothermometerService] 附加公式执行失败 [{af.FormulaName}]: {ex.Message}");
                        return null;
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GeothermometerService] 注册附加公式失败 [{af.FormulaName}]: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行详细计算（中间步骤）
        /// </summary>
        public static List<CalculationStep> ExecuteDetailedCalculation(GeothermometerEntity entity, double[] inputValues)
        {
            var steps = new List<CalculationStep>();

            // 需要完整脚本内容
            var fullEntity = entity;
            if (string.IsNullOrEmpty(fullEntity.ScriptContent))
            {
                fullEntity = GeothermometerDatabaseService.Instance.GetEntityForRegistration(entity.Id)
                    ?? GeothermometerDatabaseService.Instance.GetEntity(entity.Id);
            }

            if (fullEntity == null || string.IsNullOrEmpty(fullEntity.ScriptContent))
                return steps;

            try
            {
                var cached = GetOrCreateScriptEngine(fullEntity);
                lock (cached.SyncRoot)
                {
                    var funcValue = cached.Engine.GetValue("calculateDetailed");
                    if (funcValue == null || funcValue.IsUndefined())
                    {
                        var simpleResult = cached.Engine.Invoke("calculate", new object[] { inputValues });
                        if (simpleResult != null && !simpleResult.IsNull() && !simpleResult.IsUndefined())
                        {
                            steps.Add(new CalculationStep
                            {
                                Name = "T(K)",
                                Value = simpleResult.AsNumber().ToString("F2"),
                                IsResult = true
                            });
                        }
                        ApplyCalculationStepGroupVisibility(steps);
                        return steps;
                    }

                    var result = cached.Engine.Invoke("calculateDetailed", new object[] { inputValues });
                    if (result != null && result.IsArray())
                    {
                        var array = result.AsArray();
                        foreach (var item in array)
                        {
                            if (item.IsObject())
                            {
                                var obj = item.AsObject();
                                steps.Add(ParseCalculationStep(obj));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GeothermometerService] 详细计算失败 [{entity.FormulaName}]: {ex.Message}");
                steps.Add(new CalculationStep { Name = "Error", Value = ex.Message });
            }

            ApplyCalculationStepGroupVisibility(steps);
            return steps;
        }

        /// <summary>
        /// 将 JS 步骤对象映射为 CalculationStep（未知字段忽略，保证旧脚本兼容）。
        /// </summary>
        private static CalculationStep ParseCalculationStep(ObjectInstance obj)
        {
            var step = new CalculationStep
            {
                Name = ResolveStepName(obj),
                Value = FormatStepValue(obj.Get("value")),
                Description = ResolveStepDescription(obj),
                IsResult = GetOptionalBoolean(obj, "isResult"),
                IsHighlight = GetOptionalBoolean(obj, "isHighlight"),
                IsSeparator = GetOptionalBoolean(obj, "isSeparator"),
                IsCollapsed = GetOptionalBoolean(obj, "collapsed")
            };

            if (TryNormalizeHexColor(GetOptionalString(obj, "backgroundColor"), out string hex))
                step.BackgroundColor = hex;

            return step;
        }

        /// <summary>
        /// 按分隔标题的折叠状态，刷新组内行的可见性。
        /// </summary>
        public static void ApplyCalculationStepGroupVisibility(IList<CalculationStep> steps)
        {
            if (steps == null || steps.Count == 0)
                return;

            bool hiding = false;
            foreach (var step in steps)
            {
                if (step.IsSeparator)
                {
                    step.IsVisible = true;
                    hiding = step.IsCollapsed;
                    continue;
                }

                step.IsVisible = !hiding;
            }
        }

        private static string ResolveStepName(ObjectInstance stepObject)
        {
            string defaultName = stepObject.Get("name")?.AsString() ?? string.Empty;

            var nameLangValue = stepObject.Get("nameLang");
            if (nameLangValue == null || nameLangValue.IsUndefined() || nameLangValue.IsNull() || !nameLangValue.IsObject())
                return defaultName;

            return ResolveLocalizedMap(nameLangValue.AsObject(), defaultName);
        }

        private static string FormatStepValue(JsValue? value)
        {
            if (value == null || value.IsUndefined() || value.IsNull())
                return string.Empty;
            if (value.IsString())
                return value.AsString();
            return value.ToString() ?? string.Empty;
        }

        private static bool GetOptionalBoolean(ObjectInstance obj, string propertyName)
        {
            var prop = obj.Get(propertyName);
            return prop.IsBoolean() && prop.AsBoolean();
        }

        private static string? GetOptionalString(ObjectInstance obj, string propertyName)
        {
            var prop = obj.Get(propertyName);
            if (prop == null || prop.IsUndefined() || prop.IsNull())
                return null;
            if (prop.IsString())
                return prop.AsString();
            return prop.ToString();
        }

        private static bool TryNormalizeHexColor(string? input, out string hex)
        {
            hex = string.Empty;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            string s = input.Trim();
            if (!s.StartsWith("#", StringComparison.Ordinal))
                s = "#" + s;

            if (Regex.IsMatch(s, @"^#[0-9A-Fa-f]{3}$"))
            {
                hex = $"#{s[1]}{s[1]}{s[2]}{s[2]}{s[3]}{s[3]}";
                return true;
            }

            if (Regex.IsMatch(s, @"^#[0-9A-Fa-f]{6}$") || Regex.IsMatch(s, @"^#[0-9A-Fa-f]{8}$"))
            {
                hex = s;
                return true;
            }

            return false;
        }

        private static string ResolveLocalizedMap(ObjectInstance langObject, string defaultText)
        {
            var translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in langObject.GetOwnProperties())
            {
                string? key = property.Key.AsString();
                string? text = langObject.Get(property.Key)?.AsString();
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(text))
                    translations[key] = text;
            }

            if (translations.Count == 0)
                return defaultText;

            string requested = LanguageService.CurrentLanguage;
            if (AppCultureRegistry.TryNormalize(requested, out string normalized) &&
                translations.TryGetValue(normalized, out string? localized) &&
                !string.IsNullOrEmpty(localized))
            {
                return localized;
            }

            foreach (var pair in translations)
            {
                if (string.Equals(pair.Key, requested, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(pair.Value))
                {
                    return pair.Value;
                }
            }

            return defaultText;
        }

        private static GeothermometerEntity CloneSummary(GeothermometerEntity entity)
            => ToSummary(entity);

        private static GeothermometerEntity ToSummary(GeothermometerEntity entity)
        {
            return new GeothermometerEntity
            {
                Id = entity.Id,
                PluginId = entity.PluginId,
                Version = entity.Version,
                FileHash = entity.FileHash,
                LastModified = entity.LastModified,
                IsOfficial = entity.IsOfficial,
                IsFavorite = entity.IsFavorite,
                Category = entity.Category,
                Tags = entity.Tags != null ? new List<string>(entity.Tags) : new List<string>(),
                Capabilities = GeoTCapabilityHelper.NormalizeList(entity.Capabilities),
                Name = entity.Name,
                NameLangKey = entity.NameLangKey,
                Author = entity.Author,
                Year = entity.Year,
                Reference = entity.Reference,
                IconCode = entity.IconCode,
                IconColor = entity.IconColor,
                Headers = entity.Headers != null ? new List<string>(entity.Headers) : new List<string>(),
                ExampleRow = entity.ExampleRow != null ? new List<string>(entity.ExampleRow) : new List<string>(),
                FormulaName = entity.FormulaName,
                InputColumns = entity.InputColumns != null ? new List<string>(entity.InputColumns) : new List<string>(),
                AdditionalFormulas = entity.AdditionalFormulas != null
                    ? new List<AdditionalFormula>(entity.AdditionalFormulas)
                    : new List<AdditionalFormula>(),
                ScriptContent = string.Empty,
                HelpDocuments = new Dictionary<string, string>()
            };
        }

        private static GeothermometerEntity CloneForRegistration(GeothermometerEntity entity)
        {
            return new GeothermometerEntity
            {
                Id = entity.Id,
                PluginId = entity.PluginId,
                FormulaName = entity.FormulaName,
                ScriptContent = entity.ScriptContent,
                AdditionalFormulas = entity.AdditionalFormulas
            };
        }

        private sealed class CachedScriptEngine
        {
            public CachedScriptEngine(Engine engine)
            {
                Engine = engine;
            }

            public Engine Engine { get; }
            public object SyncRoot { get; } = new();
        }

        /// <summary>
        /// 解析计算步骤注释：desc 为默认文本；可选 descLang 按当前界面语言取值，未命中时回退 desc。
        /// </summary>
        private static string ResolveStepDescription(ObjectInstance stepObject)
        {
            string defaultDesc = stepObject.Get("desc")?.AsString() ?? string.Empty;

            var descLangValue = stepObject.Get("descLang");
            if (descLangValue == null || descLangValue.IsUndefined() || descLangValue.IsNull() || !descLangValue.IsObject())
                return defaultDesc;

            return ResolveLocalizedMap(descLangValue.AsObject(), defaultDesc);
        }

        /// <summary>
        /// 获取按类别分组的温压计列表（官方温压计按三个固定类别分组）
        /// </summary>
        /// <param name="isOfficial">null=全部, true=仅官方, false=仅自定义</param>
        public static List<GeoTCategoryGroup> GetGroupedEntities(bool? isOfficial = null)
        {
            var groups = new Dictionary<string, GeoTCategoryGroup>(StringComparer.OrdinalIgnoreCase);

            foreach (string categoryKey in GeoTCategoryHelper.GetCategoryKeys())
            {
                groups[categoryKey] = new GeoTCategoryGroup
                {
                    CategoryKey = categoryKey,
                    DisplayName = GeoTCategoryHelper.GetDisplayName(categoryKey),
                    Plugins = new List<Geothermometer>()
                };
            }

            var entities = isOfficial.HasValue
                ? _loadedEntities.Where(e => e.IsOfficial == isOfficial.Value)
                : _loadedEntities.AsEnumerable();

            foreach (var entity in entities)
            {
                string categoryKey = GeoTCategoryHelper.NormalizeCategoryKey(entity.Category);
                if (!groups.TryGetValue(categoryKey, out var group))
                {
                    group = new GeoTCategoryGroup
                    {
                        CategoryKey = categoryKey,
                        DisplayName = GeoTCategoryHelper.GetDisplayName(categoryKey),
                        Plugins = new List<Geothermometer>()
                    };
                    groups[categoryKey] = group;
                }

                group.Plugins.Add(EntityToGeothermometer(entity));
            }

            return GeoTCategoryHelper.GetCategoryKeys()
                .Select(key => groups.TryGetValue(key, out var group) ? group : null)
                .Where(group => group != null && group.Plugins.Count > 0)
                .Cast<GeoTCategoryGroup>()
                .ToList();
        }

        /// <summary>
        /// 获取自定义温压计的平面列表（不按矿物分组）
        /// </summary>
        public static List<Geothermometer> GetCustomPlugins()
        {
            return _loadedEntities
                .Where(e => !e.IsOfficial)
                .Select(e => EntityToGeothermometer(e))
                .ToList();
        }

        /// <summary>
        /// 保存温压计到数据库并重新注册公式（支持官方和自定义）
        /// </summary>
        public static GeothermometerEntity SaveEntity(GeothermometerEntity entity)
        {
            ValidateFormulaNames(entity, entity.Id == Guid.Empty ? null : entity.Id);

            entity.ExampleRow = CommaSeparatedListHelper.AlignToHeaderCount(entity.Headers, entity.ExampleRow);
            entity.Capabilities = GeoTCapabilityHelper.NormalizeList(entity.Capabilities);

            entity.LastModified = DateTime.Now;
            entity.FileHash = GeothermometerDatabaseService.ComputeEntityHash(entity);

            // 如果是新建，生成 ID
            if (entity.Id == Guid.Empty)
            {
                if (string.IsNullOrEmpty(entity.PluginId))
                    entity.PluginId = (entity.IsOfficial ? "official_" : "custom_") + Guid.NewGuid().ToString("N");
                entity.Id = GeothermometerDatabaseService.GenerateId(entity.PluginId);
            }

            GeothermometerDatabaseService.Instance.UpsertEntity(entity);
            UpsertLoadedPlugin(entity);
            return entity;
        }

        /// <summary>
        /// 保存自定义温压计（兼容旧调用，强制 IsOfficial=false）
        /// </summary>
        public static GeothermometerEntity SaveCustomEntity(GeothermometerEntity entity)
        {
            entity.IsOfficial = false;
            return SaveEntity(entity);
        }

        /// <summary>
        /// 将自定义温压计转换为官方温压计（重新生成 PluginId 和 Guid）
        /// </summary>
        public static bool ConvertToOfficial(Guid entityId)
        {
            var dbService = GeothermometerDatabaseService.Instance;
            var entity = dbService.GetEntity(entityId);
            if (entity == null) return false;

            // 删除旧记录
            dbService.DeleteEntity(entityId);
            UnloadPlugin(entityId);

            // 生成新的官方 PluginId 和 Guid
            entity.PluginId = "official_" + Guid.NewGuid().ToString("N");
            entity.Id = GeothermometerDatabaseService.GenerateId(entity.PluginId);
            entity.IsOfficial = true;
            entity.LastModified = DateTime.Now;
            entity.FileHash = GeothermometerDatabaseService.ComputeEntityHash(entity);

            dbService.UpsertEntity(entity);
            UpsertLoadedPlugin(entity);
            return true;
        }

        /// <summary>
        /// 将官方温压计降级为自定义温压计（重新生成 PluginId 和 Guid）
        /// </summary>
        public static bool ConvertToCustom(Guid entityId)
        {
            var dbService = GeothermometerDatabaseService.Instance;
            var entity = dbService.GetEntity(entityId);
            if (entity == null) return false;

            // 删除旧记录
            dbService.DeleteEntity(entityId);
            UnloadPlugin(entityId);

            // 生成新的自定义 PluginId 和 Guid
            entity.PluginId = "custom_" + Guid.NewGuid().ToString("N");
            entity.Id = GeothermometerDatabaseService.GenerateId(entity.PluginId);
            entity.IsOfficial = false;
            entity.LastModified = DateTime.Now;
            entity.FileHash = GeothermometerDatabaseService.ComputeEntityHash(entity);

            dbService.UpsertEntity(entity);
            UpsertLoadedPlugin(entity);
            return true;
        }

        /// <summary>
        /// 删除温压计（支持删除官方和自定义）
        /// </summary>
        public static bool DeleteEntity(Guid entityId)
        {
            var entity = GeothermometerDatabaseService.Instance.GetEntity(entityId);
            if (entity == null)
                return false;

            GeothermometerDatabaseService.Instance.DeleteEntity(entityId);
            UnloadPlugin(entityId);
            return true;
        }

        /// <summary>
        /// 验证脚本是否可以正确执行 calculate 函数
        /// </summary>
        public static (bool success, string result, string error) TestScript(string scriptContent, double[] testInputs)
        {
            try
            {
                var engine = new Engine(cfg => cfg.TimeoutInterval(TimeSpan.FromSeconds(5)));
                engine.Execute(MathHelpers);
                engine.Execute(scriptContent);

                var result = engine.Invoke("calculate", new object[] { testInputs });
                if (result == null || result.IsNull() || result.IsUndefined())
                    return (false, "", "calculate() returned null or undefined");

                double value = Convert.ToDouble(result.AsNumber());
                return (true, value.ToString("F4"), "");
            }
            catch (Exception ex)
            {
                return (false, "", ex.Message);
            }
        }

        /// <summary>
        /// 将数据库实体转换为 UI 用的轻量对象
        /// </summary>
        public static Geothermometer CreateGeothermometerFromEntity(GeothermometerEntity entity)
            => EntityToGeothermometer(entity);

        /// <summary>
        /// 将数据库实体转换为 UI 用的轻量对象
        /// </summary>
        private static Geothermometer EntityToGeothermometer(GeothermometerEntity entity)
        {
            return new Geothermometer
            {
                Id = entity.PluginId ?? string.Empty,
                Version = entity.Version ?? string.Empty,
                Category = GeoTCategoryHelper.NormalizeCategoryKey(entity.Category),
                Tags = ResolveTagsDisplayNames(entity),
                StorageTags = GetEntityTagSourceNames(entity).ToList(),
                Capabilities = GeoTCapabilityHelper.NormalizeList(entity.Capabilities),
                Name = entity.Name ?? string.Empty,
                NameLangKey = entity.NameLangKey ?? string.Empty,
                Author = entity.Author ?? string.Empty,
                Year = entity.Year,
                Reference = entity.Reference ?? string.Empty,
                IconCode = entity.IconCode ?? "\ue60d",
                IconColor = entity.IconColor ?? "#555555",
                Headers = entity.Headers ?? new List<string>(),
                ExampleRow = CommaSeparatedListHelper.AlignToHeaderCount(entity.Headers, entity.ExampleRow),
                FormulaName = entity.FormulaName ?? string.Empty,
                InputColumns = entity.InputColumns ?? new List<string>(),
                AdditionalFormulas = entity.AdditionalFormulas ?? new List<AdditionalFormula>(),
                IsBuiltIn = entity.IsOfficial,
                IsFavorite = entity.IsFavorite
            };
        }

        // ==================== 导出/导入 ZIP ====================

        /// <summary>
        /// 导出温压计为 ZIP 文件
        /// ZIP 包含: {pluginId}.json (元数据) + {pluginId}.js (脚本) + *.rtf (帮助文档)
        /// </summary>
        public static void ExportToZip(Guid entityId, string zipFilePath)
        {
            var entity = GeothermometerDatabaseService.Instance.GetEntity(entityId);
            if (entity == null)
                throw new InvalidOperationException("Geothermometer entity not found");

            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // 1. 导出 JSON 元数据（不含脚本和帮助文档内容）
                var exportMeta = new Dictionary<string, object>
                {
                    ["Id"] = entity.PluginId,
                    ["Version"] = entity.Version,
                    ["IsOfficial"] = entity.IsOfficial,
                    ["Category"] = GeoTCategoryHelper.NormalizeCategoryKey(entity.Category),
                    ["Tags"] = entity.Tags ?? new List<string>(),
                    ["Name"] = entity.Name,
                    ["NameLangKey"] = entity.NameLangKey,
                    ["Author"] = entity.Author,
                    ["Year"] = entity.Year,
                    ["Reference"] = entity.Reference,
                    ["IconCode"] = entity.IconCode,
                    ["IconColor"] = entity.IconColor,
                    ["Headers"] = entity.Headers,
                    ["ExampleRow"] = entity.ExampleRow,
                    ["FormulaName"] = entity.FormulaName,
                    ["InputColumns"] = entity.InputColumns,
                    ["AdditionalFormulas"] = entity.AdditionalFormulas,
                    ["ScriptFile"] = $"{entity.PluginId}.js"
                };

                // 与哈希口径一致：仅在有能力标签时写入，避免空数组破坏与旧版清单的兼容
                var exportCapabilities = GeoTCapabilityHelper.NormalizeList(entity.Capabilities);
                if (exportCapabilities.Count > 0)
                    exportMeta["Capabilities"] = exportCapabilities;

                string jsonPath = Path.Combine(tempDir, $"{entity.PluginId}.json");
                File.WriteAllText(jsonPath, JsonSerializer.Serialize(exportMeta, JsonOptions));

                // 2. 导出 JS 脚本
                if (!string.IsNullOrEmpty(entity.ScriptContent))
                {
                    string jsPath = Path.Combine(tempDir, $"{entity.PluginId}.js");
                    File.WriteAllText(jsPath, entity.ScriptContent);
                }

                // 3. 导出帮助文档（RTF）
                if (entity.HelpDocuments != null)
                {
                    foreach (var doc in entity.HelpDocuments)
                    {
                        string rtfPath = Path.Combine(tempDir, $"{doc.Key}.rtf");
                        File.WriteAllText(rtfPath, doc.Value);
                    }
                }

                // 4. 打包
                if (File.Exists(zipFilePath))
                    File.Delete(zipFilePath);

                ZipFile.CreateFromDirectory(tempDir, zipFilePath);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// 从 ZIP 文件导入温压计
        /// </summary>
        /// <param name="zipFilePath">ZIP 文件路径</param>
        /// <param name="persist">是否立即写入数据库（服务器下载场景应在哈希校验后再写入）</param>
        /// <param name="keepPluginIdentity">是否保留 ZIP 中的 PluginId（服务器下载为 true；用户导入为 false，生成新的自定义 ID）</param>
        /// <returns>导入的实体</returns>
        public static GeothermometerEntity ImportFromZip(string zipFilePath, bool persist = true, bool keepPluginIdentity = false)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                try
                {
                    ZipFile.ExtractToDirectory(zipFilePath, tempDir);
                }
                catch (Exception ex) when (ex is InvalidDataException or IOException)
                {
                    throw new GeothermometerImportException(GeothermometerImportFailureReason.InvalidOrCorrupted, ex);
                }

                // 查找 JSON 文件
                var jsonFiles = Directory.GetFiles(tempDir, "*.json");
                if (jsonFiles.Length == 0)
                    throw new GeothermometerImportException(GeothermometerImportFailureReason.InvalidOrCorrupted);

                string jsonContent = File.ReadAllText(jsonFiles[0]);
                var plugin = ParseAndValidateImportJson(jsonContent);

                plugin.Version = ContentVersionHelper.Normalize(plugin.Version);
                if (!ContentVersionHelper.IsGeothermometerFormatCompatible(plugin.Version))
                {
                    Debug.WriteLine($"[GeothermometerService] GTM 格式版本不兼容 [{plugin.Id}]: {plugin.Version}");
                    throw new GeothermometerImportException(GeothermometerImportFailureReason.VersionIncompatible);
                }

                // 用户导入：始终作为自定义温压计，分配新 PluginId，避免覆盖已有官方条目
                string pluginId = keepPluginIdentity
                    ? plugin.Id
                    : "custom_" + Guid.NewGuid().ToString("N");

                // 加载 JS 脚本
                string scriptContent = string.Empty;
                if (!string.IsNullOrEmpty(plugin.ScriptFile))
                {
                    string jsPath = Path.Combine(tempDir, plugin.ScriptFile);
                    if (File.Exists(jsPath))
                        scriptContent = File.ReadAllText(jsPath);
                }
                // 也尝试按 pluginId 查找
                if (string.IsNullOrEmpty(scriptContent))
                {
                    var jsFiles = Directory.GetFiles(tempDir, "*.js");
                    if (jsFiles.Length > 0)
                        scriptContent = File.ReadAllText(jsFiles[0]);
                }
                if (string.IsNullOrEmpty(scriptContent) && !string.IsNullOrEmpty(plugin.Script))
                {
                    scriptContent = plugin.Script;
                }

                if (string.IsNullOrWhiteSpace(scriptContent))
                    throw new GeothermometerImportException(GeothermometerImportFailureReason.InvalidOrCorrupted);

                // 加载帮助文档
                var helpDocs = new Dictionary<string, string>();
                foreach (var rtfFile in Directory.GetFiles(tempDir, "*.rtf"))
                {
                    string langCode = Path.GetFileNameWithoutExtension(rtfFile);
                    helpDocs[langCode] = File.ReadAllText(rtfFile);
                }

                // 创建实体
                var entity = new GeothermometerEntity
                {
                    Id = GeothermometerDatabaseService.GenerateId(pluginId),
                    PluginId = pluginId,
                    Version = plugin.Version,
                    LastModified = DateTime.Now,
                    IsOfficial = keepPluginIdentity,
                    Category = plugin.Category,
                    Tags = plugin.Tags ?? new List<string>(),
                    Capabilities = GeoTCapabilityHelper.NormalizeList(plugin.Capabilities),
                    Name = plugin.Name,
                    NameLangKey = plugin.NameLangKey,
                    Author = plugin.Author,
                    Year = plugin.Year,
                    Reference = plugin.Reference,
                    IconCode = plugin.IconCode,
                    IconColor = plugin.IconColor,
                    Headers = plugin.Headers,
                    ExampleRow = CommaSeparatedListHelper.AlignToHeaderCount(plugin.Headers, plugin.ExampleRow),
                    FormulaName = plugin.FormulaName,
                    InputColumns = plugin.InputColumns ?? new List<string>(),
                    AdditionalFormulas = plugin.AdditionalFormulas ?? new List<AdditionalFormula>(),
                    ScriptContent = scriptContent,
                    HelpDocuments = helpDocs
                };
                entity.FileHash = GeothermometerDatabaseService.ComputeEntityHash(entity);

                if (persist)
                {
                    ValidateFormulaNames(entity, entity.Id);
                    GeothermometerDatabaseService.Instance.UpsertEntity(entity);
                    UpsertLoadedPlugin(entity);
                }

                return entity;
            }
            catch (GeothermometerImportException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GeothermometerService] 导入 ZIP 失败: {ex.Message}");
                throw new GeothermometerImportException(GeothermometerImportFailureReason.InvalidOrCorrupted, ex);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        private static Geothermometer ParseAndValidateImportJson(string jsonContent)
        {
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(jsonContent);
            }
            catch (JsonException ex)
            {
                throw new GeothermometerImportException(GeothermometerImportFailureReason.InvalidOrCorrupted, ex);
            }

            using (document)
            {
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                    throw new GeothermometerImportException(GeothermometerImportFailureReason.InvalidOrCorrupted);

                ValidateRequiredImportProperties(document.RootElement);
            }

            Geothermometer? plugin;
            try
            {
                plugin = JsonSerializer.Deserialize<Geothermometer>(jsonContent, JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new GeothermometerImportException(GeothermometerImportFailureReason.InvalidOrCorrupted, ex);
            }

            if (plugin == null
                || string.IsNullOrWhiteSpace(plugin.Id)
                || string.IsNullOrWhiteSpace(plugin.Name)
                || plugin.Headers == null
                || plugin.Headers.Count == 0
                || string.IsNullOrWhiteSpace(plugin.FormulaName))
            {
                throw new GeothermometerImportException(GeothermometerImportFailureReason.InvalidOrCorrupted);
            }

            return plugin;
        }

        private static void ValidateRequiredImportProperties(JsonElement root)
        {
            if (!TryGetJsonProperty(root, "Id", out _))
                throw new GeothermometerImportException(GeothermometerImportFailureReason.InvalidOrCorrupted);

            if (!TryGetJsonProperty(root, "Name", out _))
                throw new GeothermometerImportException(GeothermometerImportFailureReason.InvalidOrCorrupted);

            if (!TryGetJsonProperty(root, "FormulaName", out _))
                throw new GeothermometerImportException(GeothermometerImportFailureReason.InvalidOrCorrupted);

            if (!TryGetJsonProperty(root, "Headers", out var headersElement)
                || headersElement.ValueKind != JsonValueKind.Array
                || headersElement.GetArrayLength() == 0)
            {
                throw new GeothermometerImportException(GeothermometerImportFailureReason.InvalidOrCorrupted);
            }
        }

        private static bool TryGetJsonProperty(JsonElement element, string propertyName, out JsonElement value)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static IEnumerable<string> GetEntityTagSourceNames(GeothermometerEntity entity)
        {
            return (entity.Tags ?? new List<string>()).Where(tag => !string.IsNullOrWhiteSpace(tag));
        }

        private static List<string> ResolveTagsDisplayNames(GeothermometerEntity entity)
        {
            return GetEntityTagSourceNames(entity)
                .Select(ResolveTagDisplayName)
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string ResolveTagDisplayName(string tagKey)
        {
            if (string.IsNullOrWhiteSpace(tagKey))
                return tagKey ?? string.Empty;

            return GeoTMineralCategoryHelper.GetDisplayName(tagKey.Trim());
        }

        // ==================== 服务器更新 ====================

        /// <summary>
        /// 从服务器检查可用更新（两级校验）
        /// 1. 下载 GeoT-index.json 获取 GeoT-List.json 的哈希值
        /// 2. 对比本地 GeoT-List.json 的哈希，不一致则下载新的列表并校验完整性
        /// 3. 使用清单与本地数据库对账：仅返回需下载项与待下架项，不执行删除
        /// </summary>
        public static async Task<GeothermometerUpdateCheckResult> CheckForUpdatesAsync()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

                string indexUrl = $"{_serverBaseUrl}/{GeoTIndexFileName}";
                string indexJson = await client.GetStringAsync(indexUrl);
                var geoTIndex = JsonSerializer.Deserialize<GeoTIndex>(indexJson, JsonOptions);
                if (geoTIndex == null || string.IsNullOrEmpty(geoTIndex.ListHash))
                {
                    return new GeothermometerUpdateCheckResult
                    {
                        Status = GeothermometerUpdateCheckStatus.Failed,
                        ErrorMessage = "Invalid GeoT-index.json"
                    };
                }

                bool mineralCategoriesSynced = await SyncMineralCategoriesAsync(geoTIndex, client);

                bool needDownloadList = true;
                if (File.Exists(LocalListFilePath))
                {
                    string localListContent = await File.ReadAllTextAsync(LocalListFilePath);
                    string localListHash = GeothermometerDatabaseService.ComputeHash(localListContent);
                    if (string.Equals(localListHash, geoTIndex.ListHash, StringComparison.OrdinalIgnoreCase))
                        needDownloadList = false;
                }

                string listJson;
                if (needDownloadList)
                {
                    string listUrl = $"{_serverBaseUrl}/{GeoTListFileName}";
                    listJson = await client.GetStringAsync(listUrl);

                    string downloadedHash = GeothermometerDatabaseService.ComputeHash(listJson);
                    if (!string.Equals(downloadedHash, geoTIndex.ListHash, StringComparison.OrdinalIgnoreCase))
                    {
                        return new GeothermometerUpdateCheckResult
                        {
                            Status = GeothermometerUpdateCheckStatus.Failed,
                            ErrorMessage = LanguageService.Instance["downloaded_file_hash_mismatch"]
                        };
                    }

                    string? listDir = Path.GetDirectoryName(LocalListFilePath);
                    if (!string.IsNullOrEmpty(listDir) && !Directory.Exists(listDir))
                        Directory.CreateDirectory(listDir);
                    await File.WriteAllTextAsync(LocalListFilePath, listJson);
                }
                else if (File.Exists(LocalListFilePath))
                {
                    listJson = await File.ReadAllTextAsync(LocalListFilePath);
                }
                else
                {
                    return new GeothermometerUpdateCheckResult
                    {
                        Status = GeothermometerUpdateCheckStatus.Failed,
                        ErrorMessage = "Local GeoT-List.json is missing"
                    };
                }

                var pluginList = JsonSerializer.Deserialize<PluginIndex>(listJson, JsonOptions);
                if (pluginList?.Plugins == null)
                {
                    return new GeothermometerUpdateCheckResult
                    {
                        Status = GeothermometerUpdateCheckStatus.Failed,
                        ErrorMessage = "Invalid GeoT-List.json"
                    };
                }

                return BuildUpdateCheckResult(pluginList, mineralCategoriesSynced);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GeothermometerService] 检查更新失败: {ex.Message}");
                return new GeothermometerUpdateCheckResult
                {
                    Status = GeothermometerUpdateCheckStatus.Failed,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 从服务器同步矿物分类多语言文件（GeoT-index 中的 MineralCategoriesHash 校验）。
        /// </summary>
        public static async Task<bool> SyncMineralCategoriesAsync(GeoTIndex? geoTIndex, HttpClient? client = null)
        {
            if (geoTIndex == null || string.IsNullOrEmpty(geoTIndex.MineralCategoriesHash))
                return false;

            bool ownsClient = client == null;
            client ??= new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

            try
            {
                string localHash = string.Empty;
                if (File.Exists(LocalMineralCategoriesFilePath))
                {
                    string localContent = await File.ReadAllTextAsync(LocalMineralCategoriesFilePath);
                    localHash = GeothermometerDatabaseService.ComputeHash(localContent);
                }

                if (string.Equals(localHash, geoTIndex.MineralCategoriesHash, StringComparison.OrdinalIgnoreCase))
                    return false;

                string downloadUrl = $"{_serverBaseUrl}/{OfficialContentEndpoints.GeoTMineralCategoriesFileName}";
                string categoriesJson = await client.GetStringAsync(downloadUrl);

                string downloadedHash = GeothermometerDatabaseService.ComputeHash(categoriesJson);
                if (!string.Equals(downloadedHash, geoTIndex.MineralCategoriesHash, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine("[GeothermometerService] GeoTMineralCategories.json hash mismatch.");
                    return false;
                }

                try
                {
                    JsonDocument.Parse(categoriesJson);
                }
                catch (JsonException)
                {
                    Debug.WriteLine("[GeothermometerService] GeoTMineralCategories.json is not valid JSON.");
                    return false;
                }

                string? directory = Path.GetDirectoryName(LocalMineralCategoriesFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                await File.WriteAllTextAsync(LocalMineralCategoriesFilePath, categoriesJson);
                GeoTMineralCategoryHelper.InvalidateCache();
                WeakReferenceMessenger.Default.Send(new GeoTMineralCategoryUpdatedMessage("Updated"));
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GeothermometerService] Sync mineral categories failed: {ex.Message}");
                return false;
            }
            finally
            {
                if (ownsClient)
                    client.Dispose();
            }
        }

        /// <summary>
        /// 根据服务器清单对账本地官方温压计，返回需下载项与待下架项（无副作用）。
        /// </summary>
        private static GeothermometerUpdateCheckResult BuildUpdateCheckResult(
            PluginIndex pluginList,
            bool mineralCategoriesSynced = false)
        {
            var updatable = new List<PluginIndexEntry>();
            var requiresAppUpgrade = new List<PluginIndexEntry>();
            var serverPluginIds = new HashSet<string>();
            string appFormatVersion = ContentVersionHelper.GetGeothermometerFormatVersion();

            foreach (var entry in pluginList.Plugins)
            {
                serverPluginIds.Add(entry.Id);

                if (ContentVersionHelper.RequiresAppUpgrade(entry.Version, appFormatVersion))
                {
                    requiresAppUpgrade.Add(entry);
                    continue;
                }

                var local = _loadedEntities.FirstOrDefault(p => p.PluginId == entry.Id);
                if (local == null)
                {
                    updatable.Add(entry);
                }
                else if (!ContentVersionHelper.IsGeothermometerFormatCompatible(local.Version))
                {
                    updatable.Add(entry);
                }
                else if (ContentVersionHelper.HasContentUpdate(local.Version, entry.Version)
                         || ContentVersionHelper.Compare(entry.Version, local.Version) > 0)
                {
                    updatable.Add(entry);
                }
                else if (!string.IsNullOrEmpty(entry.Hash) && !string.Equals(entry.Hash, local.FileHash, StringComparison.OrdinalIgnoreCase))
                {
                    updatable.Add(entry);
                }
            }

            var removals = _loadedEntities
                .Where(e => e.IsOfficial && !serverPluginIds.Contains(e.PluginId))
                .Select(e => e.Id)
                .ToList();

            return new GeothermometerUpdateCheckResult
            {
                Status = GeothermometerUpdateCheckStatus.Success,
                Updates = updatable,
                Removals = removals,
                RequiresAppUpgrade = requiresAppUpgrade,
                MineralCategoriesSynced = mineralCategoriesSynced
            };
        }

        /// <summary>
        /// 删除已从服务器清单下架的官方温压计（在用户确认更新后调用）。
        /// </summary>
        public static int ApplyRemovals(IEnumerable<Guid> entityIds)
        {
            if (entityIds == null) return 0;

            int count = 0;
            foreach (var id in entityIds)
            {
                if (DeleteEntity(id))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// 从服务器拉取最新 GeoT-List.json 并查找指定温压计条目（强制更新用，跳过本地哈希缓存）。
        /// </summary>
        public static async Task<(PluginIndexEntry? Entry, string? ErrorMessage)> FetchFreshPluginIndexEntryAsync(string pluginId)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                return (null, "Plugin ID is empty");

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

                string indexUrl = $"{_serverBaseUrl}/{GeoTIndexFileName}";
                string indexJson = await client.GetStringAsync(indexUrl);
                var geoTIndex = JsonSerializer.Deserialize<GeoTIndex>(indexJson, JsonOptions);
                if (geoTIndex == null || string.IsNullOrEmpty(geoTIndex.ListHash))
                    return (null, "Invalid GeoT-index.json");

                await SyncMineralCategoriesAsync(geoTIndex, client);

                string listUrl = $"{_serverBaseUrl}/{GeoTListFileName}";
                string listJson = await client.GetStringAsync(listUrl);

                string downloadedHash = GeothermometerDatabaseService.ComputeHash(listJson);
                if (!string.Equals(downloadedHash, geoTIndex.ListHash, StringComparison.OrdinalIgnoreCase))
                    return (null, LanguageService.Instance["downloaded_file_hash_mismatch"]);

                string? listDir = Path.GetDirectoryName(LocalListFilePath);
                if (!string.IsNullOrEmpty(listDir) && !Directory.Exists(listDir))
                    Directory.CreateDirectory(listDir);
                await File.WriteAllTextAsync(LocalListFilePath, listJson);

                var pluginList = JsonSerializer.Deserialize<PluginIndex>(listJson, JsonOptions);
                if (pluginList?.Plugins == null)
                    return (null, "Invalid GeoT-List.json");

                var entry = pluginList.Plugins.FirstOrDefault(p =>
                    string.Equals(p.Id, pluginId, StringComparison.OrdinalIgnoreCase));

                return (entry, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GeothermometerService] Fetch fresh plugin entry failed [{pluginId}]: {ex.Message}");
                return (null, ex.Message);
            }
        }

        /// <summary>
        /// 从服务器下载 ZIP 并导入
        /// </summary>
        public static async Task<GeothermometerDownloadItemResult> DownloadPluginAsync(PluginIndexEntry entry)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                string downloadUrl = entry.DownloadUrl.StartsWith("http")
                    ? entry.DownloadUrl
                    : $"{_serverBaseUrl}/{entry.DownloadUrl}";

                byte[] zipBytes = await client.GetByteArrayAsync(downloadUrl);

                string tempZip = Path.Combine(Path.GetTempPath(), $"{entry.Id}.zip");
                await File.WriteAllBytesAsync(tempZip, zipBytes);

                try
                {
                    var imported = ImportFromZip(tempZip, persist: false, keepPluginIdentity: true);

                    if (!string.IsNullOrEmpty(entry.Hash) &&
                        !string.Equals(imported.FileHash, entry.Hash, StringComparison.OrdinalIgnoreCase))
                    {
                        string errorMessage = LanguageService.Instance["downloaded_file_hash_mismatch"];
                        Debug.WriteLine($"[GeothermometerService] GTM 哈希校验失败 [{entry.Id}]: expected={entry.Hash}, actual={imported.FileHash}");
                        return GeothermometerDownloadItemResult.Failed(entry.Id, errorMessage);
                    }

                    try
                    {
                        ValidateFormulaNames(imported, imported.Id);
                    }
                    catch (InvalidOperationException ex)
                    {
                        Debug.WriteLine($"[GeothermometerService] GTM 公式名冲突 [{entry.Id}]: {ex.Message}");
                        return GeothermometerDownloadItemResult.Failed(entry.Id, ex.Message);
                    }

                    GeothermometerDatabaseService.Instance.UpsertEntity(imported);
                    UpsertLoadedPlugin(imported);
                    return GeothermometerDownloadItemResult.Succeeded(entry.Id);
                }
                finally
                {
                    if (File.Exists(tempZip)) File.Delete(tempZip);
                }
            }
            catch (GeothermometerImportException ex)
            {
                string errorMessage = ex.Reason switch
                {
                    GeothermometerImportFailureReason.VersionIncompatible =>
                        LanguageService.Instance["template_version_too_high"],
                    _ => LanguageService.Instance["geo_msg_import_invalid_format"]
                };
                Debug.WriteLine($"[GeothermometerService] GTM 导入失败 [{entry.Id}]: {errorMessage}");
                return GeothermometerDownloadItemResult.Failed(entry.Id, errorMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GeothermometerService] 下载 GTM 失败 [{entry.Id}]: {ex.Message}");
                return GeothermometerDownloadItemResult.Failed(entry.Id, ex.Message);
            }
        }

        /// <summary>
        /// 批量下载、删除下架项并重新加载
        /// </summary>
        public static async Task<GeothermometerBatchDownloadResult> DownloadAndReloadAsync(
            List<PluginIndexEntry> entries,
            IEnumerable<Guid>? removals = null,
            IProgress<(int current, int total, string name)>? progress = null)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                string indexUrl = $"{_serverBaseUrl}/{GeoTIndexFileName}";
                string indexJson = await client.GetStringAsync(indexUrl);
                var geoTIndex = JsonSerializer.Deserialize<GeoTIndex>(indexJson, JsonOptions);
                await SyncMineralCategoriesAsync(geoTIndex, client);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GeothermometerService] Sync mineral categories before download failed: {ex.Message}");
            }

            var removalList = removals?.ToList() ?? new List<Guid>();
            int total = removalList.Count + entries.Count;
            int current = 0;
            int removalCount = 0;

            if (removalList.Count > 0)
            {
                removalCount = ApplyRemovals(removalList);
                current = removalList.Count;
                progress?.Report((current, total, LanguageService.Instance["geo_msg_downloading_update"]));
            }

            int successCount = 0;
            var failures = new List<GeothermometerDownloadItemResult>();
            foreach (var entry in entries)
            {
                current++;
                progress?.Report((current, total, entry.Id));
                var itemResult = await DownloadPluginAsync(entry);
                if (itemResult.Success)
                {
                    successCount++;
                }
                else
                {
                    failures.Add(itemResult);
                }
            }

            if (successCount > 0 || removalCount > 0)
                ReloadPlugins();

            return new GeothermometerBatchDownloadResult
            {
                SuccessCount = successCount,
                RemovalCount = removalCount,
                Failures = failures
            };
        }

        private static IEnumerable<string> EnumerateFormulaNames(GeothermometerEntity entity)
        {
            if (entity == null)
                yield break;

            if (!string.IsNullOrWhiteSpace(entity.FormulaName))
                yield return entity.FormulaName.Trim();

            if (entity.AdditionalFormulas == null)
                yield break;

            foreach (var additionalFormula in entity.AdditionalFormulas)
            {
                if (additionalFormula != null && !string.IsNullOrWhiteSpace(additionalFormula.FormulaName))
                    yield return additionalFormula.FormulaName.Trim();
            }
        }

        /// <summary>
        /// 检测候选温压计与数据库中已有温压计的公式名冲突
        /// </summary>
        public static List<FormulaNameConflict> FindFormulaNameConflicts(
            GeothermometerEntity candidate,
            Guid? excludeEntityId = null)
        {
            var conflicts = new List<FormulaNameConflict>();
            if (candidate == null)
                return conflicts;

            // 摘要已含 FormulaName / AdditionalFormulas，无需再读完整实体
            var summaries = GeothermometerDatabaseService.Instance.GetSummaries();

            foreach (var formulaName in EnumerateFormulaNames(candidate))
            {
                foreach (var existing in summaries)
                {
                    if (excludeEntityId.HasValue && existing.Id == excludeEntityId.Value)
                        continue;

                    if (!EnumerateFormulaNames(existing).Any(name =>
                            string.Equals(name, formulaName, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    conflicts.Add(new FormulaNameConflict
                    {
                        FormulaName = formulaName,
                        ExistingPluginId = existing.PluginId,
                        ExistingName = existing.Name,
                        CandidatePluginId = candidate.PluginId,
                        CandidateName = candidate.Name
                    });
                    break;
                }
            }

            return conflicts;
        }

        /// <summary>
        /// 检测一组温压计之间的公式名冲突（按 PluginId 排序，先出现者视为已占用）
        /// </summary>
        public static List<FormulaNameConflict> FindAllFormulaNameConflicts(IEnumerable<GeothermometerEntity> entities)
        {
            var conflicts = new List<FormulaNameConflict>();
            var ownerByFormulaName = new Dictionary<string, GeothermometerEntity>(StringComparer.OrdinalIgnoreCase);

            foreach (var entity in entities.OrderBy(e => e.PluginId, StringComparer.Ordinal))
            {
                foreach (var formulaName in EnumerateFormulaNames(entity))
                {
                    if (ownerByFormulaName.TryGetValue(formulaName, out var existing))
                    {
                        conflicts.Add(new FormulaNameConflict
                        {
                            FormulaName = formulaName,
                            ExistingPluginId = existing.PluginId,
                            ExistingName = existing.Name,
                            CandidatePluginId = entity.PluginId,
                            CandidateName = entity.Name
                        });
                    }
                    else
                    {
                        ownerByFormulaName[formulaName] = entity;
                    }
                }
            }

            return conflicts;
        }

        /// <summary>
        /// 校验公式名无内部重复且不与已有温压计冲突；不通过时抛出 InvalidOperationException
        /// </summary>
        public static void ValidateFormulaNames(GeothermometerEntity candidate, Guid? excludeEntityId = null)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var formulaName in EnumerateFormulaNames(candidate))
            {
                if (!seen.Add(formulaName))
                {
                    throw new InvalidOperationException(
                        string.Format(
                            LanguageService.Instance["geo_msg_formula_name_duplicate"],
                            formulaName,
                            candidate.Name));
                }
            }

            var conflicts = FindFormulaNameConflicts(candidate, excludeEntityId);
            if (conflicts.Count == 0)
                return;

            var conflict = conflicts[0];
            throw new InvalidOperationException(FormatFormulaNameConflictMessage(conflict));
        }

        private static string FormatFormulaNameConflictMessage(FormulaNameConflict conflict)
        {
            return string.Format(
                LanguageService.Instance["geo_msg_formula_name_conflict"],
                conflict.FormulaName,
                conflict.ExistingName,
                conflict.ExistingPluginId);
        }

        /// <summary>
        /// 判断温压计版本是否可在当前程序中加载。
        /// </summary>
        public static bool IsVersionCompatible(string? version)
            => ContentVersionHelper.IsGeothermometerFormatCompatible(version);

        private static int CompareVersions(string v1, string v2)
            => ContentVersionHelper.Compare(v1, v2);

        // ==================== 开发者工具 ====================

        /// <summary>
        /// 增量导出官方温压计到指定目录
        /// 生成 GeoT-List.json（官方温压计列表）和 GeoT-index.json（列表文件的哈希）
        /// 对比目录中已有的 GeoT-List.json，只导出新增或 Hash 变化的 ZIP
        /// </summary>
        /// <returns>(导出数量, 总官方数量)</returns>
        public static (int exported, int total) ExportAllOfficialToDirectory(string outputDir)
        {
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            var dbService = GeothermometerDatabaseService.Instance;
            var officialEntities = _loadedEntities.Where(e => e.IsOfficial).ToList();

            // 读取已有 GeoT-List.json（如果存在），用于增量对比
            var existingHashes = new Dictionary<string, string>();
            string listPath = Path.Combine(outputDir, GeoTListFileName);
            if (File.Exists(listPath))
            {
                try
                {
                    string existingJson = File.ReadAllText(listPath);
                    var existingList = JsonSerializer.Deserialize<PluginIndex>(existingJson, JsonOptions);
                    if (existingList?.Plugins != null)
                    {
                        foreach (var entry in existingList.Plugins)
                            existingHashes[entry.Id] = entry.Hash;
                    }
                }
                catch { /* 列表损坏则全量导出 */ }
            }

            var indexEntries = new List<PluginIndexEntry>();
            int exportedCount = 0;

            foreach (var summary in officialEntities)
            {
                try
                {
                    var fullEntity = dbService.GetEntity(summary.Id);
                    if (fullEntity == null) continue;

                    string currentHash = GeothermometerDatabaseService.ComputeEntityHash(fullEntity);
                    string zipFileName = $"{fullEntity.PluginId}.zip";
                    string zipPath = Path.Combine(outputDir, zipFileName);

                    // 增量判断：不存在旧 Hash 或 Hash 不同 或 ZIP 文件不存在 → 需要导出
                    bool needExport = !existingHashes.TryGetValue(fullEntity.PluginId, out var oldHash)
                                      || oldHash != currentHash
                                      || !File.Exists(zipPath);

                    if (needExport)
                    {
                        ExportToZip(fullEntity.Id, zipPath);
                        exportedCount++;
                    }

                    indexEntries.Add(new PluginIndexEntry
                    {
                        Id = fullEntity.PluginId,
                        Version = fullEntity.Version,
                        Reference = fullEntity.Reference ?? string.Empty,
                        DownloadUrl = zipFileName,
                        Hash = currentHash
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[GeothermometerService] 导出官方 GTM 失败 [{summary.PluginId}]: {ex.Message}");
                }
            }

            // 生成 GeoT-List.json（官方温压计列表）
            var pluginList = new PluginIndex
            {
                IndexVersion = DateTime.Now.ToString("yyyy.MM.dd"),
                Plugins = indexEntries
            };

            string listContent = JsonSerializer.Serialize(pluginList, JsonOptions);
            File.WriteAllText(listPath, listContent);

            string categoriesPath = Path.Combine(outputDir, OfficialContentEndpoints.GeoTMineralCategoriesFileName);
            var categoriesConfig = GeoTMineralCategoryHelper.LoadConfigFromPath(GeoTMineralCategoryHelper.ResolveExportConfigPath());
            categoriesConfig = GeoTMineralCategoryHelper.MergeMissingMinerals(
                categoriesConfig,
                officialEntities.SelectMany(GetEntityTagSourceNames));

            GeoTMineralCategoryHelper.SaveConfig(categoriesConfig, categoriesPath);
            string categoriesHash = File.Exists(categoriesPath)
                ? UpdateHelper.ComputeFileMd5(categoriesPath)
                : string.Empty;

            // 生成 GeoT-index.json（包含 GeoT-List.json 与矿物分类文件的哈希值）
            string listHash = GeothermometerDatabaseService.ComputeHash(listContent);
            var geoTIndex = new GeoTIndex
            {
                ListHash = listHash,
                MineralCategoriesHash = categoriesHash,
                IndexVersion = DateTime.Now.ToString("yyyy.MM.dd")
            };

            string indexPath = Path.Combine(outputDir, GeoTIndexFileName);
            File.WriteAllText(indexPath, JsonSerializer.Serialize(geoTIndex, JsonOptions));

            return (exportedCount, officialEntities.Count);
        }

    }

    /// <summary>
    /// 计算步骤模型（由 calculateDetailed 返回对象映射）
    /// </summary>
    public class CalculationStep : ObservableObject
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsResult { get; set; }
        public bool IsHighlight { get; set; }
        public bool IsSeparator { get; set; }

        /// <summary>
        /// 自定义背景色（#RGB / #RRGGBB / #AARRGGBB）；优先于语义高亮色。
        /// </summary>
        public string? BackgroundColor { get; set; }

        public bool HasCustomBackground => !string.IsNullOrWhiteSpace(BackgroundColor);

        private bool _isCollapsed;

        /// <summary>
        /// 分隔标题是否折叠其后续分组（仅 IsSeparator 有效）。
        /// </summary>
        public bool IsCollapsed
        {
            get => _isCollapsed;
            set => SetProperty(ref _isCollapsed, value);
        }

        private bool _isVisible = true;

        /// <summary>
        /// 是否因分组折叠而隐藏。
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }
    }
}
