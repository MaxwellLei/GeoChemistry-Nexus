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
    /// 地质温度计（GTM）管理服务
    /// 从 LiteDB 加载温度计，通过 Jint 注册 JS 脚本为 ReoGrid 自定义函数
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
        /// 本地 GeoT-List.json 存储路径
        /// </summary>
        private static string LocalListFilePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Plugins", "GeoT-List.json");

        /// <summary>
        /// 获取已加载的全部温度计实体（摘要）
        /// </summary>
        public static IReadOnlyList<GeothermometerEntity> LoadedEntities => _loadedEntities.AsReadOnly();

        /// <summary>
        /// 旧版 GTM 目录（用于数据迁移）
        /// </summary>
        private static string LegacyPluginDirectory =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Plugins", "Geothermometer");

        public static void SetServerBaseUrl(string url)
        {
            if (!string.IsNullOrWhiteSpace(url))
                _serverBaseUrl = url.TrimEnd('/');
        }

        /// <summary>
        /// 初始化：检查数据库是否为空，如果是则从旧的 JSON+JS+RTF 文件迁移数据
        /// 然后加载所有温度计并注册公式
        /// </summary>
        public static void Initialize()
        {
            var dbService = GeothermometerDatabaseService.Instance;

            // 如果数据库为空，执行一次性迁移
            if (dbService.IsDatabaseEmpty())
            {
                MigrateFromLegacyFiles();
            }

            // 加载并注册
            ReloadPlugins();
        }

        /// <summary>
        /// 从旧的 JSON + JS + RTF 文件迁移数据到 LiteDB
        /// </summary>
        private static void MigrateFromLegacyFiles()
        {
            string pluginDir = LegacyPluginDirectory;
            if (!Directory.Exists(pluginDir))
                return;

            var jsonFiles = Directory.GetFiles(pluginDir, "*.json", SearchOption.TopDirectoryOnly);
            string docBaseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Documents", "GTM");

            foreach (var jsonFile in jsonFiles)
            {
                try
                {
                    string json = File.ReadAllText(jsonFile);
                    var plugin = JsonSerializer.Deserialize<Geothermometer>(json, JsonOptions);
                    if (plugin == null || string.IsNullOrEmpty(plugin.Id))
                        continue;

                    // 加载 JS 脚本
                    string scriptContent = string.Empty;
                    if (!string.IsNullOrEmpty(plugin.ScriptFile))
                    {
                        string jsPath = Path.Combine(pluginDir, plugin.ScriptFile);
                        if (File.Exists(jsPath))
                        {
                            scriptContent = File.ReadAllText(jsPath);
                        }
                    }
                    if (string.IsNullOrEmpty(scriptContent) && !string.IsNullOrEmpty(plugin.Script))
                    {
                        scriptContent = plugin.Script;
                    }

                    // 加载帮助文档
                    var helpDocs = new Dictionary<string, string>();
                    if (!string.IsNullOrEmpty(plugin.HelpDocPath))
                    {
                        string helpDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, plugin.HelpDocPath);
                        if (Directory.Exists(helpDir))
                        {
                            foreach (var rtfFile in Directory.GetFiles(helpDir, "*.rtf"))
                            {
                                string langCode = Path.GetFileNameWithoutExtension(rtfFile);
                                helpDocs[langCode] = File.ReadAllText(rtfFile);
                            }
                        }
                    }

                    // 创建数据库实体
                    var entity = new GeothermometerEntity
                    {
                        Id = GeothermometerDatabaseService.GenerateId(plugin.Id),
                        PluginId = plugin.Id,
                        Version = plugin.Version,
                        FileHash = GeothermometerDatabaseService.ComputeHash(scriptContent),
                        LastModified = DateTime.Now,
                        IsOfficial = true, // 从内置文件迁移的标记为官方
                        Mineral = plugin.Mineral,
                        MineralLangKey = plugin.MineralLangKey,
                        Name = plugin.Name,
                        NameLangKey = plugin.NameLangKey,
                        Author = plugin.Author,
                        Year = plugin.Year,
                        Reference = plugin.Reference,
                        Description = plugin.Description,
                        IconCode = plugin.IconCode,
                        IconColor = plugin.IconColor,
                        Headers = plugin.Headers,
                        ExampleRow = plugin.ExampleRow,
                        WorksheetName = plugin.WorksheetName,
                        FormulaName = plugin.FormulaName,
                        InputColumns = plugin.InputColumns,
                        AdditionalFormulas = plugin.AdditionalFormulas ?? new List<AdditionalFormula>(),
                        ScriptContent = scriptContent,
                        HelpDocuments = helpDocs
                    };

                    GeothermometerDatabaseService.Instance.UpsertEntity(entity);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[GeothermometerService] 迁移 GTM 失败: {jsonFile}, 错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 从数据库加载所有温度计并注册 JS 公式
        /// </summary>
        public static void ReloadPlugins()
        {
            _loadedEntities.Clear();

            var dbService = GeothermometerDatabaseService.Instance;
            var summaries = dbService.GetSummaries();

            foreach (var summary in summaries)
            {
                _loadedEntities.Add(summary);

                // 获取完整实体（含脚本）来注册公式
                var fullEntity = dbService.GetEntity(summary.Id);
                if (fullEntity != null && !string.IsNullOrEmpty(fullEntity.ScriptContent)
                    && !string.IsNullOrEmpty(fullEntity.FormulaName))
                {
                    RegisterScriptFormula(fullEntity);

                    // 注册附加公式
                    if (fullEntity.AdditionalFormulas != null)
                    {
                        foreach (var af in fullEntity.AdditionalFormulas)
                        {
                            RegisterAdditionalFormula(fullEntity, af);
                        }
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
        /// 获取按矿物分组的温度计列表
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
        /// 获取自定义温度计的平面列表（不按矿物分组）
        /// </summary>
        public static List<Geothermometer> GetCustomPlugins()
        {
            return _loadedEntities
                .Where(e => !e.IsOfficial)
                .Select(e => EntityToGeothermometer(e))
                .ToList();
        }

        /// <summary>
        /// 保存温度计到数据库并重新注册公式（支持官方和自定义）
        /// </summary>
        public static GeothermometerEntity SaveEntity(GeothermometerEntity entity)
        {
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
        /// 保存自定义温度计（兼容旧调用，强制 IsOfficial=false）
        /// </summary>
        public static GeothermometerEntity SaveCustomEntity(GeothermometerEntity entity)
        {
            entity.IsOfficial = false;
            return SaveEntity(entity);
        }

        /// <summary>
        /// 将自定义温度计转换为官方温度计（重新生成 PluginId 和 Guid）
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
        /// 将官方温度计降级为自定义温度计（重新生成 PluginId 和 Guid）
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
        /// 删除温度计（支持删除官方和自定义）
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
                Description = entity.Description,
                IconCode = entity.IconCode,
                IconColor = entity.IconColor,
                Headers = entity.Headers,
                ExampleRow = entity.ExampleRow,
                WorksheetName = entity.WorksheetName,
                FormulaName = entity.FormulaName,
                InputColumns = entity.InputColumns,
                AdditionalFormulas = entity.AdditionalFormulas,
                LoadedScript = entity.ScriptContent,
                IsBuiltIn = entity.IsOfficial,
                Source = entity.IsOfficial ? PluginSource.BuiltIn : PluginSource.Local
            };
        }

        // ==================== 导出/导入 ZIP ====================

        /// <summary>
        /// 导出温度计为 ZIP 文件
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
                    ["Description"] = entity.Description,
                    ["IconCode"] = entity.IconCode,
                    ["IconColor"] = entity.IconColor,
                    ["Headers"] = entity.Headers,
                    ["ExampleRow"] = entity.ExampleRow,
                    ["WorksheetName"] = entity.WorksheetName,
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
        /// 从 ZIP 文件导入温度计
        /// </summary>
        /// <returns>导入的实体，如果失败则返回 null</returns>
        public static GeothermometerEntity ImportFromZip(string zipFilePath)
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
                    Description = plugin.Description,
                    IconCode = plugin.IconCode,
                    IconColor = plugin.IconColor,
                    Headers = plugin.Headers,
                    ExampleRow = plugin.ExampleRow,
                    WorksheetName = plugin.WorksheetName,
                    FormulaName = plugin.FormulaName,
                    InputColumns = plugin.InputColumns ?? new List<string>(),
                    AdditionalFormulas = plugin.AdditionalFormulas ?? new List<AdditionalFormula>(),
                    ScriptContent = scriptContent,
                    HelpDocuments = helpDocs
                };

                // 写入数据库
                GeothermometerDatabaseService.Instance.UpsertEntity(entity);

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
        /// 2. 对比本地 GeoT-List.json 的哈希，不一致则下载新的列表
        /// 3. 对比列表中的 GTM 与本地数据库，找出需要新增/更新的，删除已移除的
        /// </summary>
        public static async Task<List<PluginIndexEntry>> CheckForUpdatesAsync()
        {
            var updatable = new List<PluginIndexEntry>();
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

                // 1. 下载 GeoT-index.json，获取 GeoT-List.json 的哈希值
                string indexUrl = $"{_serverBaseUrl}/{GeoTIndexFileName}";
                string indexJson = await client.GetStringAsync(indexUrl);
                var geoTIndex = JsonSerializer.Deserialize<GeoTIndex>(indexJson, JsonOptions);
                if (geoTIndex == null || string.IsNullOrEmpty(geoTIndex.ListHash))
                    return updatable;

                // 2. 检查本地 GeoT-List.json 是否需要更新
                bool needDownloadList = true;
                if (File.Exists(LocalListFilePath))
                {
                    string localListContent = File.ReadAllText(LocalListFilePath);
                    string localListHash = GeothermometerDatabaseService.ComputeHash(localListContent);
                    if (localListHash == geoTIndex.ListHash)
                        needDownloadList = false;
                }

                // 3. 如果需要则从服务器下载 GeoT-List.json 并保存到本地
                string listJson;
                if (needDownloadList)
                {
                    string listUrl = $"{_serverBaseUrl}/{GeoTListFileName}";
                    listJson = await client.GetStringAsync(listUrl);

                    string listDir = Path.GetDirectoryName(LocalListFilePath);
                    if (!string.IsNullOrEmpty(listDir) && !Directory.Exists(listDir))
                        Directory.CreateDirectory(listDir);
                    File.WriteAllText(LocalListFilePath, listJson);
                }
                else
                {
                    // 本地列表哈希一致，无需更新
                    return updatable;
                }

                // 4. 解析 GTM 列表
                var pluginList = JsonSerializer.Deserialize<PluginIndex>(listJson, JsonOptions);
                if (pluginList?.Plugins == null) return updatable;

                // 5. 对比本地，找出需要更新/新增的 GTM
                var serverPluginIds = new HashSet<string>();
                foreach (var entry in pluginList.Plugins)
                {
                    serverPluginIds.Add(entry.Id);
                    var local = _loadedEntities.FirstOrDefault(p => p.PluginId == entry.Id);
                    if (local == null)
                    {
                        // 本地没有此 GTM → 需要下载
                        updatable.Add(entry);
                    }
                    else if (CompareVersions(entry.Version, local.Version) > 0)
                    {
                        // 服务器版本更高 → 需要更新
                        updatable.Add(entry);
                    }
                    else if (!string.IsNullOrEmpty(entry.Hash) && entry.Hash != local.FileHash)
                    {
                        // 版本相同但 Hash 不同 → 内容有变化，需要更新
                        updatable.Add(entry);
                    }
                }

                // 6. 删除本地已不存在于服务器列表中的官方温度计
                var localOfficials = _loadedEntities.Where(e => e.IsOfficial).ToList();
                foreach (var local in localOfficials)
                {
                    if (!serverPluginIds.Contains(local.PluginId))
                    {
                        Debug.WriteLine($"[GeothermometerService] 删除已移除的官方温度计: {local.PluginId}");
                        DeleteEntity(local.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GeothermometerService] 检查更新失败: {ex.Message}");
            }
            return updatable;
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
                    var imported = ImportFromZip(tempZip);
                    if (imported != null)
                    {
                        // 从服务器下载的 GTM 标记为官方
                        imported.IsOfficial = true;
                        GeothermometerDatabaseService.Instance.UpsertEntity(imported);
                        return true;
                    }
                    return false;
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
        /// 批量下载并重新加载
        /// </summary>
        public static async Task<int> DownloadAndReloadAsync(List<PluginIndexEntry> entries)
        {
            int successCount = 0;
            foreach (var entry in entries)
            {
                if (await DownloadPluginAsync(entry))
                    successCount++;
            }

            if (successCount > 0)
                ReloadPlugins();

            return successCount;
        }

        private static int CompareVersions(string v1, string v2)
        {
            try
            {
                return new Version(v1).CompareTo(new Version(v2));
            }
            catch
            {
                return string.Compare(v1, v2, StringComparison.Ordinal);
            }
        }

        // ==================== 开发者工具 ====================

        /// <summary>
        /// 增量导出官方温度计到指定目录
        /// 生成 GeoT-List.json（官方温度计列表）和 GeoT-index.json（列表文件的哈希）
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

            // 生成 GeoT-List.json（官方温度计列表）
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
