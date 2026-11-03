using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService;

/// <summary>
/// Turns a Fabrication recipe into its Fabricator minigame values. The category divides the work and the quality between the
/// Frame, the Shell and the Fittings (a bed is mostly Frame, a terminal mostly Fittings), and the recipe's components set how fast
/// its work wears the heads. Each value then moves a little by the recipe's seeded draw.
/// </summary>
public sealed class FabricatorParameterProfile : RecipeParameterProfile<FabricatorParameters>
{
    public const string ProfileKey = "Fabricator";

    /// <summary>The smallest share of the work, or of the quality, any one sub-assembly may have.</summary>
    public const int MinimumShare = 15;

    public const int MinimumWear = 2;
    public const int MaximumWear = 9;

    /// <summary>Wear a recipe of the softest materials starts from, and the wear each point of hardness adds.</summary>
    private const double WearAtSoftest = 2.0;
    private const double WearPerHardness = 1.15;

    /// <summary>The difficulty a value outside the share rules adds, so the generator never accepts it.</summary>
    private const double InvalidShareDifficulty = 1000;

    private static readonly IReadOnlyList<RecipeParameterDefinition> _definitions = new[]
    {
        RecipeParameterDefinition.Integer("frameSizePercent", "Fabricator Frame Size %", MinimumShare, 70, 5),
        RecipeParameterDefinition.Integer("shellSizePercent", "Fabricator Shell Size %", MinimumShare, 70, 5),
        RecipeParameterDefinition.Integer("frameQualityPercent", "Fabricator Frame Quality %", MinimumShare, 70, 5),
        RecipeParameterDefinition.Integer("shellQualityPercent", "Fabricator Shell Quality %", MinimumShare, 70, 5),
        RecipeParameterDefinition.Integer("wearRate", "Fabricator Wear Rate", MinimumWear, MaximumWear, 1)
    };

    public override string Key => ProfileKey;

    public override SkillType Skill => SkillType.Fabrication;

    public override IReadOnlyList<RecipeParameterDefinition> Definitions => _definitions;

    /// <summary>The wear rate a recipe of the given hardness starts from, before the recipe's own variation.</summary>
    public static int WearForHardness(double hardness) =>
        Math.Clamp((int)Math.Round(WearAtSoftest + WearPerHardness * hardness, MidpointRounding.AwayFromZero), MinimumWear, MaximumWear);

    /// <summary>The wear a recipe of the level is expected to have: the heads wear faster on the harder materials of higher levels.</summary>
    public static double ExpectedWear(int recipeLevel) => 3.6 + 4.2 * (Math.Clamp(recipeLevel, 1, 52) - 1) / 51.0;

    public override IReadOnlyDictionary<string, double> BaseValues(RecipeParameterContext recipe)
    {
        var (frameSize, shellSize, frameQuality, shellQuality) = Shares(recipe.Category);
        var hardness = FabricatorMaterials.ForRecipe(recipe.Components);

        return new Dictionary<string, double>
        {
            ["frameSizePercent"] = frameSize,
            ["shellSizePercent"] = shellSize,
            ["frameQualityPercent"] = frameQuality,
            ["shellQualityPercent"] = shellQuality,
            ["wearRate"] = WearForHardness(hardness)
        };
    }

    protected override FabricatorParameters Create(RecipeParameterValues values) =>
        new(
            values.Int("frameSizePercent"),
            values.Int("shellSizePercent"),
            values.Int("frameQualityPercent"),
            values.Int("shellQualityPercent"),
            values.Int("wearRate"));

    /// <summary>
    /// Wear is the difficulty: every point costs the machine 10 more over a craft. A split that leaves any sub-assembly under
    /// <see cref="MinimumShare"/> is refused outright.
    /// </summary>
    protected override double MeasureDifficulty(FabricatorParameters parameters)
    {
        var invalid = parameters.FittingsSizePercent < MinimumShare || parameters.FittingsQualityPercent < MinimumShare;
        return 10.0 * parameters.WearRate + (invalid ? InvalidShareDifficulty : 0);
    }

    public override DifficultyBudget BudgetFor(int recipeLevel)
    {
        var expected = 10.0 * ExpectedWear(recipeLevel);
        return new DifficultyBudget(Math.Floor(expected - 25), Math.Ceiling(expected + 25));
    }

    /// <summary>
    /// How a category divides the recipe's work and quality: the Frame and the Shell shares of each, the Fittings taking the rest.
    /// Beds and seating are mostly Frame, statues and flooring mostly Shell, and terminals, lighting and tools mostly Fittings.
    /// </summary>
    private static (int FrameSize, int ShellSize, int FrameQuality, int ShellQuality) Shares(RecipeCategoryType category) =>
        category switch
        {
            RecipeCategoryType.Bed => (55, 30, 60, 25),
            RecipeCategoryType.Seating => (45, 40, 45, 40),
            RecipeCategoryType.Surfaces => (40, 45, 40, 45),
            RecipeCategoryType.Statues => (30, 55, 25, 60),
            RecipeCategoryType.Wall => (35, 50, 30, 55),
            RecipeCategoryType.Flooring => (25, 60, 25, 60),
            RecipeCategoryType.Doors => (40, 30, 40, 30),
            RecipeCategoryType.Lighting => (20, 30, 15, 35),
            RecipeCategoryType.Electronics => (25, 25, 20, 25),
            RecipeCategoryType.Fixtures => (35, 25, 30, 25),
            RecipeCategoryType.Structure => (30, 25, 25, 25),
            RecipeCategoryType.Tool => (25, 20, 20, 20),
            _ => (34, 33, 34, 33)
        };
}
