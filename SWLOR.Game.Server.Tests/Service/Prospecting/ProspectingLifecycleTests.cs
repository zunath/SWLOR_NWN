using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.ProspectingService;
using static SWLOR.Game.Server.Tests.Service.Prospecting.ProspectingTestData;

namespace SWLOR.Game.Server.Tests.Service.Prospecting;

/// <summary>
/// Covers a session from the use of a harvester to its end: the one-player lock, what each action hands the player and pays, how the
/// charges and the node are kept, every way of leaving, and Quick Harvest against the harvests it replaces.
/// </summary>
public class ProspectingLifecycleTests
{
    private const uint Player = 11;
    private const uint OtherPlayer = 12;
    private const uint Harvester = 22;
    private const uint OtherHarvester = 23;
    private const uint NodeId = 33;

    /// <summary>A roll out of 10000 above the chance of a gem pocket, so a fresh node's seam is a rich one.</summary>
    private const int RichRoll = 10000;

    private FakeProspectingWorld _world;

    [OneTimeSetUp]
    public void LoadTheXpChart()
    {
        Skill.CacheXPChartData();
    }

    [SetUp]
    public void SetUp()
    {
        _world = new FakeProspectingWorld();
        _world.Charges[Harvester] = 50;
        _world.Charges[OtherHarvester] = 50;
    }

    private static ProspectingRequest Request(uint player = Player, uint harvester = Harvester, int tier = 3, int rank = 25, uint node = NodeId) =>
        new(player, harvester, node, "HARVESTING_PLAGIONITE", tier, rank, new Vector3(10f, 20f, 0f));

    private ProspectingLifecycle Lifecycle(params int[] rolls) => new(_world, new ScriptedMinigameRandom(rolls));

    private ProspectingSession Open(ProspectingLifecycle lifecycle, ProspectingNode node, int rank = 25, int tier = 3)
    {
        _world.SaveNode(NodeId, node.Serialize());
        var begin = lifecycle.Begin(Request(rank: rank, tier: tier));
        begin.Status.Should().Be(ProspectingBeginStatus.Opened);
        return begin.Session;
    }

    private ProspectingActionReport Do(ProspectingLifecycle lifecycle, ProspectingSession session, string action)
    {
        ProspectingActionReport report = null;
        var start = lifecycle.StartAction(session, action, r => report = r);
        start.Started.Should().BeTrue($"{action}: {start.Reason}");
        var seconds = ProspectingRules.SecondsFor(action);
        if (seconds > 0)
        {
            report.Should().BeNull("the action takes its seconds before it resolves");
            _world.RunNextWork((float)seconds);
        }

        report.Should().NotBeNull();
        return report;
    }

    private static int XpPerUnit(int tier, int rank) => Skill.GetDeltaXP(GatheringLevels.ForTier(tier) - rank);

    // Beginning a session.

    [Test]
    public void UsingAHarvesterOpensTheWindow_MarksThePlayerBusy_AndHoldsTheNode()
    {
        var lifecycle = Lifecycle();
        _world.SaveNode(NodeId, Node(3, "rg").Serialize());

        var begin = lifecycle.Begin(Request());

        begin.Opened.Should().BeTrue();
        _world.Opened.Should().Equal(begin.Session);
        _world.Busy.Should().BeTrue();
        lifecycle.GetActive(Player).Should().BeSameAs(begin.Session);
        lifecycle.GetHolder(NodeId).Should().BeSameAs(begin.Session);
        begin.Session.Engine.Pool.Should().Be(3);
        begin.Session.Engine.Charges.Should().Be(50, "the harvester's charges are the budget");
        begin.Session.Engine.Setup.SurveyReveals.Should().Be(CraftRankMilestones.ProspectingSurveyRevealsForRank(25));
        begin.Session.Engine.Setup.YieldTier.Should().Be(CraftRankMilestones.ProspectingYieldTierForRank(25));
        _world.ItemsHarvested.Should().Be(0, "opening the window takes nothing");
    }

    [Test]
    public void ANodeNobodyHasWorkedIsMadeFresh_AndStoredAtOnce()
    {
        // Next(1, 5) is the pool, Next(1, 101) the seam count, then Next(1, 10001) for the seam's kind.
        var lifecycle = Lifecycle(4, 1, RichRoll);

        var begin = lifecycle.Begin(Request());

        begin.Opened.Should().BeTrue();
        _world.Nodes[NodeId].Should().Be("P4S100:r");
        begin.Session.Engine.Pool.Should().Be(4);
    }

    [Test]
    public void AnUnreadableStoredNodeIsTreatedAsOneNobodyHasWorked()
    {
        var lifecycle = Lifecycle(2, 1, RichRoll);
        _world.Nodes[NodeId] = "not a node";

        lifecycle.Begin(Request()).Opened.Should().BeTrue();

        _world.Nodes[NodeId].Should().Be("P2S100:r");
    }

