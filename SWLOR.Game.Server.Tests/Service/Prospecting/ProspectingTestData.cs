using System.Collections.Immutable;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

namespace SWLOR.Game.Server.Tests.Service.Prospecting;

/// <summary>
/// Nodes and engines for the Prospecting tests. A node is written as its pool and its seams in the letters a stored node uses: a lower
/// case letter is a hidden seam and an upper case one a revealed seam, <c>r</c> for a rich seam and <c>g</c> for a gem pocket.
/// </summary>
internal static class ProspectingTestData
{
    /// <summary>The rolls that pick a Strike's outcome at yield tier 0: 1 unit on 1-60, 2 units on 61-100.</summary>
    public const int StrikeOneUnit = 1;
    public const int StrikeTwoUnits = 100;

    /// <summary>The rolls that pick a Heavy Strike's outcome at yield tier 0 (2: 25%, 3: 50%, 4: 25%; 20% of each shatters).</summary>
    public const int HeavyTwoClean = 1;
    public const int HeavyTwoShatters = 21;
    public const int HeavyThreeClean = 26;
    public const int HeavyThreeShatters = 66;
    public const int HeavyFourClean = 76;
    public const int HeavyFourShatters = 96;

    /// <summary>The roll that gives a rich seam its bonus (25%), and the one that does not.</summary>
    public const int RichBonus = 1;
    public const int RichNoBonus = 100;

    public static ProspectingNode Node(int pool, string seams = "", int stability = 100)
    {
        var builder = ImmutableArray.CreateBuilder<ProspectingSeam>();
        foreach (var letter in seams)
        {
            builder.Add(new ProspectingSeam(
                char.ToLowerInvariant(letter) == 'g' ? ProspectingSeamKind.GemPocket : ProspectingSeamKind.Rich,
                char.IsUpper(letter)));
        }

        return new ProspectingNode(pool, stability, builder.ToImmutable());
    }

    public static ProspectingSetup Setup(ProspectingNode node, int charges = 20, int rank = 0, int tier = 1) =>
        ProspectingSetup.ForNode(node, charges, rank, tier);

    public static ProspectingEngine Engine(ProspectingNode node, int charges = 20, int rank = 0, int[] rolls = null) =>
        new(new ScriptedMinigameRandom(rolls ?? []), Setup(node, charges, rank));

    public static ProspectingEngine Engine(string seams, int pool, int charges = 20, int rank = 0, int stability = 100, int[] rolls = null) =>
        new(new ScriptedMinigameRandom(rolls ?? []), Setup(Node(pool, seams, stability), charges, rank));
}
