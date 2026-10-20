using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SlicingService;

namespace SWLOR.Game.Server.Tests.Service;

public class SlicingTests
{
    // Design Bible "Tier Board and Progression": grid, extra trace over the cheapest repair,
    // smallest cheapest repair, and most distinct routes a player may power within base trace.
    [TestCase(1, 3, 3, 4, 5, 3)]
    [TestCase(2, 4, 3, 3, 7, 3)]
    [TestCase(3, 4, 4, 3, 9, 4)]
    [TestCase(4, 5, 4, 2, 11, 4)]
    [TestCase(5, 5, 5, 2, 13, 5)]
    public void CatalogBoards_MatchTheBibleTierRules(
        int tier,
        int width,
        int height,
        int extraTrace,
        int minimumCost,
        int maximumRoutes)
    {
        foreach (var board in Enumerable.Range(1, Slicing.GetBoardCount(tier)).Select(number => Slicing.GetBoard(tier, number)))
        {
            board.Width.Should().Be(width);
            board.Height.Should().Be(height);
            board.Tiles.Should().HaveCount(width * height);
            board.BaseTrace.Should().Be(board.SolutionActionCost + extraTrace, board.BoardId);
            board.SolutionActionCost.Should().BeGreaterThanOrEqualTo(minimumCost, board.BoardId);
            Slicing.IsSolved(board).Should().BeFalse();
            Slicing.CanSolveWithSingleAction(board).Should().BeFalse();
            if (tier > 1)
                board.SolutionSwaps.Should().NotBeEmpty("{0} needs a swap within base trace", board.BoardId);
        }

        // Exhaustive route counting is slow, so spot-check one board per tier here; the
        // generator enforces the cap on every board.
        var sample = Slicing.GetBoard(tier, 53);
        SlicingBoardSolver.CountRoutes(sample, sample.BaseTrace, maximumRoutes + 1)
            .Should().BeLessThanOrEqualTo(maximumRoutes);
    }

    // One case per tier: thousands of parameterized cases make the NUnit adapter take minutes
    // even though each replay runs in well under a millisecond.
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void CatalogBoards_EveryRecordedSolutionConnectsCore(int tier)
    {
        for (var boardNumber = 1; boardNumber <= Slicing.GetBoardCount(tier); boardNumber++)
        {
            var board = Slicing.GetBoard(tier, boardNumber);
            var id = $"T{tier}-{boardNumber:000}";

            board.BoardNumber.Should().Be(boardNumber);
            board.BoardId.Should().Be(id);
            board.Tiles
                .Where(tile => tile.Type is SlicingTileType.Entry or SlicingTileType.Core)
                .Should().OnlyContain(tile => tile.Orientation == tile.SolutionOrientation, id);
            Slicing.CanSolveWithSingleAction(board).Should().BeFalse(id);

            var spent = 0;
            foreach (var swap in board.SolutionSwaps)
            {
                Slicing.SwapAdjacent(board, swap.FirstIndex, swap.SecondIndex).Should().BeTrue(id);
                spent += 2;
            }

            foreach (var (tile, index) in board.Tiles.Select((tile, index) => (tile, index)).Where(x => x.tile.SolutionIndex >= 0))
            {
                tile.SolutionIndex.Should().Be(index, "every route tile on {0} sits in its own cell once the swaps are done", id);
                var rotations = Slicing.GetClockwiseSolutionRotationCost(tile);
                for (var turn = 0; turn < rotations; turn++)
                    tile.Orientation = (tile.Orientation + 1) % 4;
                spent += rotations;
            }

            spent.Should().Be(board.SolutionActionCost, id);
            board.BaseTrace.Should().BeGreaterThan(spent, "{0} must be solvable within its starting trace", id);
            Slicing.IsSolved(board).Should().BeTrue("the recorded solution of {0} must power the core", id);
        }
    }

    // The generator proves every board with this solver. Exhaustive proofs on 2,500 boards are too
    // slow for a Debug test run, so this regression check samples each tier.
    [TestCaseSource(nameof(SampledTiersAndBoardNumbers))]
    public void CatalogBoard_TraceIsBuiltOnTheCheapestRepair(int tier, int boardNumber)
    {
        var board = Slicing.GetBoard(tier, boardNumber);

        SlicingBoardSolver.Solve(board, board.SolutionActionCost).Should().BeNull(
            "no repair may cost less than the recorded solution, or the trace margin grows silently");
        if (tier > 1)
        {
            SlicingBoardSolver.Solve(board, board.BaseTrace + 1, allowSwaps: false).Should().BeNull(
                "rotation alone must not reach the core within base trace");
        }
    }

