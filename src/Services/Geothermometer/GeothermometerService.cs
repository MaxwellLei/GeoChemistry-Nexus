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
            if (string.IsNullOrWhiteSpace(url))
                return;

            string trimmed = url.Trim().TrimEnd('/');
            const string suffix = "/Geothermometer";
            if (trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring(0, trimmed.Length - suffix.Length);

            OfficialContentMirrorClient.RememberPreferred(trimmed);
        }

        private static string GeoRelative(string fileName) =>
            $"{OfficialContentEndpoints.GeothermometerFolderName}/{fileName.TrimStart('/')}";

        private static async Task DownloadPluginPackageAsync(string downloadUrl, string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
                throw new InvalidOperationException("Plugin download URL is empty.");

            if (downloadUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                string? relative = OfficialContentMirrorClient.TryGetRelativePath(downloadUrl);
                if (string.IsNullOrEmpty(relative))
                {
                    await UpdateHelper.DownloadFileAsync(downloadUrl, destinationPath);
                    return;
                }

                await OfficialContentMirrorClient.DownloadFileAsync(relative, destinationPath);
                return;
            }

            string file = downloadUrl.Trim().TrimStart('/');
            string relativePath = file.Contains('/') ? file : GeoRelative(file);
            await OfficialContentMirrorClient.DownloadFileAsync(relativePath, destinationPath);
        }

        /// <summary>
        /// 初始化：从 LiteDB 加载所有温压计并注册公式
        /// </summary>
        public static void Initialize()
        {
            MigrateInstallStatusIfNeeded();
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
                ServerHash = entity.ServerHash ?? string.Empty,
                Status = entity.Status ?? string.Empty,
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

            // 如果是新建，生成 ID
            if (entity.Id == Guid.Empty)
            {
                if (string.IsNullOrEmpty(entity.PluginId))
                    entity.PluginId = (entity.IsOfficial ? "official_" : "custom_") + Guid.NewGuid().ToString("N");
                entity.Id = GeothermometerDatabaseService.GenerateId(entity.PluginId);
            }

            var dbService = GeothermometerDatabaseService.Instance;
            var existing = entity.Id != Guid.Empty ? dbService.GetEntity(entity.Id) : null;
            if (existing != null)
            {
                // 编辑器 BuildEntity 不带这些字段，保存时从原记录合并
                if (string.IsNullOrEmpty(entity.ServerHash))
                    entity.ServerHash = existing.ServerHash ?? string.Empty;
                entity.IsFavorite = existing.IsFavorite;
                if (string.IsNullOrEmpty(entity.NameLangKey))
                    entity.NameLangKey = existing.NameLangKey ?? string.Empty;
                if (string.IsNullOrEmpty(entity.IconCode))
                    entity.IconCode = existing.IconCode ?? "\ue60d";
                if (string.IsNullOrEmpty(entity.IconColor))
                    entity.IconColor = existing.IconColor ?? "#555555";
            }

            entity.LastModified = DateTime.Now;
            entity.FileHash = GeothermometerDatabaseService.ComputeEntityHash(entity);

            if (entity.IsOfficial)
            {
                if (string.IsNullOrEmpty(entity.ScriptContent))
                {
                    entity.Status = GeothermometerInstallStatus.NotInstalled;
                }
                else if (!string.IsNullOrEmpty(entity.ServerHash)
                         && !string.Equals(entity.FileHash, entity.ServerHash, StringComparison.OrdinalIgnoreCase))
                {
                    entity.Status = GeothermometerInstallStatus.Outdated;
                }
                else if (!string.Equals(entity.Status, GeothermometerInstallStatus.RequiresAppUpgrade, StringComparison.Ordinal))
                {
                    entity.Status = GeothermometerInstallStatus.UpToDate;
                }
            }
            else
            {
                entity.Status = string.Empty;
                entity.ServerHash = string.Empty;
            }

            dbService.UpsertEntity(entity);
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
            entity.Status = string.IsNullOrEmpty(entity.ScriptContent)
                ? GeothermometerInstallStatus.NotInstalled
                : GeothermometerInstallStatus.UpToDate;
            entity.ServerHash = entity.FileHash ?? string.Empty;

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
            entity.Status = string.Empty;
            entity.ServerHash = string.Empty;
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
        /// 按从左到右的出现顺序解析输入列，支持多个矿物块使用相同的标准元素表头。
        /// </summary>
        public static bool TryResolveInputColumnIndices(
            IReadOnlyList<string> headers,
            IReadOnlyList<string> inputColumns,
            out List<int> indices)
        {
            indices = new List<int>();
            if (headers == null || inputColumns == null)
                return false;

            int searchStartIndex = 0;
            foreach (string inputColumn in inputColumns)
            {
                int matchIndex = -1;
                for (int index = searchStartIndex; index < headers.Count; index++)
                {
                    if (string.Equals(headers[index], inputColumn, StringComparison.Ordinal))
                    {
                        matchIndex = index;
                        break;
                    }
                }

                if (matchIndex < 0)
                {
                    indices.Clear();
                    return false;
                }

                indices.Add(matchIndex);
                searchStartIndex = matchIndex + 1;
            }

            return true;
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
                IsFavorite = entity.IsFavorite,
                Status = string.IsNullOrWhiteSpace(entity.Status)
                    ? (entity.IsOfficial ? GeothermometerInstallStatus.UpToDate : string.Empty)
                    : entity.Status
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
                    HelpDocuments = helpDocs,
                    Status = keepPluginIdentity
                        ? GeothermometerInstallStatus.UpToDate
                        : string.Empty
                };
                entity.FileHash = GeothermometerDatabaseService.ComputeEntityHash(entity);
                if (keepPluginIdentity)
                    entity.ServerHash = entity.FileHash;

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
        /// 从服务器检查并同步官方目录（两级校验）
        /// 1. 下载 GeoT-index.json 获取 GeoT-List.json 的哈希值
        /// 2. 对比本地列表哈希，不一致则下载新列表并校验完整性
        /// 3. 将清单同步到本地数据库（未安装占位 / OUTDATED / 下架删除），不自动下载 ZIP
        /// </summary>
        public static async Task<GeothermometerUpdateCheckResult> CheckForUpdatesAsync(bool downloadListIfOutdated = true)
        {
            try
            {
                string indexJson = await OfficialContentMirrorClient.GetStringAsync(GeoRelative(GeoTIndexFileName));
                var geoTIndex = JsonSerializer.Deserialize<GeoTIndex>(indexJson, JsonOptions);
                if (geoTIndex == null || string.IsNullOrEmpty(geoTIndex.ListHash))
                {
                    return new GeothermometerUpdateCheckResult
                    {
                        Status = GeothermometerUpdateCheckStatus.Failed,
                        ErrorMessage = "Invalid GeoT-index.json"
                    };
                }

                bool mineralCategoriesSynced = await SyncMineralCategoriesAsync(geoTIndex);

                bool needDownloadList = true;
                if (File.Exists(LocalListFilePath))
                {
                    string localListContent = await File.ReadAllTextAsync(LocalListFilePath);
                    string localListHash = GeothermometerDatabaseService.ComputeHash(localListContent);
                    if (string.Equals(localListHash, geoTIndex.ListHash, StringComparison.OrdinalIgnoreCase))
                        needDownloadList = false;
                }

                bool listDownloaded = false;
                string listJson;
                if (needDownloadList)
                {
                    if (!downloadListIfOutdated)
                    {
                        return new GeothermometerUpdateCheckResult
                        {
                            Status = GeothermometerUpdateCheckStatus.Success,
                            ListDownloaded = false,
                            CatalogChanged = false,
                            MineralCategoriesSynced = mineralCategoriesSynced,
                            NotInstalledCount = CountByStatus(GeothermometerInstallStatus.NotInstalled),
                            OutdatedCount = CountByStatus(GeothermometerInstallStatus.Outdated)
                        };
                    }

                    listJson = await OfficialContentMirrorClient.GetStringAsync(GeoRelative(GeoTListFileName));

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
                    listDownloaded = true;
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

                var syncResult = SyncOfficialPluginsFromServerListCore(pluginList);
                if (syncResult.CatalogChanged)
                    ReloadPlugins();

                return new GeothermometerUpdateCheckResult
                {
                    Status = GeothermometerUpdateCheckStatus.Success,
                    ListDownloaded = listDownloaded,
                    CatalogChanged = syncResult.CatalogChanged,
                    NotInstalledCount = syncResult.NotInstalledCount,
                    OutdatedCount = syncResult.OutdatedCount,
                    RemovalCount = syncResult.RemovalCount,
                    RequiresAppUpgrade = syncResult.RequiresAppUpgrade,
                    MineralCategoriesSynced = mineralCategoriesSynced
                };
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
        /// 远端列表哈希是否与本地 GeoT-List.json 不一致（用于提示“是否更新列表”）
        /// </summary>
        public static async Task<(bool Success, bool ListOutdated, string? ErrorMessage)> IsRemoteListOutdatedAsync()
        {
            try
            {
                string indexJson = await OfficialContentMirrorClient.GetStringAsync(GeoRelative(GeoTIndexFileName));
                var geoTIndex = JsonSerializer.Deserialize<GeoTIndex>(indexJson, JsonOptions);
                if (geoTIndex == null || string.IsNullOrEmpty(geoTIndex.ListHash))
                    return (false, false, "Invalid GeoT-index.json");

                if (!File.Exists(LocalListFilePath))
                    return (true, true, null);

                string localListContent = await File.ReadAllTextAsync(LocalListFilePath);
                string localListHash = GeothermometerDatabaseService.ComputeHash(localListContent);
                bool outdated = !string.Equals(localListHash, geoTIndex.ListHash, StringComparison.OrdinalIgnoreCase);
                return (true, outdated, null);
            }
            catch (Exception ex)
            {
                return (false, false, ex.Message);
            }
        }

        /// <summary>
        /// 从服务器同步矿物分类多语言文件（GeoT-index 中的 MineralCategoriesHash 校验）。
        /// </summary>
        public static async Task<bool> SyncMineralCategoriesAsync(GeoTIndex? geoTIndex)
        {
            if (geoTIndex == null || string.IsNullOrEmpty(geoTIndex.MineralCategoriesHash))
                return false;

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

                string categoriesJson = await OfficialContentMirrorClient.GetStringAsync(
                    GeoRelative(OfficialContentEndpoints.GeoTMineralCategoriesFileName));

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
        }

        private sealed class CatalogSyncResult
        {
            public bool CatalogChanged { get; set; }
            public int NotInstalledCount { get; set; }
            public int OutdatedCount { get; set; }
            public int RemovalCount { get; set; }
            public List<PluginIndexEntry> RequiresAppUpgrade { get; set; } = new();
        }

        /// <summary>
        /// 将服务器 GeoT-List 同步到本地数据库：创建未安装占位、标记过期、删除下架官方项。
        /// </summary>
        public static bool SyncOfficialPluginsFromServerList(PluginIndex pluginList)
            => SyncOfficialPluginsFromServerListCore(pluginList).CatalogChanged;

        private static CatalogSyncResult SyncOfficialPluginsFromServerListCore(PluginIndex pluginList)
        {
            var result = new CatalogSyncResult();
            if (pluginList?.Plugins == null)
                return result;

            var dbService = GeothermometerDatabaseService.Instance;
            var existingOfficial = dbService.GetSummaries(isOfficial: true)
                .ToDictionary(e => e.PluginId, e => e, StringComparer.OrdinalIgnoreCase);
            var serverPluginIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string appFormatVersion = ContentVersionHelper.GetGeothermometerFormatVersion();

            foreach (var entry in pluginList.Plugins)
            {
                if (string.IsNullOrWhiteSpace(entry.Id))
                    continue;

                serverPluginIds.Add(entry.Id);
                bool requiresUpgrade = ContentVersionHelper.RequiresAppUpgrade(entry.Version, appFormatVersion);
                if (requiresUpgrade)
                    result.RequiresAppUpgrade.Add(entry);

                if (existingOfficial.TryGetValue(entry.Id, out var summary))
                {
                    var fullEntity = dbService.GetEntity(summary.Id);
                    if (fullEntity == null)
                        continue;

                    bool isNotInstalled = string.Equals(fullEntity.Status, GeothermometerInstallStatus.NotInstalled, StringComparison.Ordinal)
                                          || string.IsNullOrEmpty(fullEntity.ScriptContent);

                    string newStatus;
                    if (requiresUpgrade)
                    {
                        newStatus = GeothermometerInstallStatus.RequiresAppUpgrade;
                    }
                    else if (isNotInstalled)
                    {
                        newStatus = GeothermometerInstallStatus.NotInstalled;
                    }
                    else
                    {
                        bool isHashSame = string.IsNullOrEmpty(entry.Hash)
                            || string.Equals(fullEntity.FileHash, entry.Hash, StringComparison.OrdinalIgnoreCase);
                        bool hasVersionUpdate = ContentVersionHelper.HasContentUpdate(fullEntity.Version, entry.Version)
                                               || ContentVersionHelper.Compare(entry.Version, fullEntity.Version) > 0;
                        newStatus = isHashSame && !hasVersionUpdate
                            ? GeothermometerInstallStatus.UpToDate
                            : GeothermometerInstallStatus.Outdated;
                    }

                    bool metadataChanged = ApplyCatalogMetadata(fullEntity, entry)
                        || !string.Equals(fullEntity.Status, newStatus, StringComparison.Ordinal)
                        || !string.Equals(fullEntity.ServerHash, entry.Hash ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                        || (!string.IsNullOrWhiteSpace(entry.Version)
                            && ContentVersionHelper.Compare(fullEntity.Version, entry.Version) != 0);

                    if (metadataChanged || string.IsNullOrEmpty(fullEntity.Status))
                    {
                        fullEntity.Status = newStatus;
                        fullEntity.ServerHash = entry.Hash ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(entry.Version))
                            fullEntity.Version = ContentVersionHelper.Normalize(entry.Version);

                        // 未安装占位同步服务器哈希；已安装保留本地内容哈希
                        if (string.Equals(newStatus, GeothermometerInstallStatus.NotInstalled, StringComparison.Ordinal)
                            || (string.Equals(newStatus, GeothermometerInstallStatus.RequiresAppUpgrade, StringComparison.Ordinal)
                                && isNotInstalled))
                        {
                            fullEntity.FileHash = entry.Hash ?? string.Empty;
                            fullEntity.ScriptContent = string.Empty;
                            fullEntity.HelpDocuments = new Dictionary<string, string>();
                        }

                        fullEntity.LastModified = DateTime.Now;
                        dbService.UpsertEntity(fullEntity);
                        result.CatalogChanged = true;
                    }
                }
                else
                {
                    var stub = CreateCatalogStub(entry, requiresUpgrade
                        ? GeothermometerInstallStatus.RequiresAppUpgrade
                        : GeothermometerInstallStatus.NotInstalled);
                    dbService.UpsertEntity(stub);
                    result.CatalogChanged = true;
                }
            }

            foreach (var local in existingOfficial.Values)
            {
                if (serverPluginIds.Contains(local.PluginId))
                    continue;

                dbService.DeleteEntity(local.Id);
                UnloadPlugin(local.Id);
                result.RemovalCount++;
                result.CatalogChanged = true;
            }

            // 统计以数据库最新摘要为准
            var officialSummaries = dbService.GetSummaries(isOfficial: true);
            result.NotInstalledCount = officialSummaries.Count(e =>
                string.Equals(e.Status, GeothermometerInstallStatus.NotInstalled, StringComparison.Ordinal));
            result.OutdatedCount = officialSummaries.Count(e =>
                string.Equals(e.Status, GeothermometerInstallStatus.Outdated, StringComparison.Ordinal));

            return result;
        }

        private static GeothermometerEntity CreateCatalogStub(PluginIndexEntry entry, string status)
        {
            return new GeothermometerEntity
            {
                Id = GeothermometerDatabaseService.GenerateId(entry.Id),
                PluginId = entry.Id,
                Version = ContentVersionHelper.Normalize(entry.Version),
                FileHash = entry.Hash ?? string.Empty,
                ServerHash = entry.Hash ?? string.Empty,
                Status = status,
                LastModified = DateTime.Now,
                IsOfficial = true,
                IsFavorite = false,
                Category = GeoTCategoryHelper.NormalizeCategoryKey(entry.Category),
                Tags = entry.Tags != null ? new List<string>(entry.Tags) : new List<string>(),
                Capabilities = GeoTCapabilityHelper.NormalizeList(entry.Capabilities),
                Name = string.IsNullOrWhiteSpace(entry.Name) ? entry.Id : entry.Name,
                NameLangKey = entry.NameLangKey ?? string.Empty,
                Author = entry.Author ?? string.Empty,
                Year = entry.Year,
                Reference = entry.Reference ?? string.Empty,
                IconCode = string.IsNullOrWhiteSpace(entry.IconCode) ? "\ue60d" : entry.IconCode,
                IconColor = string.IsNullOrWhiteSpace(entry.IconColor) ? "#555555" : entry.IconColor,
                Headers = new List<string>(),
                ExampleRow = new List<string>(),
                FormulaName = string.Empty,
                InputColumns = new List<string>(),
                AdditionalFormulas = new List<AdditionalFormula>(),
                ScriptContent = string.Empty,
                HelpDocuments = new Dictionary<string, string>()
            };
        }

        private static bool ApplyCatalogMetadata(GeothermometerEntity entity, PluginIndexEntry entry)
        {
            bool changed = false;

            if (!string.IsNullOrWhiteSpace(entry.Name) && !string.Equals(entity.Name, entry.Name, StringComparison.Ordinal))
            {
                entity.Name = entry.Name;
                changed = true;
            }
            else if (string.IsNullOrWhiteSpace(entity.Name) && !string.IsNullOrWhiteSpace(entry.Id))
            {
                entity.Name = entry.Id;
                changed = true;
            }

            if (!string.Equals(entity.NameLangKey ?? string.Empty, entry.NameLangKey ?? string.Empty, StringComparison.Ordinal))
            {
                entity.NameLangKey = entry.NameLangKey ?? string.Empty;
                changed = true;
            }

            string category = GeoTCategoryHelper.NormalizeCategoryKey(entry.Category);
            if (!string.IsNullOrWhiteSpace(entry.Category)
                && !string.Equals(GeoTCategoryHelper.NormalizeCategoryKey(entity.Category), category, StringComparison.Ordinal))
            {
                entity.Category = category;
                changed = true;
            }

            if (entry.Tags != null && entry.Tags.Count > 0
                && !ListEqualsOrdinal(entity.Tags, entry.Tags))
            {
                entity.Tags = new List<string>(entry.Tags);
                changed = true;
            }

            var caps = GeoTCapabilityHelper.NormalizeList(entry.Capabilities);
            if (entry.Capabilities != null && entry.Capabilities.Count > 0
                && !ListEqualsOrdinal(entity.Capabilities, caps))
            {
                entity.Capabilities = caps;
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(entry.Author)
                && !string.Equals(entity.Author ?? string.Empty, entry.Author, StringComparison.Ordinal))
            {
                entity.Author = entry.Author;
                changed = true;
            }

            if (entry.Year > 0 && entity.Year != entry.Year)
            {
                entity.Year = entry.Year;
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(entry.Reference)
                && !string.Equals(entity.Reference ?? string.Empty, entry.Reference, StringComparison.Ordinal))
            {
                entity.Reference = entry.Reference;
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(entry.IconCode)
                && !string.Equals(entity.IconCode ?? string.Empty, entry.IconCode, StringComparison.Ordinal))
            {
                entity.IconCode = entry.IconCode;
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(entry.IconColor)
                && !string.Equals(entity.IconColor ?? string.Empty, entry.IconColor, StringComparison.Ordinal))
            {
                entity.IconColor = entry.IconColor;
                changed = true;
            }

            return changed;
        }

        private static bool ListEqualsOrdinal(List<string>? a, List<string>? b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!string.Equals(a[i], b[i], StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private static int CountByStatus(string status)
            => _loadedEntities.Count(e => e.IsOfficial
                && string.Equals(e.Status, status, StringComparison.Ordinal));

        /// <summary>
        /// 读取本地缓存的 GeoT-List.json
        /// </summary>
        public static PluginIndex? TryLoadLocalPluginIndex()
        {
            try
            {
                if (!File.Exists(LocalListFilePath))
                    return null;
                string json = File.ReadAllText(LocalListFilePath);
                return JsonSerializer.Deserialize<PluginIndex>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GeothermometerService] 读取本地 GeoT-List 失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从本地清单查找指定官方温压计条目
        /// </summary>
        public static PluginIndexEntry? FindLocalListEntry(string pluginId)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                return null;

            var index = TryLoadLocalPluginIndex();
            return index?.Plugins?.FirstOrDefault(p =>
                string.Equals(p.Id, pluginId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 获取当前所有未安装官方项对应的清单条目（用于批量下载）
        /// </summary>
        public static List<PluginIndexEntry> GetNotInstalledPluginEntries()
            => GetEntriesByStatus(GeothermometerInstallStatus.NotInstalled);

        /// <summary>
        /// 获取当前所有可更新官方项对应的清单条目（用于批量更新）
        /// </summary>
        public static List<PluginIndexEntry> GetOutdatedPluginEntries()
            => GetEntriesByStatus(GeothermometerInstallStatus.Outdated);

        private static List<PluginIndexEntry> GetEntriesByStatus(string status)
        {
            var index = TryLoadLocalPluginIndex();
            if (index?.Plugins == null || index.Plugins.Count == 0)
                return new List<PluginIndexEntry>();

            var map = index.Plugins
                .Where(p => !string.IsNullOrWhiteSpace(p.Id))
                .GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var result = new List<PluginIndexEntry>();
            foreach (var entity in _loadedEntities.Where(e => e.IsOfficial
                         && string.Equals(e.Status, status, StringComparison.Ordinal)))
            {
                if (map.TryGetValue(entity.PluginId, out var entry))
                    result.Add(entry);
            }

            return result;
        }

        /// <summary>
        /// 当前未安装 / 可更新数量（供菜单可见性）
        /// </summary>
        public static (int NotInstalled, int Outdated) GetPendingInstallCounts()
        {
            int notInstalled = CountByStatus(GeothermometerInstallStatus.NotInstalled);
            int outdated = CountByStatus(GeothermometerInstallStatus.Outdated);
            return (notInstalled, outdated);
        }

        /// <summary>
        /// 为缺少 Status 的旧数据补齐安装状态（启动时调用）
        /// </summary>
        public static void MigrateInstallStatusIfNeeded()
        {
            var dbService = GeothermometerDatabaseService.Instance;
            bool changed = false;

            foreach (var summary in dbService.GetSummaries())
            {
                if (!string.IsNullOrWhiteSpace(summary.Status))
                    continue;

                var full = dbService.GetEntity(summary.Id);
                if (full == null)
                    continue;

                if (!full.IsOfficial)
                {
                    full.Status = string.Empty;
                    full.ServerHash = string.Empty;
                }
                else if (string.IsNullOrEmpty(full.ScriptContent))
                {
                    full.Status = GeothermometerInstallStatus.NotInstalled;
                    if (string.IsNullOrEmpty(full.ServerHash))
                        full.ServerHash = full.FileHash ?? string.Empty;
                }
                else
                {
                    full.Status = GeothermometerInstallStatus.UpToDate;
                    if (string.IsNullOrEmpty(full.ServerHash))
                        full.ServerHash = full.FileHash ?? string.Empty;
                }

                dbService.UpsertEntity(full);
                changed = true;
            }

            if (changed)
                Debug.WriteLine("[GeothermometerService] Migrated install Status for legacy entities.");
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
                string indexJson = await OfficialContentMirrorClient.GetStringAsync(GeoRelative(GeoTIndexFileName));
                var geoTIndex = JsonSerializer.Deserialize<GeoTIndex>(indexJson, JsonOptions);
                if (geoTIndex == null || string.IsNullOrEmpty(geoTIndex.ListHash))
                    return (null, "Invalid GeoT-index.json");

                await SyncMineralCategoriesAsync(geoTIndex);

                string listJson = await OfficialContentMirrorClient.GetStringAsync(GeoRelative(GeoTListFileName));

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
                string tempZip = Path.Combine(Path.GetTempPath(), $"{entry.Id}.zip");
                await DownloadPluginPackageAsync(entry.DownloadUrl, tempZip);

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

                    var existing = GeothermometerDatabaseService.Instance.GetEntity(imported.Id);
                    if (existing != null)
                        imported.IsFavorite = existing.IsFavorite;

                    imported.Status = GeothermometerInstallStatus.UpToDate;
                    imported.ServerHash = entry.Hash ?? imported.FileHash ?? string.Empty;
                    imported.IsOfficial = true;

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
                string indexJson = await OfficialContentMirrorClient.GetStringAsync(GeoRelative(GeoTIndexFileName));
                var geoTIndex = JsonSerializer.Deserialize<GeoTIndex>(indexJson, JsonOptions);
                await SyncMineralCategoriesAsync(geoTIndex);
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
            var officialEntities = _loadedEntities
                .Where(e => e.IsOfficial
                    && !string.Equals(e.Status, GeothermometerInstallStatus.NotInstalled, StringComparison.Ordinal))
                .ToList();

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
                    if (fullEntity == null || string.IsNullOrEmpty(fullEntity.ScriptContent))
                        continue;

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
                        Name = fullEntity.Name ?? string.Empty,
                        NameLangKey = fullEntity.NameLangKey ?? string.Empty,
                        Category = GeoTCategoryHelper.NormalizeCategoryKey(fullEntity.Category),
                        Tags = fullEntity.Tags != null ? new List<string>(fullEntity.Tags) : new List<string>(),
                        Capabilities = GeoTCapabilityHelper.NormalizeList(fullEntity.Capabilities),
                        Author = fullEntity.Author ?? string.Empty,
                        Year = fullEntity.Year,
                        Reference = fullEntity.Reference ?? string.Empty,
                        IconCode = fullEntity.IconCode ?? "\ue60d",
                        IconColor = fullEntity.IconColor ?? "#555555",
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

            return (exportedCount, indexEntries.Count);
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
