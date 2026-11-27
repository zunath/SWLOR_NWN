using Nwn.Hosting.Contracts.Generations;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.Host.Services;

/// <summary>
/// The host's <see cref="IHostCallbacks"/>: the number of the frame being delivered, the signal handler of the active
/// generation and the script dispatcher of a generation that is activating after the module has loaded. Engine thread only.
/// </summary>
public sealed class HostCallbacks : IHostCallbacks
{
    private IHostSignalHandler? _signals;
    private IScriptDispatcher? _activationScripts;

    /// <summary>The dispatcher of the generation activating by a reload, while it is bound; null otherwise.</summary>
    public IScriptDispatcher? ActivationScripts => Volatile.Read(ref _activationScripts);

    /// <inheritdoc/>
    public ulong Frame { get; private set; }

    /// <summary>True while a generation's signal handler is bound.</summary>
    public bool HasSignalHandler => Volatile.Read(ref _signals) is not null;

    /// <inheritdoc/>
    public void BindSignals(IHostSignalHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        Volatile.Write(ref _signals, handler);
    }

    /// <inheritdoc/>
    public void UnbindSignals(IHostSignalHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        Interlocked.CompareExchange(ref _signals, null, handler);
    }

    /// <inheritdoc/>
    public void BindActivationScripts(IScriptDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        Volatile.Write(ref _activationScripts, dispatcher);
    }

    /// <inheritdoc/>
    public void UnbindActivationScripts(IScriptDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        Interlocked.CompareExchange(ref _activationScripts, null, dispatcher);
    }

    /// <summary>Records the frame NWNX is delivering, before the active generation ticks.</summary>
    public void BeginFrame(ulong frame) => Frame = frame;

    /// <summary>Hands <paramref name="signal"/> to the bound handler, if any.</summary>
    public void DeliverSignal(string signal) => Volatile.Read(ref _signals)?.OnSignal(signal);
}
