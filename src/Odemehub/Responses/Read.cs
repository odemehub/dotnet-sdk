using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Odemehub.Responses;

/// <summary>
/// How answers are read. A field the gateway left out reads as an empty
/// string, or as null where the answer may genuinely not carry it.
/// </summary>
internal static class Read
{
    /// <summary>
    /// A field of an object, or nothing when the object does not carry it or
    /// is not an object at all.
    /// </summary>
    internal static JsonElement Field(this JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value : default;
    }

    internal static bool IsSaid(JsonElement value)
    {
        return value.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null);
    }

    internal static string String(JsonElement value)
    {
        return OptionalString(value) ?? "";
    }

    internal static string? OptionalString(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Undefined or JsonValueKind.Null => null,
            JsonValueKind.String => value.GetString(),
            JsonValueKind.True => "1",
            JsonValueKind.False => "",
            _ => value.GetRawText(),
        };
    }

    /// <summary>
    /// A field the gateway left empty reads as nothing rather than as an empty
    /// string, so there is one way of asking whether it was said.
    /// </summary>
    internal static string? NonEmptyString(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } text ? text : null;
    }

    internal static bool Bool(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.Number => value.GetDouble() != 0,
            JsonValueKind.String => value.GetString() is { Length: > 0 } text && text != "0",
            JsonValueKind.Object => value.EnumerateObject().Any(),
            JsonValueKind.Array => value.GetArrayLength() > 0,
            _ => false,
        };
    }

    internal static bool? OptionalBool(JsonElement value)
    {
        return IsSaid(value) ? Bool(value) : null;
    }

    internal static int Int(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number)
        {
            return value.TryGetInt32(out var number) ? number : (int)value.GetDouble();
        }

        return value.ValueKind == JsonValueKind.String
            && double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? (int)parsed
            : 0;
    }

    internal static IReadOnlyList<T> List<T>(JsonElement value, Func<JsonElement, T> read)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Array => value.EnumerateArray().Select(read).ToArray(),
            JsonValueKind.Object => value.EnumerateObject().Select(property => read(property.Value)).ToArray(),
            _ => Array.Empty<T>(),
        };
    }
}
