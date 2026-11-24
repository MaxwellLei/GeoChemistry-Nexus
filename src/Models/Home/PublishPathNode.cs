using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 发布器「路径结构预览」里的一个节点：文件夹，或带发布状态的图解。
    /// </summary>
    public sealed partial class PublishPathNode : ObservableObject
    {
        public const string ActionNew = "new";
        public const string ActionUpdate = "update";
        public const string ActionPathChanged = "path";
        public const string ActionRemove = "remove";
        public const string ActionSame = "same";

        public string Name { get; init; } = string.Empty;

        public bool IsFolder { get; init; }

        /// <summary>图解的发布状态（ActionXxx 常量）；文件夹为空。</summary>
        public string Action { get; init; } = string.Empty;

        /// <summary>状态标签；文件夹为空。</summary>
        public string ActionLabel { get; init; } = string.Empty;

        /// <summary>补充说明，例如路径变更时的原路径。</summary>
        public string Detail { get; init; } = string.Empty;

        /// <summary>文件夹下各类变更的汇总文字；没有变更时为空。</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>自身及子孙中有发布改动的图解数量（未变化不计）。</summary>
        public int ChangedCount { get; set; }

        public List<PublishPathNode> Children { get; } = new();

        public bool HasStatus => !IsFolder && !string.IsNullOrEmpty(ActionLabel);

        public bool HasDetail => !string.IsNullOrEmpty(Detail);

        public bool HasSummary => !string.IsNullOrEmpty(Summary);

        [ObservableProperty]
        private bool isExpanded;
    }
}
