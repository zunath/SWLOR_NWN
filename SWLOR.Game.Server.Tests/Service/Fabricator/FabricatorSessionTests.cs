using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Tests.Service.Fabricator.FabricatorTestData;

namespace SWLOR.Game.Server.Tests.Service.Fabricator;

/// <summary>
/// Covers a Fabricator craft from the committed materials to its end: a finished item settles at the engine's quality, a broken
/// piece and a closed window settle under the loss rule, a settled session cannot create a second item, and the minigame's XP
/// multiplier scales what is granted.
/// </summary>
public class FabricatorSessionTests
{
    private const RecipeType TestRecipe = RecipeType.BedRoll;

    private FakeCraftSessionWorld _world;
    private Player _player;

    [SetUp]
    public void SetUp()
    {
        _world = new FakeCraftSessionWorld();
        _world.Recipes[TestRecipe] = new RecipeDetail
        {
            Resref = "test_fabricator",
            Skill = SkillType.Fabrication,
            Level = 25,
            Category = RecipeCategoryType.Bed,
            Components = new Dictionary<string, int> { ["lth_good"] = 3, ["wood"] = 2 },
            EnhancementType = RecipeEnhancementType.Structure,
            EnhancementSlots = 2,
            MinigameParameters = Parameters()
        };
        _world.Inventory["lth_good"] = 3;
        _world.Inventory["wood"] = 2;
        _world.Enhancements[FakeCraftSessionWorld.FirstEnhancement] = "enh-a";
        _world.Enhancements[FakeCraftSessionWorld.SecondEnhancement] = "enh-b";

        _player = new Player("player-1");
        _player.Skills[SkillType.Fabrication] = new PlayerSkill { Rank = 25 };
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
        var engine = Built(Setup());
        Refine(engine);
        engine.Perform(FabricatorActionIds.Assemble).Performed.Should().BeTrue();

        var success = lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, engine.Meters.Quality);

