using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Gives a <see cref="FakeNWScript"/> the local variables of a real engine: what the code under test sets with
/// <c>SetLocalInt</c>, <c>SetLocalBool</c>, <c>SetLocalString</c>, <c>SetLocalFloat</c> or <c>SetLocalObject</c> is what
/// the matching <c>GetLocal...</c> answers until a <c>DeleteLocal...</c> removes it. Integers and booleans are one
/// variable, as in the engine. The fake only records calls, so the variables are rebuilt from the recorded writes, in
/// order: create this before the code runs, and clear the fake's recorded calls through <see cref="ClearCalls"/> so
/// the variables survive it.
/// </summary>
public sealed class LocalVariables
{
    private const uint ObjectInvalid = 0x7F000000;

    private readonly FakeNWScript _engine;
    private readonly Dictionary<(string Kind, uint Target, string Name), object> _values = new();
    private int _processed;

    public LocalVariables(FakeNWScript engine)
    {
        _engine = engine;
        engine.SetReturnHandler("GetLocalInt", arguments => Int((uint)arguments[0]!, (string)arguments[1]!));
        engine.SetReturnHandler("GetLocalBool", arguments => Int((uint)arguments[0]!, (string)arguments[1]!) != 0);
        engine.SetReturnHandler("GetLocalString", arguments => String((uint)arguments[0]!, (string)arguments[1]!));
        engine.SetReturnHandler("GetLocalFloat", arguments => Read("Float", (uint)arguments[0]!, (string)arguments[1]!) ?? 0f);
        engine.SetReturnHandler("GetLocalObject", arguments => Object((uint)arguments[0]!, (string)arguments[1]!));
    }

    /// <summary>The integer (or boolean) variable <paramref name="name"/> of an object right now, 0 when it is not set.</summary>
    public int Int(uint target, string name) => Read("Int", target, name) switch
    {
        null => 0,
        bool flag => flag ? 1 : 0,
        var value => Convert.ToInt32(value)
    };

    /// <summary>The object variable <paramref name="name"/> of an object right now, OBJECT_INVALID when it is not set.</summary>
    public uint Object(uint target, string name) => (uint)(Read("Object", target, name) ?? ObjectInvalid);

    /// <summary>The string variable <paramref name="name"/> of an object right now, empty when it is not set.</summary>
    public string String(uint target, string name) => (string?)Read("String", target, name) ?? string.Empty;

    /// <summary>True when the object has an integer, boolean, string, float or object variable called <paramref name="name"/>.</summary>
    public bool Has(uint target, string name) =>
        new[] { "Int", "String", "Float", "Object" }.Any(kind => Read(kind, target, name) != null);

    /// <summary>Forgets the fake's recorded calls but keeps the variables they set.</summary>
    public void ClearCalls()
    {
        Process();
        _engine.ClearCalls();
        _processed = 0;
    }

    private object? Read(string kind, uint target, string name)
    {
        Process();
        return _values.TryGetValue((kind, target, name), out var value) ? value : null;
    }

    private void Process()
    {
        var calls = _engine.Calls;
        for (; _processed < calls.Count; _processed++)
        {
            var call = calls[_processed];
            if (call.Arguments.Count < 2 || call.Arguments[0] is not uint target || call.Arguments[1] is not string name)
                continue;

            switch (call.Member)
            {
                case "SetLocalInt":
                case "SetLocalBool":
                    _values[("Int", target, name)] = call.Arguments[2]!;
                    break;
                case "SetLocalString":
                case "SetLocalFloat":
                case "SetLocalObject":
                    _values[(call.Member["SetLocal".Length..], target, name)] = call.Arguments[2]!;
                    break;
                case "DeleteLocalInt":
                case "DeleteLocalBool":
                    _values.Remove(("Int", target, name));
                    break;
                case "DeleteLocalString":
                case "DeleteLocalFloat":
                case "DeleteLocalObject":
                    _values.Remove((call.Member["DeleteLocal".Length..], target, name));
                    break;
            }
        }
    }
}
