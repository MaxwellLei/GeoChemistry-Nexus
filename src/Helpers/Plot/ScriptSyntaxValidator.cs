using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 图解脚本的语法校验（数据参数格式 + JavaScript 语法），供脚本控件与图解模板编辑器共用。
    /// </summary>
    public static class ScriptSyntaxValidator
    {
        /// <summary>
        /// 校验数据参数（逗号分隔的变量名）与脚本语法。
        /// </summary>
        /// <returns>是否通过，以及要展示的提示文字</returns>
        public static (bool Success, string Message) ValidateJavaScript(string script, string requiredDataSeries)
        {
            if (string.IsNullOrEmpty(requiredDataSeries))
                return (false, LanguageService.Instance["data_parameters_are_empty"]);

            // 只允许字母、数字、下划线和英文逗号
            if (!Regex.IsMatch(requiredDataSeries, @"^([a-zA-Z0-9_]+(,[a-zA-Z0-9_]+)*)?$"))
                return (false, LanguageService.Instance["invalid_data_parameter_format"]);

            try
            {
                var engine = JintHelper.CreateEngine();
                JintHelper.InjectTraceFunction(engine, new List<string>());

                foreach (string variable in requiredDataSeries.Split(',').Select(v => v.Trim()))
                    engine.SetValue(variable, 1);

                // 语法错误会抛出异常
                engine.Execute(script);
                return (true, LanguageService.Instance["syntax_validation_passed"]);
            }
            catch (Exception ex)
            {
                return (false, LanguageService.Instance["error"] + ex.Message);
            }
        }
    }
}
