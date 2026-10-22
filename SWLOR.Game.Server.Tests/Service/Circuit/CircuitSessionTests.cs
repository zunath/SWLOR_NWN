using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Tests.Service.Circuit.CircuitTestData;

namespace SWLOR.Game.Server.Tests.Service.Circuit;

/// <summary>
/// Covers a Circuit craft from the committed materials to its end: a finished item settles at the engine's quality, a broken piece
/// and a closed window settle under the loss rule, and a settled session cannot create a second item.
/// </summary>
public class CircuitSessionTests
{
    private const RecipeType TestRecipe = RecipeType.VenomCoating1;

    private FakeCraftSessionWorld _world;
    private Player _player;

    [SetUp]
    public void SetUp()
    {
        _world = new FakeCraftSessionWorld();
        _world.Recipes[TestRecipe] = new RecipeDetail
        {
            Resref = "test_circuit",
            Skill = SkillType.Engineering,
            Level = 25,
            Category = RecipeCategoryType.ShipModule,
            Components = new Dictionary<string, int> { ["elec_good"] = 3, ["ref_idailia"] = 2 },
            EnhancementType = RecipeEnhancementType.Module,
            EnhancementSlots = 2,
            MinigameParameters = new CircuitParameters(6, 10, 50)
        };
        _world.Inventory["elec_good"] = 3;
        _world.Inventory["ref_idailia"] = 2;
        _world.Enhancements[FakeCraftSessionWorld.FirstEnhancement] = "enh-a";
        _world.Enhancements[FakeCraftSessionWorld.SecondEnhancement] = "enh-b";

        _player = new Player("player-1");
        _player.Skills[SkillType.Engineering] = new PlayerSkill { Rank = 25 };
    }

    private CraftSessionLifecycle Lifecycle(params int[] rolls) => new(_world, new ScriptedMinigameRandom(rolls));

    private CraftSession StartSession(CraftSessionLifecycle lifecycle)
    {
        var request = new CraftSessionStartRequest(
            TestRecipe,
            SWLOR.NWN.API.NWScript.NWScript.OBJECT_INVALID,
            new[] { FakeCraftSessionWorld.FirstEnhancement, FakeCraftSessionWorld.SecondEnhancement });
        var result = lifecycle.Start(FakeCraftSessionWorld.PlayerHandle, _player, request);
        result.Succeeded.Should().BeTrue(result.Message);
        return result.Session;
    }

    [Test]
    public void AFinishedCraft_SettlesAtTheEnginesQuality_AndTransfersTheEnhancementsItsThresholdsAllow()
    {
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle);
        var setup = Setup();
        var engine = new CircuitEngine(new SeededMinigameRandom(3), setup);

        var (status, _, quality, _) = PlaySafely(engine, setup);
        var success = lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, engine.Meters.Quality);

        status.Should().Be(MinigameStatus.Succeeded);
        quality.Should().BeGreaterThanOrEqualTo(CraftOutcomeRules.SecondSlotQualityThresholdPercent);
        success.Should().NotBeNull();
        success!.Outcome.QualityPercent.Should().Be(quality);
        success.Outcome.Enhancements.Select(transfer => (transfer.ThresholdPercent, transfer.Transfers))
            .Should().Equal((50, true), (85, true));
        _world.Events.Should().Contain("create:test_circuit");
        _world.AppliedEnhancements.Should().Equal("enh-a", "enh-b");
        _world.XpGiven.Should().ContainSingle().Which.Skill.Should().Be(SkillType.Engineering);
        _world.XpGiven[0].Xp.Should().Be(success.Outcome.Xp).And.BeGreaterThan(0);
        _player.ActiveCraftSession.Should().BeNull();
    }

    [Test]
    public void AFinishedCraftWithNoCalibration_TransfersNoEnhancement_AndSaysWhichThresholdsItMissed()
    {
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle);
        var engine = new CircuitEngine(new SeededMinigameRandom(7), Setup(line: 9));
        while (engine.Status == MinigameStatus.InProgress)
            engine.Perform(CircuitActionIds.Wire);

        var success = lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, engine.Meters.Quality);

        engine.Status.Should().Be(MinigameStatus.Succeeded);
        success!.Outcome.QualityPercent.Should().Be(0);
        _world.AppliedEnhancements.Should().BeEmpty();
        _world.Messages.Should().Contain(message => message.Contains("quality did not reach 50%"));
        _world.Messages.Should().Contain(message => message.Contains("quality did not reach 85%"));
        _world.Events.Should().Contain("create:test_circuit", "an item is still made with the progress meter full");
    }

    [Test]
    public void ASettledSessionCannotCreateASecondItem()
    {
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle);
        var quality = new MinigameMeter(60, 100);

        lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, quality).Should().NotBeNull();
        var again = lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, quality);

        again.Should().BeNull();
        _world.Events.Count(entry => entry == "create:test_circuit").Should().Be(1);
    }

    [Test]
    public void AnIntegrityBreak_EndsTheEngineFailed_AndTheSessionLosesItemsUnderTheSixtyFivePercentRule()
    {
        // Rolls above 65 keep an item and rolls of 65 or less lose it: the enhancements first, then the component stacks.
        var lifecycle = Lifecycle(100, 1, 100, 1);
        StartSession(lifecycle);
        var engine = new CircuitEngine(new SeededMinigameRandom(1), Setup(integrity: 6));
        while (engine.Status == MinigameStatus.InProgress)
            engine.Perform(CircuitActionIds.Wire);

        var settlement = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Broken);

        engine.Status.Should().Be(MinigameStatus.Failed);
        settlement.Settled.Should().BeTrue();
        settlement.ItemsLost.Should().Be(2);
        settlement.ItemsKept.Should().Be(2);
        settlement.Xp.Should().BeGreaterThan(0, "a failed craft still gives some XP");
        _world.Returned.Should().Equal("enh-a", "elec_good x3");
        _world.Events.Should().NotContain("create:test_circuit");
        _player.ActiveCraftSession.Should().BeNull();
    }

    [Test]
    public void ClosingTheWindowMidCraft_FailsTheSessionAndLosesEveryItemOnLowRolls()
    {
        var lifecycle = Lifecycle(1, 1, 1, 1);
        var session = StartSession(lifecycle);
        var engine = new CircuitEngine(new SeededMinigameRandom(2), Setup());
        engine.Perform(CircuitActionIds.Wire);
        engine.Perform(CircuitActionIds.Tune);

        var settlement = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.WindowClosed);

        engine.Status.Should().Be(MinigameStatus.InProgress);
        CraftSessionLifecycle.IsActive(_player, session).Should().BeFalse();
        settlement.ItemsLost.Should().Be(4);
        settlement.ItemsKept.Should().Be(0);
        _world.Messages.Should().Contain(message => message.Contains("4 of 4 items were lost"));
        _world.Returned.Should().BeEmpty();
    }
}
