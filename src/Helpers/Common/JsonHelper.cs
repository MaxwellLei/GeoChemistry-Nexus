using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Helpers
{
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions _options = CreateDefaultOptions();

        /// <summary>
        /// 默认选项：缩进、大小写不敏感、枚举按字符串读写。
        /// </summary>
        public static JsonSerializerOptions DefaultOptions => _options;

        /// <summary>
        /// 发布/导出常用：缩进 + 不转义非 ASCII，可附加额外转换器。
        /// </summary>
        public static JsonSerializerOptions CreateRelaxedIndentedOptions(params JsonConverter[] extraConverters)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters = { new JsonStringEnumConverter() }
            };

            if (extraConverters != null)
            {
                foreach (var converter in extraConverters)
                {
                    if (converter != null)
                        options.Converters.Add(converter);
                }
            }

            return options;
        }

        private static JsonSerializerOptions CreateDefaultOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        // 从 JSON 文件读取和反序列化内容
        public static string? ReadJsonFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                string content = File.ReadAllText(filePath);
                return string.IsNullOrWhiteSpace(content) ? null : content;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 从文件反序列化；文件不存在或失败时返回 new T()。
        /// </summary>
        public static T LoadFromFileOrNew<T>(string filePath) where T : class, new()
        {
            string? json = ReadJsonFile(filePath);
            if (json == null)
                return new T();

            return Deserialize<T>(json) ?? new T();
        }

        // 将对象序列化为 JSON 字符串
        public static string Serialize<T>(T obj, JsonSerializerOptions? options = null)
        {
            try
            {
                return JsonSerializer.Serialize(obj, options ?? _options);
            }
            catch
            {
                return string.Empty;
            }
        }

        // 将对象序列化为 JSON 字符串，并存储到文件中
        public static void SerializeToJsonFile<T>(T obj, string filePath, JsonSerializerOptions? options = null)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(obj, options ?? _options);
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(filePath, jsonString);
            }
            catch
            {

            }
        }

        // 从 JSON 字符串反序列化为对象
        public static T? Deserialize<T>(string jsonString, JsonSerializerOptions? options = null)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(jsonString, options ?? _options);
            }
            catch
            {
                return default;
            }
        }

        // 将 JSON 字符串格式化为可读的格式
        public static string FormatJson(string jsonString)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonString))
                {
                    return JsonSerializer.Serialize(doc.RootElement, _options);
                }
            }
            catch
            {
                return jsonString; // 返回原始字符串
            }
        }
    }
}
