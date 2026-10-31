using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service.Circuit;

/// <summary>Covers how the Circuit profile turns a recipe's components and category into parameters.</summary>
public class CircuitParameterProfileTests
{
    private readonly CircuitParameterProfile _profile = new();

    private static RecipeParameterContext Recipe(RecipeCategoryType category, int level, params string[] components) =>
        new(
            RecipeType.Invalid,
            SkillType.Engineering,
            category,
            level,
            true,
            components.ToDictionary(resref => resref, _ => 1));

    private static IReadOnlyDictionary<string, double> Base(RecipeCategoryType category, int level, params string[] components) =>
        new CircuitParameterProfile().BaseValues(Recipe(category, level, components));

    [Test]
    public void TheProfileIsWellFormed_AndCoversActiveEngineeringRecipesOnly()
    {
        RecipeParameterAudit.ValidateProfile(_profile).Should().BeEmpty();
        _profile.Key.Should().Be("Circuit");
        _profile.AppliesTo(Recipe(RecipeCategoryType.ShipModule, 10)).Should().BeTrue();
        _profile.AppliesTo(new RecipeParameterContext(
            RecipeType.Invalid, SkillType.Smithery, RecipeCategoryType.ShipModule, 10, true, new Dictionary<string, int>()))
            .Should().BeFalse();
        _profile.AppliesTo(new RecipeParameterContext(
            RecipeType.Invalid, SkillType.Engineering, RecipeCategoryType.ShipModule, 10, false, new Dictionary<string, int>()))
            .Should().BeFalse();
        RecipeParameterBibleColumns.Headers(_profile).Should().Equal(
            "Circuit Overload Line", "Circuit Load Boost %", "Circuit Assembly Share %", "Circuit Override");
    }

    [TestCase(1, 4)]
    [TestCase(2, 5)]
    [TestCase(3, 6)]
    [TestCase(4, 7)]
    [TestCase(5, 8)]
    [TestCase(6, 9)]
    public void TheComponentGradeSetsTheOverloadLine_AndABetterGradeToleratesMoreLoad(int grade, int line)
    {
        CircuitParameterProfile.LineForGrade(grade).Should().Be(line);
    }

    [Test]
    public void TheBestComponentOfTheRecipeSetsItsLine()
    {
        Base(RecipeCategoryType.ShipModule, 12, "elec_flawed", "ref_currian", "jade")["overloadLine"].Should().Be(5);
        Base(RecipeCategoryType.ShipModule, 12, "elec_ruined", "ref_currian")["overloadLine"].Should().Be(5);
        Base(RecipeCategoryType.ShipModule, 44, "elec_high", "ruby")["overloadLine"].Should().Be(8);
        Base(RecipeCategoryType.CapitalConstruction, 52, "capc_power", "elec_high")["overloadLine"].Should().Be(9);
        Base(RecipeCategoryType.Tool, 15, "sr_token")["overloadLine"].Should().Be(5, "an ungraded recipe takes its level's tier");
    }

    [Test]
    public void DroidPartsEnhancementsAndModulesAmplifyDifferently()
    {
        Base(RecipeCategoryType.DroidArms, 20, "elec_flawed")["loadBoostPercent"].Should().Be(12);
        Base(RecipeCategoryType.DroidCPU, 20, "elec_flawed")["loadBoostPercent"].Should().Be(12);
        Base(RecipeCategoryType.Helmet, 20, "elec_flawed")["loadBoostPercent"].Should().Be(12);
        Base(RecipeCategoryType.WeaponEnhancement, 20, "elec_flawed")["loadBoostPercent"].Should().Be(8);
        Base(RecipeCategoryType.DroidEnhancement, 20, "elec_flawed")["loadBoostPercent"].Should().Be(8);
        Base(RecipeCategoryType.DroidInstruction, 20, "elec_flawed")["loadBoostPercent"].Should().Be(8);
        Base(RecipeCategoryType.ShipModule, 20, "elec_flawed")["loadBoostPercent"].Should().Be(10);
        Base(RecipeCategoryType.Starship, 20, "elec_flawed")["loadBoostPercent"].Should().Be(10);
    }

