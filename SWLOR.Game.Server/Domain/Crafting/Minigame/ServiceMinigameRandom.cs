namespace SWLOR.Game.Server.Domain.Crafting.Minigame;

/// <summary>The production source of chance, backed by the shared random service.</summary>
public sealed class ServiceMinigameRandom : IMinigameRandom
{
    public static ServiceMinigameRandom Instance => new();

    public int D100() => Domain.Foundation.Random.D100(1);

    public int Next(int minInclusive, int maxExclusive) => Domain.Foundation.Random.Next(minInclusive, maxExclusive);
}
