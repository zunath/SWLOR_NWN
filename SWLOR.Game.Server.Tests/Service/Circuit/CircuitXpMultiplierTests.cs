using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript;

namespace SWLOR.Game.Server.Tests.Service.Circuit;

/// <summary>Covers the XP multiplier CraftSim's XP-rate gate set for the Circuit and the way every craft's XP applies it.</summary>
public class CircuitXpMultiplierTests
{
    private sealed record Declaration(SkillType Skill, GuiWindowType Window, CraftMinigameType Minigame, double XpMultiplier)
        : ICraftMinigameRoute;

    private sealed record PlainDeclaration(SkillType Skill, GuiWindowType Window, CraftMinigameType Minigame) : ICraftMinigameRoute;

    [Test]
    public void TheCircuitRouteDeclaresTheMultiplier_AndEngineeringCraftsUseIt()
    {
        new CircuitRoute().XpMultiplier.Should().Be(CircuitRules.XpMultiplier);
        CraftMinigameRouting.GetRoute(SkillType.Engineering).XpMultiplier.Should().Be(CircuitRules.XpMultiplier);
        CircuitRules.XpMultiplier.Should().BeGreaterThan(1.0, "the Circuit's low-level crafts are longer than the old window's, so they pay more per craft");
    }

    [Test]
    public void ARouteMultipliesBaseXpAndRoundsToWholeXp()
    {
        CraftMinigameRouting.ScaleBaseXp(SkillType.Engineering, 100).Should().Be((int)Math.Round(100 * CircuitRules.XpMultiplier));
        CraftMinigameRouting.ScaleBaseXp(SkillType.Engineering, 0).Should().Be(0);
        CraftMinigameRouting.ScaleBaseXp(SkillType.Engineering, 7).Should().Be((int)Math.Round(7 * CircuitRules.XpMultiplier, MidpointRounding.AwayFromZero));
    }

    [Test]
    public void ARouteThatDeclaresNoMultiplierPaysBaseXp_AndTheOriginalWindowNever()
    {
        var routes = CraftMinigameRouting.BuildRoutes(new ICraftMinigameRoute[]
        {
            new Declaration(SkillType.Smithery, GuiWindowType.DebugEnmity, CraftMinigameType.Forge, 1.5),
            new PlainDeclaration(SkillType.Fabrication, GuiWindowType.DebugNuiGallery, CraftMinigameType.Fabricator)
        });

        routes[SkillType.Smithery].XpMultiplier.Should().Be(1.5);
        routes[SkillType.Fabrication].XpMultiplier.Should().Be(1.0);
        CraftMinigameRouting.Legacy(SkillType.Agriculture).XpMultiplier.Should().Be(1.0);
    }

    [Test]
    public void AFinishedCraftAndABrokenOneBothPayTheMultipliedBaseXp()
    {
        var world = new FakeCraftSessionWorld();
        var recipe = new RecipeDetail
        {
            Resref = "test_circuit",
            Skill = SkillType.Engineering,
            Level = 25,
            Category = RecipeCategoryType.ShipModule,
            Components = new Dictionary<string, int> { ["elec_good"] = 1 },
            MinigameParameters = new CircuitParameters(6, 10, 50)
        };
        world.Recipes[RecipeType.VenomCoating1] = recipe;
        world.Inventory["elec_good"] = 2;
        var player = new Player("player-1");
        player.Skills[SkillType.Engineering] = new PlayerSkill { Rank = 25 };
        var lifecycle = new CraftSessionLifecycle(world, new ScriptedMinigameRandom(100));
        var request = new CraftSessionStartRequest(RecipeType.VenomCoating1, NWScript.OBJECT_INVALID, Array.Empty<uint>());
        var scaledBaseXp = CraftMinigameRouting.ScaleBaseXp(SkillType.Engineering, Craft.GetBaseRecipeXP(recipe, 25));

        var session = lifecycle.Start(FakeCraftSessionWorld.PlayerHandle, player, request).Session;
        var success = lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, player, session, new MinigameMeter(50, 100));
        lifecycle.Start(FakeCraftSessionWorld.PlayerHandle, player, request);
        var failure = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, player, CraftSessionEndReason.Broken);

        scaledBaseXp.Should().BeGreaterThan(Craft.GetBaseRecipeXP(recipe, 25));
        success!.Outcome.Xp.Should().Be(CraftOutcomeRules.CalculateXp(scaledBaseXp, 0, true, 0.5f));
        failure.Xp.Should().Be(CraftOutcomeRules.CalculateFailureXp(scaledBaseXp, 0));
        world.XpGiven.Select(given => given.Xp).Should().Equal(success.Outcome.Xp, failure.Xp);
    }
}
