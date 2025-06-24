using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Helpers
{
    public static class JsonHelper
    {
        // 从 JSON 文件读取和反序列化内容
        public static String ReadJsonFile(string filePath)
        {
            try
            {
                // 检查文件是否存在
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"文件未找到: {filePath}");
                    return null;
                }

                // 读取 JSON 文件内容
                string jsonString = File.ReadAllText(filePath);

                // 如果文件内容为空，则返回一个默认的空对象
                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    Console.WriteLine("JSON 文件为空.");
                    return null;
                }

                return jsonString;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"从 JSON 文件读取时发生错误: {ex.Message}");
                return null;
            }
        }

        // 将对象序列化为 JSON 字符串
        public static string Serialize<T>(T obj)
        {
            try
            {
                return JsonSerializer.Serialize(obj);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Serialization Error: {ex.Message}");
                return string.Empty;
            }
        }

        // 将对象序列化为 JSON 字符串，并存储到文件中
        public static void SerializeToJsonFile<T>(T obj, string filePath)
        {
            try
            {
                // 将对象序列化为 JSON 字符串
                string jsonString = JsonSerializer.Serialize(obj);

                // 将 JSON 字符串写入文件
                File.WriteAllText(filePath, jsonString);

                Console.WriteLine("JSON 文件已成功保存.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Serialization Error: {ex.Message}");
            }
        }

        // 从 JSON 字符串反序列化为对象
        public static T Deserialize<T>(string jsonString)
        {
            try
            {
                var temp = JsonSerializer.Deserialize<T>(jsonString);
                return temp;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Deserialization Error: {ex.Message}");
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
                    return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Format Error: {ex.Message}");
                return jsonString; // 返回原始字符串
            }
        }



        // 压缩字符串
        public static string CompressString(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            var memoryStream = new MemoryStream();
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gZipStream.Write(buffer, 0, buffer.Length);
            }

            memoryStream.Position = 0;
            var compressedData = new byte[memoryStream.Length];
            memoryStream.Read(compressedData, 0, compressedData.Length);

            var gZipBuffer = new byte[compressedData.Length + 4];
            Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
            return Convert.ToBase64String(gZipBuffer);
        }

        // 解压缩字符串
        public static string DecompressString(string compressedText)
        {
            byte[] gZipBuffer = Convert.FromBase64String(compressedText);
            using (var memoryStream = new MemoryStream())
            {
                int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
                memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

                var buffer = new byte[dataLength];

                memoryStream.Position = 0;
                using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    gZipStream.Read(buffer, 0, buffer.Length);
                }

                return Encoding.UTF8.GetString(buffer);
            }
        }


        // 比较两个字符串数组是否相等
        private static bool AreNodesEqual(string[] node1, string[] node2)
        {
            if (node1 == null || node2 == null)
            {
                return false;
            }

            if (node1.Length != node2.Length)
            {
                return false;
            }

            for (int i = 0; i < node1.Length; i++)
            {
                if (node1[i] != node2[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
