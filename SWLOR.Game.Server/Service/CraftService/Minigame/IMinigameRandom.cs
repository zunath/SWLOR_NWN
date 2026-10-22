namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// The only source of chance a minigame engine may use, so a run can be repeated from a seed.
/// </summary>
public interface IMinigameRandom
{
    /// <summary>Rolls 1 to 100.</summary>
    int D100();

    /// <summary>Returns a value from <paramref name="minInclusive"/> up to but not including <paramref name="maxExclusive"/>.</summary>
    int Next(int minInclusive, int maxExclusive);
}
