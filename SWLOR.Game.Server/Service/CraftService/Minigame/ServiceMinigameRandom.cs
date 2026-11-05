namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>The production source of chance, backed by the shared random service.</summary>
public sealed class ServiceMinigameRandom : IMinigameRandom
{
    public static readonly ServiceMinigameRandom Instance = new();

    public int D100() => Service.Random.D100(1);

    public int Next(int minInclusive, int maxExclusive) => Service.Random.Next(minInclusive, maxExclusive);
}
