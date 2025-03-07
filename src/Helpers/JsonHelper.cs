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

        // 将新对象添加到现有 JSON 文件中的数组
        public static void AddToJsonFile(ListNodeConfig newNode, string filePath)
        {
            try
            {
                PlotListConfig config;

                // 如果文件不存在，创建一个新的 PlotListConfig 对象
                if (!File.Exists(filePath))
                {
                    config = new PlotListConfig
                    {
                        listNodeConfigs = new List<ListNodeConfig> { newNode }
                    };
                }
                else
                {
                    // 读取现有文件内容
                    string existingJson = File.ReadAllText(filePath);

                    // 反序列化为 PlotListConfig 对象
                    config = Deserialize<PlotListConfig>(existingJson) ?? new PlotListConfig();

                    // 如果 listNodeConfigs 为 null，初始化它
                    if (config.listNodeConfigs == null)
                    {
                        config.listNodeConfigs = new List<ListNodeConfig>();
                    }

                    // 添加新节点
                    config.listNodeConfigs.Add(newNode);
                }

                // 序列化并写回文件
                string updatedJson = JsonSerializer.Serialize(config);
                File.WriteAllText(filePath, updatedJson);

                Console.WriteLine("新节点已成功添加到 JSON 文件.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AddToJsonFile Error: {ex.Message}");
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

        // 删除json文件中的节点
        public static void RemoveNodeFromJson(string jsonFilePath, string[] targetRootNode)
        {
            try
            {
                // 读取 JSON 文件内容
                string jsonString = File.ReadAllText(jsonFilePath);

                // 反序列化为 PlotListConfig 对象
                PlotListConfig config = JsonHelper.Deserialize<PlotListConfig>(jsonString);

                if (config == null || config.listNodeConfigs == null)
                {
                    Console.WriteLine("No nodes to remove.");
                    return;
                }

                // 查找并移除目标节点
                config.listNodeConfigs.RemoveAll(node => AreNodesEqual(node.rootNode, targetRootNode));

                // 序列化更新后的对象并写回文件
                string updatedJson = JsonHelper.Serialize(config);
                File.WriteAllText(jsonFilePath, updatedJson);

                Console.WriteLine("Node removed successfully if it existed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RemoveNodeFromJson: {ex.Message}");
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
