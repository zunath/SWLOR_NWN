using System.Text.Json.Nodes;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Installs a fake engine with a working JSON model for the length of a test, so NUI element builders produce real JSON
/// that the test reads back with <see cref="Parse"/>.
/// </summary>
public sealed class NuiJsonScope : IDisposable
{
    private readonly FakeJsonEngine _json;
    private readonly IDisposable _scope;

    public NuiJsonScope()
    {
        Engine = new FakeNWScript();
        _json = new FakeJsonEngine(Engine).WithValueConstructors();
        _scope = NWScriptRuntime.Override(Engine);
    }

    public FakeNWScript Engine { get; }

    /// <summary>Reads back the JSON a builder produced.</summary>
    public JsonNode Parse(Json json) => JsonNode.Parse(_json.Dump(json))!;

    /// <summary>Hands the engine a JSON document, as a client or a saved value would.</summary>
    public Json From(string text) => _json.Wrap(JsonNode.Parse(text));

    public void Dispose() => _scope.Dispose();
}
