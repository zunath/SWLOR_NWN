namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>
/// What the cook brings to the pot: skill rank and gear. Craftsmanship powers Doneness, Control narrows the spread of a push, and
/// CP is gear CP plus 0.75 per rank, with the rank passive on top.
/// </summary>
public sealed record KitchenStats(int Rank, int Craftsmanship, int Control, int GearCp)
{
    /// <summary>The CP the cook starts with.</summary>
    public int MaxCp
    {
        get
        {
            var cp = (int)(GearCp + Rank * KitchenTuning.CpPerRank);
            return Rank >= KitchenTuning.CpPassiveRank ? cp + KitchenTuning.CpPassiveBonus : cp;
        }
    }

    /// <summary>
    /// How far, in percent, a push lands from its listed strength with this Control: the light push is this far below it and the
    /// heavy push this far above. More Control narrows it but never removes it.
    /// </summary>
    public int PushSpreadPercent => (int)Math.Round(
        KitchenTuning.BasePushSpreadPercent * 100.0 / (100 + KitchenTuning.ControlPerSpreadStep * Math.Max(0, Control)),
        MidpointRounding.AwayFromZero);

    /// <summary>How much the recipe's level against the cook's rank scales Doneness.</summary>
    public double LevelFactor(int recipeLevel) => Math.Clamp(
        1 + KitchenTuning.LevelFactorPerRank * (Rank - recipeLevel),
        KitchenTuning.LevelFactorMin,
        KitchenTuning.LevelFactorMax);

    /// <summary>The Doneness an action of the given base strength gives this cook on this recipe.</summary>
    public int Doneness(int baseAmount, int recipeLevel) => (int)Math.Round(
        baseAmount * (1 + KitchenTuning.CraftsmanshipDonenessPerPoint * Math.Max(0, Craftsmanship)) * LevelFactor(recipeLevel),
        MidpointRounding.AwayFromZero);
}
