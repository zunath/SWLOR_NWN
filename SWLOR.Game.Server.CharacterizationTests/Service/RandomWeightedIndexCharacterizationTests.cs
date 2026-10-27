using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Support;
using GameRandom = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>
/// Locks the weighted pick that loot tables and blueprint bonuses draw with, <see cref="GameRandom.GetRandomWeightedIndex"/>:
/// how weights turn into a position, and what it returns when there is nothing to pick from.
/// </summary>
[NonParallelizable]
public class RandomWeightedIndexCharacterizationTests
{
    private const int Seed = 7001;
    private const int Draws = 60;

    [Test]
    public void GetRandomWeightedIndex()
    {
        var table = new SnapshotTable(
            "Random.GetRandomWeightedIndex(int[] weights) (seeded)",
            "Picks a position with a chance proportional to its weight. No weights, or weights that add up to zero or less, give -1. " +
            "A zero weight is never picked; a negative weight lowers the total and moves the pick to later positions. Used by loot tables and blueprint bonuses.",
            $"Hand-written weight lists. Each row reseeds the generator with {Seed} and records {Draws} picks.",
            new[] { "weights" },
            new[] { "picks" },
            Seed)
        {
            AllowConstantOutputs = true
        };

        var cases = new (string Name, int[]? Weights)[]
        {
            ("null", null),
            ("empty", Array.Empty<int>()),
            ("[0]", new[] { 0 }),
            ("[0,0,0]", new[] { 0, 0, 0 }),
            ("[7]", new[] { 7 }),
            ("[1,1]", new[] { 1, 1 }),
            ("[1,2,3]", new[] { 1, 2, 3 }),
            ("[10,0,5]", new[] { 10, 0, 5 }),
            ("[0,0,7]", new[] { 0, 0, 7 }),
            ("[-3,5]", new[] { -3, 5 }),
            ("[5,-3]", new[] { 5, -3 }),
            ("[-4,-4]", new[] { -4, -4 }),
            ("[100,1,1,1]", new[] { 100, 1, 1, 1 }),
            ("[1000000,1]", new[] { 1000000, 1 }),
            ("[2147483647,1] (the sum overflows)", new[] { int.MaxValue, 1 }),
            ("[1,1,1,1,1,1,1,1,1,1]", Enumerable.Repeat(1, 10).ToArray()),
        };

        using var random = new SeededRandom(Seed);
        foreach (var (name, weights) in cases)
        {
            random.Reseed(Seed);
            var picks = new List<int>();
            for (var draw = 0; draw < Draws; draw++)
                picks.Add(GameRandom.GetRandomWeightedIndex(weights!));

            table.Add(name, picks);
        }

        Snapshot.Verify("Random", "GetRandomWeightedIndex", table);
    }
}