    [Test]
    public void ATableWithAGemMakesPocketsAtAShareOfItsGemChance_AskedOfTheWorldForTheNodesTable()
    {
        // The world reports the table's gem chance (a tenth here); a seam is a pocket when its roll out of 10000 is within 0.15 of that.
        var pocket = ProspectingRules.GemPocketBasisPoints(_world.GemChanceValue);
        var lifecycle = Lifecycle(4, 100, pocket, pocket + 1, pocket);

        var begin = lifecycle.Begin(Request());

        begin.Session.Engine.ToNode().Seams.Select(seam => seam.Kind).Should().Equal(
            ProspectingSeamKind.GemPocket, ProspectingSeamKind.Rich, ProspectingSeamKind.GemPocket);
        pocket.Should().Be(150);
    }

    [Test]
    public void AGemlessTableMakesRichSeamsOnly()
    {
        var lifecycle = Lifecycle(4, 100, 1, 1, 1);
        _world.GemChanceValue = 0;

        var begin = lifecycle.Begin(Request());

        begin.Session.Engine.ToNode().Seams.Should().OnlyContain(seam => seam.Kind == ProspectingSeamKind.Rich);
    }

    [Test]
    public void OnlyOnePlayerWorksANodeAtATime_AndOthersAreToldItIsInUse()
    {
        var lifecycle = Lifecycle();
        Open(lifecycle, Node(3, "r"));

        var second = lifecycle.Begin(Request(OtherPlayer, OtherHarvester));

        second.Status.Should().Be(ProspectingBeginStatus.InUse);
        second.Session.Should().BeNull();
        _world.Messages.Should().Contain(ProspectingLifecycle.InUseMessage);
        lifecycle.IsInUseByAnother(NodeId, OtherPlayer).Should().BeTrue();
        lifecycle.IsInUseByAnother(NodeId, Player).Should().BeFalse("it is the holder's own node");
        lifecycle.GetActive(OtherPlayer).Should().BeNull();
    }

    [Test]
    public void ADifferentNodeIsNotLocked()
    {
        var lifecycle = Lifecycle(2, 1, 99);
        Open(lifecycle, Node(3, "r"));

        lifecycle.IsInUseByAnother(99, OtherPlayer).Should().BeFalse();
    }

    [Test]
    public void TheNodeIsReleasedWhenTheHolderLeaves_AndTheNextPlayerStartsWhereTheLastOneStopped()
    {
        var lifecycle = Lifecycle(StrikeOneUnit);
        var session = Open(lifecycle, Node(4, "r"));
        Do(lifecycle, session, ProspectingActionIds.Strike);
        Do(lifecycle, session, ProspectingActionIds.Leave);

        lifecycle.GetHolder(NodeId).Should().BeNull();
        lifecycle.IsInUseByAnother(NodeId, OtherPlayer).Should().BeFalse();

        var next = lifecycle.Begin(Request(OtherPlayer, OtherHarvester));

        next.Opened.Should().BeTrue();
        next.Session.Engine.Pool.Should().Be(3, "the unit the first player took is gone");
        next.Session.Engine.Stability.Should().Be(100 - ProspectingRules.StrikeStability, "the node keeps its damage");
        next.Session.Engine.ToNode().Seams.Should().ContainSingle();
    }

    [Test]
    public void AHolderWhoHasGoneDoesNotKeepTheNodeLocked()
    {
        var lifecycle = Lifecycle();
        var session = Open(lifecycle, Node(3, "r"));
        _world.IsValid = false;

        lifecycle.IsInUseByAnother(NodeId, OtherPlayer).Should().BeFalse("a holder who logged out lets go of the node");

        session.Settled.Should().BeTrue();
        session.Abandoned.Should().Be(ProspectingEnd.Left);
        lifecycle.GetHolder(NodeId).Should().BeNull();
    }

    [Test]
    public void BeginningASecondSessionEndsTheFirst()
    {
        var lifecycle = Lifecycle();
        var first = Open(lifecycle, Node(3, "r"));
        _world.SaveNode(44, Node(2, "").Serialize());

        var second = lifecycle.Begin(Request(node: 44));

        second.Opened.Should().BeTrue();
        first.Abandoned.Should().Be(ProspectingEnd.Replaced);
        lifecycle.GetHolder(NodeId).Should().BeNull();
        lifecycle.GetActive(Player).Should().BeSameAs(second.Session);
    }

    [Test]
    public void ASpentNodeIsRemovedAndNotWorked()
    {
        var lifecycle = Lifecycle();
        _world.Nodes[NodeId] = "P0S100:";

        var begin = lifecycle.Begin(Request());

        begin.Status.Should().Be(ProspectingBeginStatus.Spent);
        _world.DestroyedNodes.Should().Equal(NodeId);
        _world.Messages.Should().Contain(ProspectingLifecycle.SpentMessage);
        _world.Opened.Should().BeEmpty();
    }

