using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using Jint;
using Jint.Native;
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
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Plugins", "GeoT-List.json");

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
                _loadedEntities.Add(summary);

                var fullEntity = dbService.GetEntity(summary.Id);
                if (fullEntity != null)
                    fullEntities.Add(fullEntity);
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
                                Description = obj.Get("desc")?.AsString() ?? "",
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
        /// 获取按矿物分组的温压计列表
        /// </summary>
        /// <param name="isOfficial">null=全部, true=仅官方, false=仅自定义</param>
        public static List<MineralGroup> GetGroupedEntities(bool? isOfficial = null)
        {
            var groups = new Dictionary<string, MineralGroup>();

            var entities = isOfficial.HasValue
                ? _loadedEntities.Where(e => e.IsOfficial == isOfficial.Value)
                : _loadedEntities.AsEnumerable();

            foreach (var entity in entities)
            {
                string key = entity.Mineral;
                if (!groups.ContainsKey(key))
                {
                    string displayName = !string.IsNullOrEmpty(entity.MineralLangKey)
                        ? (LanguageService.Instance[entity.MineralLangKey] ?? entity.Mineral)
                        : entity.Mineral;

                    groups[key] = new MineralGroup
                    {
                        MineralKey = key,
                        DisplayName = displayName,
                        IconCode = entity.IconCode,
                        IconColor = entity.IconColor,
                        Plugins = new List<Geothermometer>()
                    };
                }

                // 将 entity 转为轻量对象用于 UI 展示
                groups[key].Plugins.Add(EntityToGeothermometer(entity));
            }

            return groups.Values.ToList();
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

            entity.LastModified = DateTime.Now;
            entity.FileHash = GeothermometerDatabaseService.ComputeHash(entity.ScriptContent ?? "");

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
                Mineral = entity.Mineral,
                MineralLangKey = entity.MineralLangKey,
                Name = entity.Name,
                NameLangKey = entity.NameLangKey,
                Author = entity.Author,
                Year = entity.Year,
                Reference = entity.Reference,
                IconCode = entity.IconCode,
                IconColor = entity.IconColor,
                Headers = entity.Headers,
                ExampleRow = entity.ExampleRow,
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
                    ["Mineral"] = entity.Mineral,
                    ["MineralLangKey"] = entity.MineralLangKey,
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
        /// <returns>导入的实体，如果失败则返回 null</returns>
        public static GeothermometerEntity? ImportFromZip(string zipFilePath, bool persist = true)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                ZipFile.ExtractToDirectory(zipFilePath, tempDir);

                // 查找 JSON 文件
                var jsonFiles = Directory.GetFiles(tempDir, "*.json");
                if (jsonFiles.Length == 0)
                    return null;

                string jsonContent = File.ReadAllText(jsonFiles[0]);
                var plugin = JsonSerializer.Deserialize<Geothermometer>(jsonContent, JsonOptions);
                if (plugin == null || string.IsNullOrEmpty(plugin.Id))
                    return null;

                plugin.Version = ContentVersionHelper.Normalize(plugin.Version);
                if (!ContentVersionHelper.IsGeothermometerFormatCompatible(plugin.Version))
                {
                    Debug.WriteLine($"[GeothermometerService] GTM 格式版本不兼容 [{plugin.Id}]: {plugin.Version}");
                    return null;
                }

                // 尝试读取 JSON 中的 IsOfficial 标志
                bool isOfficial = false;
                try
                {
                    using var jsonDoc = JsonDocument.Parse(jsonContent);
                    if (jsonDoc.RootElement.TryGetProperty("IsOfficial", out var officialProp))
                        isOfficial = officialProp.GetBoolean();
                }
                catch { /* 忽略解析错误，默认为自定义 */ }

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
                    Id = GeothermometerDatabaseService.GenerateId(plugin.Id),
                    PluginId = plugin.Id,
                    Version = plugin.Version,
                    FileHash = GeothermometerDatabaseService.ComputeHash(scriptContent),
                    LastModified = DateTime.Now,
                    IsOfficial = isOfficial, // 保留 ZIP 中携带的官方标志
                    Mineral = plugin.Mineral,
                    MineralLangKey = plugin.MineralLangKey,
                    Name = plugin.Name,
                    NameLangKey = plugin.NameLangKey,
                    Author = plugin.Author,
                    Year = plugin.Year,
                    Reference = plugin.Reference,
                    IconCode = plugin.IconCode,
                    IconColor = plugin.IconColor,
                    Headers = plugin.Headers,
                    ExampleRow = plugin.ExampleRow,
                    FormulaName = plugin.FormulaName,
                    InputColumns = plugin.InputColumns ?? new List<string>(),
                    AdditionalFormulas = plugin.AdditionalFormulas ?? new List<AdditionalFormula>(),
                    ScriptContent = scriptContent,
                    HelpDocuments = helpDocs
                };

                if (persist)
                {
                    ValidateFormulaNames(entity);
                    GeothermometerDatabaseService.Instance.UpsertEntity(entity);
                }

                return entity;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GeothermometerService] 导入 ZIP 失败: {ex.Message}");
                return null;
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
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

                    string listDir = Path.GetDirectoryName(LocalListFilePath);
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

                return BuildUpdateCheckResult(pluginList);
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
        /// 根据服务器清单对账本地官方温压计，返回需下载项与待下架项（无副作用）。
        /// </summary>
        private static GeothermometerUpdateCheckResult BuildUpdateCheckResult(PluginIndex pluginList)
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
                Removals = removals
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
        public static async Task<bool> DownloadPluginAsync(PluginIndexEntry entry)
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
                    var imported = ImportFromZip(tempZip, persist: false);
                    if (imported == null)
                        return false;

                    if (!string.IsNullOrEmpty(entry.Hash) &&
                        !string.Equals(imported.FileHash, entry.Hash, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"[GeothermometerService] GTM 哈希校验失败 [{entry.Id}]: expected={entry.Hash}, actual={imported.FileHash}");
                        return false;
                    }

                    try
                    {
                        ValidateFormulaNames(imported);
                    }
                    catch (InvalidOperationException ex)
                    {
                        Debug.WriteLine($"[GeothermometerService] GTM 公式名冲突 [{entry.Id}]: {ex.Message}");
                        return false;
                    }

                    imported.IsOfficial = true;
                    GeothermometerDatabaseService.Instance.UpsertEntity(imported);
                    return true;
                }
                finally
                {
                    if (File.Exists(tempZip)) File.Delete(tempZip);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GeothermometerService] 下载 GTM 失败 [{entry.Id}]: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 批量下载、删除下架项并重新加载
        /// </summary>
        public static async Task<int> DownloadAndReloadAsync(
            List<PluginIndexEntry> entries,
            IEnumerable<Guid>? removals = null)
        {
            if (removals != null)
                ApplyRemovals(removals);

            int successCount = 0;
            foreach (var entry in entries)
            {
                if (await DownloadPluginAsync(entry))
                    successCount++;
            }

            if (successCount > 0 || (removals != null && removals.Any()))
                ReloadPlugins();

            return successCount;
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

                    string currentHash = GeothermometerDatabaseService.ComputeHash(fullEntity.ScriptContent ?? "");
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

            // 生成 GeoT-index.json（包含 GeoT-List.json 的哈希值）
            string listHash = GeothermometerDatabaseService.ComputeHash(listContent);
            var geoTIndex = new GeoTIndex
            {
                ListHash = listHash,
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
                    Hash = GeothermometerDatabaseService.ComputeHash(fullEntity.ScriptContent ?? "")
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
