using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Line;

/// <summary>
/// How a fish picks its next move: a weight for each move, given the move before it. A common fish picks each move on its own;
/// a rare fish chains them (a Run keeps running, a Dive turns into a Thrash, and a rest is rarely followed by another) and rests
/// less. The pattern is immutable, and every roll uses the attempt's own source of chance.
/// </summary>
public sealed class LineMovePattern
{
    private const int StartRow = 0;
    private const int MoveCount = 4;
    private const int MinimumWeight = 2;

    // Common fish: the same weights after every move. The Tire weight comes from how easy the fish is.
    private const int CommonRun = 30;
    private const int CommonDive = 25;
    private const int CommonThrash = 25;

    // Rare fish: the weights after each move (Run, Dive, Thrash, Tire), written for a Tire weight of RareReferenceTire.
    private const int RareReferenceTire = 20;
    private static readonly int[][] RareRows =
    {
        new[] { 30, 25, 25, 20 },
        new[] { 45, 15, 30, 10 },
        new[] { 15, 20, 50, 15 },
        new[] { 35, 30, 20, 15 },
        new[] { 30, 30, 35, 5 }
    };

    private static readonly FishMove[] Moves = { FishMove.Run, FishMove.Dive, FishMove.Thrash, FishMove.Tire };

    // Row 0 follows no move (the first roll); rows 1-4 follow Run, Dive, Thrash and Tire. Never changed once built.
    private readonly int[][] _weights;

    private LineMovePattern(int[][] weights)
    {
        _weights = weights;
    }

    /// <summary>The weights of a common fish, which picks every move independently; <paramref name="tireWeight"/> is its chance to rest.</summary>
    public static LineMovePattern Common(int tireWeight)
    {
        var row = new[] { CommonRun, CommonDive, CommonThrash, Math.Max(MinimumWeight, tireWeight) };
        return new LineMovePattern(Enumerable.Range(0, MoveCount + 1).Select(_ => (int[])row.Clone()).ToArray());
    }

    /// <summary>The weights of a rare fish. Its chance to rest moves half as far as a common fish's with <paramref name="tireWeight"/>.</summary>
    public static LineMovePattern Rare(int tireWeight)
    {
        var shift = (tireWeight - RareReferenceTire) / 2;
        return new LineMovePattern(RareRows
            .Select(row => new[] { row[0], row[1], row[2], Math.Max(MinimumWeight, row[3] + shift) })
            .ToArray());
    }

    /// <summary>The pattern for a fish of the given rarity whose chance to rest is <paramref name="tireWeight"/>.</summary>
    public static LineMovePattern For(FishRarity rarity, int tireWeight) =>
        rarity == FishRarity.Rare ? Rare(tireWeight) : Common(tireWeight);

    /// <summary>The weight of <paramref name="next"/> after <paramref name="previous"/>; null means the first move of the fight.</summary>
    public int Weight(FishMove? previous, FishMove next) => _weights[RowOf(previous)][Array.IndexOf(Moves, next)];

    /// <summary>The share, in percent, of each move after <paramref name="previous"/>, in the order Run, Dive, Thrash, Tire.</summary>
    public IReadOnlyList<double> Shares(FishMove? previous)
    {
        var row = _weights[RowOf(previous)];
        var total = row.Sum();
        return row.Select(weight => 100.0 * weight / total).ToList();
    }

    /// <summary>Picks the move that follows <paramref name="previous"/>, or the first move of the fight when it is null.</summary>
    public FishMove Roll(IMinigameRandom random, FishMove? previous)
    {
        var row = _weights[RowOf(previous)];
        var roll = random.Next(0, row.Sum());
        for (var index = 0; index < row.Length; index++)
        {
            roll -= row[index];
            if (roll < 0)
                return Moves[index];
        }

        return Moves[^1];
    }

    private static int RowOf(FishMove? previous) => previous.HasValue ? Array.IndexOf(Moves, previous.Value) + 1 : StartRow;
}
