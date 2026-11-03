using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paging.Common.Enums.Converters;

/// <summary>
/// Reusable JsonConverter that maps enum values using [EnumMember(Value = "...")] attributes 
/// (supporting comma-separated aliases) and numeric values for any enum.
/// </summary>
public class EnumMemberJsonConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum {
    private static readonly Dictionary<string, TEnum> ToEnum = new(StringComparer.OrdinalIgnoreCase);
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
    private static readonly Dictionary<TEnum, string> ToString = [];
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword

    static EnumMemberJsonConverter() {
        var type = typeof(TEnum);
        foreach (var value in Enum.GetValues<TEnum>()) {
            var memInfo = type.GetMember(value.ToString());
            var attributes = memInfo[0].GetCustomAttributes<EnumMemberAttribute>().ToList();

            var primaryName = attributes.FirstOrDefault()?.Value?.Split(',')[0].Trim() ?? value.ToString();
            ToEnum[primaryName] = value;
            ToString[value] = primaryName;

            // Register all explicit [EnumMember(Value = "...")] values (supports comma-separated aliases like "ascending,asc")
            foreach (var attr in attributes) {
                if (string.IsNullOrEmpty(attr.Value)) 
                    continue;

                var aliases = attr.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var alias in aliases)
                    ToEnum[alias] = value;
            }

            // Register standard enum names and lower cased variants as fallbacks
            ToEnum[value.ToString()] = value;
            ToEnum[value.ToString().ToLowerInvariant()] = value;
        }
    }

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch {
            // Handle numeric tokens (e.g., 0, 1) if they map to a defined enum value
            JsonTokenType.Number when reader.TryGetInt32(out var intVal) && Enum.IsDefined(typeof(TEnum), Enum.ToObject(typeof(TEnum), intVal))
                => (TEnum)Enum.ToObject(typeof(TEnum), intVal),

            // Handle string tokens (checking custom dictionary or fallback parsing in a single check)
            JsonTokenType.String when reader.GetString() is { } str && (ToEnum.TryGetValue(str, out var enumVal) || Enum.TryParse<TEnum>(str, true, out enumVal))
                => enumVal,

            // Fallback error
            _ => throw new JsonException($"Unable to convert token '{reader.TokenType}' to {typeof(TEnum).Name}.")
        };

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) {
        if (ToString.TryGetValue(value, out var stringValue))
            writer.WriteStringValue(stringValue);
        else
            writer.WriteStringValue(value.ToString());
    }
}