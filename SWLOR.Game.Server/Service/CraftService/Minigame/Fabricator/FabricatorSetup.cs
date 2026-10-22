using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;

/// <summary>
/// Everything the Fabricator engine takes as fixed for one craft: the recipe's size and parameters, the crafter's rank and gear, and
/// the progress the selected enhancements add. It holds no state; the engine's state record does.
/// </summary>
public sealed class FabricatorSetup
{
    /// <param name="parameters">The recipe's Fabricator parameters; null plays with <see cref="FabricatorParameters.Fallback"/>.</param>
    /// <param name="recipeLevel">The recipe's level.</param>
    /// <param name="baseProgress">The recipe level's progress size, from the level chart, in whole points.</param>
    /// <param name="rank">The crafter's Fabrication rank.</param>
    /// <param name="craftsmanship">The crafter's Fabrication Craftsmanship, which powers Run.</param>
    /// <param name="control">The crafter's Fabrication Control, which powers Precision Pass.</param>
    /// <param name="gearCp">The CP from the crafter's gear.</param>
    /// <param name="enhancementProgressPenalty">The progress points the selected enhancements add to what the piece needs.</param>
    public FabricatorSetup(
        FabricatorParameters parameters,
        int recipeLevel,
        int baseProgress,
        int rank,
        int craftsmanship,
        int control,
        int gearCp,
        int enhancementProgressPenalty = 0)
    {
        Parameters = parameters ?? FabricatorParameters.Fallback;
        RecipeLevel = recipeLevel;
        Rank = Math.Max(0, rank);
        Craftsmanship = Math.Max(0, craftsmanship);
        Control = Math.Max(0, control);
        GearCp = Math.Max(0, gearCp);
        EnhancementProgressPenalty = Math.Max(0, enhancementProgressPenalty);

        var basePoints = Math.Max(1, baseProgress);
        BaseWork = basePoints * FabricatorRules.ProgressScale;
        var total = (basePoints + EnhancementProgressPenalty) * FabricatorRules.ProgressScale;
        MaxMachineCondition = (int)Math.Round(
            FabricatorRules.MaxMachineCondition * (basePoints + EnhancementProgressPenalty) / (double)basePoints, MidpointRounding.AwayFromZero);
        FrameSize = Math.Max(1, (int)Math.Round(total * Parameters.FrameSizePercent / 100.0, MidpointRounding.AwayFromZero));
        ShellSize = Math.Max(1, (int)Math.Round(total * Parameters.ShellSizePercent / 100.0, MidpointRounding.AwayFromZero));
        FittingsSize = Math.Max(1, total - FrameSize - ShellSize);
        MaxCp = (int)(GearCp + Rank * FabricatorRules.CpPerRank) +
                (Rank >= FabricatorRules.RankPassiveCpRank ? FabricatorRules.RankPassiveCp : 0);
    }

    /// <summary>
    /// The setup for crafting a recipe: its parameters and level, and the progress its level gives in the level chart. The window and
    /// the simulator both build their setups here, so they cannot disagree about a recipe.
    /// </summary>
    public static FabricatorSetup ForRecipe(
        RecipeDetail recipe,
        int rank,
        int craftsmanship,
        int control,
        int gearCp,
        int enhancementProgressPenalty = 0)
    {
        if (recipe == null)
            throw new ArgumentNullException(nameof(recipe));

        return new FabricatorSetup(
            recipe.MinigameParameters as FabricatorParameters,
            recipe.Level,
            Craft.GetRecipeLevelDetail(recipe.Level).Progress,
            rank,
            craftsmanship,
            control,
            gearCp,
            enhancementProgressPenalty);
    }

    public FabricatorParameters Parameters { get; }

    public int RecipeLevel { get; }

    public int Rank { get; }

    public int Craftsmanship { get; }

    public int Control { get; }

    public int GearCp { get; }

    public int EnhancementProgressPenalty { get; }

    /// <summary>The recipe's own work in meter units. A Run is a share of this, so enhancements add Runs instead of shrinking each one.</summary>
    public int BaseWork { get; }

    public int FrameSize { get; }

    public int ShellSize { get; }

    public int FittingsSize { get; }

    public int MaxCp { get; }

    /// <summary>
    /// The Machine Condition the fabricator starts with: <see cref="FabricatorRules.MaxMachineCondition"/> for a plain piece, and more
    /// in the same proportion as the work an enhanced piece needs, so an enhancement lengthens the production run (more Runs and Precision
    /// Passes, and a longer craft) without making the machine give out before the piece is built.
    /// </summary>
    public int MaxMachineCondition { get; }

    public int WearRate => Parameters.WearRate;

    /// <summary>The progress that finishes all three sub-assemblies.</summary>
    public int MaxProgress => FrameSize + ShellSize + FittingsSize;

    public int RetoolCp => FabricatorRules.RetoolCp(RecipeLevel);

    public int ServiceCp => FabricatorRules.ServiceCp(RecipeLevel);