    [Test]
    public void AHarvesterWithNoChargesCannotBegin()
    {
        var lifecycle = Lifecycle();
        _world.SaveNode(NodeId, Node(3, "r").Serialize());
        _world.Charges[Harvester] = 0;

        var begin = lifecycle.Begin(Request());

        begin.Status.Should().Be(ProspectingBeginStatus.NoCharges);
        _world.Messages.Should().Contain(ProspectingLifecycle.NoChargesMessage);
        lifecycle.GetHolder(NodeId).Should().BeNull();
        _world.Busy.Should().BeFalse();
    }

    [Test]
    public void AWindowThatCannotOpenLeavesNothingHeld()
    {
        var lifecycle = Lifecycle();
        _world.SaveNode(NodeId, Node(3, "r").Serialize());
        _world.CanOpenWindow = false;

        lifecycle.Begin(Request()).Status.Should().Be(ProspectingBeginStatus.WindowFailed);

        lifecycle.GetHolder(NodeId).Should().BeNull();
        lifecycle.GetActive(Player).Should().BeNull();
        _world.Busy.Should().BeFalse();
    }

    [Test]
    public void AWindowThatThrowsLeavesNothingHeld_AndTheErrorSurfaces()
    {
        var lifecycle = Lifecycle();
        _world.SaveNode(NodeId, Node(3, "r").Serialize());
        _world.OpeningTheWindowThrows = true;

        var act = () => lifecycle.Begin(Request());

        act.Should().Throw<InvalidOperationException>();
        lifecycle.GetHolder(NodeId).Should().BeNull();
        _world.Busy.Should().BeFalse();
    }

    // Actions.

    [Test]
    public void AnActionTakesItsSecondsWithItsWorkAnimation_ThenResolves()
    {
        var lifecycle = Lifecycle(StrikeTwoUnits);
        var session = Open(lifecycle, Node(4, "r"));
        ProspectingActionReport report = null;

        var start = lifecycle.StartAction(session, ProspectingActionIds.Strike, r => report = r);

        start.Started.Should().BeTrue();
        _world.Work.Should().Equal((float)ProspectingRules.StrikeSeconds);
        _world.WorkActions.Should().Equal(ProspectingActionIds.Strike); // the world plays the Strike clip for it
        session.Working.Should().Be(ProspectingActionIds.Strike);
        session.Engine.Turn.Should().Be(0);
        report.Should().BeNull();
        _world.RunNextWork((float)ProspectingRules.StrikeSeconds);
        report.Should().NotBeNull();
        session.Working.Should().BeNull();
        session.Engine.Turn.Should().Be(1);
    }

    [Test]
    public void EveryActionTakesTheSecondsTheRulesName()
    {
        ProspectingRules.SecondsFor(ProspectingActionIds.Survey).Should().Be(ProspectingRules.SurveySeconds);
        ProspectingRules.SecondsFor(ProspectingActionIds.Strike).Should().Be(ProspectingRules.StrikeSeconds);
        ProspectingRules.SecondsFor(ProspectingActionIds.HeavyStrike).Should().Be(ProspectingRules.HeavyStrikeSeconds);
        ProspectingRules.SecondsFor(ProspectingActionIds.Extract).Should().Be(ProspectingRules.ExtractSeconds);
        ProspectingRules.SecondsFor(ProspectingActionIds.Leave).Should().Be(0);
    }

    [Test]
    public void AStrikeHandsOverItsUnitsPaysTheirXpAndSpendsItsCharges()
    {
        var lifecycle = Lifecycle(StrikeTwoUnits);
        var session = Open(lifecycle, Node(4, "r"), rank: 25, tier: 3);

        var report = Do(lifecycle, session, ProspectingActionIds.Strike);

        report.Delivered.Should().Be(new HarvestDelivery(2, 0, 0));
        _world.Harvests.Should().Equal(new HarvestDelivery(2, 0, 0));
        report.Xp.Should().Be(2 * XpPerUnit(3, 20), "the rank is the one the world reports now");
        _world.Xp.Should().Equal(report.Xp);
        _world.Charges[Harvester].Should().Be(50 - ProspectingRules.StrikeCharges);
        session.HarvesterCharges.Should().Be(50 - ProspectingRules.StrikeCharges);
        _world.Nodes[NodeId].Should().Be(session.Engine.ToNode().Serialize());
        report.Ended.Should().BeFalse();
    }

