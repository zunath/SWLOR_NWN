#nullable enable
using System.Reflection;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A stand-in for a service interface that answers only the members the test scripts with <see cref="On"/> and
/// throws for any other, so a test of one instance supplies exactly the neighbours it relies on without writing a
/// fake that implements every member of the interface.
/// </summary>
public class ScriptedProxy : DispatchProxy
{
    private readonly Dictionary<string, Func<object?[], object?>> _members = new();
    private readonly List<(string Member, object?[] Arguments)> _calls = new();

    /// <summary>Every scripted member that was called, in order.</summary>
    public IReadOnlyList<(string Member, object?[] Arguments)> Calls => _calls;

    public static TContract Create<TContract>(out ScriptedProxy proxy)
        where TContract : class
    {
        var created = Create<TContract, ScriptedProxy>();
        proxy = (ScriptedProxy)(object)created;
        return created;
    }

    /// <summary>Answers calls to <paramref name="member"/> with the result of <paramref name="answer"/>.</summary>
    public ScriptedProxy On(string member, Func<object?[], object?> answer)
    {
        _members[member] = answer;
        return this;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var arguments = args ?? Array.Empty<object?>();
        if (!_members.TryGetValue(targetMethod!.Name, out var answer))
            throw new NotSupportedException($"{targetMethod.DeclaringType?.Name}.{targetMethod.Name} was not scripted for this test.");

        _calls.Add((targetMethod.Name, arguments));
        return answer(arguments);
    }
}
