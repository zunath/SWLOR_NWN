using Nwn.Hosting.Contracts.Generations;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The host's frame number and signal binding, as the game sees them.</summary>
public sealed class RecordingHostCallbacks : IHostCallbacks
{
    public ulong Frame { get; set; }

    public IHostSignalHandler Bound { get; private set; }

    public List<string> Calls { get; } = new();

    public void BindSignals(IHostSignalHandler handler)
    {
        Calls.Add("bind");
        Bound = handler;
    }

    public void UnbindSignals(IHostSignalHandler handler)
    {
        Calls.Add("unbind");
        if (ReferenceEquals(Bound, handler))
            Bound = null;
    }

    /// <summary>The dispatcher bound for an activation, while it is.</summary>
    public IScriptDispatcher Activating { get; private set; }

    public void BindActivationScripts(IScriptDispatcher dispatcher)
    {
        Calls.Add("bind-activation");
        Activating = dispatcher;
    }

    public void UnbindActivationScripts(IScriptDispatcher dispatcher)
    {
        Calls.Add("unbind-activation");
        if (ReferenceEquals(Activating, dispatcher))
            Activating = null;
    }
}
