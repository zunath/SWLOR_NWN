using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

/// <summary>Covers how the Plasma Forge profile turns a recipe's category, level and components into parameters.</summary>
public class ForgeParameterProfileTests
{
    private static readonly ForgeParameterProfile Profile = new();

    private static readonly IReadOnlyDictionary<string, int> Metal = new Dictionary<string, int> { ["ref_veldite"] = 4, ["fiberp_ruined"] = 2 };
    private static readonly IReadOnlyDictionary<string, int> Leather = new Dictionary<string, int> { ["lth_ruined"] = 4, ["fiberp_ruined"] = 2 };

    private static ForgeParameters Generate(RecipeCategoryType category, int level, IReadOnlyDictionary<string, int> components) =>
        (ForgeParameters)RecipeParameterGenerator.Generate(
            Profile,
            new RecipeParameterContext(RecipeType.Invalid, SkillType.Smithery, category, level, true, components)).Parameters;

    [Test]
    public void TheProfileCoversSmitheryAndKeysItsBibleColumnsWithForge()
    {
        Profile.Key.Should().Be("Forge");
        Profile.Skill.Should().Be(SkillType.Smithery);
        Profile.ParametersType.Should().Be(typeof(ForgeParameters));
        RecipeParameterAudit.ValidateProfile(Profile).Should().BeEmpty();
        RecipeParameterBibleColumns.Headers(Profile).Should().Equal(
            "Forge Range Center",
            "Forge Range Width",
            "Forge Cooling Rate",
            "Forge Starting Thermal",
            "Forge Shape Share",
            "Forge Override");
    }

    [Test]
    public void LeatherAndFiberplastGoodsWithoutRefinedMetalAreHeatSealed()
    {
        ForgeParameterProfile.IsHeatSealed(Leather).Should().BeTrue();
        ForgeParameterProfile.IsHeatSealed(new Dictionary<string, int> { ["fiberp_high"] = 3, ["ruby"] = 1 }).Should().BeTrue();
        ForgeParameterProfile.IsHeatSealed(Metal).Should().BeFalse("it has refined metal");
        ForgeParameterProfile.IsHeatSealed(new Dictionary<string, int> { ["ref_keromber"] = 2, ["lth_flawed"] = 2 }).Should().BeFalse();
        ForgeParameterProfile.IsHeatSealed(new Dictionary<string, int> { ["wood"] = 2, ["ruby"] = 1 }).Should().BeFalse("it has no leather or fiberplast");
    }

    [TestCase(1)]
    [TestCase(10)]
    [TestCase(25)]
    [TestCase(50)]
    public void HeatSealedGoodsUseALowerWiderRangeThanMetalAtTheSameLevel(int level)
    {
        var sealed_ = Generate(RecipeCategoryType.Tunic, level, Leather);
        var metal = Generate(RecipeCategoryType.Tunic, level, Metal);

        sealed_.RangeCenter.Should().BeLessThan(metal.RangeCenter);
        sealed_.RangeWidth.Should().BeGreaterThanOrEqualTo(metal.RangeWidth);
        sealed_.CoolingRate.Should().BeLessThanOrEqualTo(metal.CoolingRate);
    }

    [Test]
    public void HigherLevelsNarrowTheRangeAndCoolFaster()
    {
        var easy = Generate(RecipeCategoryType.Ring, 5, Metal);
        var hard = Generate(RecipeCategoryType.Ring, 50, Metal);

        hard.RangeWidth.Should().BeLessThan(easy.RangeWidth);
        hard.CoolingRate.Should().BeGreaterThan(easy.CoolingRate);
    }

    [TestCase(10)]
    [TestCase(30)]
    [TestCase(50)]
    public void LightGoodsLoseHeatFasterThanHeavyOnes(int level)
    {
        var light = Generate(RecipeCategoryType.Ring, level, Metal);
        var heavy = Generate(RecipeCategoryType.Breastplate, level, Metal);

        light.CoolingRate.Should().BeGreaterThanOrEqualTo(heavy.CoolingRate);
    }

