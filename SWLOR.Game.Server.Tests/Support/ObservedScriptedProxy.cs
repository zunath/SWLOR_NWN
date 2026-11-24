#nullable enable
using System.Reflection;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A <see cref="ScriptedProxy"/> that writes every call it receives to an <see cref="OrderedLog"/> before it answers, for a transcript of
/// the calls a neighbour service receives, in order with the engine calls.
/// </summary>
public class ObservedScriptedProxy : ScriptedProxy
{
    /// <summary>The log the call is written to, with the label of the neighbour.</summary>
    public OrderedLog? Log { get; set; }

    public string Label { get; set; } = string.Empty;

    /// <summary>Creates a scripted stand-in for <typeparamref name="TContract"/> whose calls are written to <paramref name="log"/> as "label.Member(arguments)".</summary>
    public static TContract Create<TContract>(OrderedLog log, string label, out ScriptedProxy proxy)
        where TContract : class
    {
        var created = DispatchProxy.Create<TContract, ObservedScriptedProxy>();
        var observed = (ObservedScriptedProxy)(object)created;
        observed.Log = log;
        observed.Label = label;
        proxy = observed;
        return created;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        Log?.Add(Label, targetMethod!.Name, args ?? Array.Empty<object?>());
        return base.Invoke(targetMethod, args);
    }
}
