using SWLOR.Game.Contracts;

namespace SWLOR.Game.Host.Services;

/// <summary>
/// The host's <see cref="IHostCallbacks"/>: the number of the frame being delivered and the signal handler of the active
/// generation. Engine thread only.
/// </summary>
public sealed class HostCallbacks : IHostCallbacks
{
    private IHostSignalHandler? _signals;

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

    /// <summary>Records the frame NWNX is delivering, before the active generation ticks.</summary>
    public void BeginFrame(ulong frame) => Frame = frame;

    /// <summary>Hands <paramref name="signal"/> to the bound handler, if any.</summary>
    public void DeliverSignal(string signal) => Volatile.Read(ref _signals)?.OnSignal(signal);
}
