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
