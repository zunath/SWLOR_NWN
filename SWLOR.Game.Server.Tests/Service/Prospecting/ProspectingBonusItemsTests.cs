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
/// The extra item a finished harvest can gain from the harvester's city (Prospectors' Charter, Home Ground): when the rolls are made,
/// what each success adds to the delivery, and that the extra items pay no Gathering XP.
/// </summary>
public class ProspectingBonusItemsTests
{
    private const uint Player = 11;
    private const uint Harvester = 22;
    private const uint NodeId = 33;

    private FakeProspectingWorld _world;
    private ScriptedMinigameRandom _random;

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
    }

    private ProspectingLifecycle Lifecycle(params int[] rolls)
    {
        _random = new ScriptedMinigameRandom(rolls);
        return new ProspectingLifecycle(_world, _random);
    }

    private ProspectingSession Open(ProspectingLifecycle lifecycle, ProspectingNode node)
    {
        _world.SaveNode(NodeId, node.Serialize());
        var request = new ProspectingRequest(Player, Harvester, NodeId, "HARVESTING_PLAGIONITE", 3, 25, new Vector3(10f, 20f, 0f));
        var begin = lifecycle.Begin(request);
        begin.Status.Should().Be(ProspectingBeginStatus.Opened);
        return begin.Session;
    }

    private ProspectingActionReport Do(ProspectingLifecycle lifecycle, ProspectingSession session, string action)
    {
        ProspectingActionReport report = null;
        lifecycle.StartAction(session, action, r => report = r).Started.Should().BeTrue();
        var seconds = ProspectingRules.SecondsFor(action);
        if (seconds > 0)
            _world.RunNextWork((float)seconds);

        report.Should().NotBeNull();
        return report;
    }

    private static int XpPerUnit(int tier, int rank) => Skill.GetDeltaXP(GatheringLevels.ForTier(tier) - rank);

    private void RunQuickHarvest(ProspectingLifecycle lifecycle, ProspectingSession session, int units)
    {
        lifecycle.StartQuickHarvest(session, _ => { }).Started.Should().BeTrue();
        for (var unit = 0; unit < units; unit++)
            _world.RunNextWork((float)ProspectingRules.HarvestSeconds);
    }

    // Quick Harvest

    [Test]
    public void AQuickHarvestRollsOnlyOnItsLastUnit_AndBothSuccessesAddTwoItems()
    {
        var lifecycle = Lifecycle(10, 5);
        _world.BonusChances = new HarvestBonusChances(10, 5);
        var session = Open(lifecycle, Node(3, ""));

        RunQuickHarvest(lifecycle, session, 3);

        _world.Harvests.Should().Equal(new HarvestDelivery(1, 0, 0), new HarvestDelivery(1, 0, 0), new HarvestDelivery(3, 0, 0));
        _random.Calls.Should().Be(2, "only the unit that finished the Quick Harvest rolls, once for each chance");
        _world.Messages.Should().Contain(ProspectingLifecycle.BonusItemsMessage);
    }

    [Test]
    public void TheExtraItemsOfAQuickHarvestPayNoGatheringXp()
    {
        var lifecycle = Lifecycle(1, 100);
        _world.BonusChances = new HarvestBonusChances(10, 5);
        var session = Open(lifecycle, Node(2, ""));

        RunQuickHarvest(lifecycle, session, 2);

        _world.Harvests.Last().Should().Be(new HarvestDelivery(2, 0, 0), "the last unit and the Prospectors' Charter item");
        _world.Xp.Should().Equal(XpPerUnit(3, 20), XpPerUnit(3, 20));
        _world.Messages.Should().Contain(ProspectingLifecycle.BonusItemMessage);
        _world.Reserved.Should().BeEmpty();
    }

    [Test]
    public void ARollAboveTheChanceAddsNothing()
    {
        var lifecycle = Lifecycle(11, 6);
        _world.BonusChances = new HarvestBonusChances(10, 5);
        var session = Open(lifecycle, Node(1, ""));

        RunQuickHarvest(lifecycle, session, 1);

        _world.Harvests.Should().Equal(new HarvestDelivery(1, 0, 0));
        _world.Messages.Should().NotContain(ProspectingLifecycle.BonusItemMessage);
        _world.Messages.Should().NotContain(ProspectingLifecycle.BonusItemsMessage);
    }

    [Test]
    public void AHomeGroundOnlyCityRollsOnlyItsOwnChance()
    {
        var lifecycle = Lifecycle(5);
        _world.BonusChances = new HarvestBonusChances(0, 5);
        var session = Open(lifecycle, Node(1, ""));

        RunQuickHarvest(lifecycle, session, 1);

        _random.Calls.Should().Be(1);
        _world.Harvests.Should().Equal(new HarvestDelivery(2, 0, 0));
    }

    [Test]
    public void NoChancesMeansNoRollsAndNoExtraItems()
    {
        var lifecycle = Lifecycle();
        var session = Open(lifecycle, Node(2, ""));

        RunQuickHarvest(lifecycle, session, 2);

        _random.Calls.Should().Be(0);
        _world.Harvests.Should().Equal(new HarvestDelivery(1, 0, 0), new HarvestDelivery(1, 0, 0));
    }

    [Test]
    public void AQuickHarvestCutShortDoesNotFinishAndRollsNothing()
    {
        var lifecycle = Lifecycle();
        _world.BonusChances = new HarvestBonusChances(100, 100);
        var session = Open(lifecycle, Node(3, ""));

        lifecycle.StartQuickHarvest(session, _ => { }).Started.Should().BeTrue();
        _world.RunNextWork((float)ProspectingRules.HarvestSeconds);
        lifecycle.Abandon(session, ProspectingEnd.WindowClosed);

        _random.Calls.Should().Be(0);
        _world.Harvests.Should().Equal(new HarvestDelivery(1, 0, 0));
    }

    [Test]
    public void AQuickHarvestEndedByTheLastChargeAlsoRolls()
    {
        var lifecycle = Lifecycle(10);
        _world.Charges[Harvester] = 2;
        _world.BonusChances = new HarvestBonusChances(10, 0);
        var session = Open(lifecycle, Node(4, ""));

        RunQuickHarvest(lifecycle, session, 2);

        session.Finished.Should().Be(ProspectingEnding.OutOfCharges);
        _world.Harvests.Should().Equal(new HarvestDelivery(1, 0, 0), new HarvestDelivery(2, 0, 0));
    }

    // Prospecting board

    [Test]
    public void ASessionThatEndsWithSomethingGatheredRollsOnItsLastAction()
    {
        var lifecycle = Lifecycle(StrikeOneUnit, 10, 5);
        _world.BonusChances = new HarvestBonusChances(10, 5);
        var session = Open(lifecycle, Node(4, "r"));

        var strike = Do(lifecycle, session, ProspectingActionIds.Strike);
        strike.Delivered.Items.Should().Be(1);
        _random.Calls.Should().Be(1, "a Strike does not finish the session, so only the Strike's own roll was made");

        var leave = Do(lifecycle, session, ProspectingActionIds.Leave);

        leave.Ended.Should().BeTrue();
        leave.Delivered.Should().Be(new HarvestDelivery(2, 0, 0), "the Leave itself gathers nothing, so the two extra items are all it hands over");
        leave.Xp.Should().Be(0, "the extra items pay no XP");
        _world.Xp.Should().Equal(strike.Xp);
        _world.Harvests.Select(harvest => harvest.Items).Should().Equal(1, 2);
        _world.Messages.Should().Contain(ProspectingLifecycle.BonusItemsMessage);
    }

    [Test]
    public void LeavingWithNothingGatheredNeverRolls()
    {
        var lifecycle = Lifecycle();
        _world.BonusChances = new HarvestBonusChances(100, 100);
        var session = Open(lifecycle, Node(4, "r"));

        var leave = Do(lifecycle, session, ProspectingActionIds.Leave);

        leave.Ended.Should().BeTrue();
        leave.Delivered.IsEmpty.Should().BeTrue("a session that gathered nothing earns no extra item");
        _random.Calls.Should().Be(0);
        _world.Harvests.Should().BeEmpty();
    }

    [Test]
    public void AnActionThatDoesNotEndTheSessionNeverRolls()
    {
        var lifecycle = Lifecycle(StrikeOneUnit);
        _world.BonusChances = new HarvestBonusChances(100, 100);
        var session = Open(lifecycle, Node(4, "r"));

        var strike = Do(lifecycle, session, ProspectingActionIds.Strike);

        strike.Ended.Should().BeFalse();
        strike.Delivered.Should().Be(new HarvestDelivery(1, 0, 0));
        _random.Calls.Should().Be(1);
    }

    [Test]
    public void AnActionThatGatheredNothingOfItsOwnPaysNoXp_WhateverExtraItemsItHandsOver()
    {
        var lifecycle = Lifecycle(StrikeOneUnit, 10);
        _world.BonusChances = new HarvestBonusChances(10, 0);
        var session = Open(lifecycle, Node(4, "r"));
        Do(lifecycle, session, ProspectingActionIds.Strike);
        _world.Order.Clear();

        Do(lifecycle, session, ProspectingActionIds.Leave);

        _world.Harvests.Last().Should().Be(new HarvestDelivery(1, 0, 0));
        _world.Order.Should().Equal("reserve", "node", "harvest");
    }
}
