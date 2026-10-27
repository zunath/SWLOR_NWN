using System.Reflection;
using System.Runtime.CompilerServices;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A recording stand-in for any interface: it implements the interface at run time, records every call (member and
/// arguments), and returns a fixed value per member. Used to prove that a static facade forwards each member to its
/// instance without writing a fake by hand for every converted service.
/// </summary>
public class RecordingProxy : DispatchProxy
{
    private readonly List<RecordedInvocation> _calls = new();
    private readonly Dictionary<MethodInfo, object?> _returns = new();

    /// <summary>Every call received, in order.</summary>
    public IReadOnlyList<RecordedInvocation> Calls => _calls;

    /// <summary>
    /// Creates a proxy that implements <paramref name="contract"/>. A call returns nothing for <c>void</c>, a distinct
    /// object for a reference type (so a test can see that the facade passes the result through), and the default for a
    /// value type.
    /// </summary>
    public static object Create(Type contract, out RecordingProxy proxy)
    {
        var create = typeof(DispatchProxy).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == nameof(DispatchProxy.Create) && method.GetGenericArguments().Length == 2)
            .MakeGenericMethod(contract, typeof(RecordingProxy));
        var instance = create.Invoke(null, null)!;
        proxy = (RecordingProxy)instance;
        return instance;
    }

    /// <summary>The object the proxy returns for <paramref name="method"/>, or null for <c>void</c> and abstract types.</summary>
    public object? ReturnedBy(MethodInfo method) => _returns.TryGetValue(method, out var value) ? value : null;

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        _calls.Add(new RecordedInvocation(targetMethod!, args ?? Array.Empty<object?>()));
        if (!_returns.TryGetValue(targetMethod!, out var value))
        {
            value = ValueFor(targetMethod!.ReturnType);
            _returns[targetMethod] = value;
        }

        return value;
    }

    private static object? ValueFor(Type returnType)
    {
        if (returnType == typeof(void))
            return null;
        if (returnType == typeof(string))
            return "returned";
        if (returnType.IsValueType)
            return Activator.CreateInstance(returnType);
        if (returnType.IsAbstract || returnType.IsInterface)
            return null;

        return RuntimeHelpers.GetUninitializedObject(returnType);
    }
}