    [Test]
    public void AStrikeSavesTheNodeBeforeItPaysTheXp_SoARestartCannotLeavePaidUnitsOnTheNode()
    {
        var lifecycle = Lifecycle(StrikeTwoUnits);
        var session = Open(lifecycle, Node(4, "r"), rank: 25, tier: 3);
        _world.Order.Clear();

        Do(lifecycle, session, ProspectingActionIds.Strike);

        _world.Order.Should().Equal("node", "xp");
    }

    [Test]
    public void ASurveyHandsOverNothing_ButRevealsSeamsAndIsStored()
    {
        var lifecycle = Lifecycle();
        var session = Open(lifecycle, Node(4, "rg"), rank: 0);

        var report = Do(lifecycle, session, ProspectingActionIds.Survey);

        report.Delivered.IsEmpty.Should().BeTrue();
        report.Xp.Should().Be(0);
        report.SeamsFound.Should().Be(1);
        _world.Harvests.Should().BeEmpty();
        _world.Xp.Should().BeEmpty();
        _world.Nodes[NodeId].Should().Be("P4S88:Rg", "a seam that has been found stays found for the next player");
        _world.Charges[Harvester].Should().Be(50 - ProspectingRules.SurveyCharges);
    }

    [Test]
    public void ExtractingARichSeamHandsOverItsUnitAndTheBonusRoll_AndAGemPocketAGem()
    {
        var lifecycle = Lifecycle(RichBonus);
        var session = Open(lifecycle, Node(4, "RG"));

        var gem = Do(lifecycle, session, ProspectingActionIds.Extract);
        var rich = Do(lifecycle, session, ProspectingActionIds.Extract);

        gem.Delivered.Should().Be(new HarvestDelivery(0, 0, 1));
        rich.Delivered.Should().Be(new HarvestDelivery(1, 1, 0));
        _world.Harvests.Should().Equal(new HarvestDelivery(0, 0, 1), new HarvestDelivery(1, 1, 0));
        _world.Xp.Sum().Should().Be(3 * XpPerUnit(3, 20), "a gem and an extra roll pay XP like any other item");
        session.Delivered.Should().Be(new HarvestDelivery(1, 1, 1));
    }

    [Test]
    public void TheXpOfASessionIsTodaysXpForTheUnit_ScaledByTheMinigameMultiplier()
    {
        var lifecycle = Lifecycle(StrikeOneUnit);
        var session = Open(lifecycle, Node(4, ""), tier: 2);
        _world.GatheringRank = 12;

        Do(lifecycle, session, ProspectingActionIds.Strike);

        var expected = CraftMinigameXp.ScaleBaseXp(
            Skill.GetDeltaXP(15 - 12), CraftMinigameXp.MultiplierFor(CraftMinigameType.Prospecting, 15));
        _world.Xp.Should().Equal(expected);
        lifecycle.XpPerItem(session).Should().Be(expected);
        expected.Should().Be(Skill.GetDeltaXP(3), "the multiplier is 1 until the XP-rate gate asks for more");
    }

    [Test]
    public void AnActionTheEngineRefusesCostsNothingAndStartsNothing()
    {
        var lifecycle = Lifecycle();
        var session = Open(lifecycle, Node(4, "r"));

        var start = lifecycle.StartAction(session, ProspectingActionIds.Extract, _ => { });

        start.Started.Should().BeFalse();
        start.Reason.Should().Be(ProspectingEngine.NoRevealedSeam);
        _world.Work.Should().BeEmpty();
        _world.Charges[Harvester].Should().Be(50);
        session.Working.Should().BeNull();
    }

    [Test]
    public void OnlyOneActionRunsAtATime()
    {
        var lifecycle = Lifecycle(StrikeOneUnit);
        var session = Open(lifecycle, Node(4, "r"));
        lifecycle.StartAction(session, ProspectingActionIds.Strike, _ => { }).Started.Should().BeTrue();

        var second = lifecycle.StartAction(session, ProspectingActionIds.Survey, _ => { });
        var quick = lifecycle.StartQuickHarvest(session, _ => { });

        second.Started.Should().BeFalse();
        second.Reason.Should().Be(ProspectingLifecycle.StillWorkingMessage);
        quick.Started.Should().BeFalse();
        _world.Work.Should().HaveCount(1);
    }

    [Test]
    public void TheLastUnitSpendsTheNode_RemovesIt_AndEndsTheSession()
    {
        var lifecycle = Lifecycle();
        var session = Open(lifecycle, Node(1, ""));

        var report = Do(lifecycle, session, ProspectingActionIds.Strike);

        report.Ended.Should().BeTrue();
        report.Message.Should().Be(ProspectingLifecycle.MinedOutMessage);
        session.Settled.Should().BeTrue();
        session.Finished.Should().Be(ProspectingEnding.MinedOut);
        _world.DestroyedNodes.Should().Equal(NodeId);
        _world.Nodes.Should().NotContainKey(NodeId);
        lifecycle.GetHolder(NodeId).Should().BeNull();
        lifecycle.GetActive(Player).Should().BeNull();
        _world.Busy.Should().BeFalse();
        _world.Messages.Should().Contain(ProspectingLifecycle.MinedOutMessage);
        _world.Harvests.Should().ContainSingle("what was gathered is handed over before the node is removed").Which.Should().Be(new HarvestDelivery(1, 0, 0));
    }

