using GeoChemistryNexus.Models;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Services
{
    public static class GraphMapTemplateService
    {
        /// <summary>
        /// 根据数据库实体列表构建模板树
        /// </summary>
        public static GraphMapTemplateNode BuildTreeFromEntities(List<GraphMapTemplateEntity> entities)
        {
            var rootNode = new GraphMapTemplateNode { Name = "Root" };

            if (entities == null || !entities.Any())
            {
                return rootNode;
            }

            const string separator = ">";

            foreach (var item in entities)
            {
                // 获取分类路径 (强制使用当前 App 界面语言，忽略图解内容语言上下文)
                string selectedPath = GetPathForAppLanguage(item.NodeList);

                if (item.IsCustom)
                {
                    string customRoot = LanguageService.Instance["custom_templates"];
                    if (string.IsNullOrEmpty(customRoot)) customRoot = "Custom Templates";
                    selectedPath = $"{customRoot}{separator}{selectedPath}";
                }

                // 分割选择的路径
                var pathParts = selectedPath.Split(separator).Select(p => p.Trim()).ToArray();

                var currentNode = rootNode;

                // 遍历路径的每个部分，构建节点树
                for (int i = 0; i < pathParts.Length; i++)
                {
                    string partName = pathParts[i];
                    bool isLeaf = (i == pathParts.Length - 1);
                    
                    GraphMapTemplateNode? existingNode = null;
                    
                    if (!isLeaf)
                    {
                        // 找到一个名字相同，且 GraphMapPath 为空的节点（纯文件夹）
                        existingNode = currentNode.Children.FirstOrDefault(c => c.Name == partName && string.IsNullOrEmpty(c.GraphMapPath));
                    }
                    else
                    {
                        existingNode = null; 
                    }

                    if (existingNode != null)
                    {
                        // 如果存在可复用的容器节点，则进入该节点
                        currentNode = existingNode;
                    }
                    else
                    {
                        // 如果节点不存在，则创建一个新节点
                        var newNode = new GraphMapTemplateNode
                        {
                            Name = partName,
                            Parent = currentNode    //设置父节点
                        };
                        currentNode.Children.Add(newNode);
                        currentNode = newNode;
                    }
                }

                // 将模板信息赋值给叶子节点
                currentNode.GraphMapPath = item.GraphMapPath;
                currentNode.FileHash = item.FileHash;
                currentNode.IsCustomTemplate = item.IsCustom;
                currentNode.TemplateId = item.Id;
                currentNode.Status = item.Status ?? string.Empty;
            }

            return rootNode;
        }

        /// <summary>
        /// 官方模板清单同步结果（仅目录/状态，不含 ZIP 下载）。
        /// </summary>
        public sealed class OfficialTemplateCatalogSyncResult
        {
            public bool CatalogChanged { get; set; }
            public int AddedCount { get; set; }
            public int OutdatedCount { get; set; }
            public int RequiresAppUpgradeCount { get; set; }
            public int RemovalCount { get; set; }
        }

        /// <summary>
        /// 将本地官方模板与服务器 GraphMapList 对齐：更新元数据/状态，并删除已从清单下架的项。
        /// </summary>
        /// <returns>若数据库或状态有变更则为 true。</returns>
        public static bool SyncOfficialTemplatesFromServerList(List<JsonTemplateItem> serverList)
            => SyncOfficialTemplatesFromServerListDetailed(serverList).CatalogChanged;

        /// <summary>
        /// 将本地官方模板与服务器 GraphMapList 对齐，并返回新增/可更新/下架计数。
        /// </summary>
        public static OfficialTemplateCatalogSyncResult SyncOfficialTemplatesFromServerListDetailed(
            List<JsonTemplateItem> serverList)
        {
            var result = new OfficialTemplateCatalogSyncResult();
            if (serverList == null || serverList.Count == 0)
                return result;

            var dbService = GraphMapDatabaseService.Instance;
            var existingTemplates = dbService.GetSummaries().ToDictionary(x => x.Id, x => x);
            var serverIds = new HashSet<Guid>();
            string appFormatVersion = ContentVersionHelper.GetDiagramFormatVersion();

            foreach (var item in serverList)
            {
                if (!Guid.TryParse(item.ID, out Guid itemId))
                    continue;

                serverIds.Add(itemId);

                if (existingTemplates.TryGetValue(itemId, out var summary))
                {
                    // 已安装 = 本地有模板内容（占位记录没有 Content）。摘要投影已带上该标记，无需读库；
                    // 状态规则与温压计 / EPMA 共用
                    bool isInstalled = summary.IsInstalled;
                    string newStatus = ContentInstallStatus.Resolve(
                        isInstalled,
                        summary.FileHash,
                        item.FileHash,
                        summary.Version,
                        item.Version,
                        appFormatVersion);

                    // 待发布的本地改动（如移动、重命名文件夹）以本地分类路径为准，不被服务器目录覆盖
                    bool syncNodeList = !summary.PendingPublish;

                    // 本地版本号只在未安装占位上跟随服务器；已安装的保留模板内容自带的版本号
                    bool metadataChanged =
                        summary.Status != newStatus
                        || summary.GraphMapPath != item.GraphMapPath
                        || (syncNodeList && !NodeListEquals(summary.NodeList, item.NodeList))
                        || (!isInstalled
                            && !string.IsNullOrWhiteSpace(item.Version)
                            && ContentVersionHelper.Compare(summary.Version, item.Version) != 0)
                        || summary.IsNewTemplate;

                    if (metadataChanged)
                    {
                        // 仅在需要写回时才读取完整实体
                        var fullEntity = dbService.GetTemplate(itemId);
                        if (fullEntity == null)
                            continue;

                        fullEntity.Status = newStatus;
                        fullEntity.GraphMapPath = item.GraphMapPath;
                        if (syncNodeList)
                            fullEntity.NodeList = item.NodeList;
                        fullEntity.IsNewTemplate = false;

                        if (!isInstalled && !string.IsNullOrWhiteSpace(item.Version))
                            fullEntity.Version = ContentVersionHelper.Normalize(item.Version);

                        // 仅未安装占位记录同步服务器哈希；已安装模板保留本地 Content 对应的 FileHash
                        if (!isInstalled)
                            fullEntity.FileHash = item.FileHash;

                        dbService.UpsertTemplate(fullEntity);
                        result.CatalogChanged = true;
                    }
                }
                else
                {
                    var newEntity = new GraphMapTemplateEntity
                    {
                        Id = itemId,
                        NodeList = item.NodeList,
                        GraphMapPath = item.GraphMapPath,
                        FileHash = item.FileHash,
                        IsCustom = false,
                        Status = ContentInstallStatus.Resolve(false, null, item.FileHash, null, item.Version, appFormatVersion),
                        LastModified = DateTime.Now,
                        Content = null,
                        TemplateType = string.Empty,
                        Version = ContentVersionHelper.Normalize(item.Version),
                        HelpDocuments = new Dictionary<string, string>()
                    };
                    dbService.UpsertTemplate(newEntity);
                    result.AddedCount++;
                    result.CatalogChanged = true;
                }
            }

            foreach (var local in existingTemplates.Values.Where(e => !e.IsCustom))
            {
                if (!serverIds.Contains(local.Id))
                {
                    // 开发者本地新建 / 由自定义转成的官方模板尚未发布，不能当作“服务器已下架”删除；
                    // 待发布的修改同理（与 EPMA 对本地未发布官方包的保护一致）
                    if (local.IsNewTemplate || local.PendingPublish)
                        continue;

                    dbService.DeleteTemplate(local.Id);
                    result.RemovalCount++;
                    result.CatalogChanged = true;
                }
            }

            var officialSummaries = dbService.GetSummaries().Where(e => !e.IsCustom);
            result.OutdatedCount = officialSummaries.Count(e =>
                string.Equals(e.Status, ContentInstallStatus.Outdated, StringComparison.Ordinal));
            result.RequiresAppUpgradeCount = officialSummaries.Count(e =>
                ContentInstallStatus.IsRequiresAppUpgrade(e.Status));

            return result;
        }

        private static bool NodeListEquals(LocalizedString a, LocalizedString b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a.Default != b.Default) return false;
            if (a.Translations == null && b.Translations == null) return true;
            if (a.Translations == null || b.Translations == null) return false;
            if (a.Translations.Count != b.Translations.Count) return false;
            foreach (var kv in a.Translations)
            {
                if (!b.Translations.TryGetValue(kv.Key, out var other) || other != kv.Value)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 将表示样式的字符串转换为 ScottPlot 的 LinePattern 枚举。
        /// </summary>
        public static LinePattern GetLinePattern(string style)
        {
            return style.ToString()?.ToLower() switch
            {
                "solid" => LinePattern.Solid,
                "dash" => LinePattern.Dashed,
                "dot" => LinePattern.Dotted,
                "denselydashed" => LinePattern.DenselyDashed,
                _ => LinePattern.Solid, // 默认值为实线
            };
        }

        // 颜色转换辅助方法
        public static string ConvertWpfHexToScottPlotHex(string wpfHex)
        {
            // 移除 '#'
            if (wpfHex.StartsWith("#"))
            {
                wpfHex = wpfHex.Substring(1);
            }

            // 如果是 #RRGGBB (长度为6)
            if (wpfHex.Length == 6)
            {
                return "#" + wpfHex;
            }

            // 如果是 #AARRGGBB (长度为8)
            if (wpfHex.Length == 8)
            {
                string alpha = wpfHex.Substring(0, 2);
                string rgb = wpfHex.Substring(2, 6);
                return $"#{rgb}{alpha}"; // 重新排列为 #RRGGBBAA
            }

            // 对于无效格式，返回一个默认值，例如黑色
            return "#000000";
        }

        /// <summary>
        /// 用于临时匹配 JSON 结构的辅助类
        /// </summary>
        public class JsonTemplateItem
        {
            public string ID { get; set; } = string.Empty;
            public LocalizedString NodeList { get; set; } = new();
            public string GraphMapPath { get; set; } = string.Empty;
            public string FileHash { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
        }

        /// <summary>
        /// 获取当前 App 界面语言对应的路径，忽略图解内容语言上下文。
        /// </summary>
        public static string GetPathForAppLanguage(LocalizedString? localizedString)
        {
            if (localizedString == null) return string.Empty;

            string appLang = AppCultureRegistry.ResolveAppLanguage(LanguageService.CurrentLanguage);
            return AppCultureRegistry.GetLocalizedValue(
                localizedString.Translations,
                appLang,
                localizedString.Default);
        }

        /// <summary>
        /// 验证模板版本是否兼容
        /// </summary>
        /// <param name="template">模板对象</param>
        /// <returns>如果是 true 则兼容，否则不兼容</returns>
        public static bool IsVersionCompatible(GraphMapTemplate? template)
        {
            if (template == null) return false;
            return ContentVersionHelper.IsDiagramFormatCompatible(template.Version);
        }

        /// <summary>
        /// 校验 JSON 文本是否具备图解模板必需结构（反序列化前，避免非图解 JSON 被默认属性误判为有效）。
        /// </summary>
        public static bool HasValidDiagramTemplateJsonStructure(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
                return false;

            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                return HasValidDiagramTemplateJsonStructure(document.RootElement);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// 校验已反序列化的图解模板内容是否完整有效。
        /// </summary>
        public static bool IsValidDiagramTemplateContent([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] GraphMapTemplate? template)
        {
            if (template?.Info == null || template.Script == null)
                return false;

            if (!IsSupportedTemplateType(template.TemplateType))
                return false;

            int requiredAxes = template.TemplateType == "Ternary" ? 3 : 2;
            if (template.Info.Axes == null
                || template.Info.Axes.Count < requiredAxes
                || template.Info.Axes.Any(axis => axis == null || string.IsNullOrWhiteSpace(axis.Type)))
            {
                return false;
            }

            return template.Info.Title != null && template.Info.Grid != null;
        }

        /// <summary>
        /// 解析并校验图解模板 JSON；失败时 template 为 null（不含版本校验）。
        /// </summary>
        public static bool TryParseDiagramTemplate(string jsonContent, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out GraphMapTemplate? template)
        {
            template = null;
            if (!HasValidDiagramTemplateJsonStructure(jsonContent))
                return false;

            try
            {
                template = JsonSerializer.Deserialize<GraphMapTemplate>(jsonContent, CreateTemplateParseJsonOptions());
            }
            catch (JsonException)
            {
                template = null;
                return false;
            }

            if (template == null || !IsValidDiagramTemplateContent(template))
            {
                template = null;
                return false;
            }

            return true;
        }

        private static bool HasValidDiagramTemplateJsonStructure(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object)
                return false;

            if (!TryGetJsonProperty(root, "TemplateType", out var templateTypeElement)
                || templateTypeElement.ValueKind != JsonValueKind.String
                || !IsSupportedTemplateType(templateTypeElement.GetString()))
            {
                return false;
            }

            string templateType = templateTypeElement.GetString()!;
            int requiredAxes = templateType == "Ternary" ? 3 : 2;

            if (!TryGetJsonProperty(root, "Info", out var infoElement)
                || infoElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!TryGetJsonProperty(infoElement, "Axes", out var axesElement)
                || axesElement.ValueKind != JsonValueKind.Array
                || axesElement.GetArrayLength() < requiredAxes)
            {
                return false;
            }

            foreach (var axisElement in axesElement.EnumerateArray())
            {
                if (axisElement.ValueKind != JsonValueKind.Object)
                    return false;

                if (!TryGetJsonProperty(axisElement, "Type", out var typeElement)
                    || typeElement.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(typeElement.GetString()))
                {
                    return false;
                }
            }

            if (!TryGetJsonProperty(infoElement, "Title", out var titleElement)
                || titleElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!TryGetJsonProperty(infoElement, "Grid", out var gridElement)
                || gridElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!TryGetJsonProperty(root, "Script", out var scriptElement)
                || scriptElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            return true;
        }

        private static bool IsSupportedTemplateType(string? templateType)
        {
            return string.Equals(templateType, "Cartesian", StringComparison.OrdinalIgnoreCase)
                || string.Equals(templateType, "Ternary", StringComparison.OrdinalIgnoreCase);
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

        /// <summary>
        /// 图解模板 JSON 反序列化选项（导入/打开时使用，与持久化口径一致并忽略大小写）。
        /// </summary>
        public static JsonSerializerOptions CreateTemplateParseJsonOptions() => TemplateParseJsonOptions;

        /// <summary>
        /// 模板内容 JSON 序列化选项（与 FileHash 计算口径一致）
        /// </summary>
        public static JsonSerializerOptions CreateTemplateJsonOptions() => TemplateJsonOptions;

        // System.Text.Json 的类型元数据缓存挂在 options 实例上，必须复用同一实例；
        // 返回的实例首次使用后即为只读，调用方不得修改。
        private static readonly JsonSerializerOptions TemplateJsonOptions = BuildTemplateJsonOptions(false);

        private static readonly JsonSerializerOptions TemplateParseJsonOptions = BuildTemplateJsonOptions(true);

        private static JsonSerializerOptions BuildTemplateJsonOptions(bool caseInsensitive)
        {
            return new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters = { new JsonStringEnumConverter() },
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                PropertyNameCaseInsensitive = caseInsensitive
            };
        }

        /// <summary>
        /// 序列化模板内容为 JSON 字符串（与 FileHash 口径一致）
        /// </summary>
        public static string SerializeTemplateContent(GraphMapTemplate? template)
        {
            if (template == null) return string.Empty;
            return JsonSerializer.Serialize(template, CreateTemplateJsonOptions());
        }

        /// <summary>
        /// 深拷贝模板内容（JSON 往返，与持久化口径一致）。
        /// </summary>
        public static GraphMapTemplate? CloneTemplateContent(GraphMapTemplate? template)
        {
            if (template == null) return null;

            string json = SerializeTemplateContent(template);
            if (string.IsNullOrWhiteSpace(json)) return null;

            return JsonSerializer.Deserialize<GraphMapTemplate>(json, CreateTemplateJsonOptions());
        }

        /// <summary>
        /// 计算模板内容的 MD5 哈希（与保存/清单 FileHash 口径一致）
        /// </summary>
        public static string ComputeTemplateContentHash(GraphMapTemplate template)
        {
            if (template == null) return string.Empty;

            string json = SerializeTemplateContent(template);
            using var md5 = MD5.Create();
            byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(json));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// 开发者操作：将已下载的官方图解降级为个人图解（保留内容与缩略图，替换同路径的个人图解）。
        /// 与 EPMA 的 DemoteToCustomAsync、温压计的 ConvertToCustom 对应；不会重新校验内容版本。
        /// </summary>
        /// <returns>降级后的个人图解 Id；记录不存在、已是个人图解或尚未下载内容时返回 null。</returns>
        public static Guid? DemoteToCustom(Guid officialId)
        {
            var db = GraphMapDatabaseService.Instance;
            var entity = db.GetTemplate(officialId);
            if (entity == null || entity.IsCustom || entity.Content == null) return null;

            Guid customId = GraphMapDatabaseService.GenerateId(entity.GraphMapPath, true);

            byte[]? thumbnailBytes = null;
            using (var stream = db.GetThumbnail(officialId))
            {
                if (stream != null)
                {
                    using var ms = new System.IO.MemoryStream();
                    stream.CopyTo(ms);
                    thumbnailBytes = ms.ToArray();
                }
            }

            // 同路径的个人图解会被替换（含其缩略图）；收藏状态沿用被替换者（与 EPMA 一致）
            bool isFavorite = db.GetTemplate(customId)?.IsFavorite ?? entity.IsFavorite;
            db.DeleteTemplate(customId);
            db.DeleteTemplate(officialId);

            entity.Id = customId;
            entity.IsCustom = true;
            entity.IsFavorite = isFavorite;
            entity.IsNewTemplate = false;
            entity.PendingPublish = false;
            entity.Status = string.Empty;
            entity.LastModified = DateTime.Now;
            entity.FileHash = ComputeTemplateContentHash(entity.Content);
            db.UpsertTemplate(entity);

            if (thumbnailBytes != null)
            {
                using var ms = new System.IO.MemoryStream(thumbnailBytes);
                db.UploadThumbnail(customId, ms);
            }

            return customId;
        }
    }
}
