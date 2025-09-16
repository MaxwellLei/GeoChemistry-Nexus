using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Converter
{
    public class AxisDefinitionConverter : JsonConverter<BaseAxisDefinition>
    {
        public override BaseAxisDefinition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // 创建 reader 的一个副本，用于预读和判断类型，而不影响主 reader 的状态
            var readerClone = reader;

            if (readerClone.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            // 使用 JsonDocument 解析 JSON 对象
            using (var jsonDoc = JsonDocument.ParseValue(ref reader))
            {
                var root = jsonDoc.RootElement;

                // === 类型判断逻辑 ===
                if (root.TryGetProperty("ScaleType", out _))
                {
                    // 如果存在 "ScaleType" 属性，认定它是一个 CartesianAxisDefinition
                    // 然后使用对象的原始文本重新进行反序列化为具体类型
                    return JsonSerializer.Deserialize<CartesianAxisDefinition>(root.GetRawText(), options);
                }
                else
                {
                    // 否则，我们认为它是一个 TernaryAxisDefinition
                    return JsonSerializer.Deserialize<TernaryAxisDefinition>(root.GetRawText(), options);
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, BaseAxisDefinition value, JsonSerializerOptions options)
        {
            // 在写入时，根据对象的实际运行时类型进行序列化
            switch (value)
            {
                case CartesianAxisDefinition cartesian:
                    JsonSerializer.Serialize(writer, cartesian, options);
                    break;
                case TernaryAxisDefinition ternary:
                    JsonSerializer.Serialize(writer, ternary, options);
                    break;
                default:
                    throw new NotSupportedException($"Type {value.GetType()} is not supported.");
            }
        }
    }
}
