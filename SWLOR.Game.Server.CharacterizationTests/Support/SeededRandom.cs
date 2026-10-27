using GameRandom = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.CharacterizationTests.Support;

/// <summary>
/// Seeds the game's global <c>Random</c> for the lifetime of the scope and restores a fresh time-seeded generator
/// when it is disposed. The generator is shared by the whole process, so a fixture that uses this scope must be
/// <c>[NonParallelizable]</c>.
/// </summary>
/// <example><code>using var random = new SeededRandom(1234); var roll = Combat.CalculateDamage(...);</code></example>
public sealed class SeededRandom : IDisposable
{
    public SeededRandom(int seed)
    {
        Reseed(seed);
    }

    /// <summary>Restarts the sequence, for example to give every row of a snapshot its own reproducible stream.</summary>
    public void Reseed(int seed)
    {
        GameRandom.SetSeed(seed);
    }

    public void Dispose()
    {
        GameRandom.ResetSeed();
    }
}
