using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.Services
{
    /// <summary>
    /// 把图解发布预览整理成文件夹树，供发布器只读展示。
    /// 文件夹结构取自各图解 NodeList（与图解页一致），状态取自对比线上清单的结果。
    /// </summary>
    public static class PublishPathTreeBuilder
    {
        private sealed class FolderBucket
        {
            public Dictionary<string, FolderBucket> Folders { get; } = new(StringComparer.Ordinal);

            public List<PublishPathNode> Leaves { get; } = new();
        }

        private readonly record struct Counts(int New, int Updated, int PathChanged, int Removed)
        {
            public int Total => New + Updated + PathChanged + Removed;

            public static Counts operator +(Counts a, Counts b)
                => new(a.New + b.New, a.Updated + b.Updated, a.PathChanged + b.PathChanged, a.Removed + b.Removed);
        }

        public static List<PublishPathNode> Build(PublishPreview? preview)
        {
            var result = new List<PublishPathNode>();
            if (preview == null)
                return result;

            var root = new FolderBucket();
            foreach (var item in preview.NewItems)
                AddLeaf(root, item, PublishPathNode.ActionNew, "新增");
            foreach (var item in preview.UpdatedItems)
            {
                if (item.PathChanged)
                    AddLeaf(root, item, PublishPathNode.ActionPathChanged, "路径变更");
                else
                    AddLeaf(root, item, PublishPathNode.ActionUpdate, "更新");
            }
            foreach (var item in preview.RemovedItems)
                AddLeaf(root, item, PublishPathNode.ActionRemove, "已下架");
            foreach (var item in preview.UnchangedItems)
                AddLeaf(root, item, PublishPathNode.ActionSame, "未变化");

            ConvertChildren(root, result);
            return result;
        }

        private static void AddLeaf(FolderBucket root, PublishPreviewItem item, string action, string label)
        {
            var nodeList = item.Action == PublishAction.Remove ? item.RemoteNodeList : item.LocalNodeList;
            var segments = TemplateFolderService.GetDisplaySegments(nodeList);

            string name = segments.Length > 0
                ? segments[^1]
                : (string.IsNullOrWhiteSpace(item.Name) ? item.GraphMapPath : item.Name);

            var bucket = root;
            for (int i = 0; i < segments.Length - 1; i++)
            {
                if (!bucket.Folders.TryGetValue(segments[i], out var child))
                {
                    child = new FolderBucket();
                    bucket.Folders[segments[i]] = child;
                }
                bucket = child;
            }

            bucket.Leaves.Add(new PublishPathNode
            {
                Name = name,
                Action = action,
                ActionLabel = label,
                Detail = action == PublishPathNode.ActionPathChanged ? BuildPathChangeDetail(item, segments) : string.Empty
            });
        }

        private static string BuildPathChangeDetail(PublishPreviewItem item, string[] currentSegments)
        {
            var oldSegments = TemplateFolderService.GetDisplaySegments(item.RemoteNodeList);
            if (oldSegments.SequenceEqual(currentSegments, StringComparer.Ordinal))
                return "其他语言的路径有改动";

            return "原路径：" + string.Join(" / ", oldSegments);
        }

        private static Counts ConvertChildren(FolderBucket bucket, List<PublishPathNode> output)
        {
            var total = default(Counts);

            foreach (var (name, child) in bucket.Folders.OrderBy(f => f.Key, StringComparer.OrdinalIgnoreCase))
            {
                var node = new PublishPathNode { Name = name, IsFolder = true };
                var counts = ConvertChildren(child, node.Children);

                node.ChangedCount = counts.Total;
                node.Summary = BuildSummary(counts);
                node.IsExpanded = counts.Total > 0;
                output.Add(node);
                total += counts;
            }

            foreach (var leaf in bucket.Leaves.OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase))
            {
                leaf.ChangedCount = leaf.Action == PublishPathNode.ActionSame ? 0 : 1;
                output.Add(leaf);
                total += leaf.Action switch
                {
                    PublishPathNode.ActionNew => new Counts(1, 0, 0, 0),
                    PublishPathNode.ActionUpdate => new Counts(0, 1, 0, 0),
                    PublishPathNode.ActionPathChanged => new Counts(0, 0, 1, 0),
                    PublishPathNode.ActionRemove => new Counts(0, 0, 0, 1),
                    _ => default
                };
            }

            return total;
        }

        private static string BuildSummary(Counts counts)
        {
            var parts = new List<string>();
            if (counts.New > 0) parts.Add($"新增 {counts.New}");
            if (counts.Updated > 0) parts.Add($"更新 {counts.Updated}");
            if (counts.PathChanged > 0) parts.Add($"路径变更 {counts.PathChanged}");
            if (counts.Removed > 0) parts.Add($"已下架 {counts.Removed}");
            return string.Join(" · ", parts);
        }
    }
}
