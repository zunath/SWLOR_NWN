using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>What a successful craft produces, decided from its final quality by <see cref="CraftOutcomeRules"/>.</summary>
/// <param name="QualityFraction">The quality meter as a share of its maximum, 0 to 1.</param>
/// <param name="QualityPercent">The quality as a whole percentage, rounded down.</param>
/// <param name="Xp">The XP to grant.</param>
/// <param name="VendorValueBonus">The credits added to the item's vendor value.</param>
/// <param name="Enhancements">One entry per filled enhancement slot.</param>
/// <param name="FoodDurationBonusSeconds">Extra food duration in seconds; 0 for anything that is not food.</param>
/// <param name="FoodDurationBonusUnits">The same bonus in the whole steps a food item property stores.</param>
/// <param name="FoodBonusCharges">Extra food charges from the blueprint level, paid only at full quality as before.</param>
public sealed record CraftOutcome(
    float QualityFraction,
    int QualityPercent,
    int Xp,
    int VendorValueBonus,
    IReadOnlyList<EnhancementTransfer> Enhancements,
    int FoodDurationBonusSeconds,
    int FoodDurationBonusUnits,
    int FoodBonusCharges);
