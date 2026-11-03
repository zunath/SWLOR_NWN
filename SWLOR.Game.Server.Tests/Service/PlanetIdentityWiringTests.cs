using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Pins that the planet identity check (Player City Revamp, "Planet identity") still sits where every craft passes: the recipe's own
/// requirements, which the recipe window shows and a craft start and a quick craft both enforce. The engine-bound code cannot run in a
/// unit test, so the rule is tested in <see cref="PlanetIdentityRulesTests"/> and this class checks the wiring that feeds it.
/// </summary>
public class PlanetIdentityWiringTests
{
    private static string ReadServerSource(params string[] path)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;

        root.Should().NotBeNull();
        return File.ReadAllText(Path.Combine(new[] { root!.FullName, "SWLOR.Game.Server" }.Concat(path).ToArray()));
    }

    [Test]
    public void TheRequirementReadsTheCraftersPlaceAndAppliesTheRule()
    {
        var requirement = ReadServerSource("Service", "CraftService", "RecipeIdentityRequirement.cs");

        requirement.Should().Contain("PlanetIdentityRules.DescribeRefusal(Place, PlanetIdentity.GetCraftingPlace(player))");
    }

    [Test]
    public void ThePlaceIsTheCityOfTheCraftersArea_SoAHouseInsideACityCounts()
    {
        var service = ReadServerSource("Service", "PlanetIdentity.cs");

        service.Should().Contain("Property.GetCityIdForArea(area)");
        service.Should().Contain("Station.IsStation(city)");
        service.Should().Contain("Station.GetCityPlanet(city)");
        service.Should().Contain("GetCraftingPlaceInArea(GetArea(player))");
    }

    [Test]
    public void ACraftStartAndAQuickCraftBothGiveTheRequirementsOwnWordsWhenTheyRefuse()
    {
        ReadServerSource("Service", "CraftService", "Minigame", "CraftSessionLifecycle.cs").Should()
            .Contain("_world.GetRequirementProblem(player, request.Recipe)");
        ReadServerSource("Service", "CraftService", "Minigame", "NwnCraftSessionWorld.cs").Should()
            .Contain("Craft.GetRequirementProblem(player, recipe)");
        ReadServerSource("Service", "CraftService", "Minigame", "QuickCraftService.cs").Should()
            .Contain("Craft.GetRequirementProblem(player, recipeType)");
    }

    [Test]
    public void EveryRequirementIsCheckedByTheSameGateThatTheRecipeWindowAndTheCraftStartUse()
    {
        var craft = ReadServerSource("Service", "Craft.cs");

        craft.Should().Contain("foreach (var requirement in recipe.Requirements)");
        craft.Should().Contain("requirement.CheckRequirements(player)");
        craft.Should().Contain("public static string GetRequirementProblem(uint player, RecipeType recipeType)");
    }
}
