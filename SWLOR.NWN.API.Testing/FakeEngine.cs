using System.Globalization;
using SWLOR.NWN.API.Engine;

namespace SWLOR.NWN.API.Testing;

/// <summary>
/// The shared behaviour of the generated engine fakes: record every call, and answer with a configured value or a
/// default. Tests install a fake with `NWScriptRuntime.Override(fake)`, run game code, then assert on <see cref="Calls"/>.
/// </summary>
/// <remarks>
/// Defaults are chosen so typical engine loops end: numbers and booleans are zero/false, strings are empty, object ids
/// (every `uint` return) are OBJECT_INVALID, and engine structures are empty structures (handle 0) whose finalizer is
/// suppressed. Anything else is `default`. A call never throws unless a configured value has the wrong type.
/// </remarks>
public abstract class FakeEngine
{
    private const uint ObjectInvalid = 0x7F000000;

    private readonly object _gate = new();
    private readonly List<RecordedCall> _calls = new();
    private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Queue<object?>> _sequences = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Func<IReadOnlyList<object?>, object?>> _handlers = new(StringComparer.Ordinal);

    /// <summary>Every call received so far, in order.</summary>
    public IReadOnlyList<RecordedCall> Calls
    {
        get
        {
            lock (_gate)
            {
                return _calls.ToArray();
            }
        }
    }

    /// <summary>The calls received on one member, in order.</summary>
    public IReadOnlyList<RecordedCall> CallsTo(string member) => Calls.Where(c => c.Member == member).ToArray();

    /// <summary>How many times one member was called.</summary>
    public int CountOf(string member) => Calls.Count(c => c.Member == member);

    /// <summary>Forgets the recorded calls. Configured return values stay.</summary>
    public void ClearCalls()
    {
        lock (_gate)
        {
            _calls.Clear();
        }
    }

    /// <summary>Makes a member always return <paramref name="value"/>. Numbers are converted to the member's return type.</summary>
    public void SetReturn(string member, object? value)
    {
        lock (_gate)
        {
            _values[member] = value;
        }
    }

    /// <summary>
    /// Makes a member return the given values one call at a time, then fall back to its fixed value or default. Use it for
    /// engine iterators such as `GetFirstObjectInArea` / `GetNextObjectInArea`.
    /// </summary>
    public void SetReturnSequence(string member, params object?[] values)
    {
        lock (_gate)
        {
            _sequences[member] = new Queue<object?>(values);
        }
    }

    /// <summary>Makes a member compute its return value from the call's arguments.</summary>
    public void SetReturnHandler(string member, Func<IReadOnlyList<object?>, object?> handler)
    {
        lock (_gate)
        {
            _handlers[member] = handler;
        }
    }

    /// <summary>Called by the generated members that return nothing.</summary>
    protected void RecordCall(string member, object?[] arguments)
    {
        lock (_gate)
        {
            _calls.Add(new RecordedCall(member, arguments));
        }
    }

    /// <summary>Called by the generated members that return a value.</summary>
    protected T InvokeCall<T>(string member, object?[] arguments)
    {
        Func<IReadOnlyList<object?>, object?>? handler;
        object? value;
        bool configured;

        lock (_gate)
        {
            _calls.Add(new RecordedCall(member, arguments));

            if (_sequences.TryGetValue(member, out var sequence) && sequence.Count > 0)
            {
                return Convert<T>(member, sequence.Dequeue());
            }

            _handlers.TryGetValue(member, out handler);
            configured = _values.TryGetValue(member, out value);
        }

        if (handler != null)
        {
            return Convert<T>(member, handler(arguments));
        }

        return configured ? Convert<T>(member, value) : DefaultFor<T>();
    }

    private static T DefaultFor<T>()
    {
        var type = typeof(T);
        if (type == typeof(uint)) return (T)(object)ObjectInvalid;
        if (type == typeof(string)) return (T)(object)string.Empty;
        if (typeof(EngineStructureBase).IsAssignableFrom(type)) return (T)(object)EngineStructures.Create(type, 0);

        return default!;
    }

    private static T Convert<T>(string member, object? value)
    {
        if (value is T typed) return typed;
        if (value == null)
        {
            if (default(T) == null) return default!;
            throw WrongType<T>(member, value);
        }

        var type = typeof(T);
        try
        {
            if (type.IsEnum) return (T)Enum.ToObject(type, value);
            if (type.IsPrimitive && value is IConvertible) return (T)System.Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }
        catch (Exception exception) when (exception is InvalidCastException or FormatException or OverflowException or ArgumentException)
        {
            throw WrongType<T>(member, value);
        }

        throw WrongType<T>(member, value);
    }

    private static InvalidOperationException WrongType<T>(string member, object? value)
    {
        return new InvalidOperationException(
            $"The configured return value for {member} is {value?.GetType().Name ?? "null"}, but the member returns {typeof(T).Name}.");
    }
}
