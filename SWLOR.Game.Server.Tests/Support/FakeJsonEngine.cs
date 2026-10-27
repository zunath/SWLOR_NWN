using System.Text.Json.Nodes;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Gives a <see cref="FakeNWScript"/> a working model of the engine's JSON values and templates, for tests of code that
/// reads blueprints and palettes (<c>TemplateToJson</c>, <c>JsonObjectGet</c>, ...). A <c>Json</c> value is a handle to a
/// <see cref="JsonNode"/>; handle 0 is JSON null, as in the engine. A template is registered by its resref and resource
/// type with <see cref="AddTemplate"/> and read back as the same value each time.
/// </summary>
public sealed class FakeJsonEngine
{
    private readonly Dictionary<nint, JsonNode?> _values = new();
    private readonly Dictionary<(string Resref, ResType Type), nint> _templates = new();
    private nint _nextHandle = 1;

    public FakeJsonEngine(FakeNWScript engine)
    {
        engine.SetReturnHandler("TemplateToJson", arguments =>
            _templates.TryGetValue((((string)arguments[0]!).ToLowerInvariant(), (ResType)arguments[1]!), out var handle)
                ? Create(handle)
                : Null());
        engine.SetReturnHandler("JsonParse", arguments => Wrap(JsonNode.Parse((string)arguments[0]!)));
        engine.SetReturnHandler("JsonDump", arguments => Node((Json)arguments[0]!)?.ToJsonString() ?? "null");
        engine.SetReturnHandler("JsonGetType", arguments => TypeOf(Node((Json)arguments[0]!)));
        engine.SetReturnHandler("JsonGetLength", arguments => Node((Json)arguments[0]!) switch
        {
            JsonObject obj => obj.Count,
            JsonArray array => array.Count,
            JsonValue value when value.TryGetValue<string>(out var text) => text.Length,
            _ => 0
        });
        engine.SetReturnHandler("JsonObject", _ => Wrap(new JsonObject()));
        engine.SetReturnHandler("JsonArray", _ => Wrap(new JsonArray()));
        engine.SetReturnHandler("JsonGetString", arguments =>
            Node((Json)arguments[0]!) is JsonValue value && value.TryGetValue<string>(out var text) ? text : string.Empty);
        engine.SetReturnHandler("JsonGetInt", arguments =>
            Node((Json)arguments[0]!) is JsonValue value && value.TryGetValue<int>(out var number) ? number : 0);
        engine.SetReturnHandler("JsonObjectKeys", arguments =>
        {
            var keys = new JsonArray();
            if (Node((Json)arguments[0]!) is JsonObject obj)
            {
                foreach (var pair in obj)
                    keys.Add(JsonValue.Create(pair.Key));
            }

            return Wrap(keys);
        });
        engine.SetReturnHandler("JsonObjectGet", arguments =>
            Node((Json)arguments[0]!) is JsonObject obj && obj.TryGetPropertyValue((string)arguments[1]!, out var child)
                ? Wrap(child?.DeepClone())
                : Null());
        engine.SetReturnHandler("JsonObjectSet", arguments =>
        {
            var copy = Node((Json)arguments[0]!)?.DeepClone() as JsonObject ?? new JsonObject();
            copy[(string)arguments[1]!] = Node((Json)arguments[2]!)?.DeepClone();
            return Wrap(copy);
        });
        engine.SetReturnHandler("JsonArrayGet", arguments =>
            Node((Json)arguments[0]!) is JsonArray array && (int)arguments[1]! >= 0 && (int)arguments[1]! < array.Count
                ? Wrap(array[(int)arguments[1]!]?.DeepClone())
                : Null());
    }

    /// <summary>Registers the template the engine returns for a resref and resource type.</summary>
    /// <param name="resref">The blueprint's resref.</param>
    /// <param name="type">The resource type, such as <see cref="ResType.UTC"/>.</param>
    /// <param name="json">The template in the engine's JSON form.</param>
    public void AddTemplate(string resref, ResType type, string json)
    {
        var handle = _nextHandle++;
        _values[handle] = JsonNode.Parse(json);
        _templates[(resref.ToLowerInvariant(), type)] = handle;
    }

    /// <summary>Wraps a value, as the engine hands it to game code.</summary>
    public Json Wrap(JsonNode? node)
    {
        if (node == null)
            return Null();

        var handle = _nextHandle++;
        _values[handle] = node;
        return Create(handle);
    }

    private static Json Null() => EngineStructures.Create<Json>(0);

    private static Json Create(nint handle) => EngineStructures.Create<Json>(handle);

    private JsonNode? Node(Json json) => json.Handle != 0 && _values.TryGetValue(json.Handle, out var node) ? node : null;

    private static JsonType TypeOf(JsonNode? node)
    {
        switch (node)
        {
            case null:
                return JsonType.Null;
            case JsonObject:
                return JsonType.Object;
            case JsonArray:
                return JsonType.Array;
        }

        var element = node.GetValueKind();
        return element switch
        {
            System.Text.Json.JsonValueKind.String => JsonType.String,
            System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False => JsonType.Bool,
            System.Text.Json.JsonValueKind.Number => node.ToJsonString().Contains('.') ? JsonType.Float : JsonType.Integer,
            _ => JsonType.Null
        };
    }
}