    public int SizeOf(SubAssembly part) => part switch
    {
        SubAssembly.Frame => FrameSize,
        SubAssembly.Shell => ShellSize,
        _ => FittingsSize
    };

    /// <summary>The weight, in percent, the part's quality has in the piece's quality.</summary>
    public int QualityShareOf(SubAssembly part) => part switch
    {
        SubAssembly.Frame => Parameters.FrameQualityPercent,
        SubAssembly.Shell => Parameters.ShellQualityPercent,
        _ => Parameters.FittingsQualityPercent
    };

    /// <summary>
    /// The head the fabricator starts loaded with: the one whose sub-assembly weighs most in the piece's quality, the Cutter on a tie.
    /// </summary>
    public FabricatorHead StartingHead =>
        FabricatorHeads.AllParts.OrderByDescending(QualityShareOf).ThenBy(part => (int)part).First().HeadFor();

    /// <summary>The factor on every Run: the crafter's Craftsmanship and rank against the recipe's level.</summary>
    public double ProgressFactor =>
        (1.0 + Craftsmanship * FabricatorRules.GearPercentPerPoint / 100.0) * RankFactor(FabricatorRules.MaxRanksAboveRecipe);

    /// <summary>The factor on every Precision Pass: the crafter's Control and rank against the recipe's level.</summary>
    public double QualityFactor =>
        (1.0 + Control * FabricatorRules.GearPercentPerPoint / 100.0) * RankFactor(0);

    /// <summary>The progress a Run adds before the sub-assembly's size limits it.</summary>
    /// <param name="onHead">True when the active head serves the sub-assembly; otherwise the Run has half effect.</param>
    /// <param name="primed">True when Prime Feedstock gives the Run its bonus.</param>
    public int RunGain(bool onHead, bool primed)
    {
        var gain = BaseWork * FabricatorRules.RunPercent / 100.0 * ProgressFactor;
        if (!onHead)
            gain *= FabricatorRules.OffHeadFactor;
        if (primed)
            gain *= 1.0 + FabricatorRules.PrimeBonusPercent / 100.0;

        return Math.Max(1, (int)Math.Round(gain, MidpointRounding.AwayFromZero));
    }

    /// <summary>The quality points a Precision Pass adds before the sub-assembly's maximum limits it.</summary>
    public int PrecisionGain(bool primed)
    {
        var gain = FabricatorRules.PrecisionPoints * QualityFactor;
        if (primed)
            gain *= 1.0 + FabricatorRules.PrimeBonusPercent / 100.0;

        return Math.Max(1, (int)Math.Round(gain, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// The average of the three sub-assemblies' quality, weighted by the recipe's quality shares. The quality meter shows it while
    /// the piece is built, and Assemble then caps it.
    /// </summary>
    public int AverageQuality(int frame, int shell, int fittings) =>
        Math.Min(
            FabricatorRules.MaxQuality,
            (frame * Parameters.FrameQualityPercent + shell * Parameters.ShellQualityPercent +
             fittings * Parameters.FittingsQualityPercent) / 100);

    /// <summary>
    /// The quality the item is made at: the weighted average of the three sub-assemblies' quality, no higher than the weakest
    /// sub-assembly plus <see cref="FabricatorRules.AssembleCapPoints"/>.
    /// </summary>
    public int PieceQuality(int frame, int shell, int fittings) =>
        Math.Min(AverageQuality(frame, shell, fittings), CapFor(frame, shell, fittings));

    /// <summary>The highest quality the piece can have with these sub-assemblies: the weakest plus the cap.</summary>
    public int CapFor(int frame, int shell, int fittings) =>
        Math.Min(FabricatorRules.MaxQuality, Math.Min(frame, Math.Min(shell, fittings)) + FabricatorRules.AssembleCapPoints);

    public MinigameMeters StartingMeters() =>
        MinigameMeters.Start(MaxProgress, FabricatorRules.MaxQuality, MaxMachineCondition, MaxCp);

    /// <summary>The unlock rank of an action, or -1 for an unknown action.</summary>
    public static int UnlockRank(string actionId) => actionId switch
    {
        FabricatorActionIds.RunFrame or FabricatorActionIds.RunShell or FabricatorActionIds.RunFittings or
            FabricatorActionIds.PrecisionPass or FabricatorActionIds.RetoolCutter or FabricatorActionIds.RetoolExtruder or
            FabricatorActionIds.RetoolBonder or FabricatorActionIds.Assemble => 0,
        FabricatorActionIds.Service => FabricatorRules.ServiceRank,
        _ => -1
    };

    public bool IsUnlocked(string actionId) => Rank >= UnlockRank(actionId);

    /// <summary>A crafter below the recipe's level works at a penalty; one above it works faster, up to <paramref name="maxAbove"/> ranks.</summary>
    private double RankFactor(int maxAbove)
    {
        var difference = Math.Clamp(Rank - RecipeLevel, -FabricatorRules.MaxRanksBelowRecipe, maxAbove);
        return 1.0 + FabricatorRules.RankStep * difference;
    }
}
