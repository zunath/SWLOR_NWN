namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>A repeatable source of chance: the same seed always produces the same sequence.</summary>
public sealed class SeededMinigameRandom : IMinigameRandom
{
    private readonly System.Random _random;

    public SeededMinigameRandom(int seed)
    {
        _random = new System.Random(seed);
    }

    public int D100() => _random.Next(1, 101);

    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
}
