namespace SWLOR.Game.Host.Reload;

/// <summary>
/// One reload's hold on the engine thread. The reload arms it once its candidate has passed validation, right before the
/// running build is quiesced; the host mode then keeps the engine thread inside the frame it is running, pumping the
/// reload's engine-thread work, until the reload finishes, and releases it with the time it held.
/// </summary>
public sealed class ReloadPause
{
    private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _armed;
    private int _finished;

    /// <summary>True once the reload has passed validation: the engine thread must be held from now on.</summary>
    public bool Armed => Volatile.Read(ref _armed) != 0;

    /// <summary>True once the reload has ended, whatever its outcome: the hold may end.</summary>
    public bool Finished => Volatile.Read(ref _finished) != 0;

    /// <summary>How long the engine thread was held, once released.</summary>
    public TimeSpan? Held { get; private set; }

    /// <summary>The frame that was held, once released.</summary>
    public ulong? Frame { get; private set; }

    /// <summary>Completes when the engine thread has left the hold.</summary>
    public Task Released => _released.Task;

    /// <summary>Called by the reload once its candidate passed validation, before quiescence.</summary>
    public void Arm() => Volatile.Write(ref _armed, 1);

    /// <summary>Called by the reload when it has ended.</summary>
    public void Finish() => Volatile.Write(ref _finished, 1);

    /// <summary>Called on the engine thread when it leaves the hold.</summary>
    public void Release(TimeSpan held, ulong frame)
    {
        Held = held;
        Frame = frame;
        _released.TrySetResult();
    }
}
