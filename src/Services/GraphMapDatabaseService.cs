using LiteDB;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Services
{
    public class GraphMapDatabaseService
    {
        private static GraphMapDatabaseService _instance = null!;
        public static GraphMapDatabaseService Instance => _instance ??= new GraphMapDatabaseService();

        private readonly string _dbPath;
        private const string CollectionName = "diagram_templates";
        private const string ThumbnailSuffix = "_thumb.png";

        // Namespace for UUID v5 generation (as per design doc)
        private static readonly Guid NamespaceGuid = Guid.Parse("6ba7b814-9dad-11d1-80b4-00c04fd430c8");

        private readonly object _summariesCacheLock = new();
        private List<GraphMapTemplateEntity>? _summariesCache;

        private GraphMapDatabaseService()
        {
            AppDataPathHelper.Initialize();
            string dataDir = AppDataPathHelper.GetDataPath("PlotData");
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }
            _dbPath = Path.Combine(dataDir, "Templates.db");
        }

        public LiteDatabase GetDatabase()
        {
            return new LiteDatabase($"Filename={_dbPath};Connection=Shared");
        }

        /// <summary>
        /// 获取所有模板的摘要信息（仅元数据，不含 Content / HelpDocuments）。
        /// 结果带进程内缓存；调用方拿到的是副本，可安全改状态字段。
        /// </summary>
        public List<GraphMapTemplateEntity> GetSummaries()
        {
            lock (_summariesCacheLock)
            {
                if (_summariesCache == null)
                    _summariesCache = LoadSummariesFromDatabase();

                return _summariesCache.Select(CloneSummary).ToList();
            }
        }

        /// <summary>
        /// 使摘要缓存失效（模板增删改后调用）。
        /// </summary>
        public void InvalidateSummariesCache()
        {
            lock (_summariesCacheLock)
            {
                _summariesCache = null;
            }
        }

        private List<GraphMapTemplateEntity> LoadSummariesFromDatabase()
        {
            using var db = GetDatabase();
            var col = db.GetCollection<GraphMapTemplateEntity>(CollectionName);

            var allEntities = col.FindAll().ToList();

            foreach (var entity in allEntities)
            {
                entity.Content = null;
                entity.HelpDocuments = new Dictionary<string, string>();
            }

            return allEntities;
        }

        private static GraphMapTemplateEntity CloneSummary(GraphMapTemplateEntity source)
        {
            return new GraphMapTemplateEntity
            {
                Id = source.Id,
                GraphMapPath = source.GraphMapPath,
                FileHash = source.FileHash,
                IsCustom = source.IsCustom,
                IsNewTemplate = source.IsNewTemplate,
                LastModified = source.LastModified,
                Name = source.Name,
                NodeList = CloneLocalizedString(source.NodeList),
                TemplateType = source.TemplateType,
                Version = source.Version,
                Content = null,
                HelpDocuments = new Dictionary<string, string>(),
                Status = source.Status,
                IsFavorite = source.IsFavorite,
                PendingPublish = source.PendingPublish
            };
        }

        private static LocalizedString CloneLocalizedString(LocalizedString? source)
        {
            if (source == null)
                return new LocalizedString();

            return new LocalizedString
            {
                Default = source.Default,
                Translations = source.Translations == null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string>(source.Translations)
            };
        }

        /// <summary>
        /// 重置所有模板的状态为 null
        /// </summary>
        public void ResetAllStatuses()
        {
            using var db = GetDatabase();
            var col = db.GetCollection<GraphMapTemplateEntity>(CollectionName);
            var all = col.FindAll().ToList();
            foreach (var item in all)
            {
                item.Status = string.Empty;
            }
            col.Update(all);
            InvalidateSummariesCache();
        }

        /// <summary>
        /// 根据 ID 获取完整模板
        /// </summary>
        public GraphMapTemplateEntity GetTemplate(Guid id)
        {
            using var db = GetDatabase();
            var col = db.GetCollection<GraphMapTemplateEntity>(CollectionName);
            return col.FindById(id);
        }

        /// <summary>
        /// 按语言加载帮助文档 RTF，避免反序列化完整 Content。
        /// 回退顺序：preferredLanguage → 语言前缀匹配 → en-US → 字典中第一条非空
        /// </summary>
        public string? GetHelpDocument(Guid id, string preferredLanguage)
        {
            using var db = GetDatabase();
            var col = db.GetCollection<BsonDocument>(CollectionName);
            var doc = col.FindById(id);
            if (doc == null)
                return null;

            if (!doc.TryGetValue("HelpDocuments", out BsonValue helpValue)
                || helpValue == null
                || helpValue.IsNull
                || !helpValue.IsDocument)
            {
                return null;
            }

            var helpDocs = helpValue.AsDocument;
            if (helpDocs.Count == 0)
                return null;

            if (!string.IsNullOrWhiteSpace(preferredLanguage)
                && TryGetHelpRtf(helpDocs, preferredLanguage, out string? preferred))
            {
                return preferred;
            }

            // 前缀匹配（如 "zh" → "zh-CN"）
            if (!string.IsNullOrWhiteSpace(preferredLanguage))
            {
                string prefix = preferredLanguage.Split('-')[0];
                foreach (var key in helpDocs.Keys)
                {
                    if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                        && TryGetHelpRtf(helpDocs, key, out string? prefixMatch)
                        && !string.IsNullOrEmpty(prefixMatch))
                    {
                        return prefixMatch;
                    }
                }
            }

            if (!string.Equals(preferredLanguage, "en-US", StringComparison.OrdinalIgnoreCase)
                && TryGetHelpRtf(helpDocs, "en-US", out string? english))
            {
                return english;
            }

            foreach (var key in helpDocs.Keys)
            {
                if (TryGetHelpRtf(helpDocs, key, out string? any) && !string.IsNullOrEmpty(any))
                    return any;
            }

            return null;
        }

        private static bool TryGetHelpRtf(BsonDocument helpDocs, string languageKey, out string? rtf)
        {
            rtf = null;
            if (string.IsNullOrWhiteSpace(languageKey))
                return false;

            if (helpDocs.TryGetValue(languageKey, out BsonValue exact) && exact != null && exact.IsString)
            {
                rtf = exact.AsString;
                return !string.IsNullOrEmpty(rtf);
            }

            foreach (var key in helpDocs.Keys)
            {
                if (!string.Equals(key, languageKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = helpDocs[key];
                if (value != null && value.IsString)
                {
                    rtf = value.AsString;
                    return !string.IsNullOrEmpty(rtf);
                }
            }

            return false;
        }

        /// <summary>
        /// 插入或更新模板
        /// </summary>
        public void UpsertTemplate(GraphMapTemplateEntity template)
        {
            using var db = GetDatabase();
            var col = db.GetCollection<GraphMapTemplateEntity>(CollectionName);
            col.Upsert(template);

            // 确保索引
            col.EnsureIndex(x => x.GraphMapPath);
            InvalidateSummariesCache();
        }

        /// <summary>
        /// 仅更新模板状态，避免用摘要对象覆盖 Content 等重量级字段。
        /// </summary>
        public void UpdateTemplateStatus(Guid id, string status)
        {
            using var db = GetDatabase();
            var col = db.GetCollection<GraphMapTemplateEntity>(CollectionName);
            var entity = col.FindById(id);
            if (entity == null || string.Equals(entity.Status, status, StringComparison.Ordinal))
                return;

            entity.Status = status;
            col.Update(entity);
            InvalidateSummariesCache();
        }

        /// <summary>
        /// 删除模板
        /// </summary>
        public void DeleteTemplate(Guid id)
        {
            using var db = GetDatabase();
            var col = db.GetCollection<GraphMapTemplateEntity>(CollectionName);
            col.Delete(id);

            // 删除关联的缩略图
            string fileId = $"{id}{ThumbnailSuffix}";
            if (db.FileStorage.Exists(fileId))
            {
                db.FileStorage.Delete(fileId);
            }

            InvalidateSummariesCache();
        }

        /// <summary>
        /// 上传缩略图
        /// </summary>
        public void UploadThumbnail(Guid id, Stream stream)
        {
            using var db = GetDatabase();
            string fileId = $"{id}{ThumbnailSuffix}";
            db.FileStorage.Upload(fileId, $"{id}.png", stream);
        }

        /// <summary>
        /// 获取缩略图流（独立 MemoryStream，不依赖 LiteDB 连接生命周期）
        /// </summary>
        public Stream? GetThumbnail(Guid id)
        {
            using var db = GetDatabase();
            string fileId = $"{id}{ThumbnailSuffix}";
            if (!db.FileStorage.Exists(fileId))
                return null;

            using var source = db.FileStorage.OpenRead(fileId);
            var buffer = new MemoryStream();
            source.CopyTo(buffer);
            buffer.Position = 0;
            return buffer;
        }

        /// <summary>
        /// 生成确定性的 UUID v5
        /// </summary>
        public static Guid GenerateId(string graphMapPath, bool isCustom = false)
        {
            if (string.IsNullOrEmpty(graphMapPath))
                throw new ArgumentNullException(nameof(graphMapPath));

            string input = isCustom ? "custom_" + graphMapPath : graphMapPath;

            // UUID v5 implementation using SHA1
            byte[] namespaceBytes = NamespaceGuid.ToByteArray();
            SwapByteOrder(namespaceBytes);

            byte[] nameBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashInput = new byte[namespaceBytes.Length + nameBytes.Length];

            Buffer.BlockCopy(namespaceBytes, 0, hashInput, 0, namespaceBytes.Length);
            Buffer.BlockCopy(nameBytes, 0, hashInput, namespaceBytes.Length, nameBytes.Length);

            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(hashInput);

                hash[6] = (byte)((hash[6] & 0x0F) | 0x50); // Set version to 5
                hash[8] = (byte)((hash[8] & 0x3F) | 0x80); // Set variant to RFC 4122

                byte[] newGuid = new byte[16];
                Buffer.BlockCopy(hash, 0, newGuid, 0, 16);
                SwapByteOrder(newGuid);

                return new Guid(newGuid);
            }
        }

        private static void SwapByteOrder(byte[] guid)
        {
            SwapBytes(guid, 0, 3);
            SwapBytes(guid, 1, 2);
            SwapBytes(guid, 4, 5);
            SwapBytes(guid, 6, 7);
        }

        private static void SwapBytes(byte[] guid, int left, int right)
        {
            byte temp = guid[left];
            guid[left] = guid[right];
            guid[right] = temp;
        }

        /// <summary>
        /// 检查数据库是否为空
        /// </summary>
        public bool IsDatabaseEmpty()
        {
            using var db = GetDatabase();
            var col = db.GetCollection<GraphMapTemplateEntity>(CollectionName);
            return col.Count() == 0;
        }
    }
}