    [Test]
    public void Catalog_EveryBoardHasItsOwnRoute()
    {
        for (var tier = 1; tier <= 5; tier++)
        {
            var boards = Enumerable.Range(1, Slicing.GetBoardCount(tier)).Select(number => Slicing.GetBoard(tier, number)).ToList();
            var endpoints = boards
                .Select(board => (
                    Entry: board.Tiles.FindIndex(tile => tile.Type == SlicingTileType.Entry),
                    Core: board.Tiles.FindIndex(tile => tile.Type == SlicingTileType.Core)))
                .Distinct()
                .Count();
            var routes = boards
                .Select(board => string.Join(",", board.Tiles
                    .Where(tile => tile.SolutionIndex >= 0)
                    .OrderBy(tile => tile.RouteOrder)
                    .Select(tile => tile.SolutionIndex)))
                .Distinct()
                .Count();

            endpoints.Should().BeGreaterThanOrEqualTo(20, $"tier {tier} START and GOAL positions must not be memorizable");
            routes.Should().BeGreaterThanOrEqualTo(60, $"tier {tier} routes must not be memorizable");
        }
    }

    [Test]
    public void Solver_PrefersSwappingInAFittingNeighborOverThreeTurns()
    {
        // START faces east into a straight that needs one turn. Once that cell holds a corner,
        // the only repair is swapping in the straight below it.
        var board = new SlicingBoard
        {
            Width = 3,
            Height = 2,
            Tiles =
            {
                new SlicingTile { Type = SlicingTileType.Entry, Orientation = 1 },
                new SlicingTile { Type = SlicingTileType.Straight, Orientation = 0 },
                new SlicingTile { Type = SlicingTileType.Core, Orientation = 3 },
                new SlicingTile { Type = SlicingTileType.Blocker },
                new SlicingTile { Type = SlicingTileType.Straight, Orientation = 1 },
                new SlicingTile { Type = SlicingTileType.Blocker }
            }
        };

        var rotationOnly = SlicingBoardSolver.Solve(board, 100, allowSwaps: false);
        var best = SlicingBoardSolver.Solve(board, 100);

        rotationOnly!.Cost.Should().Be(1);
        best!.Cost.Should().Be(1, "one turn is cheaper than a 2-trace swap");

        board.Tiles[1] = new SlicingTile { Type = SlicingTileType.Corner, Orientation = 0 };
        SlicingBoardSolver.Solve(board, 100, allowSwaps: false).Should().BeNull("a corner can never run straight through");
        var swapped = SlicingBoardSolver.Solve(board, 100);
        swapped!.Cost.Should().Be(2);
        swapped.Swaps.Should().ContainSingle();
        swapped.Path.Should().Equal(0, 1, 2);
    }

    [Test]
    public void Solver_FindsTilesMovedThroughSeveralSwaps()
    {
        // The only route is START (5) -> 8 -> 7 -> 4 -> GOAL (3), and every cell needs a corner.
        // Moving the corner at 4 two cells to 8 (swap 4/7, then 7/8) leaves every corner already
        // facing the right way, which beats any single swap plus turns.
        var board = new SlicingBoard
        {
            Width = 3,
            Height = 3,
            Tiles =
            {
                new SlicingTile { Type = SlicingTileType.Corner, Orientation = 0 },
                new SlicingTile { Type = SlicingTileType.Straight, Orientation = 2 },
                new SlicingTile { Type = SlicingTileType.Junction, Orientation = 1 },
                new SlicingTile { Type = SlicingTileType.Core, Orientation = 1 },
                new SlicingTile { Type = SlicingTileType.Corner, Orientation = 3 },
                new SlicingTile { Type = SlicingTileType.Entry, Orientation = 2 },
                new SlicingTile { Type = SlicingTileType.Cross, Orientation = 1 },
                new SlicingTile { Type = SlicingTileType.Corner, Orientation = 2 },
                new SlicingTile { Type = SlicingTileType.Corner, Orientation = 0 }
            }
        };

        SlicingBoardSolver.Solve(board, 100, allowSwaps: false)!.Cost.Should().Be(8);
        var best = SlicingBoardSolver.Solve(board, 100);
        best!.Cost.Should().Be(4);
        best.Swaps.Should().HaveCount(2);
        best.Path.Should().Equal(5, 8, 7, 4, 3);
        SlicingBoardSolver.CountRoutes(board, 4, 10).Should().Be(1);
        SlicingBoardSolver.CountRoutes(board, 3, 10).Should().Be(0);

        foreach (var swap in best.Swaps)
            Slicing.SwapAdjacent(board, swap.FirstIndex, swap.SecondIndex).Should().BeTrue();
        Slicing.IsSolved(board).Should().BeTrue("the two swaps alone complete the circuit");
    }

