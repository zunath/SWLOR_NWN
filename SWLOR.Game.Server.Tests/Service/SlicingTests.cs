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

    [Test]
    public void SwapAdjacent_MovesRevealMetadataWithTheTile()
    {
        var board = Slicing.BuildBoard(2, 982451653);
        var swap = board.SolutionSwaps.Single();
        board.Tiles[swap.FirstIndex].IsRouteRevealed = true;
        board.Tiles[swap.FirstIndex].IsOrientationRevealed = true;

        Slicing.SwapAdjacent(board, swap.FirstIndex, swap.SecondIndex).Should().BeTrue();

        board.Tiles[swap.FirstIndex].IsRouteRevealed.Should().BeFalse();
        board.Tiles[swap.FirstIndex].IsOrientationRevealed.Should().BeFalse();
        board.Tiles[swap.SecondIndex].IsRouteRevealed.Should().BeTrue();
        board.Tiles[swap.SecondIndex].IsOrientationRevealed.Should().BeTrue();
        board.Clone().Tiles[swap.SecondIndex].IsRouteRevealed.Should().BeTrue("rewind snapshots clone tile reveal state");
    }

    [Test]
    public void RevealOrientation_RejectsDecoysAndMarksRouteTiles()
    {
        var board = Slicing.BuildBoard(2, 982451653);
        var session = new SlicingSession.ActiveSlicingSession { Board = board };
        var decoyIndex = board.Tiles.FindIndex(tile => tile.SolutionIndex < 0);
        var routeIndex = board.Tiles.FindIndex(tile => tile.SolutionIndex >= 0);

        InvokeSessionMethod<bool>("RevealOrientation", session, decoyIndex).Should().BeFalse();
        board.Tiles[decoyIndex].IsOrientationRevealed.Should().BeFalse();
        InvokeSessionMethod<bool>("RevealOrientation", session, routeIndex).Should().BeTrue();
        board.Tiles[routeIndex].IsOrientationRevealed.Should().BeTrue();
    }

    [Test]
    public void SessionToolGuard_RemainsClosedAfterThePrimedEffectClears()
    {
        var session = new SlicingSession.ActiveSlicingSession
        {
            HasUsedTool = true,
            PrimedTool = SlicingToolType.Invalid
        };

        InvokeSessionMethod<bool>("CanActivateTool", session).Should().BeFalse();
    }

    [Test]
    public void ActionCost_DoesNotMutateFreeActionsUntilTheActionSucceeds()
    {
        var session = new SlicingSession.ActiveSlicingSession
        {
            PrimedTool = SlicingToolType.NullSignatureLattice
        };
        var arguments = new object[] { session, SlicingToolType.PhaseShuntFork, 2, false };

        InvokeSessionMethod<int>("GetActionCost", arguments).Should().Be(0);
        arguments[3].Should().Be(true);
        session.FreeActionsRemaining.Should().Be(0);

        InvokeSessionMethod<object>("ApplyFreeActionState", session, true);
        session.FreeActionsRemaining.Should().Be(2);
    }

    [TestCase("player-id", "player-id", true)]
    [TestCase("player-id", "different-player", false)]
    [TestCase("", "", false)]
    public void ClaimOwnership_RequiresTheCurrentPlayerId(string playerId, string owner, bool expected)
    {
        InvokeSessionMethod<bool>("IsClaimOwner", playerId, owner).Should().Be(expected);
    }

    [Test]
    public void SessionSource_ValidatesClaimsAndProtectedSwapsBeforeMutation()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "SWLOR.Game.Server",
            "Service",
            "SlicingService",
            "SlicingSession.cs"));
        var validateAction = Between(source, "private static string ValidateAction", "private static bool TryClaim");
        var swap = Between(source, "public static bool SwapSelectedWith", "public static bool ActivateTool");

        validateAction.IndexOf("GetIsInCombat(player)", StringComparison.Ordinal)
            .Should().BeLessThan(validateAction.IndexOf("Touch(session)", StringComparison.Ordinal));
        validateAction.IndexOf("IsClaimOwner", StringComparison.Ordinal)
            .Should().BeLessThan(validateAction.IndexOf("Touch(session)", StringComparison.Ordinal));
        validateAction.Should().Contain("RemoveSession(session);");
        swap.IndexOf("Entry and core sockets cannot be displaced.", StringComparison.Ordinal)
            .Should().BeLessThan(swap.IndexOf("GetActionCost", StringComparison.Ordinal));
    }

    private static IEnumerable<TestCaseData> TiersAndSeeds()
    {
        for (var tier = 1; tier <= 5; tier++)
        for (var seed = 1; seed <= 20; seed++)
            yield return new TestCaseData(tier, seed);
    }

    private static T InvokeSessionMethod<T>(string name, params object[] arguments)
    {
        var method = typeof(SlicingSession).GetMethod(
            name,
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        return (T)method.Invoke(null, arguments)!;
    }

    private static string Between(string source, string start, string end)
    {
        var startIndex = source.IndexOf(start, StringComparison.Ordinal);
        var endIndex = source.IndexOf(end, startIndex, StringComparison.Ordinal);
        return source[startIndex..endIndex];
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