    [Test]
    public void ACollapseRemovesTheNodeAndKeepsWhatWasGathered()
    {
        var lifecycle = Lifecycle(StrikeOneUnit);
        var session = Open(lifecycle, ProspectingTestData.Node(4, "r", stability: ProspectingRules.StrikeStability));

        var report = Do(lifecycle, session, ProspectingActionIds.Strike);

        report.Ended.Should().BeTrue();
        report.Message.Should().Be(ProspectingLifecycle.CollapsedMessage);
        session.Finished.Should().Be(ProspectingEnding.Collapsed);
        _world.DestroyedNodes.Should().Equal(NodeId);
        _world.Harvests.Should().Equal(new HarvestDelivery(1, 0, 0));
        _world.Xp.Should().HaveCount(1);
    }

    [Test]
    public void LeavingEndsTheSessionAtOnce_AndLeavesTheRestOfTheNodeStored()
    {
        var lifecycle = Lifecycle(StrikeOneUnit);
        var session = Open(lifecycle, Node(4, "rg"));
        Do(lifecycle, session, ProspectingActionIds.Strike);
        _world.Work.Clear();

        var report = Do(lifecycle, session, ProspectingActionIds.Leave);

        report.Ended.Should().BeTrue();
        session.Finished.Should().Be(ProspectingEnding.Left);
        _world.Work.Should().BeEmpty("Leave takes no seconds");
        _world.Nodes[NodeId].Should().Be(session.Engine.ToNode().Serialize());
        _world.DestroyedNodes.Should().BeEmpty();
        _world.Busy.Should().BeFalse();
    }

    [Test]
    public void TheLastChargeUsesUpTheHarvester_AndTheNodeKeepsWhatIsLeft()
    {
        var lifecycle = Lifecycle(StrikeOneUnit);
        _world.Charges[Harvester] = 1;
        var session = Open(lifecycle, Node(4, "r"));

        var report = Do(lifecycle, session, ProspectingActionIds.Strike);

        report.Ended.Should().BeTrue();
        session.Finished.Should().Be(ProspectingEnding.OutOfCharges);
        _world.DestroyedHarvesters.Should().Equal(Harvester);
        _world.Charges.Should().NotContainKey(Harvester);
        _world.Nodes[NodeId].Should().Be("P3S80:r");
        _world.DestroyedNodes.Should().BeEmpty();
    }

    [Test]
    public void AnActionThatCostsMoreChargesThanLeftIsRefused()
    {
        var lifecycle = Lifecycle();
        _world.Charges[Harvester] = 1;
        var session = Open(lifecycle, Node(4, "R"));

        var start = lifecycle.StartAction(session, ProspectingActionIds.HeavyStrike, _ => { });

        start.Started.Should().BeFalse();
        start.Reason.Should().Be(ProspectingEngine.NotEnoughCharges);
    }

    // Leaving without finishing.

    [TestCase("moved", ProspectingEnd.Moved)]
    [TestCase("combat", ProspectingEnd.Combat)]
    [TestCase("dead", ProspectingEnd.Died)]
    [TestCase("gone", ProspectingEnd.Left)]
    [TestCase("harvester", ProspectingEnd.HarvesterLost)]
    [TestCase("busy", ProspectingEnd.Interrupted)]
    [TestCase("window", ProspectingEnd.WindowClosed)]
    [TestCase("node", ProspectingEnd.NodeGone)]
    public void ASessionTheStateNoLongerSupportsEndsAndReleasesTheNode(string cause, ProspectingEnd expected)
    {
        var lifecycle = Lifecycle(StrikeOneUnit);
        var session = Open(lifecycle, Node(4, "r"));
        Do(lifecycle, session, ProspectingActionIds.Strike);
        switch (cause)
        {
            case "moved": _world.Position = new Vector3(11f, 20f, 0f); break;
            case "combat": _world.IsInCombat = true; break;
            case "dead": _world.IsDead = true; break;
            case "gone": _world.IsValid = false; break;
            case "harvester": _world.HasHarvester = false; break;
            case "busy": _world.OwnsActivity = false; break;
            case "window": _world.WindowOpen = false; break;
            case "node": _world.LiveNodes.Remove(NodeId); break;
        }

        lifecycle.Revalidate(session).Should().BeFalse();

        session.Settled.Should().BeTrue();
        session.Abandoned.Should().Be(expected);
        lifecycle.GetHolder(NodeId).Should().BeNull();
        lifecycle.GetActive(Player).Should().BeNull();
        _world.Busy.Should().BeFalse();
        _world.Harvests.Should().ContainSingle("what was gathered stays gathered").Which.Should().Be(new HarvestDelivery(1, 0, 0));
        if (cause != "node")
            _world.Nodes[NodeId].Should().Be(session.Engine.ToNode().Serialize(), "the node keeps what is left");
    }

