using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Service.Kitchen;

/// <summary>Covers the Kitchen against the craft session: committing the materials, finishing a dish and ruining one.</summary>
public class KitchenSessionTests
{
    private const RecipeType Cooked = RecipeType.MynockMeatBall;

    private FakeCraftSessionWorld _world;
    private Player _player;
    private RecipeDetail _recipe;

    [SetUp]
    public void SetUp()
    {
        _recipe = RecipeDefinitionLoader.LoadGame()[Cooked];
        _world = new FakeCraftSessionWorld();
        _world.Recipes[Cooked] = _recipe;
        foreach (var (resref, quantity) in _recipe.Components)
            _world.Inventory[resref] = quantity;

        _player = new Player("player-1");
        _player.Skills[SkillType.Agriculture] = new PlayerSkill { Rank = 20 };
    }

    private CraftSessionLifecycle Lifecycle(params int[] rolls) => new(_world, new ScriptedMinigameRandom(rolls));

    private CraftSession Start(CraftSessionLifecycle lifecycle)
    {
        var result = lifecycle.Start(FakeCraftSessionWorld.PlayerHandle, _player, new CraftSessionStartRequest(Cooked, OBJECT_INVALID, null));
        result.Succeeded.Should().BeTrue(result.Message);
        return result.Session;
    }

    private KitchenEngine Engine(int seed, int freshness = 0)
    {
        var parameters = (KitchenParameters)_recipe.MinigameParameters;
        var kitchen = KitchenRecipe.From(parameters, _recipe.Level, _recipe.Components);
        if (freshness > 0)
            kitchen = new KitchenRecipe(kitchen.Dish, kitchen.Target, freshness, kitchen.Level, kitchen.Lines);

        return new KitchenEngine(new SeededMinigameRandom(seed), kitchen, new KitchenStats(20, 12, 13, 20));
    }

    [Test]
    public void TheRecipeTheTestCooksIsAnAgricultureFoodWithKitchenParameters()
    {
        _recipe.Skill.Should().Be(SkillType.Agriculture);
        _recipe.Category.Should().Be(RecipeCategoryType.Food);
        _recipe.MinigameParameters.Should().BeOfType<KitchenParameters>();
    }

    [Test]
    public void AFinishedDish_CreatesTheFoodWithADurationBonusThatFollowsItsQuality()
    {
        var lifecycle = Lifecycle();
        var session = Start(lifecycle);
        KitchenEngine engine = null;
        for (var seed = 1; seed <= 25 && (engine == null || engine.Status != MinigameStatus.Succeeded); seed++)
        {
            engine = Engine(seed);
            KitchenSimpleCook.Cook(engine);
        }

        engine!.Status.Should().Be(MinigameStatus.Succeeded);

        var success = lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, engine.Meters.Quality);

        success.Should().NotBeNull();
        success.Outcome.QualityPercent.Should().Be(engine.Meters.QualityPercent);
        var expected = CraftOutcomeRules.CalculateFoodDurationBonusSeconds(engine.Meters.Quality.Fraction);
        success.Outcome.FoodDurationBonusSeconds.Should().Be(expected);
        _world.FoodBonuses.Should().ContainSingle().Which.Seconds.Should().Be(expected);
        _world.Events.Should().Contain($"create:{_recipe.Resref}");
        _player.ActiveCraftSession.Should().BeNull();
        _player.CraftedRecipes.Should().ContainKey(Cooked);
    }

    [Test]
    public void ADishAtFullQualityPaysTheFullFiveMinuteDurationBonus()
    {
        var lifecycle = Lifecycle();
        var session = Start(lifecycle);

        lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(100, KitchenTuning.QualityMax));

        _world.FoodBonuses.Single().Seconds.Should().Be(300);
    }

    [Test]
    public void ARuinedDish_LosesItsMaterialsUnderTheSixtyFivePercentRule()
    {
        // Three component stacks: mynock meat, mynock wing and water. 66 keeps a stack, 65 loses it.
        var lifecycle = Lifecycle(66, 65, 100);
        Start(lifecycle);
        var engine = Engine(1, freshness: 8);
        engine.Perform(KitchenActionIds.Simmer);
        engine.Perform(KitchenActionIds.Simmer);
        engine.Status.Should().Be(MinigameStatus.Failed);

        var settlement = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Broken);

        settlement.Settled.Should().BeTrue();
        settlement.ItemsLost.Should().Be(1);
        settlement.ItemsKept.Should().Be(2);
        settlement.Xp.Should().Be(CraftOutcomeRules.CalculateFailureXp(CraftXpScale.Apply(_recipe.MinigameParameters, Craft.GetBaseRecipeXP(_recipe, 20)), 0));
        _world.Returned.Should().HaveCount(2);
        _player.ActiveCraftSession.Should().BeNull();
    }

    [Test]
    public void TheDishTheEngineCooksUsesTheSameComponentsTheSessionCommits()
    {
        var lifecycle = Lifecycle();
        Start(lifecycle);

        var engine = Engine(1);

        _player.ActiveCraftSession.Components.Should().HaveCount(_recipe.Components.Count);
        engine.Recipe.Lines.Select(line => line.Key).Should().Equal(_recipe.Components.Keys);
        engine.Recipe.Lines.Select(line => line.Quantity).Should().Equal(_recipe.Components.Values);
    }

    [Test]
    public void EnhancementsThatCostProgressMakeTheDishNeedMoreDoneness()
    {
        var parameters = (KitchenParameters)_recipe.MinigameParameters;
        var kitchen = KitchenRecipe.From(parameters, _recipe.Level, _recipe.Components);
        var plain = new KitchenEngine(new FixedRolls(100), kitchen, new KitchenStats(20, 0, 0, 20));
        var enhanced = new KitchenEngine(new FixedRolls(100), kitchen, new KitchenStats(20, 0, 0, 20), KitchenTuning.EnhancementDoneness(60));

        KitchenTuning.EnhancementDoneness(60).Should().Be(30);
        KitchenTuning.EnhancementDoneness(0).Should().Be(0);
        KitchenTuning.EnhancementDoneness(-5).Should().Be(0);
        plain.Meters.Progress.Max.Should().Be(100);
        enhanced.Meters.Progress.Max.Should().Be(130);
    }
}