    [Test]
    public void Solver_CountsEachDistinctRouteWithinTheTrace()
    {
        // Crosses link any sides for free, so the top row and the dip through the bottom row
        // both reach the core.
        var board = new SlicingBoard
        {
            Width = 4,
            Height = 2,
            Tiles =
            {
                new SlicingTile { Type = SlicingTileType.Entry, Orientation = 1 },
                new SlicingTile { Type = SlicingTileType.Cross },
                new SlicingTile { Type = SlicingTileType.Cross },
                new SlicingTile { Type = SlicingTileType.Core, Orientation = 3 },
                new SlicingTile { Type = SlicingTileType.Blocker },
                new SlicingTile { Type = SlicingTileType.Cross },
                new SlicingTile { Type = SlicingTileType.Cross },
                new SlicingTile { Type = SlicingTileType.Blocker }
            }
        };

        SlicingBoardSolver.CountRoutes(board, 0, 10, allowSwaps: false)
            .Should().Be(2, "the top row and the dip through the bottom crosses both cost nothing");
        SlicingBoardSolver.Solve(board, 100)!.Cost.Should().Be(0);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void Catalog_ContainsFiveHundredIndependentBoardsPerTier(int tier)
    {
        Slicing.GetBoardCount(tier).Should().Be(500);
        var playerVisibleLayouts = Enumerable.Range(1, 500)
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
    [TestCase(500, 500)]
    [TestCase(501, 1)]
    [TestCase(982451653, 153)]
    public void LegacySelection_MapsToAStableBoardNumber(int selection, int expected)
    {
        Slicing.MapSelectionToBoardNumber(1, selection).Should().Be(expected);
    }

    [Test]
    public void Catalog_RejectsUnknownTiersAndBoardNumbers()
    {
        var invalidTier = () => Slicing.GetBoard(0, 1);
        var belowRange = () => Slicing.GetBoard(1, 0);
        var aboveRange = () => Slicing.GetBoard(1, 501);

        invalidTier.Should().Throw<ArgumentOutOfRangeException>();
        belowRange.Should().Throw<ArgumentOutOfRangeException>();
        aboveRange.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void TraceBonus_UsesRanksAboveTheTierAndCappedCombinedSkill()
    {
        Slicing.GetTraceBonus(5, 5, 100, 100).Should().Be(2, "the stat contribution caps at two");
        Slicing.GetTraceBonus(3, 3, 6, 3).Should().Be(0, "nine combined points fall short of the first trace");
        Slicing.GetTraceBonus(3, 3, 6, 4).Should().Be(1);
        Slicing.GetTraceBonus(4, 2, 14, -4).Should().Be(3, "negative Perception never reduces trace");
        Slicing.GetTraceBonus(5, 1, 20, 0).Should().Be(6);
    }

    [Test]
    public void TraceBonus_CountsEquipmentLockpicking()
    {
        var session = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "SWLOR.Game.Server", "Service", "SlicingService", "SlicingSession.cs"));
        var stat = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server", "Service", "Stat.cs"));
        var getLockpicking = Between(stat, "public static int GetLockpicking", "public static int GetStealth");

        session.Should().Contain("Stat.GetLockpicking(player)",
            "gear writes Lockpicking to the player record, which GetStatAdjustment never reads");
        session.Should().Contain("Slicing.GetTraceBonus(slicingRank, tier, lockpicking, perception)");
        getLockpicking.Should().Contain("dbPlayer.Lockpicking");
        getLockpicking.Should().Contain("GetStatAdjustment(creature, StatType.Lockpicking)");
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
        var swap = board.SolutionSwaps.First();
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

    private static IEnumerable<TestCaseData> SampledTiersAndBoardNumbers()
    {
        for (var tier = 1; tier <= 5; tier++)
        for (var boardNumber = 1; boardNumber <= 500; boardNumber += 50)
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
