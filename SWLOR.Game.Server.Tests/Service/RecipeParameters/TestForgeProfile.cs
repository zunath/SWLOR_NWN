using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

/// <summary>
/// A test-only profile. It stands in for a minigame's profile in the generator, writer, audit and Bible tests.
/// </summary>
public sealed class TestForgeProfile : RecipeParameterProfile<TestForgeParameters>
{
    private static readonly IReadOnlyList<RecipeParameterDefinition> _definitions = new[]
    {
        RecipeParameterDefinition.Integer("center", "Test Forge Center", 20, 80, 6),
        RecipeParameterDefinition.Integer("width", "Test Forge Width", 8, 40, 4),
        RecipeParameterDefinition.Decimal("cooling", "Test Forge Cooling", 0.5, 3.0, 1, 0.4),
        RecipeParameterDefinition.Enumeration<TestForgeStyle>("style", "Test Forge Style"),
    };

    private readonly SkillType _skill;
    private readonly RecipeParameterOverrides _overrides;

    public TestForgeProfile()
        : this(SkillType.Smithery, true)
    {
    }

    public TestForgeProfile(SkillType skill, bool withOverrides)
    {
        _skill = skill;
        _overrides = withOverrides
            ? new RecipeParameterOverrides()
                .ForCategory(RecipeCategoryType.Breastplate, "Breastplates are worked hot", ("center", 65))
                .ForRecipe(RecipeType.BattlemasterBreastplate, "Starter set stays forgiving", ("cooling", 1.0), ("width", 30))
            : RecipeParameterOverrides.None;
    }

    public override string Key => "TestForge";
    public override SkillType Skill => _skill;
    public override IReadOnlyList<RecipeParameterDefinition> Definitions => _definitions;
    public override RecipeParameterOverrides Overrides => _overrides;

    public override IReadOnlyDictionary<string, double> BaseValues(RecipeParameterContext recipe)
    {
        return new Dictionary<string, double>
        {
            ["center"] = 40 + recipe.Level % 5 * 4,
            ["width"] = Math.Max(8, 36 - recipe.Level / 2),
            ["cooling"] = Math.Min(3.0, 1.0 + recipe.Components.Count * 0.25),
            ["style"] = recipe.Level < 20 ? (int)TestForgeStyle.Cold : recipe.Level < 40 ? (int)TestForgeStyle.Warm : (int)TestForgeStyle.Hot,
        };
    }

    protected override TestForgeParameters Create(RecipeParameterValues values)
    {
        return new TestForgeParameters(
            values.Int("center"),
            values.Int("width"),
            values.Double("cooling"),
            values.EnumValue<TestForgeStyle>("style"));
    }

    protected override double MeasureDifficulty(TestForgeParameters parameters)
    {
        return parameters.Cooling * 10 + (40 - parameters.Width) * 0.5;
    }

    public override DifficultyBudget BudgetFor(int recipeLevel)
    {
        return new DifficultyBudget(5, 40 + recipeLevel);
    }
}
