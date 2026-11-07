using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PlanetIdentityService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>The location check of planet identity recipes: a planet's signature food and decor are crafted only in a city on that planet.</summary>
public class PlanetIdentityRulesTests
{
    private static readonly PlanetType[] CityPlanets =
    {
        PlanetType.Dantooine, PlanetType.Dathomir, PlanetType.Hutlar, PlanetType.Korriban,
        PlanetType.MonCala, PlanetType.Tatooine, PlanetType.Viscara
    };

    // A recipe for a planet

    [TestCaseSource(nameof(CityPlanets))]
    public void APlanetRecipe_IsCraftedInACityOnItsPlanet(PlanetType planet)
    {
        PlanetIdentityRules.Check(IdentityPlace.ForPlanet(planet), CraftingPlace.City(planet)).Should().Be(IdentityRefusal.None);
    }

    [Test]
    public void APlanetRecipe_IsRefusedInACityOnAnotherPlanet()
    {
        foreach (var required in CityPlanets)
        foreach (var actual in CityPlanets.Where(planet => planet != required))
        {
            PlanetIdentityRules.Check(IdentityPlace.ForPlanet(required), CraftingPlace.City(actual))
                .Should().Be(IdentityRefusal.WrongPlanet, $"{required} food is not cooked in a {actual} city");
        }
    }

    [Test]
    public void APlanetRecipe_IsRefusedOutsideEveryCity()
    {
        foreach (var planet in CityPlanets)
            PlanetIdentityRules.Check(IdentityPlace.ForPlanet(planet), CraftingPlace.Outside).Should().Be(IdentityRefusal.NotInCity);
    }

    [Test]
    public void APlanetRecipe_IsRefusedOnAStation_EvenInOrbitOfThePlanet()
    {
        foreach (var planet in CityPlanets)
            PlanetIdentityRules.Check(IdentityPlace.ForPlanet(planet), CraftingPlace.Station).Should().Be(IdentityRefusal.OnStation,
                "a station has its own identity set instead of a planet's");
    }

    [Test]
    public void APlanetRecipe_IsRefusedInACityWhosePlanetIsNotRecognized()
    {
        PlanetIdentityRules.Check(IdentityPlace.ForPlanet(PlanetType.Tatooine), CraftingPlace.City(PlanetType.Invalid))
            .Should().Be(IdentityRefusal.WrongPlanet);
    }

    // A recipe for stations

    [Test]
    public void AStationRecipe_IsCraftedOnAnyStation()
    {
        PlanetIdentityRules.Check(IdentityPlace.ForStation(), CraftingPlace.Station).Should().Be(IdentityRefusal.None);
    }

    [Test]
    public void AStationRecipe_IsRefusedInAPlanetCityAndOutsideEveryCity()
    {
        foreach (var planet in CityPlanets)
            PlanetIdentityRules.Check(IdentityPlace.ForStation(), CraftingPlace.City(planet)).Should().Be(IdentityRefusal.NotOnStation);

        PlanetIdentityRules.Check(IdentityPlace.ForStation(), CraftingPlace.Outside).Should().Be(IdentityRefusal.NotInCity);
    }

    [Test]
    public void APlaceWithoutAPlanetCannotBeRequired()
    {
        var act = () => IdentityPlace.ForPlanet(PlanetType.Invalid);

        act.Should().Throw<ArgumentException>();
    }

    // The words

    [Test]
    public void TheRequirementLine_NamesThePlanetOrTheStation()
    {
        PlanetIdentityRules.DescribeRequirement(IdentityPlace.ForPlanet(PlanetType.MonCala)).Should().Be("Crafted in a city on Mon Cala.");
        PlanetIdentityRules.DescribeRequirement(IdentityPlace.ForPlanet(PlanetType.Tatooine)).Should().Be("Crafted in a city on Tatooine.");
        PlanetIdentityRules.DescribeRequirement(IdentityPlace.ForStation()).Should().Be("Crafted on a player station.");
    }

