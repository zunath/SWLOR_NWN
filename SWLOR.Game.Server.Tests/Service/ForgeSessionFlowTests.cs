using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Plays the Plasma Forge against the real craft session lifecycle: what a finished, broken or abandoned forge settles, the
/// enhancement thresholds its Finish meets, and the 65% rule the window shows before the materials are committed.
/// </summary>
public class ForgeSessionFlowTests
{
    private const RecipeType TestRecipe = RecipeType.VenomCoating1;
    private const uint NoItem = OBJECT_INVALID;

    private FakeCraftSessionWorld _world;
    private Player _player;
    private RecipeDetail _recipe;

    [SetUp]
    public void SetUp()
    {
        _recipe = new RecipeDetail
        {
            Resref = "test_item",
            Skill = SkillType.Smithery,
            Level = 20,
            Category = RecipeCategoryType.Longsword,
            Components = new Dictionary<string, int> { ["ore"] = 3, ["wire"] = 2 },
            EnhancementType = RecipeEnhancementType.Weapon,
            EnhancementSlots = 2,
            MinigameParameters = new ForgeParameters(50, 30, 0, 45, 30)
        };

        _world = new FakeCraftSessionWorld();
        _world.Recipes[TestRecipe] = _recipe;
        _world.Inventory["ore"] = 5;
        _world.Inventory["wire"] = 2;
        _world.Enhancements[FakeCraftSessionWorld.FirstEnhancement] = "enh-a";
        _world.Enhancements[FakeCraftSessionWorld.SecondEnhancement] = "enh-b";

        _player = new Player("player-1");
        _player.Skills[SkillType.Smithery] = new PlayerSkill { Rank = 20 };
    }

    private CraftSessionLifecycle Lifecycle(params int[] rolls) => new(_world, new ScriptedMinigameRandom(rolls));

    private CraftSession StartSession(CraftSessionLifecycle lifecycle, params uint[] enhancements)
    {
        var result = lifecycle.Start(
            FakeCraftSessionWorld.PlayerHandle, _player, new CraftSessionStartRequest(TestRecipe, NoItem, enhancements));
        result.Succeeded.Should().BeTrue(result.Message);
        return result.Session;
    }

    private ForgeEngine NewEngine() =>
        new(new ForgeSetup((ForgeParameters)_recipe.MinigameParameters, _recipe.Level, 20, 0, 0, 0),
            new ScriptedMinigameRandom(Enumerable.Repeat(100, 100).ToArray()));

    /// <summary>Hones in the cool half of the range, then presses until the Shape is full: 51% Finish, enough for the first slot but not the second.</summary>
    private static void PlayToHalfQuality(ForgeEngine engine)
    {
        for (var hone = 0; hone < 8; hone++)
            engine.Perform(ForgeActionIds.VibroHone).Performed.Should().BeTrue();

        while (engine.Status == MinigameStatus.InProgress)
            engine.Perform(ForgeActionIds.HydroPress).Performed.Should().BeTrue();
    }

    [Test]
    public void AFinishedForgeSettlesThroughTheOutcomeRulesWithItsFinishMeter()
    {
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle, FakeCraftSessionWorld.FirstEnhancement, FakeCraftSessionWorld.SecondEnhancement);
        var engine = NewEngine();

        PlayToHalfQuality(engine);
        engine.Status.Should().Be(MinigameStatus.Succeeded);
        var quality = engine.Meters.Quality;
        quality.Percent.Should().BeInRange(50, 84, "the first slot's threshold is met and the second slot's is not");

        var success = lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, quality);

        success.Should().NotBeNull();
        success!.Outcome.QualityPercent.Should().Be(quality.Percent);
        success.Outcome.Enhancements.Select(transfer => (transfer.Slot, transfer.ThresholdPercent, transfer.Transfers))
            .Should().Equal((1, 50, true), (2, 85, false));
        _world.AppliedEnhancements.Should().Equal("enh-a");
        _player.ActiveCraftSession.Should().BeNull();
        _player.CraftedRecipes.Should().ContainKey(TestRecipe);
        _world.XpGiven.Should().ContainSingle().Which.Skill.Should().Be(SkillType.Smithery);
    }

    [Test]
    public void AForgeFinishedWithoutAnyHoningCreatesTheItemAtZeroQualityAndTransfersNothing()
    {
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle, FakeCraftSessionWorld.FirstEnhancement);
        var engine = NewEngine();
        while (engine.Status == MinigameStatus.InProgress)
            engine.Perform(ForgeActionIds.HydroPress);

        var success = lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, engine.Meters.Quality);

        success!.Outcome.QualityPercent.Should().Be(0);
        success.Outcome.Enhancements.Should().ContainSingle().Which.Transfers.Should().BeFalse();
        _world.AppliedEnhancements.Should().BeEmpty();
        _world.Events.Should().Contain("create:test_item");
    }

    [Test]
    public void ABrokenForgeSettlesAsAFailureUnderTheSixtyFivePercentRule()
    {
        // Rolls: enhancement kept (66), ore lost (65), wire kept (100).
        var lifecycle = Lifecycle(66, 65, 100);
        var session = StartSession(lifecycle, FakeCraftSessionWorld.FirstEnhancement);
        var hot = new ForgeEngine(
            new ForgeSetup(new ForgeParameters(50, 30, 0, 100, 60), 20, 20, 0, 0, 0, 100),
            new ScriptedMinigameRandom(Enumerable.Repeat(100, 100).ToArray()));
        while (hot.Status == MinigameStatus.InProgress)
            hot.Perform(ForgeActionIds.HydroPress);
        hot.Status.Should().Be(MinigameStatus.Failed);

        var settlement = lifecycle.FailAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Broken);

        settlement.Settled.Should().BeTrue();
        settlement.ItemsLost.Should().Be(1);
        settlement.ItemsKept.Should().Be(2);
        _world.Returned.Should().Equal("enh-a", "wire x2");
        _world.Events.Should().NotContain("create:test_item");
        settlement.Xp.Should().Be(CraftOutcomeRules.CalculateFailureXp(CraftMinigameXp.BaseXp(_recipe, 20), 0));
        _player.ActiveCraftSession.Should().BeNull();
        session.Should().NotBeNull();
    }

    [Test]
    public void StoppingMidCraftFailsTheSessionAndAWindowClosedAfterwardDoesNothing()
    {
        var lifecycle = Lifecycle(100, 100, 100);
        var session = StartSession(lifecycle);
        var engine = NewEngine();
        engine.Perform(ForgeActionIds.VibroHone);
        engine.Status.Should().Be(MinigameStatus.InProgress);

        var stopped = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.WindowClosed);
        var closed = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.WindowClosed);

        stopped.Settled.Should().BeTrue();
        closed.Settled.Should().BeFalse();
        CraftSessionLifecycle.IsActive(_player, session).Should().BeFalse("a click that arrives after the craft ended must not be treated as live");
        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, engine.Meters.Quality).Should().BeNull();
    }

    [Test]
    public void TheEnginesQualityMeterIsThePercentageTheWindowShowsAndTheOutcomeUses()
    {
        var engine = NewEngine();
        engine.Perform(ForgeActionIds.VibroHone);

        var outcome = CraftOutcomeRules.CalculateSuccess(engine.Meters.Quality, 20, 100, false, 0, false, new[] { 1 });

        outcome.QualityFraction.Should().BeApproximately(engine.Meters.Quality.Fraction, 1e-6f);
        ForgePreviewText.Percent(engine.Meters.Quality).Should().Be($"{engine.Meters.Quality.Fraction * 100:F1}%");
    }
}
