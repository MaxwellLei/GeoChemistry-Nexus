using GeoChemistryNexus.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Converter
{
    /// <summary>
    /// JSON 转换器：兼容读取纯字符串（旧格式）或多语言对象，统一写出 { default, translations }。
    /// </summary>
    public class LocalizedStringJsonConverter : JsonConverter<LocalizedString>
    {
        public override LocalizedString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return new LocalizedString();

            if (reader.TokenType == JsonTokenType.String)
                return HomeLinksLocalization.FromPlain(reader.GetString() ?? string.Empty);

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"Unable to convert value to LocalizedString. Token type: {reader.TokenType}");

            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            var result = new LocalizedString();

            if (TryGetStringProperty(root, "default", out string defaultLang) ||
                TryGetStringProperty(root, "Default", out defaultLang))
            {
                result.Default = string.IsNullOrWhiteSpace(defaultLang)
                    ? AppCultureRegistry.DefaultContentLanguage
                    : defaultLang;
            }

            if (TryGetObjectProperty(root, "translations", out JsonElement translations) ||
                TryGetObjectProperty(root, "Translations", out translations))
            {
                result.Translations ??= new Dictionary<string, string>();
                foreach (var property in translations.EnumerateObject())
                    result.Translations[property.Name] = property.Value.GetString() ?? string.Empty;
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, LocalizedString value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("default", value?.Default ?? AppCultureRegistry.DefaultContentLanguage);
            writer.WritePropertyName("translations");
            writer.WriteStartObject();

            if (value?.Translations != null)
            {
                foreach (var pair in value.Translations.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
                    writer.WriteString(pair.Key, pair.Value ?? string.Empty);
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        private static bool TryGetStringProperty(JsonElement element, string name, out string value)
        {
            value = string.Empty;
            if (!element.TryGetProperty(name, out JsonElement property))
                return false;

            value = property.GetString() ?? string.Empty;
            return true;
        }

        private static bool TryGetObjectProperty(JsonElement element, string name, out JsonElement value)
        {
            if (element.TryGetProperty(name, out value) && value.ValueKind == JsonValueKind.Object)
                return true;

            value = default;
            return false;
        }
    }
}
