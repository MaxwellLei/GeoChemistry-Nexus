using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Messages;
using CommunityToolkit.Mvvm.Messaging;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
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

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private static readonly List<GeothermometerEntity> _loadedEntities = new();
        private static string _serverBaseUrl = DefaultServerBaseUrl;

        /// <summary>
        /// 最近一次 ReloadPlugins 检测到的公式名冲突（先注册者优先生效）
        /// </summary>
        public static IReadOnlyList<FormulaNameConflict> LastFormulaNameConflicts { get; private set; } =
            Array.Empty<FormulaNameConflict>();

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
        /// 从数据库加载所有温压计并注册 JS 公式
        /// </summary>
        public static void ReloadPlugins()
        {
            _loadedEntities.Clear();

            var dbService = GeothermometerDatabaseService.Instance;
            var summaries = dbService.GetSummaries();
            var fullEntities = new List<GeothermometerEntity>();

            foreach (var summary in summaries)
            {
                var fullEntity = dbService.GetEntity(summary.Id);
                if (fullEntity != null)
                {
                    string hash = GeothermometerDatabaseService.ComputeEntityHash(fullEntity);
                    if (!string.Equals(summary.FileHash, hash, StringComparison.OrdinalIgnoreCase))
                    {
                        summary.FileHash = hash;
                        fullEntity.FileHash = hash;
                        dbService.UpsertEntity(fullEntity);
                    }

                    fullEntities.Add(fullEntity);
                }

                _loadedEntities.Add(summary);
            }

            LastFormulaNameConflicts = FindAllFormulaNameConflicts(fullEntities);
            foreach (var conflict in LastFormulaNameConflicts)
            {
                Debug.WriteLine(
                    $"[GeothermometerService] 公式名冲突: '{conflict.FormulaName}' " +
                    $"已被 '{conflict.ExistingName}' ({conflict.ExistingPluginId}) 使用，" +
                    $"'{conflict.CandidateName}' ({conflict.CandidatePluginId}) 的注册已跳过");
            }

            var registeredFormulaNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var fullEntity in fullEntities.OrderBy(e => e.PluginId, StringComparer.Ordinal))
            {
                if (fullEntity == null || string.IsNullOrEmpty(fullEntity.ScriptContent))
                    continue;

                if (!string.IsNullOrEmpty(fullEntity.FormulaName)
                    && registeredFormulaNames.Add(fullEntity.FormulaName))
                {
                    RegisterScriptFormula(fullEntity);
                }

                if (fullEntity.AdditionalFormulas != null)
                {
                    foreach (var af in fullEntity.AdditionalFormulas)
                    {
                        if (string.IsNullOrEmpty(af.FormulaName) || string.IsNullOrEmpty(af.FunctionName))
                            continue;

                        if (registeredFormulaNames.Add(af.FormulaName))
                            RegisterAdditionalFormula(fullEntity, af);
                    }
                }
            }
        }

        /// <summary>
        /// 注册主公式
        /// </summary>
        private static void RegisterScriptFormula(GeothermometerEntity entity)
        {
            try
            {
                string script = entity.ScriptContent;

                FormulaExtension.CustomFunctions[entity.FormulaName] = (cell, args) =>
                {
                    try
                    {
                        var engine = new Engine(cfg => cfg.TimeoutInterval(TimeSpan.FromSeconds(5)));
                        engine.Execute(MathHelpers);
                        engine.Execute(script);

                        var doubleArgs = new double[args.Length];
                        for (int i = 0; i < args.Length; i++)
                            doubleArgs[i] = Convert.ToDouble(args[i]);

                        var result = engine.Invoke("calculate", new object[] { doubleArgs });
                        if (result == null || result.IsNull() || result.IsUndefined())
                            return null;

                        return Convert.ToDouble(result.AsNumber());
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
                string script = entity.ScriptContent;
                string jsFuncName = af.FunctionName;

                FormulaExtension.CustomFunctions[af.FormulaName] = (cell, args) =>
                {
                    try
                    {
                        var engine = new Engine(cfg => cfg.TimeoutInterval(TimeSpan.FromSeconds(5)));
                        engine.Execute(MathHelpers);
                        engine.Execute(script);

                        var jsArgs = new object[args.Length];
                        for (int i = 0; i < args.Length; i++)
                            jsArgs[i] = args[i];

                        var result = engine.Invoke(jsFuncName, jsArgs);
                        if (result == null || result.IsNull() || result.IsUndefined())
                            return null;
                        if (result.IsNumber())
                            return Convert.ToDouble(result.AsNumber());
                        if (result.IsString())
                            return result.AsString();
                        return result.ToString();
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
                fullEntity = GeothermometerDatabaseService.Instance.GetEntity(entity.Id);
            }

            if (fullEntity == null || string.IsNullOrEmpty(fullEntity.ScriptContent))
                return steps;

            try
            {
                var engine = new Engine(cfg => cfg.TimeoutInterval(TimeSpan.FromSeconds(10)));
                engine.Execute(MathHelpers);
                engine.Execute(fullEntity.ScriptContent);

                var funcValue = engine.GetValue("calculateDetailed");
                if (funcValue == null || funcValue.IsUndefined())
                {
                    var simpleResult = engine.Invoke("calculate", new object[] { inputValues });
                    if (simpleResult != null && !simpleResult.IsNull() && !simpleResult.IsUndefined())
                    {
                        steps.Add(new CalculationStep
                        {
                            Name = "T(K)",
                            Value = simpleResult.AsNumber().ToString("F2"),
                            IsResult = true
                        });
                    }
                    return steps;
                }

                var result = engine.Invoke("calculateDetailed", new object[] { inputValues });
                if (result != null && result.IsArray())
                {
                    var array = result.AsArray();
                    foreach (var item in array)
                    {
                        if (item.IsObject())
                        {
                            var obj = item.AsObject();
                            var isResultProp = obj.Get("isResult");
                            steps.Add(new CalculationStep
                            {
                                Name = obj.Get("name")?.AsString() ?? "",
                                Value = obj.Get("value")?.ToString() ?? "",
                                Description = ResolveStepDescription(obj),
                                IsResult = isResultProp.IsBoolean() && isResultProp.AsBoolean()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GeothermometerService] 详细计算失败 [{entity.FormulaName}]: {ex.Message}");
                steps.Add(new CalculationStep { Name = "Error", Value = ex.Message });
            }

            return steps;
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

            var langObject = descLangValue.AsObject();
            var translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in langObject.GetOwnProperties())
            {
                string? key = property.Key.AsString();
                string? text = langObject.Get(property.Key)?.AsString();
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(text))
                    translations[key] = text;
            }

            if (translations.Count == 0)
                return defaultDesc;

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

            return defaultDesc;
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
            ReloadPlugins();
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
            if (!string.IsNullOrEmpty(entity.FormulaName))
                FormulaExtension.CustomFunctions.Remove(entity.FormulaName);

            // 生成新的官方 PluginId 和 Guid
            entity.PluginId = "official_" + Guid.NewGuid().ToString("N");
            entity.Id = GeothermometerDatabaseService.GenerateId(entity.PluginId);
            entity.IsOfficial = true;
            entity.LastModified = DateTime.Now;
            entity.FileHash = GeothermometerDatabaseService.ComputeEntityHash(entity);

            dbService.UpsertEntity(entity);
            ReloadPlugins();
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
            if (!string.IsNullOrEmpty(entity.FormulaName))
                FormulaExtension.CustomFunctions.Remove(entity.FormulaName);

            // 生成新的自定义 PluginId 和 Guid
            entity.PluginId = "custom_" + Guid.NewGuid().ToString("N");
            entity.Id = GeothermometerDatabaseService.GenerateId(entity.PluginId);
            entity.IsOfficial = false;
            entity.LastModified = DateTime.Now;
            entity.FileHash = GeothermometerDatabaseService.ComputeEntityHash(entity);

            dbService.UpsertEntity(entity);
            ReloadPlugins();
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

            // 注销公式
            if (!string.IsNullOrEmpty(entity.FormulaName))
                FormulaExtension.CustomFunctions.Remove(entity.FormulaName);

            ReloadPlugins();
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
        private static Geothermometer EntityToGeothermometer(GeothermometerEntity entity)
        {
            return new Geothermometer
            {
                Id = entity.PluginId,
                Version = entity.Version,
                Category = GeoTCategoryHelper.NormalizeCategoryKey(entity.Category),
                Tags = ResolveTagsDisplayNames(entity),
                Name = entity.Name,
                NameLangKey = entity.NameLangKey,
                Author = entity.Author,
                Year = entity.Year,
                Reference = entity.Reference,
                IconCode = entity.IconCode,
                IconColor = entity.IconColor,
                Headers = entity.Headers,
                ExampleRow = CommaSeparatedListHelper.AlignToHeaderCount(entity.Headers, entity.ExampleRow),
                FormulaName = entity.FormulaName,
                InputColumns = entity.InputColumns,
                AdditionalFormulas = entity.AdditionalFormulas,
                IsBuiltIn = entity.IsOfficial
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
            var serverPluginIds = new HashSet<string>();
            string appFormatVersion = ContentVersionHelper.GetGeothermometerFormatVersion();

            foreach (var entry in pluginList.Plugins)
            {
                serverPluginIds.Add(entry.Id);

                if (ContentVersionHelper.RequiresAppUpgrade(entry.Version, appFormatVersion))
                    continue;

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
                        ValidateFormulaNames(imported);
                    }
                    catch (InvalidOperationException ex)
                    {
                        Debug.WriteLine($"[GeothermometerService] GTM 公式名冲突 [{entry.Id}]: {ex.Message}");
                        return GeothermometerDownloadItemResult.Failed(entry.Id, ex.Message);
                    }

                    GeothermometerDatabaseService.Instance.UpsertEntity(imported);
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
            if (!string.IsNullOrWhiteSpace(entity.FormulaName))
                yield return entity.FormulaName.Trim();

            if (entity.AdditionalFormulas == null)
                yield break;

            foreach (var additionalFormula in entity.AdditionalFormulas)
            {
                if (!string.IsNullOrWhiteSpace(additionalFormula.FormulaName))
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

            var dbService = GeothermometerDatabaseService.Instance;

            foreach (var formulaName in EnumerateFormulaNames(candidate))
            {
                foreach (var summary in dbService.GetSummaries())
                {
                    if (excludeEntityId.HasValue && summary.Id == excludeEntityId.Value)
                        continue;

                    var existing = dbService.GetEntity(summary.Id);
                    if (existing == null)
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
            var categoriesConfig = GeoTMineralCategoryHelper.LoadConfigFromPath(GeoTMineralCategoryHelper.LocalConfigPath);
            if (File.Exists(categoriesPath))
            {
                categoriesConfig = GeoTMineralCategoryHelper.MergeMissingMinerals(
                    GeoTMineralCategoryHelper.LoadConfigFromPath(categoriesPath),
                    officialEntities.SelectMany(GetEntityTagSourceNames));
            }
            else
            {
                categoriesConfig = GeoTMineralCategoryHelper.MergeMissingMinerals(
                    categoriesConfig,
                    officialEntities.SelectMany(GetEntityTagSourceNames));
            }

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

        /// <summary>
        /// 仅生成 GeoT-List.json 内容（不导出 ZIP）
        /// 用于在已有 ZIP 文件的情况下更新索引
        /// </summary>
        public static string GeneratePluginIndex()
        {
            var officialEntities = _loadedEntities.Where(e => e.IsOfficial).ToList();
            var dbService = GeothermometerDatabaseService.Instance;

            var indexEntries = new List<PluginIndexEntry>();
            foreach (var summary in officialEntities)
            {
                var fullEntity = dbService.GetEntity(summary.Id);
                if (fullEntity == null) continue;

                indexEntries.Add(new PluginIndexEntry
                {
                    Id = fullEntity.PluginId,
                    Version = fullEntity.Version,
                    Reference = fullEntity.Reference ?? string.Empty,
                    DownloadUrl = $"{fullEntity.PluginId}.zip",
                    Hash = GeothermometerDatabaseService.ComputeEntityHash(fullEntity)
                });
            }

            var pluginList = new PluginIndex
            {
                IndexVersion = DateTime.Now.ToString("yyyy.MM.dd"),
                Plugins = indexEntries
            };

            return JsonSerializer.Serialize(pluginList, JsonOptions);
        }
    }

    /// <summary>
    /// 计算步骤模型
    /// </summary>
    public class CalculationStep
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsResult { get; set; }
    }
}
