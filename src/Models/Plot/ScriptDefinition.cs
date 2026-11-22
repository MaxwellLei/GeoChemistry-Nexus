using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Models
{
    public partial class ScriptDefinition : ObservableObject
    {
        /// <summary>
        /// 脚本语言类型
        /// </summary>
        /// 后续可能支持 C# 和 Lua
        [ObservableProperty]
        private ScriptLanguage _language = ScriptLanguage.JavaScript;

        /// <summary>
        /// 定义脚本需要哪些外部数据
        /// </summary>
        [ObservableProperty]
        private string _requiredDataSeries = "";

        /// <summary>
        /// 各数据列的单位（图解脚本格式 1.3 新增，可选）。
        /// 以逗号分隔，按位置与 <see cref="RequiredDataSeries"/> 一一对应；某一列没有单位时留空，全部没有单位时整体为空串。
        /// 数据表格的表头会在非空单位的列名下方显示该单位。
        /// </summary>
        [ObservableProperty]
        private string _dataSeriesUnits = "";

        /// <summary>
        /// 数据列分组（图解脚本格式 1.3 新增，可选）：每个分组是一个多语言标题加上它覆盖的数据列名。
        /// 没有分组时保持为 null，不写入模板内容，旧模板的内容与哈希不变。
        /// </summary>
        [ObservableProperty]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        private List<DataSeriesGroup>? _dataSeriesGroups;

        /// <summary>
        /// 要执行的脚本内容
        /// </summary>
        [ObservableProperty]
        private string _scriptBody = "";

        /// <summary>
        /// 是否为只读模式（未进入编辑状态时为 true）
        /// </summary>
        [ObservableProperty]
        [JsonIgnore] // 不序列化此属性
        private bool _isReadOnly = true;
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ScriptLanguage
    {
        JavaScript
    }
}
