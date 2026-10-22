using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

/// <summary>
/// A test-only profile whose rules the test supplies, for exercising the generator and audit on bad profiles.
/// </summary>
public sealed class ConfigurableTestProfile : RecipeParameterProfile<TestForgeParameters>
{
    private readonly Func<RecipeParameterContext, IReadOnlyDictionary<string, double>> _baseValues;
    private readonly Func<TestForgeParameters, double> _difficulty;
    private readonly Func<int, DifficultyBudget> _budget;

    public ConfigurableTestProfile(
        Func<RecipeParameterContext, IReadOnlyDictionary<string, double>> baseValues,
        Func<TestForgeParameters, double> difficulty,
        Func<int, DifficultyBudget> budget,
        RecipeParameterOverrides overrides = null,
        IReadOnlyList<RecipeParameterDefinition> definitions = null,
        IReadOnlyDictionary<RecipeType, RecipeType> derived = null)
    {
        DerivedRecipes = derived ?? base.DerivedRecipes;
        _baseValues = baseValues;
        _difficulty = difficulty;
        _budget = budget;
        Overrides = overrides ?? RecipeParameterOverrides.None;
        Definitions = definitions ?? new[]
        {
            RecipeParameterDefinition.Integer("center", "Configurable Center", 0, 100, 20),
            RecipeParameterDefinition.Integer("width", "Configurable Width", 0, 100),
            RecipeParameterDefinition.Decimal("cooling", "Configurable Cooling", 0.0, 10.0, 1),
            RecipeParameterDefinition.Enumeration<TestForgeStyle>("style", "Configurable Style"),
        };
    }

    public override string Key => "Configurable";
    public override SkillType Skill => SkillType.Smithery;
    public override IReadOnlyList<RecipeParameterDefinition> Definitions { get; }
    public override RecipeParameterOverrides Overrides { get; }
    public override IReadOnlyDictionary<RecipeType, RecipeType> DerivedRecipes { get; }

    public static IReadOnlyDictionary<string, double> FlatBase(double center = 50)
    {
        return new Dictionary<string, double>
        {
            ["center"] = center,
            ["width"] = 20,
            ["cooling"] = 1.5,
            ["style"] = 2,
        };
    }

    public override IReadOnlyDictionary<string, double> BaseValues(RecipeParameterContext recipe)
    {
        return _baseValues(recipe);
    }

    public override DifficultyBudget BudgetFor(int recipeLevel)
    {
        return _budget(recipeLevel);
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
        return _difficulty(parameters);
    }
}
