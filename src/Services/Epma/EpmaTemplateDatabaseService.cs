using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace GeoChemistryNexus.Services.Epma;

/// <summary>
/// EPMA 计算模板数据库服务。
/// 使用 LiteDB 存储模板元数据与完整的 .epma.zip 字节，设计与 GeothermometerDatabaseService 一致。
/// </summary>
public sealed class EpmaTemplateDatabaseService
{
    private static EpmaTemplateDatabaseService? _instance;
    public static EpmaTemplateDatabaseService Instance => _instance ??= new EpmaTemplateDatabaseService();

    private const string CollectionName = "epma_templates";

    // 与图解 / 温压计数据库使用同一个标准 UUID v5 namespace
    private static readonly Guid NamespaceGuid = Guid.Parse("6ba7b814-9dad-11d1-80b4-00c04fd430c8");

    // 列表查询投影：绝不读取 PackageBytes
    private const string SummaryProjection =
        "{ _id: $._id, PackageId: $.PackageId, Version: $.Version, FileHash: $.FileHash, " +
        "LastModified: $.LastModified, IsOfficial: $.IsOfficial, IsFavorite: $.IsFavorite, Status: $.Status, ServerHash: $.ServerHash, PendingPublish: $.PendingPublish, " +
        "NameJson: $.NameJson, DescriptionJson: $.DescriptionJson, TagsJson: $.TagsJson, Author: $.Author, " +
        "UpdatedAt: $.UpdatedAt, Icon: $.Icon }";

    private readonly string _dbPath;

    /// <param name="dbPath">数据库文件路径；为空时使用应用数据目录下的 Plugins/Epma.db</param>
    public EpmaTemplateDatabaseService(string? dbPath = null)
    {
        if (string.IsNullOrWhiteSpace(dbPath))
        {
            AppDataPathHelper.Initialize();
            string dataDir = AppDataPathHelper.GetDataPath("Plugins");
            Directory.CreateDirectory(dataDir);
            dbPath = Path.Combine(dataDir, "Epma.db");
        }
        else
        {
            string? directory = Path.GetDirectoryName(Path.GetFullPath(dbPath));
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
        }

        _dbPath = dbPath;
    }

    public LiteDatabase GetDatabase() => new($"Filename={_dbPath};Connection=Shared");

    /// <summary>获取全部模板的摘要（不含 PackageBytes）</summary>
    public List<EpmaTemplateEntity> GetSummaries(bool? isOfficial = null)
    {
        using var db = GetDatabase();
        var col = db.GetCollection<BsonDocument>(CollectionName);

        var query = col.Query();
        if (isOfficial.HasValue)
            query = query.Where(BsonExpression.Create("$.IsOfficial = @0", isOfficial.Value));

        var entities = query.Select(SummaryProjection).ToList()
            .Select(doc => MapEntity(db, doc))
            .ToList();

        foreach (var entity in entities)
            Normalize(entity);

        return entities;
    }

    /// <summary>按 Id 获取摘要（不含 PackageBytes）</summary>
    public EpmaTemplateEntity? GetSummary(Guid id)
    {
        using var db = GetDatabase();
        var col = db.GetCollection<BsonDocument>(CollectionName);
        var doc = col.Query().Where(BsonExpression.Create("$._id = @0", id)).Select(SummaryProjection).FirstOrDefault();
        if (doc == null)
            return null;

        var entity = MapEntity(db, doc);
        Normalize(entity);
        return entity;
    }

    /// <summary>旧记录没有 PendingPublish 字段，投影为 null 时直接映射到 bool 会抛 NullReferenceException，先移除。</summary>
    private static EpmaTemplateEntity MapEntity(LiteDatabase db, BsonDocument doc)
    {
        if (doc.TryGetValue("PendingPublish", out var pending) && pending.IsNull)
            doc.Remove("PendingPublish");
        return db.Mapper.ToObject<EpmaTemplateEntity>(doc);
    }

