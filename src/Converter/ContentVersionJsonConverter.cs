using GeoChemistryNexus.Helpers;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Converter
{
    /// <summary>
    /// JSON 转换器：兼容读取 Number（旧 float）或 String，统一写出 "x.y.z" 字符串。
    /// </summary>
    public class ContentVersionJsonConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return ContentVersionHelper.Normalize(reader.GetSingle().ToString());
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                return ContentVersionHelper.Normalize(reader.GetString());
            }

            throw new JsonException($"Unable to convert value to content version. Token type: {reader.TokenType}");
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(ContentVersionHelper.Normalize(value));
        }
    }
}
