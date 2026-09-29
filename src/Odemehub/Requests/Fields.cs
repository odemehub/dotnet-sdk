using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Odemehub.Requests;

/// <summary>
/// How request bodies are written: in the snake_case the gateway speaks, in
/// the order the fields are named.
/// </summary>
internal static class Fields
{
    /// <summary>
    /// Letters outside ASCII are written as they are rather than escaped, the
    /// way the other clients send them.
    /// </summary>
    internal static readonly JsonSerializerOptions Json = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// A body holding every field named.
    /// </summary>
    internal static JsonObject Of(params (string Key, JsonNode? Value)[] pairs)
    {
        var body = new JsonObject();

        foreach (var (key, value) in pairs)
        {
            body[key] = value;
        }

        return body;
    }

    /// <summary>
    /// A body holding only what the caller said, so an optional field is left
    /// out altogether rather than sent empty.
    /// </summary>
    internal static JsonObject Said(params (string Key, JsonNode? Value)[] pairs)
    {
        var body = new JsonObject();

        foreach (var (key, value) in pairs)
        {
            if (value is not null)
            {
                body[key] = value;
            }
        }

        return body;
    }
}
