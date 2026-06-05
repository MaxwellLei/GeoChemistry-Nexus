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
                // 获取分类路径 (强制使用当前APP语言，忽略 OverrideLanguage)
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
                    
                    GraphMapTemplateNode existingNode = null;
                    
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
                currentNode.Status = item.Status;
            }

            return rootNode;
        }

        /// <summary>
        /// 将本地官方模板与服务器 GraphMapList 对齐：更新元数据/状态，并删除已从清单下架的项。
        /// </summary>
        /// <returns>若数据库或状态有变更则为 true。</returns>
        public static bool SyncOfficialTemplatesFromServerList(List<JsonTemplateItem> serverList)
        {
            if (serverList == null || serverList.Count == 0)
                return false;

            bool changed = false;
            var dbService = GraphMapDatabaseService.Instance;
            var existingTemplates = dbService.GetSummaries().ToDictionary(x => x.Id, x => x);
            var serverIds = new HashSet<Guid>();

            foreach (var item in serverList)
            {
                if (!Guid.TryParse(item.ID, out Guid itemId))
                    continue;

                serverIds.Add(itemId);

                if (existingTemplates.TryGetValue(itemId, out _))
                {
                    var fullEntity = dbService.GetTemplate(itemId);
                    if (fullEntity == null)
                        continue;

                    bool isHashSame = string.Equals(fullEntity.FileHash, item.FileHash, StringComparison.OrdinalIgnoreCase);
                    bool hasVersionUpdate = ContentVersionHelper.HasContentUpdate(fullEntity.Version, item.Version);
                    string newStatus = string.Equals(fullEntity.Status, "NOT_INSTALLED", StringComparison.Ordinal)
                        ? "NOT_INSTALLED"
                        : (isHashSame && !hasVersionUpdate ? "UP_TO_DATE" : "OUTDATED");

                    bool metadataChanged =
                        fullEntity.Status != newStatus
                        || fullEntity.GraphMapPath != item.GraphMapPath
                        || !NodeListEquals(fullEntity.NodeList, item.NodeList)
                        || (!string.IsNullOrWhiteSpace(item.Version)
                            && ContentVersionHelper.Compare(fullEntity.Version, item.Version) != 0);

                    if (metadataChanged)
                    {
                        fullEntity.Status = newStatus;
                        fullEntity.GraphMapPath = item.GraphMapPath;
                        fullEntity.NodeList = item.NodeList;

                        if (!string.IsNullOrWhiteSpace(item.Version))
                            fullEntity.Version = ContentVersionHelper.Normalize(item.Version);

                        // 仅未安装占位记录同步服务器哈希；已安装模板保留本地 Content 对应的 FileHash
                        if (string.Equals(newStatus, "NOT_INSTALLED", StringComparison.Ordinal))
                            fullEntity.FileHash = item.FileHash;

                        dbService.UpsertTemplate(fullEntity);
                        changed = true;
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
                        Status = "NOT_INSTALLED",
                        LastModified = DateTime.Now,
                        Content = null,
                        TemplateType = null,
                        Version = ContentVersionHelper.Normalize(item.Version),
                        HelpDocuments = new Dictionary<string, string>()
                    };
                    dbService.UpsertTemplate(newEntity);
                    changed = true;
                }
            }

            foreach (var local in existingTemplates.Values.Where(e => !e.IsCustom))
            {
                if (!serverIds.Contains(local.Id))
                {
                    dbService.DeleteTemplate(local.Id);
                    changed = true;
                }
            }

            return changed;
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
            public string ID { get; set; }
            public LocalizedString NodeList { get; set; }
            public string GraphMapPath { get; set; }
            public string FileHash { get; set; }
            public string Version { get; set; }
        }

        /// <summary>
        /// 获取当前APP语言对应的路径，忽略 LocalizedString.OverrideLanguage 设置。
        /// </summary>
        private static string GetPathForAppLanguage(LocalizedString localizedString)
        {
            if (localizedString == null) return string.Empty;

            // 使用 LanguageService 获取当前界面语言
            string appLang = LanguageService.CurrentLanguage;

            if (localizedString.Translations != null && localizedString.Translations.ContainsKey(appLang))
            {
                return localizedString.Translations[appLang];
            }

            // 回退到默认语言
            if (!string.IsNullOrEmpty(localizedString.Default) &&
                localizedString.Translations != null &&
                localizedString.Translations.ContainsKey(localizedString.Default))
            {
                return localizedString.Translations[localizedString.Default];
            }

            return string.Empty;
        }

        /// <summary>
        /// 验证模板版本是否兼容
        /// </summary>
        /// <param name="template">模板对象</param>
        /// <returns>如果是 true 则兼容，否则不兼容</returns>
        public static bool IsVersionCompatible(GraphMapTemplate template)
        {
            if (template == null) return false;
            return ContentVersionHelper.IsDiagramFormatCompatible(template.Version);
        }

        /// <summary>
        /// 模板内容 JSON 序列化选项（与 FileHash 计算口径一致）
        /// </summary>
        public static JsonSerializerOptions CreateTemplateJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters = { new JsonStringEnumConverter() },
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            };
        }

        /// <summary>
        /// 序列化模板内容为 JSON 字符串（与 FileHash 口径一致）
        /// </summary>
        public static string SerializeTemplateContent(GraphMapTemplate template)
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
    }
}
