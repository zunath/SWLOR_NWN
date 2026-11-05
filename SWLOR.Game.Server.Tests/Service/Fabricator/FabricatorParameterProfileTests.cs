using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service.Fabricator;

/// <summary>Covers how the Fabricator profile turns a recipe's category and components into parameters.</summary>
public class FabricatorParameterProfileTests
{
    private readonly FabricatorParameterProfile _profile = new();

    private static RecipeParameterContext Recipe(RecipeCategoryType category, int level, params string[] components) =>
        new(
            RecipeType.Invalid,
            SkillType.Fabrication,
            category,
            level,
            true,
            components.ToDictionary(resref => resref, _ => 1));

    private static IReadOnlyDictionary<string, double> Base(RecipeCategoryType category, int level, params string[] components) =>
        new FabricatorParameterProfile().BaseValues(Recipe(category, level, components));

    [Test]
    public void TheProfileIsWellFormed_AndCoversActiveFabricationRecipesOnly()
    {
        RecipeParameterAudit.ValidateProfile(_profile).Should().BeEmpty();
        _profile.Key.Should().Be("Fabricator");
        _profile.AppliesTo(Recipe(RecipeCategoryType.Bed, 10)).Should().BeTrue();
        _profile.AppliesTo(new RecipeParameterContext(
            RecipeType.Invalid, SkillType.Smithery, RecipeCategoryType.Bed, 10, true, new Dictionary<string, int>())).Should().BeFalse();
        _profile.AppliesTo(new RecipeParameterContext(
            RecipeType.Invalid, SkillType.Fabrication, RecipeCategoryType.Bed, 10, false, new Dictionary<string, int>())).Should().BeFalse();
        RecipeParameterBibleColumns.Headers(_profile).Should().Equal(
            "Fabricator Frame Size %", "Fabricator Shell Size %", "Fabricator Frame Quality %", "Fabricator Shell Quality %",
            "Fabricator Wear Rate", "Fabricator Override");
    }

    [Test]
    public void ABedIsMostlyFrame_AStatueMostlyShell_AndATerminalMostlyFittings()
    {
        var bed = Base(RecipeCategoryType.Bed, 10, "wood");
        bed["frameSizePercent"].Should().BeGreaterThan(bed["shellSizePercent"]).And.BeGreaterThan(100 - bed["frameSizePercent"] - bed["shellSizePercent"]);
        bed["frameQualityPercent"].Should().BeGreaterThan(50);

        var statue = Base(RecipeCategoryType.Statues, 10, "ref_veldite");
        statue["shellSizePercent"].Should().BeGreaterThan(statue["frameSizePercent"]);
        statue["shellQualityPercent"].Should().BeGreaterThan(50);

        var terminal = Base(RecipeCategoryType.Electronics, 10, "elec_flawed");
        var fittingsSize = 100 - terminal["frameSizePercent"] - terminal["shellSizePercent"];
        var fittingsQuality = 100 - terminal["frameQualityPercent"] - terminal["shellQualityPercent"];
        fittingsSize.Should().BeGreaterThan(terminal["frameSizePercent"]).And.BeGreaterThan(terminal["shellSizePercent"]);
        fittingsQuality.Should().BeGreaterThan(50);
    }

    [Test]
    public void EveryCategorysBaseSplitLeavesEverySubAssemblyItsMinimumShare()
    {
        foreach (var category in Enum.GetValues<RecipeCategoryType>())
        {
            var values = Base(category, 10, "wood");
            (100 - values["frameSizePercent"] - values["shellSizePercent"]).Should().BeGreaterThanOrEqualTo(FabricatorParameterProfile.MinimumShare, category.ToString());
            (100 - values["frameQualityPercent"] - values["shellQualityPercent"]).Should().BeGreaterThanOrEqualTo(FabricatorParameterProfile.MinimumShare, category.ToString());
            values["frameSizePercent"].Should().BeGreaterThanOrEqualTo(FabricatorParameterProfile.MinimumShare);
            values["shellSizePercent"].Should().BeGreaterThanOrEqualTo(FabricatorParameterProfile.MinimumShare);
        }
    }

