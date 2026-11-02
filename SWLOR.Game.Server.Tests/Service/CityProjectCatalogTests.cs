using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition;
using SWLOR.Game.Server.Service.CityProjectService;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The materials each City Project asks for. They are a proposal the owner approves, so these tests pin how they were derived from the
/// buildings' own Fabrication recipes: change a row on purpose and the test says what the proposal no longer follows.
/// </summary>
public class CityProjectCatalogTests
{
    private static readonly (CityProjectType Type, RecipeType Building, int[] Levels)[] Facilities =
    {
        (CityProjectType.MedicalCenterUpgrade, RecipeType.MedicalCenterStyle1, new[] { 2, 3, 4, 5 }),
        (CityProjectType.StarportUpgrade, RecipeType.StarportStyle1, new[] { 2, 3, 4, 5 }),
        (CityProjectType.CantinaUpgrade, RecipeType.CantinaStyle1, new[] { 2, 3, 4, 5 }),
        (CityProjectType.JumpBeaconUpgrade, RecipeType.JumpBeacon, new[] { 2, 3 }),
    };

    private static int RoundedUp(int baseAmount, int level) => (int)Math.Ceiling(baseAmount * level / 2.0);

    [Test]
    public void EveryFacilityLevelHasMaterials_FromTheSecondUpToItsTop()
    {
        foreach (var (type, _, levels) in Facilities)
        {
            foreach (var level in levels)
                CityProjectCatalog.Get(type, level).Should().NotBeNull($"{type} level {level}");

            CityProjectCatalog.Get(type, 1).Should().BeNull("level 1 is what every city starts with");
            CityProjectCatalog.Get(type, levels.Max() + 1).Should().BeNull($"{type} stops at {levels.Max()}");
        }

        CityProjectCatalog.Get(CityProjectType.HomeGround, 0).Should().NotBeNull();
    }

    [Test]
    public void EachLevelNeedsTheBuildingsRecipeTimesLevelOverTwo_RoundedUp()
    {
        var recipes = new StructureRecipes().BuildRecipes();

        foreach (var (type, building, levels) in Facilities)
        {
            var recipe = recipes[building];
            var parts = recipe.Components[CityProjectCatalog.ReinforcedConstructionParts];
            var units = recipe.Components[CityProjectCatalog.ReinforcedPowerSupplyUnit];

            foreach (var level in levels)
            {
                var materials = CityProjectCatalog.Get(type, level).Materials;
                materials[CityProjectCatalog.ReinforcedConstructionParts].Should().Be(RoundedUp(parts, level), $"{type} level {level} parts");
                materials[CityProjectCatalog.ReinforcedPowerSupplyUnit].Should().Be(RoundedUp(units, level), $"{type} level {level} units");
            }
        }
    }

    [Test]
    public void HomeGround_IsOneAndAHalfStarportsOfMaterials()
    {
        var starport = new StructureRecipes().BuildRecipes()[RecipeType.StarportStyle1];

        var materials = CityProjectCatalog.Get(CityProjectType.HomeGround, 0).Materials;

        materials[CityProjectCatalog.ReinforcedConstructionParts].Should().Be(starport.Components[CityProjectCatalog.ReinforcedConstructionParts] * 3 / 2);
        materials[CityProjectCatalog.ReinforcedPowerSupplyUnit].Should().Be(starport.Components[CityProjectCatalog.ReinforcedPowerSupplyUnit] * 3 / 2);
    }

    [Test]
    public void EveryMaterial_IsAnItemSomeCraftingRecipeMakes_AndAsksForAWholePositiveNumber()
    {
        var made = new StructureRecipes().BuildRecipes().Values.Select(recipe => recipe.Resref).ToHashSet();

        foreach (var recipe in CityProjectCatalog.GetAll())
        {
            recipe.Materials.Should().NotBeEmpty();
            foreach (var (resref, count) in recipe.Materials)
            {
                count.Should().BePositive($"{recipe.Type} level {recipe.TargetLevel} {resref}");
                made.Should().Contain(resref, $"{resref} is an existing Fabrication product");
            }
        }
    }

    [Test]
    public void LaterLevels_NeverAskForLess_ThanEarlierOnes()
    {
        foreach (var (type, _, levels) in Facilities)
        {
            var previous = 0;
            foreach (var level in levels)
            {
                var total = CityProjectCatalog.Get(type, level).Materials.Values.Sum();
                total.Should().BeGreaterThanOrEqualTo(previous, $"{type} level {level}");
                previous = total;
            }
        }
    }

    [Test]
    public void TheCreditsFollowTheMaterials_BothClimbWithTheLevel()
    {
        CityProjectRules.GetCredits(CityProjectType.StarportUpgrade, 5)
            .Should().BeGreaterThan(CityProjectRules.GetCredits(CityProjectType.StarportUpgrade, 2));
        CityProjectRules.HomeGroundCredits.Should().Be(CityProjectRules.GetCredits(CityProjectType.StarportUpgrade, 3), "priced with the middle upgrades");
    }
}
