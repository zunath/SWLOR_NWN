using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Search;

/// <summary>
/// The adjustments a scavenger's perks and style make to the Search board, every one neutral until a perk or style grants it. The engine
/// reads these and never checks a perk by name; the perk line that sets them is drafted in
/// <c>design/testing/crafting-revamp/gathering-treasure-seeker-perk-line-draft.md</c>.
/// </summary>
/// <param name="ExtraPromisingSpots">Added to the number of promising spots on the board.</param>
/// <param name="DigRiskPercent">The share of Dig Deeper's risk of spoiling a spot that is left, from 1 to 100: 100 is unchanged, 50 halves it.</param>
/// <param name="DigFindBonus">Percentage points added to the chance that a Dig Deeper that does not spoil the spot finds something.</param>
/// <param name="SearchChanceDelta">Percentage points added to (or, when negative, taken off) a Search's chance on every spot.</param>
/// <param name="DeeperWeightBonus">Added to the multiplier a Dig Deeper puts on the weight of the loot table's rare entries (<see cref="SWLOR.Game.Server.Service.ScavengeService.ScavengeLoot.DeeperWeightMultiplier"/>). It does not change what the board finds, only what a dig's find is.</param>
/// <param name="FindWeightBonus">Added to the multiplier (1) an ordinary Search find puts on the weight of the loot table's rare entries. It does not change how often a Search finds.</param>
/// <param name="JackpotWeightBonus">Added to the rare-entry multiplier of the first Dig Deeper find of each board, on top of <paramref name="DeeperWeightBonus"/>.</param>
public sealed record SearchEffects(
    int ExtraPromisingSpots = 0,
    int DigRiskPercent = 100,
    int DigFindBonus = 0,
    int SearchChanceDelta = 0,
    int DeeperWeightBonus = 0,
    int FindWeightBonus = 0,
    int JackpotWeightBonus = 0)
{
    /// <summary>A scavenger with no perks.</summary>
    public static readonly SearchEffects None = new();

    /// <summary>The effects of a scavenger whose stat totals <paramref name="read"/> returns.</summary>
    public static SearchEffects FromStats(Func<StatType, int> read)
    {
        if (read == null)
            throw new ArgumentNullException(nameof(read));

        return new SearchEffects(
            DigRiskPercent: Math.Clamp(100 - read(StatType.SearchDigRiskReductionPercent), 1, 100),
            SearchChanceDelta: read(StatType.SearchChancePoints),
            DeeperWeightBonus: Math.Max(0, read(StatType.SearchDigRareWeight)),
            FindWeightBonus: Math.Max(0, read(StatType.SearchFindRareWeight)),
            JackpotWeightBonus: Math.Max(0, read(StatType.SearchJackpotRareWeight)));
    }
}
