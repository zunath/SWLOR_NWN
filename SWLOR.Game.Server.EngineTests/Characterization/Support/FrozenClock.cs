using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.EngineTests.Characterization.Support;

/// <summary>
/// Holds <see cref="Clock"/> at a fixed instant until it is advanced explicitly, and restores the system clock when
/// disposed. Scenarios create one first, so every duration, tick anchor and expiry they record is a function of the
/// scenario's own steps and never of how long the server happened to take.
/// </summary>
public sealed class FrozenClock : IDisposable
{
    /// <summary>The instant every scenario starts at.</summary>
    public static readonly DateTime Start = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    private const long StartTicks = 5_000_000;

    private readonly IDisposable _scope;
    private DateTime _utcNow = Start;
    private long _tickCount64 = StartTicks;

    public FrozenClock()
    {
        _scope = Clock.OverrideForTests(() => _utcNow, () => _tickCount64);
    }

    /// <summary>Seconds since the scenario started.</summary>
    public double ElapsedSeconds => (_utcNow - Start).TotalSeconds;

    /// <summary>Moves both readings forward by the same amount.</summary>
    public void Advance(double seconds)
    {
        if (seconds < 0)
            throw new ArgumentOutOfRangeException(nameof(seconds), "The clock only moves forward.");

        _utcNow = _utcNow.AddSeconds(seconds);
        _tickCount64 += (long)Math.Round(seconds * 1000d);
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
}
