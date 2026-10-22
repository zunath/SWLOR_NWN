namespace SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

/// <summary>
/// Everything about a craft that is fixed before the first action: the recipe's parameters and level, the crafter's Smithery rank
/// and gear, and the Shape the chosen enhancements add. The engine holds it as immutable configuration.
/// </summary>
/// <param name="Parameters">The recipe's Plasma Forge parameters.</param>
/// <param name="RecipeLevel">The recipe's level.</param>
/// <param name="Rank">The crafter's Smithery rank.</param>
/// <param name="Craftsmanship">The crafter's Smithery Craftsmanship, which powers Shape gains.</param>
/// <param name="Control">The crafter's Smithery Control, which powers Finish gains.</param>
/// <param name="GearCp">The CP from the crafter's gear.</param>
/// <param name="EnhancementShapePercent">How much more Shape the selected enhancements require, as a percentage.</param>
public sealed record ForgeSetup(
    ForgeParameters Parameters,
    int RecipeLevel,
    int Rank,
    int Craftsmanship,
    int Control,
    int GearCp,
    int EnhancementShapePercent = 0)
{
    public int RangeLow => Parameters.RangeLow;

    public int RangeHigh => Parameters.RangeHigh;

    public int ShapeMax => ForgeRules.ShapeMax(Parameters.ShapeShare, EnhancementShapePercent);

    public int FinishMax => ForgeRules.FinishMax(Parameters.ShapeShare);

    public int MaxCp => ForgeRules.MaxCp(GearCp, Rank);

    /// <summary>The factor on every Shape gain: the crafter's rank against the recipe's level and the Craftsmanship of their gear.</summary>
    public double ShapeScale =>
        ForgeRules.RankFactor(Rank, RecipeLevel) * (1.0 + Craftsmanship * ForgeRules.OutputPerGearPoint);

    /// <summary>The factor on every Finish gain: the crafter's rank against the recipe's level and the Control of their gear.</summary>
    public double FinishScale =>
        ForgeRules.RankFactor(Rank, RecipeLevel) * (1.0 + Control * ForgeRules.OutputPerGearPoint);
}
