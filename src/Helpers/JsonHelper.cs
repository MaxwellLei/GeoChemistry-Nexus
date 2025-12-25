using GeoChemistryNexus.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Helpers
{
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        // 从 JSON 文件读取和反序列化内容
        public static string ReadJsonFile(string filePath)
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

        // 将对象序列化为 JSON 字符串
        public static string Serialize<T>(T obj)
        {
            try
            {
                return JsonSerializer.Serialize(obj, _options);
            }
            catch
            {
                return string.Empty;
            }
        }

        // 将对象序列化为 JSON 字符串，并存储到文件中
        public static void SerializeToJsonFile<T>(T obj, string filePath)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(obj, _options);
                string directory = Path.GetDirectoryName(filePath);
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
        public static T Deserialize<T>(string jsonString)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(jsonString, _options);
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
