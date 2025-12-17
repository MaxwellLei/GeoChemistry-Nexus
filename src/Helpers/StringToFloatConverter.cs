using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// JSON转换器：兼容读取 String 或 Number 类型的 float 值
    /// </summary>
    public class StringToFloatConverter : JsonConverter<float>
    {
        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetSingle();
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (float.TryParse(stringValue, out float value))
                {
                    return value;
                }
            }
            
            // 如果无法解析，抛出异常
            throw new JsonException($"Unable to convert value to float. Token type: {reader.TokenType}");
        }

        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}
