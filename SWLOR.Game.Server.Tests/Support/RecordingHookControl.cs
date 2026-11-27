using SWLOR.Game.Contracts;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The host's native hooks as the game sees them: which hooks it enabled and which sinks it bound, in order.</summary>
public sealed class RecordingHookControl : IHookControl, INativeSinkBinding
{
    private readonly HashSet<NativeHookId> _enabled = new();

    /// <summary>Every call, in order: <c>enable &lt;id&gt;</c>, <c>bind</c>, <c>unbind</c>.</summary>
    public List<string> Calls { get; } = new();

    /// <summary>The sinks bound now, or null.</summary>
    public NativeSinkSet Bound { get; private set; }

    public void Enable(NativeHookId hook)
    {
        Calls.Add("enable " + hook);
        _enabled.Add(hook);
    }

    public bool IsEnabled(NativeHookId hook) => _enabled.Contains(hook);

    public void Bind(NativeSinkSet sinks)
    {
        Calls.Add("bind");
        Bound = sinks;
    }

    public void Unbind(NativeSinkSet sinks)
    {
        Calls.Add("unbind");
        if (ReferenceEquals(Bound, sinks))
            Bound = null;
    }
}
