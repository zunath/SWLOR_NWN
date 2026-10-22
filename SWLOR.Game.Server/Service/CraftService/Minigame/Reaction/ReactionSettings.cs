namespace SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;

/// <summary>
/// The immutable inputs of one Reaction attempt: the recipe's cycle and Peak intensity, and the amounts Espionage rank gives each
/// action. An engine holds one and never changes it.
/// </summary>
public sealed class ReactionSettings
{
    public ReactionSettings(ReactionParameters parameters, int espionageRank, int recipeLevel)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));
        if (parameters.PeakIntensity < 1 || parameters.PeakIntensity > 5)
        {
            throw new ArgumentOutOfRangeException(
                nameof(parameters), parameters.PeakIntensity, "Peak intensity runs 1 to 5.");
        }

        Cycle = new ReactionCycle(parameters.CyclePattern, parameters.CycleLength);
        PeakIntensity = parameters.PeakIntensity;
        Rank = Math.Max(espionageRank, 0);
        RecipeLevel = recipeLevel;

        Craftsmanship = ReactionRules.EquivalentCraftsmanship(Rank);
        Control = ReactionRules.EquivalentControl(Rank);
        MaxCp = ReactionRules.MaxCp(Rank);

        var progressFactor = ReactionRules.ProgressFactor(Rank, RecipeLevel);
        var qualityFactor = ReactionRules.QualityFactor(Rank, RecipeLevel);
        MeasurePotency = ReactionRules.ScaleGain(ReactionRules.MeasurePotency, Craftsmanship, progressFactor);
        ChargePotency = ReactionRules.ScaleGain(ReactionRules.ChargePotency, Craftsmanship, progressFactor);
        RefineSubtlety = ReactionRules.ScaleGain(ReactionRules.RefineSubtlety, Control, qualityFactor);
        PeakChargeStability = ReactionRules.PeakChargeStability(PeakIntensity);
    }

    public ReactionCycle Cycle { get; }

    /// <summary>How violent the Peak is, 1 to 5.</summary>
    public int PeakIntensity { get; }

    public int Rank { get; }

    public int RecipeLevel { get; }

    /// <summary>The Craftsmanship Espionage rank stands in for.</summary>
    public int Craftsmanship { get; }

    /// <summary>The Control Espionage rank stands in for.</summary>
    public int Control { get; }

    public int MaxCp { get; }

    /// <summary>The Potency Measure adds in any phase.</summary>
    public int MeasurePotency { get; }

    /// <summary>The Potency Charge adds before the Peak doubling.</summary>
    public int ChargePotency { get; }

    /// <summary>The Subtlety Refine adds before the Settling doubling or the Peak halving.</summary>
    public int RefineSubtlety { get; }

    /// <summary>The Stability a Charge at Peak costs beyond the action cost.</summary>
    public int PeakChargeStability { get; }

    /// <summary>Builds the settings for an Espionage recipe and the Espionage rank of the crafter.</summary>
    public static ReactionSettings ForRecipe(RecipeDetail recipe, int espionageRank)
    {
        if (recipe == null)
            throw new ArgumentNullException(nameof(recipe));
        if (recipe.MinigameParameters is not ReactionParameters parameters)
            throw new InvalidOperationException("The recipe has no Reaction parameters.");

        return new ReactionSettings(parameters, espionageRank, recipe.Level);
    }
}