    [Test]
    public void AnActionThatFindsThePlayerGoneResolvesNothing()
    {
        var lifecycle = Lifecycle(StrikeOneUnit);
        var session = Open(lifecycle, Node(4, "r"));
        ProspectingActionReport report = null;
        lifecycle.StartAction(session, ProspectingActionIds.Strike, r => report = r);
        _world.Position = new Vector3(0f, 0f, 0f);

        _world.RunNextWork((float)ProspectingRules.StrikeSeconds);

        report.Should().BeNull();
        session.Engine.Turn.Should().Be(0);
        session.Abandoned.Should().Be(ProspectingEnd.Moved);
        _world.Harvests.Should().BeEmpty();
        _world.Charges[Harvester].Should().Be(50, "an action that never resolved costs nothing");
    }

    [Test]
    public void ClosingTheWindowEndsTheSessionOnce_AndATwiceCalledAbandonDoesNothingMore()
    {
        var lifecycle = Lifecycle();
        var session = Open(lifecycle, Node(4, "r"));
        _world.WindowOpen = false;

        lifecycle.Abandon(session, ProspectingEnd.WindowClosed);
        _world.Messages.Clear();
        lifecycle.Abandon(session, ProspectingEnd.Died);

        session.Abandoned.Should().Be(ProspectingEnd.WindowClosed);
        _world.Messages.Should().BeEmpty();
    }

    [Test]
    public void AbandoningCloseTheWindowUnlessItWasTheWindowThatClosed()
    {
        var lifecycle = Lifecycle();
        var session = Open(lifecycle, Node(4, "r"));

        lifecycle.Abandon(session, ProspectingEnd.Moved);

        _world.WindowOpen.Should().BeFalse();
        _world.Messages.Should().Contain(message => message.Contains("moved"));
    }

    [Test]
    public void AnEndedSessionRefusesEveryClick()
    {
        var lifecycle = Lifecycle();
        var session = Open(lifecycle, Node(1, ""));
        Do(lifecycle, session, ProspectingActionIds.Strike);

        lifecycle.StartAction(session, ProspectingActionIds.Survey, _ => { }).Started.Should().BeFalse();
        lifecycle.StartQuickHarvest(session, _ => { }).Started.Should().BeFalse();
        lifecycle.Revalidate(session).Should().BeFalse();
    }

    [Test]
    public void AFailedStateCheckIsCaughtByThePeriodicCheck()
    {
        var lifecycle = Lifecycle();
        var session = Open(lifecycle, Node(4, "r"));
        _world.ScheduledSeconds.Should().Contain(ProspectingLifecycle.CheckSeconds);

        _world.IsInCombat = true;
        _world.RunNextWork(ProspectingLifecycle.CheckSeconds);

        session.Abandoned.Should().Be(ProspectingEnd.Combat);
        _world.ScheduledCount.Should().Be(0, "a session that is over stops checking");
    }

    // Idle windows.

    [Test]
    public void AWindowLeftOpenWithoutAnActionLetsGoOfTheNode_SoNobodyIsLockedOutByAnAwayPlayer()
    {
        var lifecycle = Lifecycle();
        var session = Open(lifecycle, Node(4, "r"));
        _world.UtcNow += TimeSpan.FromSeconds(ProspectingLifecycle.IdleSeconds - 1);

        lifecycle.IsInUseByAnother(NodeId, OtherPlayer).Should().BeTrue("the window was touched less than the limit ago");
        _world.UtcNow += TimeSpan.FromSeconds(2);

        lifecycle.IsInUseByAnother(NodeId, OtherPlayer).Should().BeFalse();
        session.Abandoned.Should().Be(ProspectingEnd.Idle);
        _world.Messages.Should().Contain(message => message.Contains("idle"));
        lifecycle.Begin(Request(OtherPlayer, OtherHarvester)).Opened.Should().BeTrue();
    }

    [Test]
    public void AnActionStartsTheIdleClockAgain()
    {
        var lifecycle = Lifecycle(StrikeOneUnit);
        var session = Open(lifecycle, Node(4, "r"));
        _world.UtcNow += TimeSpan.FromSeconds(ProspectingLifecycle.IdleSeconds - 5);
        Do(lifecycle, session, ProspectingActionIds.Strike);

        _world.UtcNow += TimeSpan.FromSeconds(ProspectingLifecycle.IdleSeconds - 5);

        lifecycle.Revalidate(session).Should().BeTrue();
        _world.UtcNow += TimeSpan.FromSeconds(10);
        lifecycle.Revalidate(session).Should().BeFalse();
        session.Abandoned.Should().Be(ProspectingEnd.Idle);
    }

