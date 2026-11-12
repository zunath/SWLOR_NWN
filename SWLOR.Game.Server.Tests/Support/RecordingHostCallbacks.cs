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
}