        engine.Status.Should().Be(MinigameStatus.Succeeded);
        success.Should().NotBeNull();
        success!.Outcome.QualityPercent.Should().Be(100);
        success.Outcome.Enhancements.Select(transfer => (transfer.ThresholdPercent, transfer.Transfers)).Should().Equal((50, true), (85, true));
        _world.Events.Should().Contain("create:test_fabricator");
        _world.AppliedEnhancements.Should().Equal("enh-a", "enh-b");
        _world.XpGiven.Should().ContainSingle().Which.Skill.Should().Be(SkillType.Fabrication);
        _world.XpGiven[0].Xp.Should().Be(success.Outcome.Xp).And.BeGreaterThan(0);
        _player.ActiveCraftSession.Should().BeNull();
    }

    [Test]
    public void ACraftAssembledAtTheCap_SettlesAtTheCappedQuality_AndSaysWhichThresholdsItMissed()
    {
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle);
        var engine = Built(Setup());
        // Only the loaded head's sub-assembly is refined, so the others hold the piece at the cap of weakest + 20.
        for (var i = 0; i < 4; i++)
            engine.Perform(FabricatorActionIds.PrecisionPass);
        engine.Perform(FabricatorActionIds.Assemble);

        var success = lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, engine.Meters.Quality);

        engine.Status.Should().Be(MinigameStatus.Succeeded);
        success!.Outcome.QualityPercent.Should().Be(FabricatorRules.AssembleCapPoints);
        _world.AppliedEnhancements.Should().BeEmpty();
        _world.Messages.Should().Contain(message => message.Contains("quality did not reach 50%"));
        _world.Messages.Should().Contain(message => message.Contains("quality did not reach 85%"));
        _world.Events.Should().Contain("create:test_fabricator", "an item is still made when it is assembled at a low quality");
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
        _world.Events.Count(entry => entry == "create:test_fabricator").Should().Be(1);
    }

    [Test]
    public void AMachineConditionBreak_EndsTheEngineFailed_AndTheSessionLosesItemsUnderTheSixtyFivePercentRule()
    {
        // Rolls above 65 keep an item and rolls of 65 or less lose it: the enhancements first, then the component stacks.
        var lifecycle = Lifecycle(100, 1, 100, 1);
        StartSession(lifecycle);
        var (engine, breaking) = WearToTheBreak();
        engine.Perform(breaking);

        var settlement = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Broken);

        engine.Status.Should().Be(MinigameStatus.Failed);
        settlement.Settled.Should().BeTrue();
        settlement.ItemsLost.Should().Be(2);
        settlement.ItemsKept.Should().Be(2);
        settlement.Xp.Should().BeGreaterThan(0, "a failed craft still gives some XP");
        _world.Returned.Should().Equal("enh-a", "lth_good x3");
        _world.Events.Should().NotContain("create:test_fabricator");
        _player.ActiveCraftSession.Should().BeNull();
    }

    [Test]
    public void ClosingTheWindowMidCraft_FailsTheSessionAndLosesEveryItemOnLowRolls()
    {
        var lifecycle = Lifecycle(1, 1, 1, 1);
        var session = StartSession(lifecycle);
        var engine = Engine(Setup(), NoPrime, NoPrime);
        engine.Perform(FabricatorActionIds.RunFittings);
        engine.Perform(FabricatorActionIds.PrecisionPass);

        var settlement = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.WindowClosed);

        engine.Status.Should().Be(MinigameStatus.InProgress);
        CraftSessionLifecycle.IsActive(_player, session).Should().BeFalse();
        settlement.ItemsLost.Should().Be(4);
        settlement.ItemsKept.Should().Be(0);
        _world.Messages.Should().Contain(message => message.Contains("4 of 4 items were lost"));
        _world.Returned.Should().BeEmpty();
    }

    [Test]
    public void TheMinigamesXpMultiplier_ScalesTheBaseXpOfASuccess_AndOfAFailure()
    {
        var lifecycle = Lifecycle(1, 1, 1, 1);
        var recipe = _world.Recipes[TestRecipe];
        var baseXp = Craft.GetBaseRecipeXP(recipe, 25);
        var gated = CraftMinigameXp.ScaleBaseXp(baseXp, FabricatorRules.XpMultiplierForLevel(recipe.Level));
        var scaled = CraftMinigameXp.ScaleBaseXp(gated, CraftMinigameXp.CraftingPaceMultiplier);
        CraftMinigameXp.BaseXp(recipe, 25).Should().Be(scaled, "the lifecycle reads the base XP through CraftMinigameXp, gate multiplier then pace");
        var session = StartSession(lifecycle);

        var success = lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(60, 100));

        success!.Outcome.Xp.Should().Be(CraftOutcomeRules.CalculateXp(scaled, 0, true, 0.6f));

        _world.Inventory["lth_good"] = 3;
        _world.Inventory["wood"] = 2;
        _world.Enhancements[FakeCraftSessionWorld.FirstEnhancement] = "enh-a";
        _world.Enhancements[FakeCraftSessionWorld.SecondEnhancement] = "enh-b";
        StartSession(lifecycle);
        var failure = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Broken);

        failure.Xp.Should().Be(CraftOutcomeRules.CalculateFailureXp(scaled, 0));
    }

    [Test]
    public void TheFabricatorsXpMultiplierDiffersByLevelBand_AndAMinigameWithoutOnePaysTodaysXp()
    {
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Fabricator, 5).Should().Be(FabricatorRules.XpMultiplierLevels1To10);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Fabricator, 10).Should().Be(FabricatorRules.XpMultiplierLevels1To10);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Fabricator, 11).Should().Be(FabricatorRules.XpMultiplierLevels11To20);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Fabricator, 30).Should().Be(FabricatorRules.XpMultiplierLevels21To30);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Fabricator, 40).Should().Be(FabricatorRules.XpMultiplierLevels31To40);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Fabricator, 53).Should().Be(FabricatorRules.XpMultiplierLevels41Up);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Fabricator).Should().Be(FabricatorRules.XpMultiplierLevels1To10, "no level takes the lowest band");
        new[]
        {
            FabricatorRules.XpMultiplierLevels1To10, FabricatorRules.XpMultiplierLevels11To20, FabricatorRules.XpMultiplierLevels21To30,
            FabricatorRules.XpMultiplierLevels31To40, FabricatorRules.XpMultiplierLevels41Up
        }.Should().OnlyContain(multiplier => multiplier >= 1.0, "a multiplier only ever lifts the XP");
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Forge, 20).Should().Be(1.0);
        CraftMinigameXp.ScaleBaseXp(600, 1.0).Should().Be(600);
        CraftMinigameXp.ScaleBaseXp(600, 2.5).Should().Be(1500);
        CraftMinigameXp.ScaleBaseXp(7, 1.5).Should().Be(11, "halves round away from zero");
    }

    [Test]
    public void ARecipeWhoseSkillHasNoMinigameKeepsTodaysXp()
    {
        var recipe = new RecipeDetail { Skill = SkillType.Armor, Level = 20 };

        CraftMinigameRouting.GetRoute(SkillType.Armor).IsLegacy.Should().BeTrue();
        CraftMinigameXp.BaseXp(recipe, 15).Should().Be(Craft.GetBaseRecipeXP(recipe, 15));
    }
}