    [Test]
    public void AnActionInProgressAndAQuickHarvestAreNeverIdle()
    {
        var lifecycle = Lifecycle(StrikeOneUnit);
        var working = Open(lifecycle, Node(4, "r"));
        lifecycle.StartAction(working, ProspectingActionIds.Strike, _ => { });
        _world.UtcNow += TimeSpan.FromSeconds(ProspectingLifecycle.IdleSeconds * 2);

        lifecycle.Revalidate(working).Should().BeTrue("an action is taking its seconds");
        lifecycle.Abandon(working, ProspectingEnd.WindowClosed);

        _world.SaveNode(44, Node(4, "").Serialize());
        var quick = lifecycle.Begin(Request(node: 44)).Session;
        lifecycle.StartQuickHarvest(quick, _ => { });
        _world.UtcNow += TimeSpan.FromSeconds(ProspectingLifecycle.IdleSeconds * 2);

        lifecycle.Revalidate(quick).Should().BeTrue("a Quick Harvest takes as long as it takes");
    }

    // Quick Harvest.

    [Test]
    public void QuickHarvestTakesTheWholePool_OneHarvestDelayAndOneChargeForEachUnit()
    {
        foreach (var pool in new[] { 1, 2, 3, 4 })
        {
            SetUp();
            var lifecycle = Lifecycle();
            _world.SaveNode(NodeId, Node(pool, "rg".Substring(0, Math.Min(pool, 2))).Serialize());
            var session = lifecycle.Begin(Request(rank: 25, tier: 3)).Session;
            var reports = new List<QuickHarvestReport>();

            lifecycle.StartQuickHarvest(session, reports.Add).Started.Should().BeTrue();
            for (var unit = 0; unit < pool; unit++)
                _world.RunNextWork((float)ProspectingRules.HarvestSeconds);

            reports.Should().HaveCount(pool, $"pool {pool}");
            reports.Last().Finished.Should().BeTrue();
            _world.Harvests.Should().OnlyContain(harvest => harvest.Equals(new HarvestDelivery(1, 0, 0)), "one plain roll for each unit, as every harvest was");
            _world.Harvests.Should().HaveCount(pool);
            _world.Work.Should().OnlyContain(seconds => seconds == (float)ProspectingRules.HarvestSeconds);
            _world.Work.Should().HaveCount(pool, "one harvest delay and animation for each unit");
            _world.WorkActions.Should().OnlyContain(action => action == null, "a Quick Harvest unit has no authored clip");
            _world.Xp.Should().OnlyContain(xp => xp == XpPerUnit(3, 20));
            _world.Xp.Sum().Should().Be(pool * XpPerUnit(3, 20));
            (50 - _world.Charges[Harvester]).Should().Be(pool, "one charge for each unit");
            _world.DestroyedNodes.Should().Equal(NodeId);
            session.Settled.Should().BeTrue();
            session.Finished.Should().Be(ProspectingEnding.MinedOut);
        }
    }

    [Test]
    public void QuickHarvestPaysExactlyWhatEachOfTodaysHarvestsPaid_AtTheAttributeReference()
    {
        // Today a node held d4 harvests, each one item (a Might modifier of 0 adds none) and Skill.GetDeltaXP(vein level - rank) XP.
        var lifecycle = Lifecycle();
        _world.SaveNode(NodeId, Node(3, "r").Serialize());
        var session = lifecycle.Begin(Request(tier: 4, rank: 33)).Session;
        _world.GatheringRank = 33;

        lifecycle.StartQuickHarvest(session, _ => { });
        for (var unit = 0; unit < 3; unit++)
            _world.RunNextWork((float)ProspectingRules.HarvestSeconds);

        _world.ItemsHarvested.Should().Be(3);
        _world.Xp.Sum().Should().Be(3 * Skill.GetDeltaXP(10 * (4 - 1) + 5 - 33));
        ProspectingQuickHarvest.Seconds(3).Should().Be(3 * 5.0);
        ProspectingQuickHarvest.Charges(3).Should().Be(3);
    }

