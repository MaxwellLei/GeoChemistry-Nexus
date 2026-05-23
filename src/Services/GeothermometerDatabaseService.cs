using LiteDB;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace GeoChemistryNexus.Services
{
    /// <summary>
    /// 温度计（GTM）数据库服务
    /// 使用 LiteDB 存储温度计的元数据、JS 脚本和帮助文档
    /// 参照 GraphMapDatabaseService 的设计模式
    /// </summary>
    public class GeothermometerDatabaseService
    {
        private static GeothermometerDatabaseService _instance;
        public static GeothermometerDatabaseService Instance => _instance ??= new GeothermometerDatabaseService();

        private readonly string _dbPath;
        private const string CollectionName = "geothermometer_plugins";

        // UUID v5 namespace（与 GraphMapDatabaseService 使用相同的标准 namespace）
        private static readonly Guid NamespaceGuid = Guid.Parse("6ba7b814-9dad-11d1-80b4-00c04fd430c8");

        private GeothermometerDatabaseService()
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Plugins");
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }
            _dbPath = Path.Combine(dataDir, "Geothermometer.db");
        }

        public LiteDatabase GetDatabase()
        {
            return new LiteDatabase($"Filename={_dbPath};Connection=Shared");
        }

        /// <summary>
        /// 获取所有温度计的摘要信息（不含 ScriptContent 和 HelpDocuments）
        /// </summary>
        public List<GeothermometerEntity> GetSummaries(bool? isOfficial = null)
        {
            using var db = GetDatabase();
            var col = db.GetCollection<GeothermometerEntity>(CollectionName);
            
            List<GeothermometerEntity> allEntities;
            if (isOfficial.HasValue)
                allEntities = col.Find(x => x.IsOfficial == isOfficial.Value).ToList();
            else
                allEntities = col.FindAll().ToList();

            // 清空重量级字段，减少内存占用
            foreach (var entity in allEntities)
            {
                entity.ScriptContent = string.Empty;
                entity.HelpDocuments = new Dictionary<string, string>();
            }

            return allEntities;
        }

        /// <summary>
        /// 根据 ID 获取完整温度计实体（含脚本和帮助文档）
        /// </summary>
        public GeothermometerEntity GetEntity(Guid id)
        {
            using var db = GetDatabase();
            var col = db.GetCollection<GeothermometerEntity>(CollectionName);
            return col.FindById(id);
        }

        /// <summary>
        /// 根据 PluginId 获取完整温度计实体
        /// </summary>
        public GeothermometerEntity GetEntityByPluginId(string pluginId)
        {
            using var db = GetDatabase();
            var col = db.GetCollection<GeothermometerEntity>(CollectionName);
            return col.FindOne(x => x.PluginId == pluginId);
        }

        /// <summary>
        /// 插入或更新温度计
        /// </summary>
        public void UpsertEntity(GeothermometerEntity entity)
        {
            using var db = GetDatabase();
            var col = db.GetCollection<GeothermometerEntity>(CollectionName);
            col.Upsert(entity);

            // 确保索引
            col.EnsureIndex(x => x.PluginId);
            col.EnsureIndex(x => x.Mineral);
            col.EnsureIndex(x => x.IsOfficial);
        }

        /// <summary>
        /// 删除温度计
        /// </summary>
        public void DeleteEntity(Guid id)
        {
            using var db = GetDatabase();
            var col = db.GetCollection<GeothermometerEntity>(CollectionName);
            col.Delete(id);
        }

        /// <summary>
        /// 检查数据库是否为空
        /// </summary>
        public bool IsDatabaseEmpty()
        {
            using var db = GetDatabase();
            var col = db.GetCollection<GeothermometerEntity>(CollectionName);
            return col.Count() == 0;
        }

        /// <summary>
        /// 获取所有温度计数量
        /// </summary>
        public int Count()
        {
            using var db = GetDatabase();
            var col = db.GetCollection<GeothermometerEntity>(CollectionName);
            return col.Count();
        }

        /// <summary>
        /// 生成确定性的 UUID v5
        /// </summary>
        public static Guid GenerateId(string pluginId)
        {
            if (string.IsNullOrEmpty(pluginId))
                throw new ArgumentNullException(nameof(pluginId));

            string input = "gtm_" + pluginId;

            byte[] namespaceBytes = NamespaceGuid.ToByteArray();
            SwapByteOrder(namespaceBytes);

            byte[] nameBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashInput = new byte[namespaceBytes.Length + nameBytes.Length];

            Buffer.BlockCopy(namespaceBytes, 0, hashInput, 0, namespaceBytes.Length);
            Buffer.BlockCopy(nameBytes, 0, hashInput, namespaceBytes.Length, nameBytes.Length);

            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(hashInput);
                hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
                hash[8] = (byte)((hash[8] & 0x3F) | 0x80);

                byte[] newGuid = new byte[16];
                Buffer.BlockCopy(hash, 0, newGuid, 0, 16);
                SwapByteOrder(newGuid);

                return new Guid(newGuid);
            }
        }

        /// <summary>
        /// 计算内容的 MD5 哈希
        /// </summary>
        public static string ComputeHash(string content)
        {
            using var md5 = MD5.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            byte[] hash = md5.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
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
            (guid[left], guid[right]) = (guid[right], guid[left]);
        }
    }
}
