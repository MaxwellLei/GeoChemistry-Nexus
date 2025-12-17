using Jint;
using Jint.Native;
using Jint.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 一个静态辅助类，提供简化 Jint JavaScript 引擎操作的实用方法。
    /// </summary>
    public static class JintHelper
    {
        public static bool IsValidFunctionExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return false;
            var expr = expression.Trim();

            if (!HasBalancedParentheses(expr)) return false;

            var compact = Regex.Replace(expr, @"\s+", "");
            if (Regex.IsMatch(compact, @"[+\-*/^]$")) return false;
            if (!Regex.IsMatch(compact, @"^[0-9a-zA-ZxX+\-*/^().,]+$")) return false;
            if (Regex.IsMatch(compact, @"x\d|\dx")) return false;
            if (Regex.IsMatch(compact, @"x\(")) return false;
            if (Regex.IsMatch(compact, @"\)x")) return false;

            try
            {
                var engine = new Engine();
                engine.Execute("var sin=Math.sin; var cos=Math.cos; var tan=Math.tan; var abs=Math.abs; var sqrt=Math.sqrt; var pow=Math.pow; var log=Math.log; var log10=Math.log10; var exp=Math.exp; var PI=Math.PI;");
                // 将用户输入的公式包装成 JS 函数 f(x)
                engine.Execute($"function f(x) {{ return {expr}; }}");

                // 尝试调用一次函数，确保公式中没有未定义的变量（例如用户输入 "xa" 时，JS语法正确但运行时会报错）
                engine.Invoke("f", 0);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool HasBalancedParentheses(string s)
        {
            int count = 0;
            foreach (var ch in s)
            {
                if (ch == '(') count++;
                else if (ch == ')')
                {
                    if (count == 0) return false;
                    count--;
                }
            }
            return count == 0;
        }

        /// <summary>
        /// 加载并执行多个 JavaScript 文件到指定的 Jint 引擎中。
        /// </summary>
        /// <param name="engine">Jint 引擎实例。</param>
        /// <param name="scriptPaths">要加载的 JavaScript 文件路径数组。</param>
        /// <exception cref="Exception">如果某个脚本加载或执行失败，则抛出异常。</exception>
        /// <remarks>
        /// 该方法按顺序读取并执行每个文件中的 JavaScript 代码，适用于初始化 JavaScript 环境。
        /// </remarks>
        public static void LoadScripts(Engine engine, params string[] scriptPaths)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (scriptPaths == null) throw new ArgumentNullException(nameof(scriptPaths));

            foreach (var path in scriptPaths)
            {
                try
                {
                    var script = File.ReadAllText(path);
                    engine.Execute(script);
                }
                catch (Exception ex)
                {
                    throw new Exception($"加载脚本 '{path}' 时出错: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 执行 JavaScript 脚本并获取指定变量的值，转换为类型 T。
        /// </summary>
        /// <typeparam name="T">要将变量值转换成的类型，通常应为基本类型（如 int、double、string 等）。</typeparam>
        /// <param name="engine">Jint 引擎实例。</param>
        /// <param name="script">要执行的 JavaScript 脚本。</param>
        /// <param name="variableName">要检索的变量名称。</param>
        /// <returns>指定变量的值，转换为类型 T。</returns>
        /// <exception cref="ArgumentNullException">如果 engine 或 script 为 null，则抛出。</exception>
        /// <exception cref="InvalidCastException">如果变量值无法转换为类型 T，则抛出。</exception>
        /// <remarks>
        /// 该方法适用于执行脚本后需要从 JavaScript 上下文中提取特定变量的情况。
        /// </remarks>
        public static T ExecuteAndGetValue<T>(Engine engine, string script, string variableName)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (script == null) throw new ArgumentNullException(nameof(script));
            if (string.IsNullOrEmpty(variableName)) throw new ArgumentException("变量名称不能为空", nameof(variableName));

            engine.Execute(script);
            var jsValue = engine.GetValue(variableName);
            var obj = jsValue.ToObject();
            try
            {
                return (T)Convert.ChangeType(obj, typeof(T));
            }
            catch (InvalidCastException ex)
            {
                throw new InvalidCastException($"无法将 '{obj?.GetType().Name ?? "null"}' 转换为 '{typeof(T).Name}'", ex);
            }
        }

        /// <summary>
        /// 评估 JavaScript 表达式并返回结果，转换为类型 T。
        /// </summary>
        /// <typeparam name="T">要将表达式结果转换成的类型，通常应为基本类型。</typeparam>
        /// <param name="engine">Jint 引擎实例。</param>
        /// <param name="expression">要评估的 JavaScript 表达式。</param>
        /// <returns>表达式结果，转换为类型 T。</returns>
        /// <exception cref="ArgumentNullException">如果 engine 或 expression 为 null，则抛出。</exception>
        /// <exception cref="InvalidCastException">如果结果无法转换为类型 T，则抛出。</exception>
        /// <remarks>
        /// 该方法适合用于直接计算表达式，例如 "1 + 1" 或 "Math.sqrt(16)"。
        /// </remarks>
        public static T Evaluate<T>(Engine engine, string expression)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var jsValue = engine.Evaluate(expression);
            var obj = jsValue.ToObject();
            try
            {
                return (T)Convert.ChangeType(obj, typeof(T));
            }
            catch (InvalidCastException ex)
            {
                throw new InvalidCastException($"无法将 '{obj?.GetType().Name ?? "null"}' 转换为 '{typeof(T).Name}'", ex);
            }
        }

        /// <summary>
        /// 调用 JavaScript 函数并返回结果，转换为类型 T。
        /// </summary>
        /// <typeparam name="T">要将函数返回值转换成的类型，通常应为基本类型。</typeparam>
        /// <param name="engine">Jint 引擎实例。</param>
        /// <param name="functionName">要调用的 JavaScript 函数名称。</param>
        /// <param name="args">传递给函数的参数。</param>
        /// <returns>函数返回值，转换为类型 T。</returns>
        /// <exception cref="ArgumentNullException">如果 engine 或 functionName 为 null，则抛出。</exception>
        /// <exception cref="InvalidCastException">如果返回值无法转换为类型 T，则抛出。</exception>
        /// <remarks>
        /// 该方法在脚本中定义了函数后，可以从 C# 调用它，例如调用 "add(2, 3)"。
        /// </remarks>
        public static T Invoke<T>(Engine engine, string functionName, params object[] args)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (string.IsNullOrEmpty(functionName)) throw new ArgumentException("函数名称不能为空", nameof(functionName));

            var jsValue = engine.Invoke(functionName, args);
            var obj = jsValue.ToObject();
            try
            {
                return (T)Convert.ChangeType(obj, typeof(T));
            }
            catch (InvalidCastException ex)
            {
                throw new InvalidCastException($"无法将 '{obj?.GetType().Name ?? "null"}' 转换为 '{typeof(T).Name}'", ex);
            }
        }

        /// <summary>
        /// 将多个值设置到 Jint 引擎的全局作用域中。
        /// </summary>
        /// <param name="engine">Jint 引擎实例。</param>
        /// <param name="values">包含名称和对象的字典，用于设置到全局作用域。</param>
        /// <exception cref="ArgumentNullException">如果 engine 或 values 为 null，则抛出。</exception>
        /// <remarks>
        /// 该方法便于一次性将多个 C# 对象暴露给 JavaScript，例如设置 console 或自定义对象。
        /// </remarks>
        public static void SetValues(Engine engine, IDictionary<string, object> values)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (values == null) throw new ArgumentNullException(nameof(values));

            foreach (var kvp in values)
            {
                engine.SetValue(kvp.Key, kvp.Value);
            }
        }
    }
}
