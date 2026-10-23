using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript;

namespace SWLOR.Game.Server.Tests.Service.Circuit;

/// <summary>Covers the XP multipliers CraftSim's XP-rate gate set for the Circuit, per recipe-level band, and the way every craft's XP applies them.</summary>
public class CircuitXpMultiplierTests
{
    private static readonly int[] BandFirstLevels = { 1, 11, 21, 31, 41 };
    private static readonly int[] BandLastLevels = { 10, 20, 30, 40, 53 };

    [Test]
    public void EachBandHasOneMultiplier_AcrossItsWholeLevelRange()
    {
        var expected = new[]
        {
            CircuitRules.XpMultiplierLevels1To10, CircuitRules.XpMultiplierLevels11To20, CircuitRules.XpMultiplierLevels21To30,
            CircuitRules.XpMultiplierLevels31To40, CircuitRules.XpMultiplierLevels41AndUp
        };

        for (var band = 0; band < expected.Length; band++)
        {
            for (var level = BandFirstLevels[band]; level <= BandLastLevels[band]; level++)
                CircuitRules.XpMultiplierFor(level).Should().Be(expected[band], $"level {level} is in band {BandFirstLevels[band]}-{BandLastLevels[band]}");
        }

        CircuitRules.XpMultiplierFor(80).Should().Be(CircuitRules.XpMultiplierLevels41AndUp);
        CircuitRules.XpMultiplierFor(0).Should().Be(CircuitRules.XpMultiplierLevels1To10);
    }

    [Test]
    public void TheMultiplierNeverFallsBelowOne_AndNeverRisesWithLevel()
    {
        var multipliers = Enumerable.Range(1, 60).Select(CircuitRules.XpMultiplierFor).ToList();

        multipliers.Should().OnlyContain(multiplier => multiplier >= 1.0, "a minigame never pays less XP than today's");
        multipliers.Zip(multipliers.Skip(1), (lower, higher) => higher <= lower).Should().OnlyContain(fallsOrHolds => fallsOrHolds,
            "the old window's crafts are shortest at low levels, so the need falls with level");
    }

    [Test]
    public void TheMinigameXpTableFollowsTheCircuitsBands_AndOtherMinigamesPayTodaysXp()
    {
        foreach (var level in new[] { 1, 10, 11, 25, 40, 53 })
            CraftMinigameXp.MultiplierFor(CraftMinigameType.Circuit, level).Should().Be(CircuitRules.XpMultiplierFor(level));

        CraftMinigameXp.MultiplierFor(CraftMinigameType.Search, 5).Should().Be(1.0);
        CraftMinigameXp.ScaleBaseXp(100, 2.5).Should().Be(250);
        CraftMinigameXp.ScaleBaseXp(7, 1.5).Should().Be(11, "10.5 rounds away from zero");
        CraftMinigameXp.ScaleBaseXp(0, 2.5).Should().Be(0);
    }

    [Test]
    public void ARecipesBaseXpAppliesItsLevelBandsMultiplier_ForEngineeringOnly()
    {
        var engineering = Recipe(SkillType.Engineering, 5);
        var smithery = Recipe(SkillType.Smithery, 5);

        // The minigame's band multiplier first, then the crafting pace multiplier every crafting skill gets on top of it.
        CraftMinigameXp.BaseXp(engineering, 5).Should().Be(
            CraftMinigameXp.ScaleBaseXp(
                CraftMinigameXp.ScaleBaseXp(Craft.GetBaseRecipeXP(engineering, 5), CircuitRules.XpMultiplierFor(5)),
                CraftMinigameXp.CraftingPaceMultiplier));
        CraftMinigameXp.BaseXp(smithery, 5).Should().Be(
            CraftMinigameXp.ScaleBaseXp(
                CraftMinigameXp.ScaleBaseXp(Craft.GetBaseRecipeXP(smithery, 5), CraftMinigameXp.MultiplierFor(CraftMinigameType.Forge, 5)),
                CraftMinigameXp.CraftingPaceMultiplier));
        CraftMinigameXp.BaseXp(Recipe(SkillType.Armor, 5), 5).Should().Be(Craft.GetBaseRecipeXP(Recipe(SkillType.Armor, 5), 5), "a skill with no minigame keeps today's XP");
    }

    [Test]
    public void AFinishedCraftAndABrokenOneBothPayTheMultipliedBaseXp()
    {
        var world = new FakeCraftSessionWorld();
        var recipe = Recipe(SkillType.Engineering, 5);
        recipe.Category = RecipeCategoryType.ShipModule;
        recipe.Components = new Dictionary<string, int> { ["elec_good"] = 1 };
        recipe.MinigameParameters = new CircuitParameters(6, 10, 50);
        world.Recipes[RecipeType.VenomCoating1] = recipe;
        world.Inventory["elec_good"] = 2;
        var player = new Player("player-1");
        player.Skills[SkillType.Engineering] = new PlayerSkill { Rank = 5 };
        var lifecycle = new CraftSessionLifecycle(world, new ScriptedMinigameRandom(100));
        var request = new CraftSessionStartRequest(RecipeType.VenomCoating1, NWScript.OBJECT_INVALID, Array.Empty<uint>());
        var scaledBaseXp = CraftMinigameXp.BaseXp(recipe, 5);

        var session = lifecycle.Start(FakeCraftSessionWorld.PlayerHandle, player, request).Session;
        var success = lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, player, session, new MinigameMeter(50, 100));
        lifecycle.Start(FakeCraftSessionWorld.PlayerHandle, player, request);
        var failure = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, player, CraftSessionEndReason.Broken);

        success!.Outcome.Xp.Should().Be(CraftOutcomeRules.CalculateXp(scaledBaseXp, 0, true, 0.5f));
        failure.Xp.Should().Be(CraftOutcomeRules.CalculateFailureXp(scaledBaseXp, 0));
        world.XpGiven.Select(given => given.Xp).Should().Equal(success.Outcome.Xp, failure.Xp);
    }

    private static RecipeDetail Recipe(SkillType skill, int level) => new()
    {
        Resref = "test_recipe",
        Skill = skill,
        Level = level
    };
}
