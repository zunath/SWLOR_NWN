using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SlicingService;

namespace SWLOR.Game.Server.Tests.Service;

public class SlicingTests
{
    [TestCase(1, 3, 3, 0)]
    [TestCase(2, 4, 3, 1)]
    [TestCase(3, 4, 4, 2)]
    [TestCase(4, 5, 4, 3)]
    [TestCase(5, 5, 5, 4)]
    public void CatalogBoard_UsesTierDimensionsAndBoundedSwaps(int tier, int width, int height, int swaps)
    {
        var board = Slicing.GetBoard(tier, 53);

        board.BoardId.Should().Be($"T{tier}-053");
        board.Width.Should().Be(width);
        board.Height.Should().Be(height);
        board.Tiles.Should().HaveCount(width * height);
        board.SolutionSwaps.Should().HaveCount(swaps);
        board.BaseTrace.Should().BeGreaterThan(board.SolutionActionCost);
        board.SolutionActionCost.Should().BeGreaterThan(1);
        Slicing.IsSolved(board).Should().BeFalse();
        Slicing.CanSolveWithSingleAction(board).Should().BeFalse();
        board.Tiles
            .Where(tile => tile.Type is SlicingTileType.Entry or SlicingTileType.Core)
            .Should().OnlyContain(tile => tile.Orientation == tile.SolutionOrientation);
    }