    /// <summary>获取完整实体（含 PackageBytes）</summary>
    public EpmaTemplateEntity? GetEntity(Guid id)
    {
        using var db = GetDatabase();
        var entity = db.GetCollection<EpmaTemplateEntity>(CollectionName).FindById(id);
        if (entity != null)
            Normalize(entity);
        return entity;
    }

    public EpmaTemplateEntity? GetEntity(string packageId, bool isOfficial)
        => string.IsNullOrWhiteSpace(packageId) ? null : GetEntity(GenerateId(packageId, isOfficial));

    public void UpsertEntity(EpmaTemplateEntity entity)
    {
        using var db = GetDatabase();
        var col = db.GetCollection<EpmaTemplateEntity>(CollectionName);
        col.Upsert(entity);
        col.EnsureIndex(x => x.PackageId);
        col.EnsureIndex(x => x.IsOfficial);
    }

    public bool DeleteEntity(Guid id)
    {
        using var db = GetDatabase();
        return db.GetCollection<EpmaTemplateEntity>(CollectionName).Delete(id);
    }

    public int Count()
    {
        using var db = GetDatabase();
        return db.GetCollection<EpmaTemplateEntity>(CollectionName).Count();
    }

    public int CountOfficial()
    {
        using var db = GetDatabase();
        return db.GetCollection<EpmaTemplateEntity>(CollectionName).Count(x => x.IsOfficial);
    }

    /// <summary>清空本地 EPMA 计算模板数据库</summary>
    public void ClearDatabase()
    {
        using var db = GetDatabase();
        db.GetCollection<EpmaTemplateEntity>(CollectionName).DeleteAll();
        db.Checkpoint();
    }

    private static void Normalize(EpmaTemplateEntity entity)
    {
        entity.PackageId ??= string.Empty;
        entity.Version ??= string.Empty;
        entity.FileHash ??= string.Empty;
        entity.Status ??= string.Empty;
        entity.ServerHash ??= string.Empty;
        entity.NameJson = string.IsNullOrWhiteSpace(entity.NameJson) ? "null" : entity.NameJson;
        entity.DescriptionJson = string.IsNullOrWhiteSpace(entity.DescriptionJson) ? "null" : entity.DescriptionJson;
        entity.TagsJson = string.IsNullOrWhiteSpace(entity.TagsJson) ? "[]" : entity.TagsJson;
        entity.Author ??= string.Empty;
        entity.UpdatedAt ??= string.Empty;
        entity.Icon = string.IsNullOrEmpty(entity.Icon) ? "Σ" : entity.Icon;
        entity.PackageBytes ??= Array.Empty<byte>();
    }

    /// <summary>生成确定性 UUID v5：官方与个人模板使用不同前缀，因此同一 PackageId 可以并存</summary>
    public static Guid GenerateId(string packageId, bool isOfficial)
    {
        if (string.IsNullOrEmpty(packageId))
            throw new ArgumentNullException(nameof(packageId));

        string input = (isOfficial ? "epma_official_" : "epma_custom_") + packageId.ToLowerInvariant();

        byte[] namespaceBytes = NamespaceGuid.ToByteArray();
        SwapByteOrder(namespaceBytes);

        byte[] nameBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashInput = new byte[namespaceBytes.Length + nameBytes.Length];
        Buffer.BlockCopy(namespaceBytes, 0, hashInput, 0, namespaceBytes.Length);
        Buffer.BlockCopy(nameBytes, 0, hashInput, namespaceBytes.Length, nameBytes.Length);

        byte[] hash = SHA1.HashData(hashInput);
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);

        byte[] newGuid = new byte[16];
        Buffer.BlockCopy(hash, 0, newGuid, 0, 16);
        SwapByteOrder(newGuid);
        return new Guid(newGuid);
    }

    /// <summary>计算清单文本的 MD5（与图解 / 温压计清单口径一致）</summary>
    public static string ComputeHash(string content)
    {
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void SwapByteOrder(byte[] guid)
    {
        (guid[0], guid[3]) = (guid[3], guid[0]);
        (guid[1], guid[2]) = (guid[2], guid[1]);
        (guid[4], guid[5]) = (guid[5], guid[4]);
        (guid[6], guid[7]) = (guid[7], guid[6]);
    }
}