    [Test]
    public void QuickHarvestIsLimitedByTheHarvestersCharges_AndUsesTheHarvesterUp()
    {
        var lifecycle = Lifecycle();
        _world.Charges[Harvester] = 2;
        _world.SaveNode(NodeId, Node(4, "rg").Serialize());
        var session = lifecycle.Begin(Request()).Session;
        var reports = new List<QuickHarvestReport>();

        lifecycle.StartQuickHarvest(session, reports.Add).Started.Should().BeTrue();
        session.QuickHarvestTotal.Should().Be(2);
        _world.RunNextWork((float)ProspectingRules.HarvestSeconds);
        _world.RunNextWork((float)ProspectingRules.HarvestSeconds);

        reports.Last().Finished.Should().BeTrue();
        reports.Last().Message.Should().Be(ProspectingLifecycle.OutOfChargesMessage);
        _world.DestroyedHarvesters.Should().Equal(Harvester);
        _world.Nodes[NodeId].Should().Be("P2S100:rg");
        _world.DestroyedNodes.Should().BeEmpty("the pool is not empty");
        session.Finished.Should().Be(ProspectingEnding.OutOfCharges);
    }

    [Test]
    public void QuickHarvestIgnoresSeams_AndALeftOverNodeKeepsOnlyWhatItsPoolHolds()
    {
        var lifecycle = Lifecycle();
        _world.Charges[Harvester] = 3;
        _world.SaveNode(NodeId, Node(4, "rgg").Serialize());
        var session = lifecycle.Begin(Request()).Session;

        lifecycle.StartQuickHarvest(session, _ => { });
        for (var unit = 0; unit < 3; unit++)
            _world.RunNextWork((float)ProspectingRules.HarvestSeconds);

        _world.Harvests.Should().OnlyContain(harvest => harvest.ExtraOreRolls == 0 && harvest.Gems == 0, "Quick Harvest never opens a seam");
        ProspectingNode.TryParse(_world.Nodes[NodeId], out var left).Should().BeTrue();
        left.Pool.Should().Be(1);
        left.Seams.Should().HaveCount(1, "a seam cannot seal a unit that is gone");
    }

    [Test]
    public void QuickHarvestIsOfferedOnlyBeforeTheSessionBegins()
    {
        var lifecycle = Lifecycle(StrikeOneUnit);
        var session = Open(lifecycle, Node(4, "r"));
        Do(lifecycle, session, ProspectingActionIds.Survey);

        var start = lifecycle.StartQuickHarvest(session, _ => { });

        start.Started.Should().BeFalse();
        start.Reason.Should().Be(ProspectingLifecycle.QuickHarvestTooLateMessage);
        session.HasActed.Should().BeTrue();
    }

    [Test]
    public void AQuickHarvestInterruptedKeepsWhatItTookAndLeavesTheRest()
    {
        var lifecycle = Lifecycle();
        _world.SaveNode(NodeId, Node(4, "").Serialize());
        var session = lifecycle.Begin(Request()).Session;
        lifecycle.StartQuickHarvest(session, _ => { });
        _world.RunNextWork((float)ProspectingRules.HarvestSeconds);

        _world.Position = new Vector3(0f, 0f, 0f);
        _world.RunNextWork((float)ProspectingRules.HarvestSeconds);

        _world.ItemsHarvested.Should().Be(1);
        session.Abandoned.Should().Be(ProspectingEnd.Moved);
        _world.Nodes[NodeId].Should().Be("P3S100:");
        _world.Charges[Harvester].Should().Be(49);
        lifecycle.GetHolder(NodeId).Should().BeNull();
    }

    [Test]
    public void ANodeBeingQuickHarvestedIsHeldLikeAnyOther()
    {
        var lifecycle = Lifecycle();
        _world.SaveNode(NodeId, Node(4, "").Serialize());
        var session = lifecycle.Begin(Request()).Session;
        lifecycle.StartQuickHarvest(session, _ => { });

        lifecycle.IsInUseByAnother(NodeId, OtherPlayer).Should().BeTrue();
        lifecycle.Begin(Request(OtherPlayer, OtherHarvester)).Status.Should().Be(ProspectingBeginStatus.InUse);
    }

    [Test]
    public void QuickHarvestUnitsAreThePoolLimitedByCharges_AndTheRulesAreTodaysHarvests()
    {
        ProspectingQuickHarvest.Units(Node(4, "rg"), 50).Should().Be(4);
        ProspectingQuickHarvest.Units(Node(4, "rg"), 3).Should().Be(3);
        ProspectingQuickHarvest.Units(Node(4, "rg"), 0).Should().Be(0);
        ProspectingQuickHarvest.Units(Node(1, "r"), 9).Should().Be(1);
        ProspectingRules.HarvestSeconds.Should().Be(5.0, "the harvester's Delay(5f) of today");
        ProspectingQuickHarvest.XpPerUnit(1, 0).Should().Be(1050);
        ProspectingQuickHarvest.XpPerUnit(2, 5).Should().Be(1200);
        ProspectingQuickHarvest.XpPerUnit(5, 45).Should().Be(600);
        ProspectingQuickHarvest.Xp(3, 3, 25).Should().Be(3 * 600);
    }
}
