using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.ProspectingService;
using static SWLOR.Game.Server.Tests.Service.Prospecting.ProspectingTestData;

namespace SWLOR.Game.Server.Tests.Service.Prospecting;

/// <summary>
/// Covers rebuilding a session's engine with the harvester's perks and style as they are now, which the window does when the style
/// is chosen: allowed until the first action, and never after it, while an action is working, or once the session is over.
/// </summary>
public class ProspectingRefreshEffectsTests
{
    private const uint Player = 11;
    private const uint Harvester = 22;
    private const uint NodeId = 33;
    private const int Rank = 25;
    private const int NodeTier = 3;
    private const int HarvesterCharges = 50;

    private static readonly Vector3 Position = new(10f, 20f, 0f);

    private static readonly ProspectingEffects StripMining = ProspectingEffects.None with
    {
        StrikeStabilityReduction = -ProspectingPerkRules.StripMiningStrikeStability,
        StrikeTimeReductionTenths = ProspectingPerkRules.StripMiningStrikeTimeTenths,
        SeamWorkTimeReductionTenths = -ProspectingPerkRules.StripMiningSeamWorkTimeTenths
    };

    private static readonly ProspectingEffects WithMotherLode = ProspectingEffects.None with { MotherLode = true };

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
        _world.Charges[Harvester] = HarvesterCharges;
    }

    private static ProspectingRequest Request(ProspectingEffects effects = null) =>
        new(Player, Harvester, NodeId, "HARVESTING_PLAGIONITE", NodeTier, Rank, Position, effects);

    private ProspectingLifecycle Lifecycle(params int[] rolls) => new(_world, new ScriptedMinigameRandom(rolls));

    private ProspectingSession Open(ProspectingLifecycle lifecycle, ProspectingNode node, ProspectingEffects effects = null)
    {
        _world.SaveNode(NodeId, node.Serialize());
        var begin = lifecycle.Begin(Request(effects));
        begin.Status.Should().Be(ProspectingBeginStatus.Opened);
        return begin.Session;
    }

    private ProspectingActionReport Do(ProspectingLifecycle lifecycle, ProspectingSession session, string action)
    {
        ProspectingActionReport report = null;
        var seconds = session.Engine.SecondsFor(action);
        var start = lifecycle.StartAction(session, action, r => report = r);
        start.Started.Should().BeTrue($"{action}: {start.Reason}");
        if (seconds > 0)
        {
            report.Should().BeNull("the action takes its seconds before it resolves");
            _world.RunNextWork((float)seconds);
        }

        report.Should().NotBeNull();
        return report;
    }

    // Before the first action.

    [Test]
    public void BeforeTheFirstAction_TheEngineIsSwapped_AndTheNodeChargesAndRankStay()
    {
        var lifecycle = Lifecycle();
        var node = Node(3, "rG", 90);
        var session = Open(lifecycle, node);
        var engineBefore = session.Engine;
        var requestBefore = session.Request;

        var refreshed = lifecycle.RefreshEffects(session, StripMining);

        refreshed.Should().BeTrue();
        session.Engine.Should().NotBeSameAs(engineBefore);
        session.Engine.ToNode().Should().Be(node);
        session.Engine.Turn.Should().Be(0);
        session.Engine.Charges.Should().Be(HarvesterCharges);
        session.Engine.ChargesAtStart.Should().Be(HarvesterCharges);
        session.Engine.Setup.Rank.Should().Be(Rank);
        session.Engine.Setup.NodeTier.Should().Be(NodeTier);
        session.Engine.Setup.YieldTier.Should().Be(CraftRankMilestones.ProspectingYieldTierForRank(Rank));
        session.Engine.Setup.SurveyReveals.Should().Be(CraftRankMilestones.ProspectingSurveyRevealsForRank(Rank));
        session.Engine.Setup.Effects.Should().Be(StripMining);
        session.Request.Effects.Should().Be(StripMining);
        (session.Request with { Effects = requestBefore.Effects }).Should().Be(requestBefore, "only the effects of the request change");
    }

    [Test]
    public void ARefreshTouchesNothingOutsideTheSession()
    {
        var lifecycle = Lifecycle();
        var session = Open(lifecycle, Node(3, "rG", 90));
        var stored = _world.Nodes[NodeId];
        var messages = _world.Messages.ToList();

        lifecycle.RefreshEffects(session, StripMining).Should().BeTrue();

        lifecycle.GetActive(Player).Should().BeSameAs(session);
        lifecycle.GetHolder(NodeId).Should().BeSameAs(session);
        lifecycle.Revalidate(session).Should().BeTrue();
        session.Settled.Should().BeFalse();
        session.HarvesterCharges.Should().Be(HarvesterCharges);
        _world.Charges[Harvester].Should().Be(HarvesterCharges);
        _world.Nodes[NodeId].Should().Be(stored);
        _world.Messages.Should().Equal(messages);
        _world.Opened.Should().Equal(session);
        _world.Busy.Should().BeTrue();
        _world.Work.Should().BeEmpty();
        _world.Harvests.Should().BeEmpty();
    }

    [Test]
    public void TheNewEnginePlaysByTheNewEffects_WithTheLifecyclesOwnRolls()
    {
        var lifecycle = Lifecycle(StrikeTwoUnits);
        var session = Open(lifecycle, Node(4, "r"));
        var strikeCost = ProspectingRules.StrikeStability + ProspectingPerkRules.StripMiningStrikeStability;
        lifecycle.RefreshEffects(session, StripMining).Should().BeTrue();

        var report = Do(lifecycle, session, ProspectingActionIds.Strike);

        _world.Work.Should().Equal(
            (float)(ProspectingRules.StrikeSeconds - ProspectingPerkRules.StripMiningStrikeTimeTenths / 10.0));
        report.Delivered.Should().Be(new HarvestDelivery(2, 0, 0));
        session.Engine.Units.Should().Be(2);
        session.Engine.Stability.Should().Be(100 - strikeCost);
        _world.Nodes[NodeId].Should().Be(session.Engine.ToNode().Serialize());
        _world.Charges[Harvester].Should().Be(HarvesterCharges - ProspectingRules.StrikeCharges);
    }

    [Test]
    public void ARefreshedMotherLodeMakesTheFirstStrikeQuickAndTheNextOneUsual()
    {
        var lifecycle = Lifecycle(StrikeOneUnit, StrikeOneUnit);
        var session = Open(lifecycle, Node(4, ""));
        lifecycle.RefreshEffects(session, WithMotherLode).Should().BeTrue();

        Do(lifecycle, session, ProspectingActionIds.Strike);
        Do(lifecycle, session, ProspectingActionIds.Strike);

        _world.Work.Should().Equal(
            (float)(ProspectingRules.StrikeSeconds * ProspectingPerkRules.MotherLodeTimePercent / 100.0),
            (float)ProspectingRules.StrikeSeconds);
    }

    [Test]
    public void TheNewEnginesSetupCarriesEveryNewEffect()
    {
        var effects = new ProspectingEffects(
            StrikeStabilityReduction: 5,
            StrikeTimeReductionTenths: 10,
            SeamWorkTimeReductionTenths: 3,
            HeavyStrikeTimeReductionTenths: 20,
            RichExtractTimeReductionTenths: 5,
            SeesSeamTypes: true,
            MotherLode: true,
            ShatterReduction: 10,
            SurveyReveals: 1,
            PocketsFirst: true);
        var lifecycle = Lifecycle();
        var session = Open(lifecycle, Node(4, "rgr"));

        lifecycle.RefreshEffects(session, effects).Should().BeTrue();

        var engine = session.Engine;
        engine.Setup.Effects.Should().Be(effects);
        engine.Setup.StrikeStability.Should().Be(ProspectingRules.StrikeStability - 5);
        engine.Setup.ShatterPercent.Should().Be(ProspectingRules.ShatterPercent - 10);
        engine.Setup.SurveyReveals.Should().Be(
            Math.Min(CraftRankMilestones.MaxProspectingSurveyReveals, CraftRankMilestones.ProspectingSurveyRevealsForRank(Rank) + 1));
        engine.SeenHiddenSeams.Should().Equal(ProspectingSeamKind.GemPocket, ProspectingSeamKind.Rich, ProspectingSeamKind.Rich);
        engine.SecondsFor(ProspectingActionIds.Strike).Should().BeApproximately(
            (ProspectingRules.StrikeSeconds - 1.0) * ProspectingPerkRules.MotherLodeTimePercent / 100.0, 1e-9);
        engine.SecondsFor(ProspectingActionIds.HeavyStrike).Should().BeApproximately(
            (ProspectingRules.HeavyStrikeSeconds - 2.0) * ProspectingPerkRules.MotherLodeTimePercent / 100.0, 1e-9);
        engine.SecondsFor(ProspectingActionIds.Survey).Should().BeApproximately(ProspectingRules.SurveySeconds - 0.3, 1e-9);
    }

    [Test]
    public void TheEffectsMayChangeAgainBeforeTheFirstAction_BackToNoneToo()
    {
        var lifecycle = Lifecycle();
        var session = Open(lifecycle, Node(4, "r"), StripMining);
        session.Engine.Setup.StrikeStability.Should().Be(
            ProspectingRules.StrikeStability + ProspectingPerkRules.StripMiningStrikeStability, "the session began with the style");

        lifecycle.RefreshEffects(session, ProspectingEffects.None).Should().BeTrue();

        session.Engine.Setup.Effects.Should().Be(ProspectingEffects.None);
        session.Engine.Setup.StrikeStability.Should().Be(ProspectingRules.StrikeStability);
        session.Engine.SecondsFor(ProspectingActionIds.Strike).Should().Be(ProspectingRules.StrikeSeconds);
        session.Request.Effects.Should().Be(ProspectingEffects.None);

        lifecycle.RefreshEffects(session, StripMining).Should().BeTrue();

        session.Engine.Setup.Effects.Should().Be(StripMining);
        session.Engine.ToNode().Should().Be(Node(4, "r"));
    }

    [Test]
    public void ARefreshWithNoEffectsGivesTheEngineNone()
    {
        var lifecycle = Lifecycle();
        var session = Open(lifecycle, Node(4, "r"), StripMining);

        lifecycle.RefreshEffects(session, null).Should().BeTrue();

        session.Engine.Setup.Effects.Should().BeSameAs(ProspectingEffects.None);
        session.Request.Effects.Should().BeNull();
    }

    [Test]
    public void ASessionIsBuiltFromTheEffectsOfItsRequest()
    {
        var lifecycle = Lifecycle();
        var styled = Open(lifecycle, Node(4, "r"), StripMining);

        styled.Engine.Setup.Effects.Should().Be(StripMining);
        styled.Request.Effects.Should().Be(StripMining);

        lifecycle.Abandon(styled, ProspectingEnd.WindowClosed);
        var plain = lifecycle.Begin(Request()).Session;

        plain.Engine.Setup.Effects.Should().BeSameAs(ProspectingEffects.None);
    }

    // Once the session has acted, is working, or is over.

    [TestCase(ProspectingActionIds.Survey)]
    [TestCase(ProspectingActionIds.Strike)]
    public void AfterAnAction_RefreshReturnsFalse_AndChangesNothing(string action)
    {
        var lifecycle = Lifecycle(StrikeOneUnit);
        var session = Open(lifecycle, Node(4, "rg"));
        Do(lifecycle, session, action);
        var engine = session.Engine;
        var request = session.Request;
        var stored = _world.Nodes[NodeId];
        var turn = engine.Turn;

        lifecycle.RefreshEffects(session, StripMining).Should().BeFalse();

        session.HasActed.Should().BeTrue();
        session.Engine.Should().BeSameAs(engine);
        session.Request.Should().BeSameAs(request);
        session.Engine.Setup.Effects.Should().Be(ProspectingEffects.None);
        session.Engine.Turn.Should().Be(turn);
        session.Settled.Should().BeFalse();
        _world.Nodes[NodeId].Should().Be(stored);
    }

    [Test]
    public void WhileAnActionIsWorking_RefreshReturnsFalse_AndTheActionResolvesWithTheEffectsItBeganWith()
    {
        var lifecycle = Lifecycle(StrikeOneUnit);
        var session = Open(lifecycle, Node(4, "r"));
        ProspectingActionReport report = null;
        lifecycle.StartAction(session, ProspectingActionIds.Strike, r => report = r).Started.Should().BeTrue();
        session.Working.Should().Be(ProspectingActionIds.Strike);
        session.HasActed.Should().BeFalse("the engine has not resolved anything yet");
        var engine = session.Engine;

        lifecycle.RefreshEffects(session, StripMining).Should().BeFalse();

        session.Engine.Should().BeSameAs(engine);
        session.Request.Effects.Should().BeNull();
        _world.RunNextWork((float)ProspectingRules.StrikeSeconds);
        report.Should().NotBeNull();
        session.Engine.Stability.Should().Be(100 - ProspectingRules.StrikeStability);
    }

    [Test]
    public void ASessionAbandonedBeforeActingReturnsFalse()
    {
        var lifecycle = Lifecycle();
        var session = Open(lifecycle, Node(4, "r"));
        lifecycle.Abandon(session, ProspectingEnd.WindowClosed);
        var engine = session.Engine;

        lifecycle.RefreshEffects(session, StripMining).Should().BeFalse();

        session.Settled.Should().BeTrue();
        session.HasActed.Should().BeFalse();
        session.Engine.Should().BeSameAs(engine);
        session.Request.Effects.Should().BeNull();
    }

    [Test]
    public void ASessionThatEndedOnItsOwnReturnsFalse()
    {
        var lifecycle = Lifecycle();
        var session = Open(lifecycle, Node(1, ""));
        Do(lifecycle, session, ProspectingActionIds.Strike);
        var engine = session.Engine;

        lifecycle.RefreshEffects(session, StripMining).Should().BeFalse();

        session.Settled.Should().BeTrue();
        session.Finished.Should().Be(ProspectingEnding.MinedOut);
        session.Engine.Should().BeSameAs(engine);
    }

    [Test]
    public void ASessionRunningAQuickHarvestReturnsFalse()
    {
        var lifecycle = Lifecycle();
        var session = Open(lifecycle, Node(4, ""));
        lifecycle.StartQuickHarvest(session, _ => { }).Started.Should().BeTrue();
        var engine = session.Engine;

        lifecycle.RefreshEffects(session, StripMining).Should().BeFalse();

        session.QuickHarvesting.Should().BeTrue();
        session.Engine.Should().BeSameAs(engine);
        session.Engine.Setup.Effects.Should().Be(ProspectingEffects.None);
    }

    [Test]
    public void RefreshingNeedsASession()
    {
        var lifecycle = Lifecycle();

        var act = () => lifecycle.RefreshEffects(null, StripMining);

        act.Should().Throw<ArgumentNullException>();
    }
}
