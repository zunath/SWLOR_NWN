using System;
using System.Collections.Generic;

namespace SWLOR.Game.NativeHooks;

/// <summary>
/// Keeps each creature's current HP calculation consistent through nested attribute changes. One instance per thread
/// (<see cref="Current"/>), which the Vitality hit point hooks share; it lives here, beside the trampolines, because the
/// engine thread never exits and a thread-static value of a game build would outlive the build. Between hook calls it is
/// empty: every <see cref="Begin"/> is ended in the same call.
/// </summary>
public sealed class VitalityRecalculation
{
    [ThreadStatic] private static VitalityRecalculation? _current;

    private readonly Dictionary<nint, int> _scores = new();

    /// <summary>The current thread's recalculation, or null when the thread has never begun one.</summary>
    public static VitalityRecalculation? Current => _current;

    /// <summary>The current thread's recalculation, created the first time the thread begins one.</summary>
    public static VitalityRecalculation ForCurrentThread() => _current ??= new VitalityRecalculation();

    public bool Begin(nint stats, int score) => _scores.TryAdd(stats, score);

    public bool TryGetScore(nint stats, out int score) => _scores.TryGetValue(stats, out score);

    public void Update(nint stats, int score)
    {
        if (_scores.ContainsKey(stats)) _scores[stats] = score;
    }

    public void End(nint stats) => _scores.Remove(stats);
}
