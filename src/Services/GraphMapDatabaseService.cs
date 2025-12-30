using LiteDB;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Services
{
    public class GraphMapDatabaseService
    {
        private static GraphMapDatabaseService _instance;
        public static GraphMapDatabaseService Instance => _instance ??= new GraphMapDatabaseService();

        private readonly string _dbPath;
        private const string CollectionName = "diagram_templates";
        private const string ThumbnailSuffix = "_thumb.png";

        // Namespace for UUID v5 generation (as per design doc)
        private static readonly Guid NamespaceGuid = Guid.Parse("6ba7b814-9dad-11d1-80b4-00c04fd430c8");

        private GraphMapDatabaseService()
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "PlotData");
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
        /// 获取所有模板的摘要信息（仅元数据，不含 Content）
        /// </summary>
        public List<GraphMapTemplateEntity> GetSummaries()
        {
            using var db = GetDatabase();
            var col = db.GetCollection<GraphMapTemplateEntity>(CollectionName);
            
            // 投影查询：只选择需要的字段
            return col.Query()
                .Select(x => new GraphMapTemplateEntity
                {
                    Id = x.Id,
                    Name = x.Name,
                    NodeList = x.NodeList,
                    IsCustom = x.IsCustom,
                    FileHash = x.FileHash,
                    GraphMapPath = x.GraphMapPath,
                    TemplateType = x.TemplateType,
                    Version = x.Version,
                    Status = x.Status,
                    IsNewTemplate = x.IsNewTemplate
                })
                .ToList();
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
                item.Status = null;
            }
            col.Update(all);
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
        /// 插入或更新模板
        /// </summary>
        public void UpsertTemplate(GraphMapTemplateEntity template)
        {
            using var db = GetDatabase();
            var col = db.GetCollection<GraphMapTemplateEntity>(CollectionName);
            col.Upsert(template);
            
            // 确保索引
            col.EnsureIndex(x => x.GraphMapPath);
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
        /// 获取缩略图流
        /// </summary>
        public Stream GetThumbnail(Guid id)
        {
            using var db = GetDatabase();
            string fileId = $"{id}{ThumbnailSuffix}";
            if (db.FileStorage.Exists(fileId))
            {
                return db.FileStorage.OpenRead(fileId);
            }
            return null;
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
