using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService;

/// <summary>
/// Turns an Engineering recipe into its Circuit minigame values. The component grade sets the overload line (a better grade
/// tolerates more Load), the category sets how much each Load point amplifies, and the category also sets how the work divides
/// between Assembly and Calibration. Each value then moves a little by the recipe's seeded draw.
/// </summary>
public sealed class CircuitParameterProfile : RecipeParameterProfile<CircuitParameters>
{
    public const string ProfileKey = "Circuit";

    public const int LineAtGradeOffset = 3;
    public const int MaxLine = 9;

    private const int DroidPartBoostPercent = 12;
    private const int EnhancementBoostPercent = 8;
    private const int ModuleBoostPercent = 10;

    private static readonly IReadOnlyList<RecipeParameterDefinition> _definitions = new[]
    {
        RecipeParameterDefinition.Integer("overloadLine", "Circuit Overload Line", 3, MaxLine, 1),
        RecipeParameterDefinition.Integer("loadBoostPercent", "Circuit Load Boost %", 6, 14, 1),
        RecipeParameterDefinition.Integer("assemblySharePercent", "Circuit Assembly Share %", 30, 70, 5)
    };

    public override string Key => ProfileKey;

    public override SkillType Skill => SkillType.Engineering;

    public override IReadOnlyList<RecipeParameterDefinition> Definitions => _definitions;

    /// <summary>The overload line a recipe of the grade starts from, before the recipe's own variation.</summary>
    public static int LineForGrade(int grade) => Math.Min(MaxLine, LineAtGradeOffset + Math.Max(1, grade));

    public override IReadOnlyDictionary<string, double> BaseValues(RecipeParameterContext recipe)
    {
        var grade = CircuitComponentGrades.ForRecipe(recipe.Level, recipe.Components.Keys);
        var (boost, share) = Classify(recipe.Category);

        return new Dictionary<string, double>
        {
            ["overloadLine"] = LineForGrade(grade),
            ["loadBoostPercent"] = boost,
            ["assemblySharePercent"] = share
        };
    }

    protected override CircuitParameters Create(RecipeParameterValues values) =>
        new(values.Int("overloadLine"), values.Int("loadBoostPercent"), values.Int("assemblySharePercent"));

    /// <summary>A lower overload line is harder, and so is a lopsided split: one meter then carries most of the work.</summary>
    protected override double MeasureDifficulty(CircuitParameters parameters) =>
        (10 - parameters.OverloadLine) * 10 + Math.Abs(parameters.AssemblySharePercent - 50) * 0.5;

    public override DifficultyBudget BudgetFor(int recipeLevel)
    {
        var expected = (10 - LineForGrade(CircuitComponentGrades.TierOfLevel(recipeLevel))) * 10;
        return new DifficultyBudget(expected - 15, expected + 22);
    }

    /// <summary>
    /// The Load boost and the Assembly share of a category. Droid parts amplify most, enhancements least; structure-like ship
    /// work is mostly Assembly and enhancements and droid CPUs are mostly Calibration.
    /// </summary>
    private static (int BoostPercent, int SharePercent) Classify(RecipeCategoryType category) => category switch
    {
        RecipeCategoryType.DroidArms => (DroidPartBoostPercent, 55),
        RecipeCategoryType.DroidBody => (DroidPartBoostPercent, 60),
        RecipeCategoryType.DroidHead => (DroidPartBoostPercent, 50),
        RecipeCategoryType.DroidLegs => (DroidPartBoostPercent, 55),
        RecipeCategoryType.DroidCPU => (DroidPartBoostPercent, 40),
        RecipeCategoryType.DroidComponent => (DroidPartBoostPercent, 50),
        RecipeCategoryType.Breastplate => (DroidPartBoostPercent, 55),
        RecipeCategoryType.Helmet or RecipeCategoryType.Cloak or RecipeCategoryType.Belt or RecipeCategoryType.Bracer or
            RecipeCategoryType.Legging or RecipeCategoryType.Glove or RecipeCategoryType.Necklace or RecipeCategoryType.Ring =>
            (DroidPartBoostPercent, 50),

        RecipeCategoryType.ArmorEnhancement or RecipeCategoryType.WeaponEnhancement or
            RecipeCategoryType.StructureEnhancement or RecipeCategoryType.ModuleEnhancement or
            RecipeCategoryType.StarshipEnhancement or RecipeCategoryType.DroidEnhancement or
            RecipeCategoryType.CookingEnhancement => (EnhancementBoostPercent, 40),
        RecipeCategoryType.DroidInstruction => (EnhancementBoostPercent, 40),

        RecipeCategoryType.ShipModule => (ModuleBoostPercent, 55),
        RecipeCategoryType.CapitalShipModule => (ModuleBoostPercent, 60),
        RecipeCategoryType.Starship or RecipeCategoryType.CapitalConstruction => (ModuleBoostPercent, 65),
        RecipeCategoryType.StarshipAmmo => (ModuleBoostPercent, 60),
        RecipeCategoryType.MusicPlayers or RecipeCategoryType.CantinaMusicDiscs or RecipeCategoryType.BattleMusicDiscs or
            RecipeCategoryType.ThemeMusicDiscs or RecipeCategoryType.LocationMusicDiscs or
            RecipeCategoryType.AmbientMusicDiscs => (ModuleBoostPercent, 45),
        RecipeCategoryType.Lightsaber or RecipeCategoryType.Saberstaff => (ModuleBoostPercent, 45),
        _ => (ModuleBoostPercent, 50)
    };
}
