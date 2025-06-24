﻿using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.PropertyEditor;
using System.Collections.Generic;
using System.ComponentModel;

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
    }

    public enum ScriptLanguage
    {
        JavaScript
    }
}