    [TestCaseSource(nameof(TiersAndBoardNumbers))]
    public void CatalogBoard_HasKnownSolutionThatConnectsCore(int tier, int boardNumber)
    {
        var first = Slicing.GetBoard(tier, boardNumber);

        first.BoardNumber.Should().Be(boardNumber);
        first.BoardId.Should().Be($"T{tier}-{boardNumber:000}");
        first.Tiles
            .Where(tile => tile.Type is SlicingTileType.Entry or SlicingTileType.Core)
            .Should().OnlyContain(tile => tile.Orientation == tile.SolutionOrientation);
        Slicing.CanSolveWithSingleAction(first).Should().BeFalse();

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

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void Catalog_ContainsOneHundredIndependentBoardsPerTier(int tier)
    {
        Slicing.GetBoardCount(tier).Should().Be(100);
        var playerVisibleLayouts = Enumerable.Range(1, 100)
            .Select(boardNumber => Slicing.GetBoard(tier, boardNumber))
            .Select(board => string.Join(";", board.Tiles.Select(tile =>
                $"{(int)tile.Type},{tile.Orientation}")))
            .ToList();
        playerVisibleLayouts.Should().OnlyHaveUniqueItems(
            "every catalog number should identify a distinct starting layout");

        var first = Slicing.GetBoard(tier, 1);
        var second = Slicing.GetBoard(tier, 1);
        first.Should().NotBeSameAs(second);
        first.Tiles.Should().NotBeSameAs(second.Tiles);
        first.Tiles[0].Orientation = (first.Tiles[0].Orientation + 1) % 4;
        first.Tiles[0].Orientation.Should().NotBe(second.Tiles[0].Orientation);
    }

    [TestCase(1, 1)]
    [TestCase(100, 100)]
    [TestCase(101, 1)]
    [TestCase(982451653, 53)]
    public void LegacySelection_MapsToAStableBoardNumber(int selection, int expected)
    {
        Slicing.MapSelectionToBoardNumber(1, selection).Should().Be(expected);
    }

    [Test]
    public void Catalog_RejectsUnknownTiersAndBoardNumbers()
    {
        var invalidTier = () => Slicing.GetBoard(0, 1);
        var belowRange = () => Slicing.GetBoard(1, 0);
        var aboveRange = () => Slicing.GetBoard(1, 101);

        invalidTier.Should().Throw<ArgumentOutOfRangeException>();
        belowRange.Should().Throw<ArgumentOutOfRangeException>();
        aboveRange.Should().Throw<ArgumentOutOfRangeException>();
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

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void RankRequirementError_IsRed(int tier)
    {
        var message = InvokeSessionMethod<string>("GetRankRequirementError", tier);

        message.Should().Be(ColorToken.Red($"Slicing rank {tier} is required for this target."));
    }

    [Test]
    public void SwapAdjacent_MovesRevealMetadataWithTheTile()
    {
        var board = Slicing.GetBoard(2, 53);
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
    public void FixedEndpoints_CannotRotate()
    {
        var board = Slicing.GetBoard(1, 53);
        var entry = board.Tiles.FindIndex(tile => tile.Type == SlicingTileType.Entry);
        var core = board.Tiles.FindIndex(tile => tile.Type == SlicingTileType.Core);
        var entryOrientation = board.Tiles[entry].Orientation;
        var coreOrientation = board.Tiles[core].Orientation;

        Slicing.RotateClockwise(board, entry).Should().BeFalse();
        Slicing.RotateClockwise(board, core).Should().BeFalse();
        board.Tiles[entry].Orientation.Should().Be(entryOrientation);
        board.Tiles[core].Orientation.Should().Be(coreOrientation);
    }

    [Test]
    public void SingleActionDetection_IgnoresFixedCoreRotationButFindsCircuitRotation()
    {
        var coreOnlyBoard = new SlicingBoard
        {
            Width = 2,
            Height = 1,
            Tiles =
            {
                new SlicingTile { Type = SlicingTileType.Entry, Orientation = 1 },
                new SlicingTile { Type = SlicingTileType.Core, Orientation = 2 }
            }
        };
        var circuitBoard = new SlicingBoard
        {
            Width = 3,
            Height = 1,
            Tiles =
            {
                new SlicingTile { Type = SlicingTileType.Entry, Orientation = 1 },
                new SlicingTile { Type = SlicingTileType.Straight, Orientation = 0 },
                new SlicingTile { Type = SlicingTileType.Core, Orientation = 3 }
            }
        };

        Slicing.CanSolveWithSingleAction(coreOnlyBoard).Should().BeFalse(
            "GOAL rotation is not a legal action even when it would connect the circuit");
        Slicing.CanSolveWithSingleAction(circuitBoard).Should().BeTrue();
        Slicing.RotateClockwise(circuitBoard, 1).Should().BeTrue();
        Slicing.IsSolved(circuitBoard).Should().BeTrue();
    }

    [Test]
    public void SingleActionDetection_FindsSolvingAdjacentSwap()
    {
        var board = new SlicingBoard
        {
            Width = 3,
            Height = 2,
            Tiles =
            {
                new SlicingTile { Type = SlicingTileType.Entry, Orientation = 1 },
                new SlicingTile { Type = SlicingTileType.Blocker },
                new SlicingTile { Type = SlicingTileType.Core, Orientation = 3 },
                new SlicingTile { Type = SlicingTileType.Blocker },
                new SlicingTile { Type = SlicingTileType.Straight, Orientation = 1 },
                new SlicingTile { Type = SlicingTileType.Blocker }
            }
        };

        Slicing.CanSolveWithSingleAction(board).Should().BeTrue();
        Slicing.SwapAdjacent(board, 1, 4).Should().BeTrue();
        Slicing.IsSolved(board).Should().BeTrue();
    }

    [Test]
    public void RevealOrientation_RejectsDecoysAndMarksRouteTiles()
    {
        var board = Slicing.GetBoard(2, 53);
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
    public void DeferredToolBenefits_RequireThePrimedItemToRemainInInventory()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "SWLOR.Game.Server",
            "Service",
            "SlicingService",
            "SlicingSession.cs"));
        var validateAction = Between(source, "private static string ValidateAction", "private static bool TryClaim");

        validateAction.Should().Contain("session.PrimedToolItem != OBJECT_INVALID");
        validateAction.Should().Contain("GetIsObjectValid(session.PrimedToolItem)");
        validateAction.Should().Contain("GetItemPossessor(session.PrimedToolItem) != player");
        validateAction.IndexOf("session.PrimedToolItem != OBJECT_INVALID", StringComparison.Ordinal)
            .Should().BeLessThan(validateAction.IndexOf("Touch(session)", StringComparison.Ordinal),
                "a detached primed tool must reject the action before session state changes");
        validateAction.Should().Contain("session.PrimedTool = SlicingToolType.Invalid;");
        validateAction.Should().Contain("session.PrimedToolItem = OBJECT_INVALID;");
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
        var rotate = Between(source, "public static bool RotateSelected", "public static bool SwapSelectedWith");

        validateAction.IndexOf("GetIsInCombat(player)", StringComparison.Ordinal)
            .Should().BeLessThan(validateAction.IndexOf("Touch(session)", StringComparison.Ordinal));
        validateAction.IndexOf("IsClaimOwner", StringComparison.Ordinal)
            .Should().BeLessThan(validateAction.IndexOf("Touch(session)", StringComparison.Ordinal));
        validateAction.Should().Contain("RemoveSession(session);");
        swap.IndexOf("Entry and core sockets cannot be displaced.", StringComparison.Ordinal)
            .Should().BeLessThan(swap.IndexOf("GetActionCost", StringComparison.Ordinal));
        swap.Should().Contain("Choose a tile directly above, below, left, or right of the selected tile.");
        swap.Should().NotContain("message = \"Tile selected.\";",
            "invalid swap destinations must not silently turn into selection changes");
        rotate.IndexOf("START and GOAL sockets are fixed", StringComparison.Ordinal)
            .Should().BeLessThan(rotate.IndexOf("GetActionCost", StringComparison.Ordinal));
        source.Should().Contain("Slicing.GetBoard(tier, boardNumber)");
        source.Should().NotContain("SlicingBoardAuthoring",
            "live sessions must load checked-in entries rather than generating boards");
        source.Should().Contain("BoardNumberVariable");
        source.Should().Contain("Slicing.MapSelectionToBoardNumber(tier, selection)");
        source.Should().Contain("completed {session.Source} slicing board {session.Board.BoardId}");
        source.Should().Contain("failed {session.Source} slicing board {session.Board.BoardId}");
    }

    [Test]
    public void TileTooltip_SymmetricStraightTileReportsConnectionEquivalentTurns()
    {
        var scrambledByTwo = new SlicingTile
        {
            Type = SlicingTileType.Straight,
            Orientation = 2,
            SolutionOrientation = 0
        };
        var scrambledByOne = new SlicingTile
        {
            Type = SlicingTileType.Straight,
            Orientation = 1,
            SolutionOrientation = 0
        };

        Slicing.GetClockwiseSolutionRotationCost(scrambledByTwo).Should().Be(0,
            "a Straight tile is 180-degree symmetric, so a 2-step scramble is already functionally correct");
        Slicing.GetClockwiseSolutionRotationCost(scrambledByOne).Should().Be(1);
    }

    [Test]
    public void StaleClaim_ResolvesAbandonedCommittedAttemptRegardlessOfClaimant()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "SWLOR.Game.Server",
            "Service",
            "SlicingService",
            "SlicingSession.cs"));
        var tryClaim = Between(source, "private static bool TryClaim", "private static bool PrepareAction");

        var ownerMismatchBlockEnd = tryClaim.IndexOf("_sessions.Remove(owner);", StringComparison.Ordinal);
        var committedCheckConditionStart = tryClaim.IndexOf(
            "if (!string.IsNullOrWhiteSpace(owner) &&",
            ownerMismatchBlockEnd,
            StringComparison.Ordinal);
        var committedCheckIndex = tryClaim.IndexOf("ResolveAbandonedFailure(target, source, tier)", StringComparison.Ordinal);
        var grantIndex = tryClaim.IndexOf("SetLocalString(target, OwnerVariable, playerId);", StringComparison.Ordinal);

        committedCheckConditionStart.Should().BeGreaterThan(ownerMismatchBlockEnd,
            "the committed-attempt check must live outside the other-player-only branch so a same-player reclaim reaches it too");
        committedCheckIndex.Should().BeLessThan(grantIndex,
            "an abandoned committed attempt must be resolved before a fresh claim is granted");

        var committedCheckCondition = tryClaim[committedCheckConditionStart..committedCheckIndex];
        committedCheckCondition.Should().NotContain("owner != playerId",
            "the stale committed-attempt check must not be gated on the claimant being a different player");
    }

    private static IEnumerable<TestCaseData> TiersAndBoardNumbers()
    {
        for (var tier = 1; tier <= 5; tier++)
        for (var boardNumber = 1; boardNumber <= 100; boardNumber++)
            yield return new TestCaseData(tier, boardNumber);
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
