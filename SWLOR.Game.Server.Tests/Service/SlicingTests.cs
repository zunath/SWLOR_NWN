using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SlicingService;

namespace SWLOR.Game.Server.Tests.Service;

public class SlicingTests
{
    [TestCase(1, 3, 3, 0)]
    [TestCase(2, 4, 3, 1)]
    [TestCase(3, 4, 4, 2)]
    [TestCase(4, 5, 4, 3)]
    [TestCase(5, 5, 5, 4)]
    public void BuildBoard_UsesTierDimensionsAndBoundedSwaps(int tier, int width, int height, int swaps)
    {
        var board = Slicing.BuildBoard(tier, 982451653);

        board.Width.Should().Be(width);
        board.Height.Should().Be(height);
        board.Tiles.Should().HaveCount(width * height);
        board.SolutionSwaps.Should().HaveCount(swaps);
        board.BaseTrace.Should().BeGreaterThan(board.SolutionActionCost);
        Slicing.IsSolved(board).Should().BeFalse();
    }

    [TestCaseSource(nameof(TiersAndSeeds))]
    public void BuildBoard_IsDeterministicAndKnownSolutionConnectsCore(int tier, int seed)
    {
        var first = Slicing.BuildBoard(tier, seed);
        var second = Slicing.BuildBoard(tier, seed);

        first.Tiles.Select(x => (x.Type, x.Orientation, x.SolutionIndex, x.RouteOrder, x.SolutionOrientation))
            .Should().Equal(second.Tiles.Select(x => (x.Type, x.Orientation, x.SolutionIndex, x.RouteOrder, x.SolutionOrientation)));

        var spent = 0;
        foreach (var swap in first.SolutionSwaps)
        {
            Slicing.SwapAdjacent(first, swap.FirstIndex, swap.SecondIndex).Should().BeTrue();
            spent += 2;
        }

        foreach (var tile in first.Tiles.Where(x => x.SolutionIndex >= 0))
        {
            var rotations = Slicing.GetClockwiseSolutionRotationCost(tile);
            for (var turn = 0; turn < rotations; turn++)
                tile.Orientation = (tile.Orientation + 1) % 4;
            spent += rotations;
        }

        spent.Should().Be(first.SolutionActionCost);
        Slicing.IsSolved(first).Should().BeTrue();
    }

    [Test]
    public void TraceBonus_UsesPerkAndCombinedPositiveSkillScaling()
    {
        Slicing.GetTraceBonus(2, 100, 100).Should().Be(5, "the stat contribution caps at five");
        Slicing.GetTraceBonus(3, 4, 1).Should().Be(2);
        Slicing.GetTraceBonus(4, 8, -4).Should().Be(3, "negative Perception never reduces trace");
        Slicing.GetTraceBonus(5, 25, 0).Should().Be(8);
    }

    [TestCase(1, 0)]
    [TestCase(2, 10)]
    [TestCase(3, 25)]
    [TestCase(4, 50)]
    [TestCase(5, 100)]
    [TestCase(10, 100)]
    public void DestructionChance_FirstFailureIsFreeAndPressureEscalates(int failures, int expected)
    {
        Slicing.GetDestructionChance(failures).Should().Be(expected);
    }

    private static IEnumerable<TestCaseData> TiersAndSeeds()
    {
        for (var tier = 1; tier <= 5; tier++)
        for (var seed = 1; seed <= 20; seed++)
            yield return new TestCaseData(tier, seed);
    }
}