    [Test]
    public void ARefusal_SaysWhatTheRecipeNeedsAndWhereTheCrafterIs()
    {
        var tatooine = IdentityPlace.ForPlanet(PlanetType.Tatooine);

        PlanetIdentityRules.DescribeRefusal(tatooine, CraftingPlace.City(PlanetType.Viscara)).Should().Be(
            "This can only be crafted at a crafting station in a city on Tatooine. This city is on Viscara.");
        PlanetIdentityRules.DescribeRefusal(tatooine, CraftingPlace.Outside).Should().Be(
            "This can only be crafted at a crafting station in a city on Tatooine. You are not in a player city.");
        PlanetIdentityRules.DescribeRefusal(tatooine, CraftingPlace.Station).Should().Be(
            "This can only be crafted at a crafting station in a city on Tatooine. You are on a player station.");
        PlanetIdentityRules.DescribeRefusal(tatooine, CraftingPlace.City(PlanetType.Invalid)).Should().Be(
            "This can only be crafted at a crafting station in a city on Tatooine. This city is not on a planet the game recognizes.");

        PlanetIdentityRules.DescribeRefusal(IdentityPlace.ForStation(), CraftingPlace.City(PlanetType.MonCala)).Should().Be(
            "This can only be crafted at a crafting station on a player station. This city is on Mon Cala.");
        PlanetIdentityRules.DescribeRefusal(IdentityPlace.ForStation(), CraftingPlace.Outside).Should().Be(
            "This can only be crafted at a crafting station on a player station. You are not in a player city.");
    }

    [Test]
    public void NoRefusal_WhenTheRecipeMayBeCraftedHere()
    {
        PlanetIdentityRules.DescribeRefusal(IdentityPlace.ForPlanet(PlanetType.Dathomir), CraftingPlace.City(PlanetType.Dathomir)).Should().BeEmpty();
        PlanetIdentityRules.DescribeRefusal(IdentityPlace.ForStation(), CraftingPlace.Station).Should().BeEmpty();
    }

    [Test]
    public void EveryMessageIsAsciiWithTheGameStandardPunctuation()
    {
        var places = new[] { CraftingPlace.Outside, CraftingPlace.Station }
            .Concat(CityPlanets.Select(CraftingPlace.City)).Append(CraftingPlace.City(PlanetType.Invalid)).ToList();
        var required = CityPlanets.Select(IdentityPlace.ForPlanet).Append(IdentityPlace.ForStation()).ToList();

        foreach (var need in required)
        {
            PlanetIdentityRules.DescribeRequirement(need).Should().MatchRegex("^[\\x20-\\x7E]+$");
            foreach (var place in places)
                PlanetIdentityRules.DescribeRefusal(need, place).Should().MatchRegex("^[\\x20-\\x7E]*$");
        }
    }

    // The requirement a recipe carries

    [Test]
    public void TheRecipeBuilder_AddsOneIdentityRequirementToTheRecipe()
    {
        var builder = new RecipeBuilder();
        builder.Create(RecipeType.ViscaraOrchardRoast, SWLOR.Game.Server.Service.SkillService.SkillType.Agriculture).RequirementPlanet(PlanetType.Viscara);
        builder.Create(RecipeType.SpacerHydroStew, SWLOR.Game.Server.Service.SkillService.SkillType.Agriculture).RequirementStation();

        var recipes = builder.Build();

        recipes[RecipeType.ViscaraOrchardRoast].Requirements.OfType<RecipeIdentityRequirement>().Should().ContainSingle()
            .Which.Place.Should().Be(IdentityPlace.ForPlanet(PlanetType.Viscara));
        recipes[RecipeType.SpacerHydroStew].Requirements.OfType<RecipeIdentityRequirement>().Should().ContainSingle()
            .Which.Place.Should().Be(IdentityPlace.ForStation());
        recipes[RecipeType.ViscaraOrchardRoast].Requirements.Single().RequirementText.Should().Be("Crafted in a city on Viscara.");
    }
}