    [Test]
    public void RecipesOfOneLevelAreSpreadSoOneSequenceDoesNotServeThemAll()
    {
        var recipes = RecipeDefinitionLoader.LoadGame()
            .Where(pair => pair.Value.Skill == SkillType.Smithery && pair.Value.IsActive && pair.Value.Level is >= 21 and <= 30)
            .Select(pair => (ForgeParameters)pair.Value.MinigameParameters)
            .ToList();

        recipes.Select(forge => forge.CoolingRate).Distinct().Count().Should().BeGreaterThanOrEqualTo(4);
        recipes.Select(forge => forge.ShapeShare).Distinct().Count().Should().BeGreaterThanOrEqualTo(5);
        recipes.Max(forge => forge.ShapeShare).Should().BeGreaterThanOrEqualTo(65);
        recipes.Min(forge => forge.ShapeShare).Should().BeLessThanOrEqualTo(35);
    }

    [Test]
    public void ArmorNeedsMoreShapeAndWeaponsMoreFinish()
    {
        var armor = new[] { RecipeCategoryType.Breastplate, RecipeCategoryType.Tunic, RecipeCategoryType.Legging, RecipeCategoryType.Shield }
            .Select(category => Generate(category, 30, Metal).ShapeShare);
        var weapons = new[]
        {
            RecipeCategoryType.Longsword, RecipeCategoryType.Pistol, RecipeCategoryType.Rifle, RecipeCategoryType.Lightsaber,
            RecipeCategoryType.Katar, RecipeCategoryType.TwinPistol
        }.Select(category => Generate(category, 30, Metal).ShapeShare);

        armor.Should().OnlyContain(share => share >= 55);
        weapons.Should().OnlyContain(share => share <= 45);
        armor.Min().Should().BeGreaterThan(weapons.Max());
    }

    [Test]
    public void EveryRecipesRangeAndStartLieOnTheThermalMeter_AndMostStartInsideTheRange()
    {
        var recipes = RecipeDefinitionLoader.LoadGame()
            .Where(pair => pair.Value.Skill == SkillType.Smithery && pair.Value.IsActive)
            .ToList();

        recipes.Should().NotBeEmpty();
        foreach (var (type, detail) in recipes)
        {
            var forge = (ForgeParameters)detail.MinigameParameters;
            forge.StartingThermal.Should().BeInRange(0, 100, type.ToString());
            forge.RangeLow.Should().BeGreaterThanOrEqualTo(0, type.ToString());
            forge.RangeHigh.Should().BeLessThanOrEqualTo(100, type.ToString());
        }

        var inside = recipes.Count(pair =>
        {
            var forge = (ForgeParameters)pair.Value.MinigameParameters;
            return forge.StartingThermal >= forge.RangeLow && forge.StartingThermal <= forge.RangeHigh;
        });
        inside.Should().BeGreaterThan(recipes.Count / 2, "a piece may start outside its range, but most start ready to work");
    }

    [Test]
    public void EveryTwinPistolCopiesItsSinglePistolsParameters()
    {
        var recipes = RecipeDefinitionLoader.LoadGame();

        Profile.DerivedRecipes.Should().HaveCount(11);
        foreach (var (pair, source) in Profile.DerivedRecipes)
        {
            recipes[pair].MinigameParameters.Should().NotBeNull(pair.ToString());
            recipes[pair].MinigameParameters.Should().BeOfType<ForgeParameters>();
            recipes[pair].MinigameParameters.Values.Should().Equal(recipes[source].MinigameParameters.Values, pair.ToString());
        }
    }

    [Test]
    public void TheBuilderMethodSetsTheParametersOfTheActiveRecipe()
    {
        var recipes = new RecipeBuilder()
            .Create(RecipeType.BattlemasterBreastplate, SkillType.Smithery)
            .ForgeParameters(rangeCenter: 60, rangeWidth: 24, coolingRate: 5, startingThermal: 58, shapeShare: 60)
            .Build();

        var forge = recipes[RecipeType.BattlemasterBreastplate].MinigameParameters.Should().BeOfType<ForgeParameters>().Subject;
        forge.Values.Select(value => (value.Name, value.Value)).Should().Equal(
            ("rangeCenter", 60d), ("rangeWidth", 24d), ("coolingRate", 5d), ("startingThermal", 58d), ("shapeShare", 60d));
        forge.RangeLow.Should().Be(48);
        forge.RangeHigh.Should().Be(72);
    }
}
