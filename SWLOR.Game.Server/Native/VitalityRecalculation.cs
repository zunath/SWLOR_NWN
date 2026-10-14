using System.Collections.Generic;

namespace SWLOR.Game.Server.Native;

/// <summary>Keeps each creature's current HP calculation consistent through nested attribute changes.</summary>
public sealed class VitalityRecalculation
{
    private readonly Dictionary<nint, int> _scores = new();

    public bool Begin(nint stats, int score) => _scores.TryAdd(stats, score);

    public bool TryGetScore(nint stats, out int score) => _scores.TryGetValue(stats, out score);

    public void Update(nint stats, int score)
    {
        if (_scores.ContainsKey(stats)) _scores[stats] = score;
    }

    public void End(nint stats) => _scores.Remove(stats);
}