    [Test]
    public void TheWearComesFromTheMaterials()
    {
        Base(RecipeCategoryType.Seating, 5, "wood", "lth_ruined")["wearRate"]
            .Should().BeLessThan(Base(RecipeCategoryType.Seating, 45, "ref_arkoxit", "ancient_wood")["wearRate"]);
        Base(RecipeCategoryType.Seating, 10, "ref_veldite")["wearRate"].Should().Be(FabricatorParameterProfile.WearForHardness(2));
    }

    [Test]
    public void ASplitThatLeavesTheFittingsUnderTheirMinimumIsRefused_ByTheDifficultyBudget()
    {
        var valid = new FabricatorParameters(40, 40, 40, 40, 4);
        var tooLittleFittingsWork = new FabricatorParameters(50, 40, 40, 40, 4);
        var tooLittleFittingsQuality = new FabricatorParameters(40, 40, 50, 40, 4);

        var budget = _profile.BudgetFor(20);
        budget.Contains(Difficulty(valid)).Should().BeTrue();
        budget.Contains(Difficulty(tooLittleFittingsWork)).Should().BeFalse();
        budget.Contains(Difficulty(tooLittleFittingsQuality)).Should().BeFalse();
    }

    [Test]
    public void Difficulty_RisesWithWear_AndTheBudgetRisesWithTheLevel()
    {
        Difficulty(new FabricatorParameters(34, 33, 34, 33, 6)).Should().BeGreaterThan(Difficulty(new FabricatorParameters(34, 33, 34, 33, 3)));
        _profile.BudgetFor(50).Max.Should().BeGreaterThan(_profile.BudgetFor(1).Max);
        _profile.BudgetFor(50).Min.Should().BeGreaterThan(_profile.BudgetFor(1).Min);
    }

    [Test]
    public void Generation_IsStableAndStaysInsideBoundsAndBudget_ForEveryLevelAndCategory()
    {
        var categories = new[]
        {
            RecipeCategoryType.Bed, RecipeCategoryType.Seating, RecipeCategoryType.Statues, RecipeCategoryType.Lighting,
            RecipeCategoryType.Electronics, RecipeCategoryType.Structure, RecipeCategoryType.Tool, RecipeCategoryType.Flooring
        };

        foreach (var category in categories)
        {
            for (var level = 1; level <= 52; level++)
            {
                var resref = level < 15 ? "wood" : level < 35 ? "ref_plagionite" : "ref_arkoxit";
                var context = Recipe(category, level, resref, "fiberp_good");

                var first = RecipeParameterGenerator.Generate(_profile, context);
                var second = RecipeParameterGenerator.Generate(_profile, context);

                first.Values.Should().BeEquivalentTo(second.Values);
                foreach (var definition in _profile.Definitions)
                    definition.IsInBounds(first.Values[definition.Name]).Should().BeTrue($"{category} level {level} {definition.Name}");
                _profile.BudgetFor(level).Contains(first.Difficulty).Should().BeTrue($"{category} level {level}");
                first.Parameters.Should().BeOfType<FabricatorParameters>();
            }
        }
    }

    [Test]
    public void TheParametersListTheirValuesInDefinitionOrder_AndTheFallbackIsAnEvenSplit()
    {
        var parameters = new FabricatorParameters(40, 35, 30, 25, 5);

        parameters.BuilderMethod.Should().Be("FabricatorParameters");
        parameters.Values.Select(value => (value.Name, value.Value)).Should().Equal(
            ("frameSizePercent", 40.0), ("shellSizePercent", 35.0), ("frameQualityPercent", 30.0), ("shellQualityPercent", 25.0),
            ("wearRate", 5.0));
        _profile.Definitions.Select(definition => definition.Name).Should().Equal(parameters.Values.Select(value => value.Name));
        parameters.FittingsSizePercent.Should().Be(25);
        parameters.FittingsQualityPercent.Should().Be(45);
        (FabricatorParameters.Fallback.FrameSizePercent + FabricatorParameters.Fallback.ShellSizePercent +
         FabricatorParameters.Fallback.FittingsSizePercent).Should().Be(100);
    }

    private double Difficulty(FabricatorParameters parameters) => ((IRecipeParameterProfile)_profile).MeasureDifficulty(parameters);
}