    [Test]
    public void TheCategoryDividesTheWorkBetweenAssemblyAndCalibration()
    {
        Base(RecipeCategoryType.Starship, 20, "elec_flawed")["assemblySharePercent"].Should().Be(65, "a hull is mostly assembly");
        Base(RecipeCategoryType.DroidCPU, 20, "elec_flawed")["assemblySharePercent"].Should().Be(40, "a CPU is mostly tuning");
        Base(RecipeCategoryType.ArmorEnhancement, 20, "elec_flawed")["assemblySharePercent"].Should().Be(40);
        Base(RecipeCategoryType.ShipModule, 20, "elec_flawed")["assemblySharePercent"].Should().Be(55);
        Base(RecipeCategoryType.Tool, 20, "elec_flawed")["assemblySharePercent"].Should().Be(50, "an unlisted category splits evenly");
    }

    [Test]
    public void Generation_IsStableAndStaysInsideBoundsAndBudget_ForEveryGradeAndCategory()
    {
        var categories = new[]
        {
            RecipeCategoryType.DroidBody, RecipeCategoryType.WeaponEnhancement, RecipeCategoryType.ShipModule,
            RecipeCategoryType.Starship, RecipeCategoryType.Tool, RecipeCategoryType.CantinaMusicDiscs
        };
        var resrefs = new[] { "elec_ruined", "elec_flawed", "elec_good", "elec_imperfect", "elec_high" };

        foreach (var category in categories)
        {
            for (var level = 1; level <= 50; level++)
            {
                var resref = resrefs[CircuitComponentGrades.TierOfLevel(level) - 1];
                var context = Recipe(category, level, resref);

                var first = RecipeParameterGenerator.Generate(_profile, context);
                var second = RecipeParameterGenerator.Generate(_profile, context);

                first.Values.Should().BeEquivalentTo(second.Values);
                foreach (var definition in _profile.Definitions)
                    definition.IsInBounds(first.Values[definition.Name]).Should().BeTrue($"{category} level {level} {definition.Name}");
                _profile.BudgetFor(level).Contains(first.Difficulty).Should().BeTrue($"{category} level {level}");
            }
        }
    }

    [Test]
    public void Difficulty_RisesAsTheLineFallsAndAsTheSplitGrowsLopsided()
    {
        var even = _profile.MeasureDifficultyOf(new CircuitParameters(6, 10, 50));
        _profile.MeasureDifficultyOf(new CircuitParameters(5, 10, 50)).Should().BeGreaterThan(even);
        _profile.MeasureDifficultyOf(new CircuitParameters(6, 10, 65)).Should().BeGreaterThan(even);
        _profile.MeasureDifficultyOf(new CircuitParameters(6, 10, 35)).Should().BeGreaterThan(even);
        _profile.MeasureDifficultyOf(new CircuitParameters(6, 14, 50)).Should().Be(even, "the Load boost rewards, it does not add difficulty");
    }

    [Test]
    public void TheBudgetFollowsTheLevelsTier()
    {
        _profile.BudgetFor(10).Contains(60).Should().BeTrue("a tier 1 recipe at line 4");
        _profile.BudgetFor(10).Contains(10).Should().BeFalse("line 9 is not a tier 1 recipe's line");
        _profile.BudgetFor(60).Contains(10).Should().BeTrue("a capital recipe at line 9");
    }

    [Test]
    public void TheParametersListTheirValuesInDefinitionOrder_AndTheFallbackIsTheMiddleOfEveryBound()
    {
        var parameters = new CircuitParameters(7, 9, 55);

        parameters.BuilderMethod.Should().Be("CircuitParameters");
        parameters.Values.Select(value => (value.Name, value.Value)).Should().Equal(
            ("overloadLine", 7.0), ("loadBoostPercent", 9.0), ("assemblySharePercent", 55.0));
        _profile.Definitions.Select(definition => definition.Name)
            .Should().Equal(parameters.Values.Select(value => value.Name));
        CircuitParameters.Fallback.OverloadLine.Should().Be(6);
    }
}

internal static class CircuitProfileTestExtensions
{
    public static double MeasureDifficultyOf(this CircuitParameterProfile profile, CircuitParameters parameters) =>
        ((IRecipeParameterProfile)profile).MeasureDifficulty(parameters);
}
